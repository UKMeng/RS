using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using RS.GamePlay;
using RS.Item;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

using RS.GMTool;
using RS.UI;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RS.Scene
{
    public class RsSceneManager: MonoBehaviour
    {
        private static RsSceneManager s_instance;

        public static RsSceneManager Instance
        {
            get
            {
                return s_instance;
            }
        }
        
        public GameObject chunkPrefab;
        public GameObject chestPrefab;
        public GameObject returnRockPrefab;
        public GameObject treasurePrefab;
        
        public Light dayLight;
        public Material daySkybox;
        public Material nightSkybox;
        public Material morningSkybox;
        public Material sunsetSkybox;
        public PlayerInput PlayerInput;
        
        public long seed = 1284752702419125144;

        private Player m_player;
        
        // 流式加载相关
        private ChunkManager m_chunkManager;
        private Vector3 m_lastPosition;
        
        // 游戏内更新相关
        private TickManager m_tickManager;
        private GameTime m_time;
        
        // 场景初始化与进度条相关
        private int m_mapSize;
        private int m_preloadSize;
        private bool m_isLoading = true;
        [SerializeField] private GameObject m_loadingUI;
        private Slider m_loadingSlider;
        
        // 地图相关
        private Map m_map;
        
        private GameStart m_gameStart;
        
        // 主城状态
        public bool InHome = true;
        private BlockModifyRecorder m_blockModifyRecorder;
        private SaveData m_lastSaveData;
        private bool m_needSave = true;
        
        [SerializeField] private MenuUI m_menu;

        public void Awake()
        {
            s_instance = this;
            
            var mapUI = GameObject.Find("MapUI");
            m_map = mapUI.GetComponent<Map>();
            mapUI.SetActive(false);

            m_menu.gameObject.SetActive(false);
            
            var gameStartUI = GameObject.Find("GameStartUI");
            if (gameStartUI)
            {
                m_gameStart = gameStartUI.GetComponent<GameStart>();
                gameStartUI.SetActive(false);
            }
            
            
            var player = GameObject.Find("Player");
            m_player = player.GetComponent<Player>();
            GetComponent<GMToolWindow>().player = m_player;
            
            if (InHome)
            {
                seed = 114514;
                m_mapSize = 256;
                m_preloadSize = 512;
                m_blockModifyRecorder = new BlockModifyRecorder();
                var saveData = SaveSystem.LoadGame();
                m_lastSaveData = saveData;
                m_blockModifyRecorder.Init(saveData);
                m_player.Load(saveData.playerData);
            }
            else
            {
                var saveData = SaveSystem.LoadGame();
                m_lastSaveData = saveData;
                seed = GameSettingTransfer.seed;
                m_mapSize = GameSettingTransfer.mapSize;
                m_preloadSize = 512;
                m_player.Load(saveData.playerData);
            }
            
            m_loadingSlider = m_loadingUI.GetComponentInChildren<Slider>();
            
            // 初始化Block UV
            Block.Init();
            
            // 初始化噪声
            NoiseManager.Init(seed);
            
            m_tickManager = gameObject.AddComponent<TickManager>();

            m_chunkManager = GetComponent<ChunkManager>();
            m_chunkManager.chunkPrefab = chunkPrefab;
        }
        
        public void Start()
        {
            // 初始化场景，需要有一个加载页面的UI
            m_isLoading = true;
            // 初始化场景，协程传递参数给进度条
            StartCoroutine(InitSceneData());
        }

        public void GameStart(long seed, int mode)
        {
            if (m_player.Status == PlayerStatus.GetShovel)
            {
                m_player.StatusChange(PlayerStatus.OnceMainGame);
            }
            
            GameSettingTransfer.seed = seed;
            switch (mode)
            {
                case 0:
                {
                    GameSettingTransfer.mapSize = 128;
                    break;
                }
                case 1:
                {
                    GameSettingTransfer.mapSize = 256;
                    break;
                }
                case 2:
                {
                    GameSettingTransfer.mapSize = 512;
                    break;
                }
            }

            SaveGame();
            m_needSave = false;
            OpenMainGameScene();
        }

        public void ReturnHome(bool success = false)
        {
            Debug.Log($"返回主城 {success}");
            if (success)
            {
                SaveGame(false);
                m_needSave = false;
                OpenHomeScene();
            }
            else
            {
                SaveSystem.SaveGame(m_lastSaveData);
                m_needSave = false;
                OpenHomeScene();
            }
        }

        private void OpenHomeScene()
        {
            SceneManager.LoadScene("Scenes/Home", LoadSceneMode.Single);
        }

        private void OpenMainGameScene()
        {
            SceneManager.LoadScene("Scenes/MainGame", LoadSceneMode.Single);
        }

        public void SaveGame(bool isGiveUp = true)
        {
            if (InHome)
            {
                var modifyData = m_blockModifyRecorder.GetModifyDataList();
                var playerData = m_player.Save();
                var saveData = new SaveData(modifyData, playerData);
                SaveSystem.SaveGame(saveData);
            }
            else
            {
                if (isGiveUp)
                {
                    SaveSystem.SaveGame(m_lastSaveData);
                    return;
                }
                var playerData = m_player.Save();
                var saveData = new SaveData(m_lastSaveData.blockModifyData, playerData);
                SaveSystem.SaveGame(saveData);
            }
        }

        private Vector3Int GetProperStartChunkPos(int preloadSize)
        {
            while (true)
            {
                var x = Random.Range(-10, 10);
                var z = Random.Range(-10, 10);
                var chunkStartPos = new Vector3Int(x * 32, 0, z * 32);
                var offset = preloadSize / 5;

                // 采样9个点的大陆性，如果大于5个点是大陆则返回
                var sampler = NoiseManager.Instance.GetOrCreateCacheSampler("Continents", new Vector3Int(x, 0, z));
                var count = 0;

                for (var ix = 0; ix < 3; ix++)
                {
                    for (var iz = 0; iz < 3; iz++)
                    {
                        var sampleValue =
                            sampler.Sample(new Vector3(x * 1024 + offset * ix, 0, z * 1024 + offset * iz));
                        if (sampleValue > -0.19f)
                        {
                            count++;
                        }
                    }
                }

                if (count > 7)
                {
                    return chunkStartPos;
                }
            }
        }

        private IEnumerator InitSceneData()
        {
            var sw = Stopwatch.StartNew();
            
            // 场景数据生成，返回进度条数值
            var batchChunkSize = m_preloadSize / 32 / 8;
            var totalProgress = 64 * batchChunkSize * batchChunkSize * 3;


            Vector3Int preloadStartChunkPos;
            // 主城加载固定
            if (InHome)
            {
                // 选点需要注意，不能跨大块的采样器进行采样（32X32) 余32要小于等于24
                preloadStartChunkPos = new Vector3Int(-432, 0, -8);
            }
            else
            {
                // 大概的做法是，在范围内选取9-16个点（根据地图范围）取样大陆性，然后至少3/4的点不能是海洋，这样能规避大部分起始点问题了
                // 需要随机
                // 这个是PreloadStartPos
                preloadStartChunkPos = GetProperStartChunkPos(m_preloadSize);
            }


            // base data阶段，其实可以分帧处理不同阶段？
            var progress = 0;
            for (var x = 0; x < batchChunkSize; x++)
            {
                for (var z = 0; z < batchChunkSize; z++)
                {
                    var chunkPos = preloadStartChunkPos + new Vector3Int(x * 8, 3, z * 8);
                    m_chunkManager.GenerateChunksBatchBaseData(chunkPos, 8, 8);

                    // 进度条更新
                    progress += 64;
                    m_loadingSlider.value = (float) progress / totalProgress;
                    Debug.Log($"加载进度: {m_loadingSlider.value}" );
                    yield return null;
                }
            }
            
            // aquifer
            for (var x = 0; x < batchChunkSize; x++)
            {
                for (var z = 0; z < batchChunkSize; z++)
                {
                    var chunkPos = preloadStartChunkPos + new Vector3Int(x * 8, 3, z * 8);
                    m_chunkManager.GenerateChunksBatchAquifer(chunkPos);

                    // 进度条更新
                    progress += 64;
                    m_loadingSlider.value = (float) progress / totalProgress;
                    Debug.Log($"加载进度: {m_loadingSlider.value}" );
                    yield return null;
                }
            }
            
            // surface
            for (var x = 0; x < batchChunkSize; x++)
            {
                for (var z = 0; z < batchChunkSize; z++)
                {
                    var chunkPos = preloadStartChunkPos + new Vector3Int(x * 8, 3, z * 8);
                    m_chunkManager.GenerateChunksBatchSurface(chunkPos);

                    // 进度条更新
                    progress += 64;
                    m_loadingSlider.value = (float) progress / totalProgress;
                    Debug.Log($"加载进度: {m_loadingSlider.value}" );
                    yield return null;
                }
            }


            Vector3Int startChunkPos;
            if (InHome)
            {
                // 加载用户修改记录
                m_chunkManager.ApplyDataModify(m_blockModifyRecorder.GetModifyDataList());
                
                // 定制下地图
                startChunkPos = new Vector3Int(-431, 0, -5);
            }
            else
            {
                startChunkPos = preloadStartChunkPos;
            }
            
            // 地图生成
            var mapTexture = m_chunkManager.GenerateMap(startChunkPos, m_mapSize);
            m_map.SetMapTexture(mapTexture);

            sw.Stop();
            Debug.Log($"[RsSceneManager]场景数据生成完毕，耗时: {sw.ElapsedMilliseconds} ms");
            
            // 随机位置
            Vector3 playerPos;
            Vector3 chestPos;
            Vector3 returnPos;

            if (InHome)
            {
                playerPos = m_player.BirthPosition; // new Vector3(-13736.0f, 66.0f, -51.0f);
                chestPos = new Vector3(-13725.98f, 63.5f, 17.497f);
                returnPos = new Vector3(-13622.0f, 69.0f, 47.0f);
            }
            else
            {
                playerPos = m_chunkManager.ChoosePlayerPos(startChunkPos, m_mapSize);
                chestPos = m_chunkManager.ChooseChestPos(startChunkPos, playerPos, m_mapSize);
                returnPos = m_chunkManager.ChooseChestPos(startChunkPos, chestPos, m_mapSize);
            }
            Debug.Log($"[RsSceneManager] 玩家初始位置: {playerPos}");
            Debug.Log($"[RsSceneManager] 宝箱位置: {chestPos}");
            Debug.Log($"[RsSceneManager] 返回点位置: {returnPos}");
            
            // 初始化物件
            if ((InHome && m_player.Status == PlayerStatus.FirstTime) || !InHome)
            {
                // 第一次进入主城，放置宝箱 另外主游戏也要放置
                var chest = Instantiate(chestPrefab, chestPos, InHome? Quaternion.identity : Quaternion.Euler(0, GetRandomRotation(), 0));
                var treasure = new Treasure(treasurePrefab, "test name", "test desc");
                chest.GetComponent<Chest>().SetTreasure(treasure);
            }
            
            Instantiate(returnRockPrefab, returnPos, InHome? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, GetRandomRotation(), 0));
            
            // 放置地图标记
            var chestMarkPos = new Vector2((chestPos.x - startChunkPos.x * 32) / m_mapSize,
                (chestPos.z - startChunkPos.z * 32) / m_mapSize);
            var returnMarkPos = new Vector2((returnPos.x - startChunkPos.x * 32) / m_mapSize,
                (returnPos.z - startChunkPos.z * 32) / m_mapSize);
            
            m_map.AddMark(0, chestMarkPos);
            m_map.AddMark(2, returnMarkPos);
            
            m_chunkManager.UpdateChunkStatus(playerPos, true);
            
            // 上午8点
            if (InHome)
            {
                // 主城时间不变动
                m_time = new GameTime(540);
            }
            else
            {
                m_time = new GameTime(360); // 480 = 8:00 360 = 6:00
                m_tickManager.Register(m_time);
            }
            
            // 放置Player
            m_player.Position = playerPos;
            m_player.Rotation = Quaternion.Euler(0, GetRandomRotation(), 0);
            m_lastPosition = new Vector3(0, 0, 0);

            if (m_player.Status == PlayerStatus.FirstTime)
            {
                m_player.InvokeTips("按Q打开地图，参考地形找到X位置的宝箱");
            }
            
            // 数据加载完成，更新游戏内时间，spawn玩家
            m_isLoading = false;
            m_loadingUI.SetActive(false);
            Destroy(m_loadingUI);
            Debug.Log($"[RsSceneManager]场景数据准备完毕");
        }

        private float GetRandomRotation()
        {
            return Random.Range(0, 180);
        }
        
        /// <summary>
        /// 统一销毁资源的位置
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("开始销毁资源");

            if (m_needSave)
            {
                SaveGame();
            }
            
            Block.UnInit();
            GetComponent<TickManager>().Unregister(m_time);
            NoiseManager.Instance.Dispose();
        }

        public void Update()
        {
            if (m_isLoading)
            {
                // 数据加载中，显示进度条UI
                return;
            }
            
            // 生成Chunk的数据不需要每次都执行
            if ((m_lastPosition - m_player.Position).sqrMagnitude > 32.0f)
            {
                m_chunkManager.GenerateNewChunk(m_player.Position);
                m_lastPosition = m_player.Position;
            }

            m_chunkManager.UpdateChunkStatus(m_player.Position);

            if (!InHome)
            {
                UpdateDayLight();
            }
        }

        public Chunk GetChunk(Vector3Int chunkPos)
        {
            return m_chunkManager.GetChunk(chunkPos);
        }

        private void UpdateDayLight()
        {
            var dayProgress = m_time.GetDayProgress();
            var hour = m_time.GetHour();
            
            // 计算太阳角度
            float sunAngle;
            if (dayProgress < 0.25f)
            {
                sunAngle = (dayProgress + 0.5f) * 360.0f - 90.0f;
            }
            else if (dayProgress == 0.25f)
            {
                // 06:00 早晨
                sunAngle = 0.0f;
            }
            else if (dayProgress < 0.75f)
            {
                sunAngle = dayProgress * 360.0f - 90.0f;
            }
            else if (dayProgress == 0.75f)
            {
                // 18:00 换成月光
                sunAngle = 0.0f;
            }
            else
            {
                sunAngle = (dayProgress - 0.5f) * 360.0f - 90.0f;
            }

            // 以x正轴（y = -90）为东
            dayLight.transform.rotation = Quaternion.Euler(sunAngle, -90.0f, 0.0f);
            
            // 更换skybox
            if (hour == 6)
            {
                if (RenderSettings.skybox != morningSkybox)
                {
                    dayLight.colorTemperature = 10000;
                    dayLight.intensity = 1.0f;
                    RenderSettings.skybox = morningSkybox;
                    DynamicGI.UpdateEnvironment();
                }
            }
            else if (hour == 7)
            {
                if (RenderSettings.skybox != daySkybox)
                {
                    dayLight.colorTemperature = 5000;
                    dayLight.intensity = 2.0f;
                    RenderSettings.skybox = daySkybox;
                    DynamicGI.UpdateEnvironment();
                }
            }
            else if (hour == 16)
            {
                if (RenderSettings.skybox != sunsetSkybox)
                {
                    dayLight.colorTemperature = 10000;
                    dayLight.intensity = 1.0f;
                    RenderSettings.skybox = sunsetSkybox;
                    DynamicGI.UpdateEnvironment();
                    
                    if (m_player.FirstNight)
                    {
                        m_player.InvokeTips("天黑之后会开始掉血，速回！");
                        m_player.FirstNight = false;
                    }
                }
            }
            else if (hour == 18)
            {
                if (RenderSettings.skybox != nightSkybox)
                {
                    dayLight.colorTemperature = 20000;
                    dayLight.intensity = 0.2f;
                    RenderSettings.skybox = nightSkybox;
                    DynamicGI.UpdateEnvironment();
                    
                    m_player.RegisterConsumeHealthEvent();
                }
            }
        }

        public void RegisterTickEvent(IUpdateByTick sub)
        {
            m_tickManager.Register(sub);
        }

        public string GetGameTime()
        {
            return m_time.GetTime();
        }

        public void SetGameTime(uint hour, uint minute)
        { 
            m_time.SetTime(hour, minute);
        }

        public BlockType GetBlockType(Vector3Int blockPos)
        {
            return m_chunkManager.GetBlockType(blockPos);
        }

        public void PlaceBlock(Vector3Int blockPos, BlockType blockType, bool delayUpdate = false)
        {
            m_chunkManager.PlaceBlock(blockPos, blockType, delayUpdate);
        }

        public void UpdateChunkMeshOnTick(Chunk chunk)
        {
            m_tickManager.UpdateChunkMeshOnTick(chunk);
        }

        public void OnToggleMap(InputValue value)
        {
            if (!m_isLoading)
            {
                m_map.Toggle();
            }
        }

        public void ToggleGameStartUI()
        {
            m_gameStart.Toggle();
        }

        public void OnMenu(InputValue value)
        {
            if (!m_isLoading)
            {
                m_menu.Toggle();
            }
        }

        public Vector3 GetPlayerPos()
        {
            return m_player.Position;
        }

        public void BlockModifyRecord(Vector3Int chunkPos, int blockIndex, BlockType blockType)
        {
            if (!InHome)
            {
                return;
            }
            
            m_blockModifyRecorder.AddModifyData(chunkPos, blockIndex, blockType);
        }

        public void QuitGame()
        {
            SaveGame();
            Application.Quit();
        }
    }
}