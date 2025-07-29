using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

using RS.Utils;
using RS.GMTool;
using RS.Scene.Biome;

namespace RS.Scene
{
    public class SceneManager: MonoBehaviour
    {
        public GameObject chunkPrefab;

        public Int64 seed = 1284752702419125144;

        private Transform m_player;
        
        private RsNoise m_noise;

        private InterpolatedSampler m_sampler;

        
        // 流式加载相关
        private ChunkManager m_chunkManager;
        private bool m_isGeneratingChunks = false;
        
        
        // private ConcurrentQueue<ChunkData> m_chunkDataQueue;
        
        
        public void Start()
        {
            var player = GameObject.Find("Player");

            m_player = player.transform;
            GetComponent<GMToolWindow>().player = player.transform;
            
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
        
        
        
        /// <summary>
        /// 后台线程执行Chunk生成，避免卡顿
        /// </summary>
        /// <param name="chunkPos"></param>
        // private void StartChunkGeneration(Vector3 chunkPos)
        // {
        //     Task.Run(() =>
        //     {
        //         try
        //         {
        //             var meshData = GenerateChunkMesh((int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, out var blocks);
        //             var data = new ChunkData { chunkPos = chunkPos, meshData = meshData, blocks = blocks };
        //             m_chunkDataQueue.Enqueue(data);
        //         }
        //         catch (Exception e)
        //         {
        //             Debug.LogError($"Chunk Generation Error: {chunkPos} {e.Message}\n{e.StackTrace}");
        //         }
        //     }); 
        // }

        public Chunk GetChunk(Vector3Int chunkPos)
        {
            return m_chunkManager.GetChunk(chunkPos);
        }
        
        // private GameObject GenerateChunk(int chunkX, int chunkY, int chunkZ)
        // {
        //     var offsetX = chunkX * 32;
        //     var offsetZ = chunkZ * 32;
        //     var offsetY = chunkY * 32;
        //     
        //     var sw = Stopwatch.StartNew();
        //     
        //     BlockType[] blocks = new BlockType[32 * 32 * 32];
        //     var index = 0;
        //     
        //     for (var sx = 0; sx < 32; sx++)
        //     {
        //         for (var sz = 0; sz < 32; sz++)
        //         {
        //             for (var sy = 0; sy < 32; sy++)
        //             {
        //                 var sampleX = offsetX + sx;
        //                 var sampleY = offsetY + sy;
        //                 var sampleZ = offsetZ + sz;
        //                 var density = m_sampler.Sample(new Vector3(sampleX, sampleY, sampleZ));
        //                 blocks[index++] = JudgeBlockType(density);
        //             }
        //         }
        //     }
        //
        //     sw.Stop();
        //     Debug.Log($"[SceneManager] 生成Chunk {chunkX} {chunkY} {chunkZ} 数据耗时 {sw.ElapsedMilliseconds} ms");
        //
        //     var chunkPos = new Vector3(offsetX, offsetY * 0.5f, offsetZ);
        //     var chunkGo = Instantiate(chunkPrefab, chunkPos, Quaternion.identity);
        //     var chunk = chunkGo.GetComponent<Chunk>();
        //     chunk.blocks = blocks;
        //     // chunk.BuildMesh();
        //     chunk.BuildMeshUsingJobSystem();
        //
        //     return chunkGo;
        // }

        
        
        
        
    }
}