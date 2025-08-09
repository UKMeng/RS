using System.Collections;
using System.Diagnostics;
using RS.GamePlay;
using RS.Item;
using UnityEngine;
using Debug = UnityEngine.Debug;

using RS.GMTool;
using RS.UI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RS.Scene
{
    public class SceneManager: MonoBehaviour
    {
        private static SceneManager s_instance;

        public static SceneManager Instance
        {
            get
            {
                return s_instance;
            }
        }
        
        public GameObject chunkPrefab;
        public GameObject chestPrefab;
        public GameObject returnRockPrefab;
        
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
        private bool m_isLoading = true;
        private GameObject m_loadingUI;
        private Slider m_loadingSlider;
        
        // 地图相关
        private Map m_map;

        public void Awake()
        {
            s_instance = this;
            
            var mapUI = GameObject.Find("MapUI");
            m_map = mapUI.GetComponent<Map>();
            mapUI.SetActive(false);
            
            seed = GameSettingTransfer.seed;
            m_mapSize = GameSettingTransfer.mapSize;
            
            var player = GameObject.Find("Player");
            m_player = player.GetComponent<Player>();
            GetComponent<GMToolWindow>().player = m_player;

            m_loadingUI = GameObject.Find("LoadingUI");
            m_loadingSlider = m_loadingUI.GetComponentInChildren<Slider>();

            

            // 初始化Block UV
            Block.Init();
            
            // 初始化噪声
            NoiseManager.Init(seed);
            
            m_tickManager = gameObject.AddComponent<TickManager>();
            
            m_chunkManager = gameObject.AddComponent<ChunkManager>();
            m_chunkManager.chunkPrefab = chunkPrefab;
        }
        
        public void Start()
        {
            // 初始化场景，需要有一个加载页面的UI
            m_isLoading = true;
            // 初始化场景，协程传递参数给进度条
            StartCoroutine(InitSceneData());
        }

        public void ReturnHome()
        {
            Debug.Log("返回主城");
        }

        private IEnumerator InitSceneData()
        {
            var sw = Stopwatch.StartNew();
            
            // 场景数据生成，返回进度条数值
            // var batchChunkSize = m_mapSize / 32 / 8;
            var batchChunkSize = m_mapSize / 32 / 8;
            // TODO: 后续地图起始点随机且要确定地图的海洋面积不能太大
            // 大概的做法是，在范围内选取9-16个点（根据地图范围）取样大陆性，然后至少3/4的点不能是海洋，这样能规避大部分起始点问题了
            var startChunkPos = new Vector3Int(0, 0, 0);

            var totalProgress = 64 * batchChunkSize * batchChunkSize * 3;
            

            // base data阶段，其实可以分帧处理不同阶段？
            var progress = 0;
            for (var x = 0; x < batchChunkSize; x++)
            {
                for (var z = 0; z < batchChunkSize; z++)
                {
                    var chunkPos = startChunkPos + new Vector3Int(x * 8, 3, z * 8);
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
                    var chunkPos = startChunkPos + new Vector3Int(x * 8, 3, z * 8);
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
                    var chunkPos = startChunkPos + new Vector3Int(x * 8, 3, z * 8);
                    m_chunkManager.GenerateChunksBatchSurface(chunkPos);

                    // 进度条更新
                    progress += 64;
                    m_loadingSlider.value = (float) progress / totalProgress;
                    Debug.Log($"加载进度: {m_loadingSlider.value}" );
                    yield return null;
                }
            }
            
            // 地图生成
            var mapTexture = m_chunkManager.GenerateMap(startChunkPos, m_mapSize);
            m_map.SetMapTexture(mapTexture);

            sw.Stop();
            Debug.Log($"[SceneManager]场景数据生成完毕，耗时: {sw.ElapsedMilliseconds} ms");
            
            // 数据加载完成，更新游戏内时间，spawn玩家
            m_isLoading = false;
            m_loadingUI.SetActive(false);
            Destroy(m_loadingUI);
            
            
            // 随机位置
            var playerPos = m_chunkManager.ChoosePlayerPos(startChunkPos, m_mapSize);
            Debug.Log($"[SceneManager] 玩家初始位置: {playerPos}");

            var chestPos = m_chunkManager.ChooseChestPos(startChunkPos, playerPos, m_mapSize);
            Debug.Log($"[SceneManager] 宝箱位置: {chestPos}");
            // 先放置宝箱在面前
            Instantiate(chestPrefab, chestPos, Quaternion.identity);

            var returnPos = m_chunkManager.ChooseChestPos(startChunkPos, chestPos, m_mapSize);
            Debug.Log($"[SceneManager] 返回点位置: {returnPos}");
            Instantiate(returnRockPrefab, returnPos, Quaternion.identity);
            
            var chestMarkPos = new Vector2((chestPos.x - startChunkPos.x * 32) / m_mapSize,
                (chestPos.z - startChunkPos.z * 32) / m_mapSize);
            
            var returnMarkPos = new Vector2((returnPos.x - startChunkPos.x * 32) / m_mapSize,
                (returnPos.z - startChunkPos.z * 32) / m_mapSize);
            
            m_map.AddMark(0, chestMarkPos);
            m_map.AddMark(2, returnMarkPos);
            
            m_chunkManager.UpdateChunkStatus(playerPos, true);
            
            // 上午8点
            m_time = new GameTime(360); // 480 = 8:00 360 = 6:00
            m_tickManager.Register(m_time);
            
            // 放置Player
            m_player.Position = playerPos;
            m_lastPosition = new Vector3(0, 0, 0);
            
            Debug.Log($"[SceneManager]场景数据准备完毕");
        }

        /// <summary>
        /// 统一销毁资源的位置
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("开始销毁资源");
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
            UpdateDayLight();
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

        public void OnMenu(InputValue value)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public Vector3 GetPlayerPos()
        {
            return m_player.Position;
        }
    }
}