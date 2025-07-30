using System;
using RS.GamePlay;
using RS.Item;
using UnityEngine;
using Debug = UnityEngine.Debug;

using RS.GMTool;

namespace RS.Scene
{
    public class SceneManager: MonoBehaviour
    {
        public GameObject chunkPrefab;

        public long seed = 1284752702419125144;

        private Transform m_player;
        
        // 流式加载相关
        private ChunkManager m_chunkManager;
        
        public void Start()
        {
            var player = GameObject.Find("Player");

            m_player = player.transform;
            GetComponent<GMToolWindow>().playerTsf = player.transform;
            GetComponent<GMToolWindow>().player = player.GetComponent<Player>();
            
            // 初始化Block UV
            Block.Init();
            
            // 初始化噪声
            NoiseManager.Init(seed);

            m_chunkManager = gameObject.AddComponent<ChunkManager>();
            m_chunkManager.chunkPrefab = chunkPrefab;
            
            // 放置Player
            // TODO: 后续位置要虽然随机但是要放在一个平地上
            var pos = new Vector3(0, 90, 0);
            m_player.position = pos;
        }

        /// <summary>
        /// 统一销毁资源的位置
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("开始销毁资源");
            Block.UnInit();
        }

        public void Update()
        {
            m_chunkManager.UpdateChunkStatus(m_player.position);
        }

        public Chunk GetChunk(Vector3Int chunkPos)
        {
            return m_chunkManager.GetChunk(chunkPos);
        }
    }
}