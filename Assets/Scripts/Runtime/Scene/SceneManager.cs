using RS.GamePlay;
using RS.Item;
using UnityEngine;
using Debug = UnityEngine.Debug;

using RS.GMTool;

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
        
        public void Start()
        {
            s_instance = this;
            
            var player = GameObject.Find("Player");

            m_player = player.GetComponent<Player>();
            GetComponent<GMToolWindow>().player = m_player;
            
            // 初始化Block UV
            Block.Init();
            
            // 初始化噪声
            NoiseManager.Init(seed);

            m_tickManager = gameObject.AddComponent<TickManager>();
            
            m_chunkManager = gameObject.AddComponent<ChunkManager>();
            m_chunkManager.chunkPrefab = chunkPrefab;

            // 上午8点
            m_time = new GameTime(480); // 480 = 8:00
            m_tickManager.Register(m_time);
            
            // 放置Player
            // TODO: 后续位置要虽然随机但是要放在一个平地上
            var pos = new Vector3(60, 90, 700);
            m_player.Position = pos;
            m_lastPosition = new Vector3(0, 0, 0);
            
            Debug.Log($"[SceneManager]初始化完毕");
        }

        /// <summary>
        /// 统一销毁资源的位置
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("开始销毁资源");
            Block.UnInit();
            GetComponent<TickManager>().Unregister(m_time);
        }

        public void Update()
        {
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