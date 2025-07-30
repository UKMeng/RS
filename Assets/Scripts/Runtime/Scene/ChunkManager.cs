using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RS.Scene.Biome;
using RS.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class ChunkManager: MonoBehaviour
    {
        public GameObject chunkPrefab;
        
        private Dictionary<Vector3Int, Chunk> m_chunks;
        private Queue<Chunk> m_chunkGeneratingQueue;
        private bool m_isGeneratingChunks;
        
        private int m_loadDistance = 6;
        private int m_deactivateDistance = 10;
        private int m_destroyDistance = 15;
        private int m_maxChunksPerFrame = 1;

        private InterpolatedSampler m_finalDensity;

        public void Start()
        {
            m_chunks = new Dictionary<Vector3Int, Chunk>();
            m_chunkGeneratingQueue = new Queue<Chunk>();
            m_isGeneratingChunks = false;
            
            m_finalDensity = RsConfigManager.Instance.GetSamplerConfig("InterTest").BuildRsSampler() as InterpolatedSampler;
        }

        public Chunk GetChunk(Vector3Int chunkPos)
        {
            return m_chunks[chunkPos];
        }
        
        public void UpdateChunkStatus(Vector3 playerPos)
        {
            var playerChunkPos = Chunk.WorldPosToChunkPos(playerPos);
            
            // 遍历已有的chunk, 根据距离判断是否需要卸载或删除
            var playerChunkX = playerChunkPos.x;
            var playerChunkY = playerChunkPos.y;
            var playerChunkZ = playerChunkPos.z;
            foreach (var chunkPos in m_chunks.Keys)
            {
                var chunkX = chunkPos.x;
                var chunkZ = chunkPos.z;
                var chunk = m_chunks[chunkPos];
                
                if (chunk.status == ChunkStatus.Loaded)
                {
                    if (Mathf.Abs(chunkX - playerChunkX) > m_deactivateDistance || Mathf.Abs(chunkZ - playerChunkZ) > m_deactivateDistance)
                    {
                        // 超出距离, 卸载
                        chunk.go.SetActive(false);
                        chunk.status = ChunkStatus.MeshReady;
                        
                        Debug.Log($"[SceneManager] 触发卸载Mesh {chunkPos}");
                    }
                }
            
                if (chunk.status == ChunkStatus.MeshReady)
                {
                    if (Mathf.Abs(chunkX - playerChunkX) > m_destroyDistance || Mathf.Abs(chunkZ - playerChunkZ) > m_destroyDistance)
                    {
                        // 超出距离, 删除
                        chunk.go.SetActive(false);
                        Destroy(chunk.go);
                        chunk.status = ChunkStatus.DataReady;
                        
                        Debug.Log($"[SceneManager] 触发删除Mesh {chunkPos}");
                    }
                }
            }
            
            // 收集需要加载或者更新的Chunk
            var toGenerate = new List<Chunk>();
            for (var offsetX = -m_loadDistance; offsetX < m_loadDistance + 1; offsetX++)
            {
                for (var offsetZ = -m_loadDistance; offsetZ < m_loadDistance + 1; offsetZ++)
                {
                    var chunkX = playerChunkPos.x + offsetX;
                    var chunkZ = playerChunkPos.z + offsetZ;

                    // 目前先假定y轴上能有192m 12个chunk
                    // 为了纵向各数据判定的准确性，y轴12个chunk的数据准备完毕才能够进行下一步地表等判断
                    for (var chunkY = 11; chunkY > -1; chunkY--)
                    {
                        var chunkPos = new Vector3Int(chunkX, chunkY, chunkZ);

                        if (!m_chunks.TryGetValue(chunkPos, out var chunk))
                        {
                            chunk = new Chunk(chunkPos);
                            m_chunks.Add(chunkPos, chunk);
                        }

                        if (chunk.status == ChunkStatus.Empty)
                        {
                            // chunk为空，需要生成blocks数据
                            toGenerate.Add(chunk);
                            chunk.status = ChunkStatus.DataPreparing;
                        }
                        else if (chunk.status == ChunkStatus.DataReady)
                        {
                            // chunk数据准备完成，生成Mesh投入场景
                            // 简易剔除，只生成玩家所在平面往下2格的Chunk
                            if (chunkY >= playerChunkPos.y - 1)
                            {
                                var chunkTsfPos = Chunk.ChunkPosToWorldPos(chunk.chunkPos);
                                var chunkGo = Instantiate(chunkPrefab, chunkTsfPos, Quaternion.identity);
                                var meshData = Chunk.BuildMesh(chunk.blocks, 32, 32);
                                var mesh = new Mesh();
                                mesh.vertices = meshData.vertices;
                                mesh.triangles = meshData.triangles;
                                mesh.uv = meshData.uvs;
                                mesh.RecalculateNormals();
                    
                                var chunkTf = chunkGo.GetComponent<MeshFilter>();
                                chunkTf.mesh = mesh;
                    
                                var chunkMc = chunkGo.GetComponent<MeshCollider>();
                                chunkMc.sharedMesh = mesh;

                                chunk.go = chunkGo;
                                chunk.status = ChunkStatus.Loaded;
                            }
                        }
                        else if (chunk.status == ChunkStatus.MeshReady)
                        {
                            // chunk的mesh之前卸载了，重新改为加载
                            chunk.go.SetActive(true);
                            chunk.status = ChunkStatus.Loaded;
                        }
                    }
                }
            }
            
            // 根据离玩家距离排序, 排序后放入待生成队列
            toGenerate.Sort((a, b) =>
                (a.chunkPos - playerChunkPos).sqrMagnitude.CompareTo((b.chunkPos - playerChunkPos).sqrMagnitude));
            foreach (var pos in toGenerate)
            {
                if (!m_chunkGeneratingQueue.Contains(pos))
                {
                    m_chunkGeneratingQueue.Enqueue(pos);
                }
            }
            
            if (!m_isGeneratingChunks && m_chunkGeneratingQueue.Count > 0)
            {
                StartCoroutine(GenerateChunksCoroutine());
            }
        }
        
        private IEnumerator GenerateChunksCoroutine()
        {
            m_isGeneratingChunks = true;
            
            while (m_chunkGeneratingQueue.Count > 0)
            {
                for (var i = 0; i < m_maxChunksPerFrame && m_chunkGeneratingQueue.Count > 0; i++)
                {
                    var chunk = m_chunkGeneratingQueue.Dequeue();
                    
                    yield return StartCoroutine(GenerateChunkDataAsync(chunk));
                }
                yield return null;
            }

            m_isGeneratingChunks = false;
        }

        private IEnumerator GenerateChunkDataAsync(Chunk chunk)
        {
            Task.Run(() =>
            {
                try
                {
                    GenerateChunkBlockData(chunk);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Chunk Generation Error: {chunk.chunkPos} {e.Message}\n{e.StackTrace}");  
                }
            });

            while (chunk.status == ChunkStatus.DataPreparing)
            {
                yield return null;
            }
        }
        
        private void GenerateChunkBlockData(Chunk chunk)
        {
            var offsetX = chunk.chunkPos.x * 32;
            var offsetZ = chunk.chunkPos.z * 32;
            var offsetY = chunk.chunkPos.y * 32;
            
            var sw = Stopwatch.StartNew();
            
            var blocks = new BlockType[32 * 32 * 32];
            var index = 0;
            
            var batchSampleResult = m_finalDensity.SampleBatch(new Vector3(offsetX, offsetY, offsetZ));
            
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    for (var sy = 0; sy < 32; sy++)
                    {
                        var density = batchSampleResult[sx, sy, sz];
                        blocks[index++] = JudgeBaseBlockType(density);
                    }
                }
            }
            
            // Surface判断，后续放到其他地方去
            index = 0;
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    var bottomPos = new Vector3(offsetX + sx, offsetY, offsetZ + sz);
                    var context = NoiseManager.Instance.SampleSurface(bottomPos);
                    
                    for (var sy = 0; sy < 32; sy++)
                    {
                        if (blocks[index] == BlockType.Stone)
                        {
                            blocks[index] = JudgeSurfaceBlockType(context);
                            // context.stoneDepthAbove++;
                        }

                        index++;
                    }
                }
            }

            chunk.blocks = blocks;
            chunk.status = ChunkStatus.DataReady;

            sw.Stop();
            Debug.Log($"[SceneManager] 生成Chunk {chunk.chunkPos} 数据耗时 {sw.ElapsedMilliseconds} ms");
        }

        private BlockType JudgeSurfaceBlockType(SurfaceContext context)
        {
            if (context.biome == BiomeType.Forest || context.biome == BiomeType.Plain)
            {
                return BlockType.Dirt;
            }
            else
            {
                return BlockType.Stone;
            }
        }
        
        private BlockType JudgeBaseBlockType(float density)
        {
            return density > 0 ? BlockType.Stone : BlockType.Air;
        }
    }
}