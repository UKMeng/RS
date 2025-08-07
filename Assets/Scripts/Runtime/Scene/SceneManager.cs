using System.Collections;
using RS.GamePlay;
using RS.Item;
using UnityEngine;
using Debug = UnityEngine.Debug;

using RS.GMTool;
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

        public GameObject dayLight;
        
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
        private Slider m_loadingSlider;

        public void Awake()
        {
            s_instance = this;
            seed = GameSettingTransfer.seed;
            m_mapSize = GameSettingTransfer.mapSize;
            
            var player = GameObject.Find("Player");
            m_player = player.GetComponent<Player>();
            GetComponent<GMToolWindow>().player = m_player;

            var Canvas = GameObject.Find("Canvas");
            m_loadingSlider = Canvas.GetComponentInChildren<Slider>();
            
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

        private IEnumerator InitSceneData()
        {
            var m_dataReady = false;

            while (!m_dataReady)
            {
                // TODO: 场景数据生成，返回进度条数值
                var batchChunkSize = m_mapSize / 32 / 8;
                // TODO: 后续地图起始点随机且要确定地图的海洋面积不能太大
                var startPos = new Vector3Int(0, 0, 0);

                // base data阶段，其实可以分帧处理不同阶段？
                var index = 0;
                for (var x = 0; x < batchChunkSize; x++)
                {
                    for (var z = 0; z < batchChunkSize; z++)
                    {
                        var chunkPosXZ = startPos + new Vector3Int(x * 8, 0, z * 8);
                        m_chunkManager.GenerateChunksBatchBaseData(chunkPosXZ);

                        m_loadingSlider.value = (float) index++ / batchChunkSize / batchChunkSize;
                        Debug.Log($"加载进度: {m_loadingSlider.value}" );
                        yield return null;
                    }
                }
                
                // aquifer
                // surface
                m_dataReady = true;
            }
            
            // 数据加载完成，更新游戏内时间，spawn玩家
            m_isLoading = false;
            // 上午8点
            m_time = new GameTime(480); // 480 = 8:00
            m_tickManager.Register(m_time);
            
            // 放置Player
            // TODO: 后续位置要虽然随机但是要放在一个平地上
            var pos = new Vector3(10, 90, 10);
            m_player.Position = pos;
            m_lastPosition = new Vector3(0, 0, 0);
            
            m_chunkManager.UpdateChunkStatus(m_player.Position);
            
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
            // if ((m_lastPosition - m_player.Position).sqrMagnitude > 32.0f)
            // {
            //     m_chunkManager.GenerateNewChunk(m_player.Position);
            //     m_lastPosition = m_player.Position;
            // }

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
            float sunAngle;
            
            if (dayProgress < 0.25f)
            {
                sunAngle = (dayProgress + 0.5f) * 360.0f - 90.0f;
            }
            else if (dayProgress == 0.25f)
            {
                // 06:00 太阳升起
                sunAngle = 0.0f;
                var light = dayLight.GetComponent<Light>();
                light.colorTemperature = 20000;
                light.intensity = 2.0f;
            }
            else if (dayProgress < 0.75f)
            {
                sunAngle = dayProgress * 360.0f - 90.0f;
            }
            else if (dayProgress == 0.75f)
            {
                // 18:00 换成月光
                sunAngle = 0.0f;
                var light = dayLight.GetComponent<Light>();
                light.colorTemperature = 20000;
                light.intensity = 0.2f;
            }
            else
            {
                sunAngle = (dayProgress - 0.5f) * 360.0f - 90.0f;
            }
            
            dayLight.transform.rotation = Quaternion.Euler(sunAngle, -30.0f, 0.0f);
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
        
        
    }
}