using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEngine.Profiling;

using RS.Utils;
using RS.GMTool;

namespace RS.Scene
{
    public struct MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
    }
    
    public struct ChunkData
    {
        public Vector3Int chunkPos;
        public MeshData meshData;
        public BlockType[] blocks;
    }

    public enum ChunkStatus
    {
        Empty, // 未生成
        Unload, // 已生成Mesh, 处于active == false
        Loaded, // 当前场景已加载显示中
        Generating, // 生成中，还没准备好
    }
    
    public class SceneManager: MonoBehaviour
    {
        public GameObject chunkPrefab;

        public Int64 seed = 1284752702419125144;

        private Transform m_player;
        
        private RsNoise m_noise;

        private RsSampler m_sampler;

        
        // 流式加载相关
        private Dictionary<Vector3Int, GameObject> m_chunks;
        private Dictionary<Vector3Int, ChunkStatus> m_chunkStatus;
        private Dictionary<Vector3Int, ChunkData> m_chunkDataBuffer;
        private Queue<Vector3Int> m_chunkGeneratingQueue;
        private bool m_isGeneratingChunks = false;
        private int m_loadDistance = 3;
        private int m_deactivateDistance = 5;
        private int m_destroyDistance = 10;
        private int m_maxChunksPerFrame = 3;
        
        // private ConcurrentQueue<ChunkData> m_chunkDataQueue;
        
        
        public void Start()
        {
            var player = GameObject.Find("Player");

            m_player = player.transform;
            GetComponent<GMToolWindow>().player = player.transform;
            
            // 初始化Block UV
            Block.Init();
            
            // 初始化噪声 ;
            RsRandom.Init(seed);
            m_sampler = RsConfigManager.Instance.GetSamplerConfig("InterTest").BuildRsSampler();

            m_chunks = new Dictionary<Vector3Int, GameObject>();
            m_chunkStatus = new Dictionary<Vector3Int, ChunkStatus>();
            // m_chunkDataQueue = new ConcurrentQueue<ChunkData>();
            m_chunkGeneratingQueue = new Queue<Vector3Int>();
            m_chunkDataBuffer = new Dictionary<Vector3Int, ChunkData>();
            
            // 放置Player
            // TODO: 后续位置要虽然随机但是要放在一个平地上
            var pos = new Vector3(0, 90, 0);
            player.transform.position = pos;
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
            UpdateChunkStatus();
            
            // var x = Mathf.FloorToInt(pos.x / 32.0f);
            // var z = Mathf.FloorToInt(pos.z / 32.0f);
            //
            // var toDeactivate = new List<Vector3>();
            // var toDestroy = new List<Vector3>();
            //
            // // 优先加载后台生成好的Chunk
            // while (m_chunkDataQueue.TryDequeue(out var chunkData))
            // {
            //     var chunkTsfPos = new Vector3(chunkData.chunkPos.x * 32, chunkData.chunkPos.y * 16,
            //         chunkData.chunkPos.z * 32);
            //
            //     var chunkGo = Instantiate(chunkPrefab, chunkTsfPos, Quaternion.identity);
            //     var chunk = chunkGo.GetComponent<Chunk>();
            //     chunk.blocks = chunkData.blocks;
            //
            //     var mesh = new Mesh();
            //     mesh.vertices = chunkData.meshData.vertices;
            //     mesh.triangles = chunkData.meshData.triangles;
            //     mesh.uv = chunkData.meshData.uvs;
            //     mesh.RecalculateNormals();
            //     
            //     var chunkTf = chunk.GetComponent<MeshFilter>();
            //     chunkTf.mesh = mesh;
            //
            //     var chunkMc = chunk.GetComponent<MeshCollider>();
            //     chunkMc.sharedMesh = mesh;
            //
            //     m_chunks[chunkData.chunkPos] = chunkGo;
            //     m_loadRecord[chunkData.chunkPos] = 2;
            // }
            //
            // // 遍历已有的chunk, 根据距离判断是否需要卸载或删除
            // foreach (var chunkPos in m_loadRecord.Keys)
            // {
            //     var chunkX = chunkPos.x;
            //     var chunkZ = chunkPos.z;
            //     
            //     if (m_loadRecord[chunkPos] == 2)
            //     {
            //         if (Mathf.Abs(chunkX - x) > m_deactivateDistance || Mathf.Abs(chunkZ - z) > m_deactivateDistance)
            //         {
            //             // 超出距离, 卸载
            //             var chunkGo = m_chunks[chunkPos];
            //             chunkGo.SetActive(false);
            //         
            //             toDeactivate.Add(chunkPos);
            //         
            //             Debug.Log($"[SceneManager] 触发卸载 {chunkPos}");
            //         }
            //     }
            //
            //     if (m_loadRecord[chunkPos] == 1)
            //     {
            //         if (Mathf.Abs(chunkX - x) > m_destroyDistance || Mathf.Abs(chunkZ - z) > m_destroyDistance)
            //         {
            //             // 超出距离, 删除
            //             var chunkGo = m_chunks[chunkPos];
            //             chunkGo.SetActive(false);
            //             Destroy(chunkGo);
            //             m_chunks.Remove(chunkPos);
            //             
            //         
            //             toDestroy.Add(chunkPos);
            //             Debug.Log($"[SceneManager] 触发删除 {chunkPos}");
            //         }
            //     }
            // }
            //
            // foreach (var chunkPos in toDeactivate)
            // {
            //     m_loadRecord[chunkPos] = 1;
            // }
            //
            // foreach (var chunkPos in toDestroy)
            // {
            //     m_loadRecord[chunkPos] = 0;
            // }
            
        }


        private void UpdateChunkStatus()
        {
            var playerPos = m_player.position;
            var px = Mathf.FloorToInt(playerPos.x / 32.0f);
            var py = Mathf.FloorToInt(playerPos.y / 16.0f);
            var pz = Mathf.FloorToInt(playerPos.z / 32.0f);
            var playerChunkPos = new Vector3Int(px, py, pz);

            var toGenerate = new List<Vector3Int>();
            
            // 计算当前位置所有应该加载的Mesh 以xz为判断标准，y全部加载, m_loadDistance可根据电脑性能进行配置
            for (var offsetX = -m_loadDistance; offsetX < m_loadDistance + 1; offsetX++)
            {
                for (var offsetZ = -m_loadDistance; offsetZ < m_loadDistance + 1; offsetZ++)
                {
                    var chunkX = px + offsetX;
                    var chunkZ = pz + offsetZ;

                    // 目前先假定y轴上能有128m 8个chunk
                    for (var chunkY = 7; chunkY > -1; chunkY--)
                    {
                        var chunkPos = new Vector3Int(chunkX, chunkY, chunkZ);
                        
                        if (!m_chunkStatus.TryGetValue(chunkPos, out var status))
                        {
                            status = ChunkStatus.Empty;
                        }

                        if (status == ChunkStatus.Empty)
                        {
                            toGenerate.Add(chunkPos);
                            m_chunkStatus[chunkPos] = ChunkStatus.Generating;
                            continue;
                        }

                        if (status == ChunkStatus.Unload)
                        {
                            var chunkGo = m_chunks[chunkPos];
                            chunkGo.SetActive(true);
                            m_chunkStatus[chunkPos] = ChunkStatus.Loaded;
                        }
                    }
                }
            }
            
            // 根据离玩家距离排序, 排序后放入待生成队列
            toGenerate.Sort((a, b) =>
                (a - playerChunkPos).sqrMagnitude.CompareTo((b - playerChunkPos).sqrMagnitude));
            foreach (var pos in toGenerate)
            {
                if (!m_chunkGeneratingQueue.Contains(pos))
                {
                    m_chunkGeneratingQueue.Enqueue(pos);
                }
            }
            
            // 启动协程
            // if (m_isGeneratingChunks)
            // {
            //     // 如果进入到此，说明分帧计算的chunk数量可能不太合适
            //     Debug.LogError($"[SceneManager] 之前的协程未完成任务，此帧跳过");
            // }
            if (!m_isGeneratingChunks && m_chunkGeneratingQueue.Count > 0)
            {
                StartCoroutine(GenerateChunksCoroutine());
            }
        }

        IEnumerator GenerateChunksCoroutine()
        {
            m_isGeneratingChunks = true;
            
            while (m_chunkGeneratingQueue.Count > 0)
            {
                for (var i = 0; i < m_maxChunksPerFrame && m_chunkGeneratingQueue.Count > 0; i++)
                {
                    var chunkPos = m_chunkGeneratingQueue.Dequeue();
                    
                    yield return StartCoroutine(GenerateChunkDataAsync(chunkPos));

                    var chunkData = m_chunkDataBuffer[chunkPos];
                    var chunkTsfPos = new Vector3(chunkData.chunkPos.x * 32, chunkData.chunkPos.y * 16,
                            chunkData.chunkPos.z * 32);
                        
                    var chunkGo = Instantiate(chunkPrefab, chunkTsfPos, Quaternion.identity);
                    var chunk = chunkGo.GetComponent<Chunk>();
                    chunk.blocks = chunkData.blocks;
                    
                    var mesh = new Mesh();
                    mesh.vertices = chunkData.meshData.vertices;
                    mesh.triangles = chunkData.meshData.triangles;
                    mesh.uv = chunkData.meshData.uvs;
                    mesh.RecalculateNormals();
                    
                    var chunkTf = chunk.GetComponent<MeshFilter>();
                    chunkTf.mesh = mesh;
                    
                    var chunkMc = chunk.GetComponent<MeshCollider>();
                    chunkMc.sharedMesh = mesh;
                    
                    m_chunks[chunkData.chunkPos] = chunkGo;
                    m_chunkStatus[chunkData.chunkPos] = ChunkStatus.Loaded;
                }
                yield return null;
            }

            m_isGeneratingChunks = false;
        }

        IEnumerator GenerateChunkDataAsync(Vector3Int chunkPos)
        {
            var isDone = false;
            
            Task.Run(() =>
            {
                try
                {
                    var meshData = GenerateChunkMesh(chunkPos.x, chunkPos.y, chunkPos.z, out BlockType[] blocks);
                    var chunkData = new ChunkData { chunkPos = chunkPos, meshData = meshData, blocks = blocks };
                    m_chunkDataBuffer[chunkPos] = chunkData;
                    isDone = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Chunk Generation Error: {chunkPos} {e.Message}\n{e.StackTrace}");  
                }
            });

            while (!isDone)
            {
                yield return null;
            }
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
            return m_chunks[chunkPos].GetComponent<Chunk>();
        }
        
        private GameObject GenerateChunk(int chunkX, int chunkY, int chunkZ)
        {
            var offsetX = chunkX * 32;
            var offsetZ = chunkZ * 32;
            var offsetY = chunkY * 32;
            
            var sw = Stopwatch.StartNew();
            
            BlockType[] blocks = new BlockType[32 * 32 * 32];
            var index = 0;
            
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    for (var sy = 0; sy < 32; sy++)
                    {
                        var sampleX = offsetX + sx;
                        var sampleY = offsetY + sy;
                        var sampleZ = offsetZ + sz;
                        var density = m_sampler.Sample(new Vector3(sampleX, sampleY, sampleZ));
                        blocks[index++] = JudgeBlockType(density, sx + offsetX, sy + offsetY, sz + offsetZ);
                    }
                }
            }

            sw.Stop();
            Debug.Log($"[SceneManager] 生成Chunk {chunkX} {chunkY} {chunkZ} 数据耗时 {sw.ElapsedMilliseconds} ms");

            var chunkPos = new Vector3(offsetX, offsetY * 0.5f, offsetZ);
            var chunkGo = Instantiate(chunkPrefab, chunkPos, Quaternion.identity);
            var chunk = chunkGo.GetComponent<Chunk>();
            chunk.blocks = blocks;
            // chunk.BuildMesh();
            chunk.BuildMeshUsingJobSystem();

            return chunkGo;
        }

        private MeshData GenerateChunkMesh(int chunkX, int chunkY, int chunkZ, out BlockType[] blocks)
        {
            var offsetX = chunkX * 32;
            var offsetZ = chunkZ * 32;
            var offsetY = chunkY * 32;
            //
            // var sampleResX = 8;
            // var sampleResY = 4;
            // var sampleResZ = 8;
            //
            // var sampleSizeX = 32 / sampleResX;
            // var sampleSizeY = 32 / sampleResY;
            // var sampleSizeZ = 32 / sampleResZ;
            
            var sw = Stopwatch.StartNew();
            
            // var noiseSamples = new float[sampleSizeX + 1, sampleSizeY + 1, sampleSizeZ + 1];

            
            
            blocks = new BlockType[32 * 32 * 32];
            var index = 0;
            
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    for (var sy = 0; sy < 32; sy++)
                    {
                        var sampleX = offsetX + sx;
                        var sampleY = offsetY + sy;
                        var sampleZ = offsetZ + sz;
                        var density = m_sampler.Sample(new Vector3(sampleX, sampleY, sampleZ));
                        blocks[index++] = JudgeBlockType(density, sx + offsetX, sy + offsetY, sz + offsetZ);
                        
                        // noiseSamples[sx, sy, sz] = SampleNoise(sampleX, sampleY, sampleZ);
                        // noiseSamples[sx, sy, sz] = m_sampler.Sample(new Vector3(sampleX, sampleY, sampleZ));
                    }
                }
            }

            sw.Stop();
            Debug.Log($"[SceneManager] 生成Chunk {chunkX} {chunkY} {chunkZ} 数据耗时 {sw.ElapsedMilliseconds} ms");
            return Chunk.BuildMesh(blocks, 32, 32);
        }

        private float SampleNoise(int x, int y, int z)
        {
            // return m_noise.SimplexNoiseEvaluate(new Vector3(x, y, z), 0.05f);
            return RsNoise.Fbm3D(new Vector3(x, y, z), 3, 0.01f, m_noise);
        }
        
        private BlockType JudgeBlockType(float density, int x, int y, int z)
        {
            if (density > 0)
            {
                return BlockType.Stone;
            }
            else
            {
                return BlockType.Air;
            }
        }
        
    }
}