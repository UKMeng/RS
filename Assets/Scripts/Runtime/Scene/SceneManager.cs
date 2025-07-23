using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

using RS.Utils;
using RS.GMTool;

namespace RS.Scene
{
    public class SceneManager: MonoBehaviour
    {
        public GameObject chunkPrefab;

        public Int64 seed = 1284752702419125144;

        private Transform m_player;
        
        private RsNoise m_noise;

        private RsRandom m_rng;

        
        // 流式加载相关
        private Dictionary<Vector3, GameObject> m_chunks;
        private Dictionary<Vector2, byte> m_loadRecord; // 0:未生成 1:未加载 2:已加载
        // private Queue<Vector3> m_chunkLoadQueue;
        private bool m_isLoadingChunks = false;
        private int m_loadDistance = 3;
        private int m_deactivateDistance = 5;
        private int m_destroyDistance = 10;

        private struct ChunkData
        {
            public Vector3 chunkPos;
            public Mesh mesh;
            public BlockType[] blocks;
        }

        private ConcurrentQueue<ChunkData> m_chunkDataQueue;
        
        

        public void Start()
        {
            var player = GameObject.Find("Player");

            m_player = player.transform;
            GetComponent<GMToolWindow>().player = player.transform;
            
            // 初始化Block UV
            Block.Init();
            
            // 初始化噪声
            m_rng = new RsRandom(seed);
            m_noise = new RsNoise(m_rng.NextUInt64());

            m_chunks = new Dictionary<Vector3, GameObject>();
            m_loadRecord = new Dictionary<Vector2, byte>();
            // m_chunkLoadQueue = new Queue<Vector3>();
            m_chunkDataQueue = new ConcurrentQueue<ChunkData>();
            
            // 放置Player
            // TODO: 后续位置要虽然随机但是要放在一个平地上
            var pos = new Vector3(30, 128, 30);
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
            var pos = m_player.position;
            
            var x = Mathf.FloorToInt(pos.x / 32.0f);
            var z = Mathf.FloorToInt(pos.z / 32.0f);
            
            var toDeactivate = new List<Vector2>();
            var toDestroy = new List<Vector2>();
            
            // 优先加载后台生成好的Chunk
            while (m_chunkDataQueue.TryDequeue(out var chunkData))
            {
                var chunkTsfPos = new Vector3(chunkData.chunkPos.x * 32, chunkData.chunkPos.y * 16,
                    chunkData.chunkPos.z * 32);

                var chunkGo = Instantiate(chunkPrefab, chunkTsfPos, Quaternion.identity);
                var chunk = chunkGo.GetComponent<Chunk>();
                chunk.blocks = chunkData.blocks;

                var chunkTf = chunk.GetComponent<MeshFilter>();
                chunkTf.mesh = chunkData.mesh;

                var chunkMc = chunk.GetComponent<MeshCollider>();
                chunkMc.sharedMesh = chunkData.mesh;

                m_chunks[chunkData.chunkPos] = chunkGo;
            }
            
            // 遍历已有的chunk, 根据距离判断是否需要卸载或删除
            foreach (var chunkPosXZ in m_loadRecord.Keys)
            {
                var chunkX = chunkPosXZ.x;
                var chunkZ = chunkPosXZ.y;

                var chunkPos = new Vector3(chunkX, 0, chunkZ);
                if (m_loadRecord[chunkPosXZ] == 2)
                {
                    
                    if (Mathf.Abs(chunkX - x) > m_deactivateDistance || Mathf.Abs(chunkZ - z) > m_deactivateDistance)
                    {
                        // 超出距离, 卸载
                        for (var y = 0; y < 7; y++)
                        {
                            chunkPos.y = y;
                            var chunkGo = m_chunks[chunkPos];
                            chunkGo.SetActive(false);
                        }
                    
                        toDeactivate.Add(chunkPosXZ);
                    
                        Debug.Log($"[SceneManager] 触发卸载 {chunkPosXZ}");
                    }
                }

                if (m_loadRecord[chunkPosXZ] == 1)
                {
                    if (Mathf.Abs(chunkX - x) > m_destroyDistance || Mathf.Abs(chunkZ - z) > m_destroyDistance)
                    {
                        // 超出距离, 删除
                        for (var y = 0; y < 7; y++)
                        {
                            chunkPos.y = y;
                            var chunkGo = m_chunks[chunkPos];
                            Destroy(chunkGo);
                            m_chunks.Remove(chunkPos);
                        }
                    
                        toDestroy.Add(chunkPosXZ);
                        Debug.Log($"[SceneManager] 触发删除 {chunkPosXZ}");
                    }
                }
            }
            
            foreach (var chunkPosXZ in toDeactivate)
            {
                m_loadRecord[chunkPosXZ] = 1;
            }
            
            foreach (var chunkPosXZ in toDestroy)
            {
                m_loadRecord[chunkPosXZ] = 0;
            }
            
            // 先测试九个位置的加载
            for (var offsetX = -m_loadDistance; offsetX < m_loadDistance + 1; offsetX++)
            {
                for (var offsetZ = -m_loadDistance; offsetZ < m_loadDistance + 1; offsetZ++)
                {
                    var chunkX = x + offsetX;
                    var chunkZ = z + offsetZ;
                
                    var chunkPosXZ = new Vector2(chunkX, chunkZ);
                
                    if (!m_loadRecord.TryGetValue(chunkPosXZ, out var loaded) || loaded != 2)
                    {
                        // 已生成未加载
                        if (loaded == 1)
                        {
                            for (var chunkY = 7; chunkY > -1; chunkY--)
                            {
                                var chunkPos = new Vector3(chunkX, chunkY, chunkZ);
                                var chunkGo = m_chunks[chunkPos];
                                chunkGo.SetActive(true);
                            }

                            m_loadRecord[chunkPosXZ] = 2;
                        }
                        else
                        {
                            // 从上到下生成
                            for (var chunkY = 7; chunkY > -1; chunkY--)
                            {
                                var chunkPos = new Vector3(chunkX, chunkY, chunkZ);
                                StartChunkGeneration(chunkPos);
                                // var go = GenerateChunk((int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z);
                                // m_chunks[chunkPos] = go;
                            }
                            m_loadRecord[chunkPosXZ] = 2;
                        }
                    }
                }
            }

            // if (!m_isLoadingChunks && m_chunkLoadQueue.Count > 0)
            // {
            //     StartCoroutine(LoadChunksCoroutine());
            // }
        }

        // IEnumerator LoadChunksCoroutine()
        // {
        //     m_isLoadingChunks = true;
        //     // TODO:后续安排成优先队列，距离越近的越先加载
        //     var chunksPerFrame = 5;
        //     while (m_chunkLoadQueue.Count > 0)
        //     {
        //         for (var i = 0; i < chunksPerFrame && m_chunkLoadQueue.Count > 0; i++)
        //         {
        //             var chunkPos = m_chunkLoadQueue.Dequeue();
        //             var go = GenerateChunk((int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z);
        //             m_chunks[chunkPos] = go;
        //         }
        //         yield return null;
        //     }
        //
        //     m_isLoadingChunks = false;
        // }

        /// <summary>
        /// 后台线程执行Chunk生成，避免卡顿
        /// </summary>
        /// <param name="chunkPos"></param>
        private void StartChunkGeneration(Vector3 chunkPos)
        {
            Task.Run(() =>
            {
                var mesh = GenerateChunkMesh((int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, out var blocks);
                var data = new ChunkData { chunkPos = chunkPos, mesh = mesh, blocks = blocks };
                m_chunkDataQueue.Enqueue(data);
            });
        }

        public Chunk GetChunk(Vector3 chunkPos)
        {
            return m_chunks[chunkPos].GetComponent<Chunk>();
        }

        private Mesh GenerateChunkMesh(int chunkX, int chunkY, int chunkZ, out BlockType[] blocks)
        {
            var offsetX = chunkX * 32;
            var offsetZ = chunkZ * 32;
            var offsetY = chunkY * 32;

            var sampleResX = 8;
            var sampleResY = 4;
            var sampleResZ = 8;

            var sampleSizeX = 32 / sampleResX;
            var sampleSizeY = 32 / sampleResY;
            var sampleSizeZ = 32 / sampleResZ;
            
            var sw = Stopwatch.StartNew();
            
            var noiseSamples = new float[sampleSizeX + 1, sampleSizeY + 1, sampleSizeZ + 1];

            for (var sx = 0; sx < sampleSizeX + 1; sx++)
            {
                for (var sz = 0; sz < sampleSizeZ + 1; sz++)
                {
                    for (var sy = 0; sy < sampleSizeY + 1; sy++)
                    {
                        var sampleX = offsetX + sx * sampleResX;
                        var sampleY = offsetY + sy * sampleResY;
                        var sampleZ = offsetZ + sz * sampleResZ;
                        noiseSamples[sx, sy, sz] = SampleNoise(sampleX, sampleY, sampleZ);
                    }
                }
            }
            
            blocks = new BlockType[32 * 32 * 32];
            var index = 0;
            // 线性插值
            for (var x = 0; x < 32; x++)
            {
                var fx = x % sampleResX;
                var tx = (float)fx / sampleResX;
                var sx = x / sampleResX;
                
                for (var z = 0; z < 32; z++)
                {
                    var fz = z % sampleResZ;
                    var tz = (float)fz / sampleResZ;
                    var sz = z / sampleResZ;
                    
                    for (var y = 0; y < 32; y++)
                    {
                        var fy = y % sampleResY;
                        var ty = (float)fy / sampleResY;
                        var sy = y / sampleResY;
                        
                        // 三线性插值
                        var c000 = noiseSamples[sx,     sy,     sz    ];
                        var c100 = noiseSamples[sx + 1, sy,     sz    ];
                        var c010 = noiseSamples[sx,     sy + 1, sz    ];
                        var c110 = noiseSamples[sx + 1, sy + 1, sz    ];
                        var c001 = noiseSamples[sx,     sy,     sz + 1];
                        var c101 = noiseSamples[sx + 1, sy,     sz + 1];
                        var c011 = noiseSamples[sx,     sy + 1, sz + 1];
                        var c111 = noiseSamples[sx + 1, sy + 1, sz + 1];
                        
                        var c00 = Mathf.Lerp(c000, c100, tx);
                        var c01 = Mathf.Lerp(c001, c101, tx);
                        var c10 = Mathf.Lerp(c010, c110, tx);
                        var c11 = Mathf.Lerp(c011, c111, tx);

                        var c0 = Mathf.Lerp(c00, c10, ty);
                        var c1 = Mathf.Lerp(c01, c11, ty);

                        var density = Mathf.Lerp(c0, c1, tz);
                        
                        blocks[index++] = JudgeBlockType(density, x + offsetX, y + offsetY, z + offsetZ);
                    }
                }
            }

            sw.Stop();
            Debug.Log($"[SceneManager] 生成Chunk {chunkX} {chunkY} {chunkZ} 数据耗时 {sw.ElapsedMilliseconds} ms");

            return Chunk.BuildMesh(blocks, 32, 32);
        }
        
        private GameObject GenerateChunk(int chunkX, int chunkY, int chunkZ)
        {
            var offsetX = chunkX * 32;
            var offsetZ = chunkZ * 32;
            var offsetY = chunkY * 32;

            var sampleResX = 8;
            var sampleResY = 4;
            var sampleResZ = 8;

            var sampleSizeX = 32 / sampleResX;
            var sampleSizeY = 32 / sampleResY;
            var sampleSizeZ = 32 / sampleResZ;
            
            var sw = Stopwatch.StartNew();
            
            var noiseSamples = new float[sampleSizeX + 1, sampleSizeY + 1, sampleSizeZ + 1];

            for (var sx = 0; sx < sampleSizeX + 1; sx++)
            {
                for (var sz = 0; sz < sampleSizeZ + 1; sz++)
                {
                    for (var sy = 0; sy < sampleSizeY + 1; sy++)
                    {
                        var sampleX = offsetX + sx * sampleResX;
                        var sampleY = offsetY + sy * sampleResY;
                        var sampleZ = offsetZ + sz * sampleResZ;
                        noiseSamples[sx, sy, sz] = SampleNoise(sampleX, sampleY, sampleZ);
                    }
                }
            }
            
            var chunkData = new BlockType[32 * 32 * 32];
            var index = 0;
            // 线性插值
            for (var x = 0; x < 32; x++)
            {
                var fx = x % sampleResX;
                var tx = (float)fx / sampleResX;
                var sx = x / sampleResX;
                
                for (var z = 0; z < 32; z++)
                {
                    var fz = z % sampleResZ;
                    var tz = (float)fz / sampleResZ;
                    var sz = z / sampleResZ;
                    
                    for (var y = 0; y < 32; y++)
                    {
                        var fy = y % sampleResY;
                        var ty = (float)fy / sampleResY;
                        var sy = y / sampleResY;
                        
                        // 三线性插值
                        var c000 = noiseSamples[sx,     sy,     sz    ];
                        var c100 = noiseSamples[sx + 1, sy,     sz    ];
                        var c010 = noiseSamples[sx,     sy + 1, sz    ];
                        var c110 = noiseSamples[sx + 1, sy + 1, sz    ];
                        var c001 = noiseSamples[sx,     sy,     sz + 1];
                        var c101 = noiseSamples[sx + 1, sy,     sz + 1];
                        var c011 = noiseSamples[sx,     sy + 1, sz + 1];
                        var c111 = noiseSamples[sx + 1, sy + 1, sz + 1];
                        
                        var c00 = Mathf.Lerp(c000, c100, tx);
                        var c01 = Mathf.Lerp(c001, c101, tx);
                        var c10 = Mathf.Lerp(c010, c110, tx);
                        var c11 = Mathf.Lerp(c011, c111, tx);

                        var c0 = Mathf.Lerp(c00, c10, ty);
                        var c1 = Mathf.Lerp(c01, c11, ty);

                        var density = Mathf.Lerp(c0, c1, tz);
                        
                        chunkData[index++] = JudgeBlockType(density, x + offsetX, y + offsetY, z + offsetZ);
                    }
                }
            }

            sw.Stop();
            Debug.Log($"[SceneManager] 生成Chunk {chunkX} {chunkY} {chunkZ} 数据耗时 {sw.ElapsedMilliseconds} ms");

            var chunkPos = new Vector3(offsetX, offsetY * 0.5f, offsetZ);
            var chunkGo = Instantiate(chunkPrefab, chunkPos, Quaternion.identity);
            var chunk = chunkGo.GetComponent<Chunk>();
            chunk.blocks = chunkData;
            // chunk.BuildMesh();
            chunk.BuildMeshUsingJobSystem();

            return chunkGo;
        }

        private float SampleNoise(int x, int y, int z)
        {
            // return m_noise.SimplexNoiseEvaluate(new Vector3(x, y, z), 0.05f);
            return RsNoise.Fbm3D(new Vector3(x, y, z), 3, 0.01f, m_noise);
        }
        
        private BlockType JudgeBlockType(float baseDensity, int x, int y, int z)
        {
            // 通过两个参数对地形进行修形
            // squashFactor越高 地形越平坦
            // heightOffset控制地形整体的高低
            var squashFactor = 1.0f;
            var heightOffset = 100.0f;
            
            // 先生成地形高度，再挖洞?
            // var baseHeight = 32 + m_noise.PerlinNoiseEvaluate(x * 0.05f, z * 0.05f) * 5;
            //
            // if (y < baseHeight)
            // {
            //     return BlockType.Stone;
            // }
            // else
            // {
            //     return BlockType.Air;
            // }
            
            // 高度修正
            var yCorrection = (heightOffset - y) / heightOffset;

            var density = baseDensity + yCorrection * squashFactor;
            
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