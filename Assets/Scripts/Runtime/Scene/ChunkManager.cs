using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RS.Scene.Biome;
using RS.Utils;
using RS.Item;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Debug = UnityEngine.Debug;


namespace RS.Scene
{
    public class ChunkManager: MonoBehaviour
    {
        private static ChunkManager s_instance;

        public static ChunkManager Instance
        {
            get
            {
                return s_instance;
            }
        }
        
        public GameObject chunkPrefab;

        private static Vector3Int[] m_directions = new Vector3Int[]
        {
            new Vector3Int(0, 1, 0), // up
            new Vector3Int(0, -1, 0), // down
            new Vector3Int(0, 0, -1), // front
            new Vector3Int(0, 0, 1), // back
            new Vector3Int(-1, 0, 0), // left
            new Vector3Int(1, 0, 0), // right
        };
        
        private Dictionary<Vector3Int, Chunk> m_chunks;
        private Queue<Vector2Int> m_chunkGeneratingQueue;
        private bool m_isGeneratingChunks;
        
        private int m_loadDistance = 6;
        private int m_deactivateDistance = 10;
        private int m_destroyDistance = 15;
        private int m_maxChunksPerFrame = 1;

        private int m_seaLevel = 127;

        private InterpolatedSampler m_finalDensity;

        public void Awake()
        {
            s_instance = this;
            m_chunks = new Dictionary<Vector3Int, Chunk>();
            m_chunkGeneratingQueue = new Queue<Vector2Int>();
            m_isGeneratingChunks = false;
            
            m_finalDensity = RsConfigManager.Instance.GetSamplerConfig("InterTest").BuildRsSampler() as InterpolatedSampler;
        }

        public Chunk GetChunk(Vector3Int chunkPos)
        {
            if (!m_chunks.TryGetValue(chunkPos, out var chunk))
            {
                return null;
            }

            return chunk;
        }

        public void GenerateNewChunk(Vector3 playerPos)
        {
            var playerChunkPos = Chunk.WorldPosToChunkPos(playerPos);

            // 遍历已有的chunk, 根据距离判断是否需要卸载或删除
            var playerChunkX = playerChunkPos.x;
            var playerChunkY = playerChunkPos.y;
            var playerChunkZ = playerChunkPos.z;

            // 收集需要加载或者更新的Chunk
            var toGenerate = new List<Vector2Int>();
            for (var offsetX = -m_loadDistance; offsetX < m_loadDistance + 1; offsetX++)
            {
                for (var offsetZ = -m_loadDistance; offsetZ < m_loadDistance + 1; offsetZ++)
                {
                    var chunkX = playerChunkPos.x + offsetX;
                    var chunkZ = playerChunkPos.z + offsetZ;
                    var chunkPos = new Vector3Int(chunkX, 0, chunkZ);

                    if (!m_chunks.TryGetValue(chunkPos, out var chunk) || chunk.status == ChunkStatus.Empty)
                    {
                        // chunk生成以xz一组为单位
                        // 为了纵向各数据判定的准确性，y轴12个chunk的数据准备完毕才能够进行下一步地表等判断
                        // 如果一个是Empty，那么全员都是没有生成，需要进入生成阶段
                        // chunk为空，需要生成blocks数据
                        toGenerate.Add(new Vector2Int(chunkX, chunkZ));
                    }
                }
            }
            
            // 根据离玩家距离排序, 排序后放入待生成队列
            var playerChunkPosXZ = new Vector2Int(playerChunkPos.x, playerChunkPos.z);
            toGenerate.Sort((a, b) =>
                (a - playerChunkPosXZ).sqrMagnitude.CompareTo((b - playerChunkPosXZ).sqrMagnitude));
            
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
            
            for (var offsetX = -m_loadDistance; offsetX < m_loadDistance + 1; offsetX++)
            {
                for (var offsetZ = -m_loadDistance; offsetZ < m_loadDistance + 1; offsetZ++)
                {
                    var chunkX = playerChunkPos.x + offsetX;
                    var chunkZ = playerChunkPos.z + offsetZ;
                    
                    // 目前先假定y轴上能有192m 12个chunk
                    for (var chunkY = 11; chunkY > -1; chunkY--)
                    {
                        var chunkPos = new Vector3Int(chunkX, chunkY, chunkZ);

                        if (!m_chunks.TryGetValue(chunkPos, out var chunk) || chunk.status == ChunkStatus.Empty)
                        {
                            // chunk生成以xz一组为单位还没有生成
                            break;
                        }
                        
                        if (chunk.status == ChunkStatus.DataReady)
                        {
                            // chunk数据准备完成，还没有生成Mesh和对应GameObject
                            // 简易剔除，只生成玩家所在平面往下2格的Chunk
                            if (chunkY >= playerChunkPos.y - 1)
                            {
                                InitChunkMesh(chunk);
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
        }

        private void InitChunkMesh(Chunk chunk)
        {
            var chunkTsfPos = Chunk.ChunkPosToWorldPos(chunk.chunkPos);
            var chunkGo = Instantiate(chunkPrefab, chunkTsfPos, Quaternion.identity);
            var waterGo = chunkGo.transform.Find("Water").gameObject;
            var extraBlocks = CollectNeighborBlocks(chunk.chunkPos);
            var meshData = Chunk.BuildMesh(chunk.blocks, 32, 32, extraBlocks);
            var mesh = new Mesh();
            mesh.vertices = meshData.vertices;
            mesh.triangles = meshData.triangles;
            mesh.uv = meshData.uvs;
            mesh.RecalculateNormals();

            var waterMesh = new Mesh();
            waterMesh.vertices = meshData.waterVertices;
            waterMesh.triangles = meshData.waterTriangles;
            waterMesh.RecalculateNormals();

            var chunkTf = chunkGo.GetComponent<MeshFilter>();
            chunkTf.mesh = mesh;

            var chunkMc = chunkGo.GetComponent<MeshCollider>();
            chunkMc.sharedMesh = mesh;

            var waterTf = waterGo.GetComponent<MeshFilter>();
            waterTf.mesh = waterMesh;

            var waterMc = waterGo.GetComponent<MeshCollider>();
            waterMc.sharedMesh = waterMesh;

            chunk.go = chunkGo;
            chunk.status = ChunkStatus.Loaded;
        }

        public BlockType[] CollectNeighborBlocks(Vector3Int chunkPos)
        {
            // var sw = Stopwatch.StartNew();
            
            var blocks = new BlockType[32 * 32 * 6];
            var index = 0;
            // 上
            var upChunk = GetChunk(chunkPos + new Vector3Int(0, 1, 0));
            if (upChunk == null || upChunk.status < ChunkStatus.DataReady)
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = BlockType.Air;
                    }
                }
            }
            else
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = upChunk.blocks[Chunk.GetBlockIndex(x, 0, z)];
                    }
                }
            }
            
            // 下
            var downChunk = GetChunk(chunkPos + new Vector3Int(0, -1, 0));
            if (downChunk == null || downChunk.status < ChunkStatus.DataReady)
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = BlockType.Air;
                    }
                }
            }
            else
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = downChunk.blocks[Chunk.GetBlockIndex(x, 31, z)];
                    }
                }
            }
            
            // 前
            var frontChunk = GetChunk(chunkPos + new Vector3Int(0, 0, -1));
            if (frontChunk == null || frontChunk.status < ChunkStatus.DataReady)
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = BlockType.Air;
                    }
                }
            }
            else
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var y = 0; y < 32; y++)
                    {
                        blocks[index++] = frontChunk.blocks[Chunk.GetBlockIndex(x, y, 31)];
                    }
                }
            }
            
            // 后
            var backChunk = GetChunk(chunkPos + new Vector3Int(0, 0, 1));
            if (backChunk == null || backChunk.status < ChunkStatus.DataReady)
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = BlockType.Air;
                    }
                }
            }
            else
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var y = 0; y < 32; y++)
                    {
                        blocks[index++] = backChunk.blocks[Chunk.GetBlockIndex(x, y, 0)];
                    }
                }
            }
            
            // 左
            var leftChunk = GetChunk(chunkPos + new Vector3Int(-1, 0, 0));
            if (leftChunk == null || leftChunk.status < ChunkStatus.DataReady)
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = BlockType.Air;
                    }
                }
            }
            else
            {
                for (var z = 0; z < 32; z++)
                {
                    for (var y = 0; y < 32; y++)
                    {
                        blocks[index++] = leftChunk.blocks[Chunk.GetBlockIndex(31, y, z)];
                    }
                }
            }
            
            // 右
            var rightChunk = GetChunk(chunkPos + new Vector3Int(1, 0, 0));
            if (rightChunk == null || rightChunk.status < ChunkStatus.DataReady)
            {
                for (var x = 0; x < 32; x++)
                {
                    for (var z = 0; z < 32; z++)
                    {
                        blocks[index++] = BlockType.Air;
                    }
                }
            }
            else
            {
                for (var z = 0; z < 32; z++)
                {
                    for (var y = 0; y < 32; y++)
                    {
                        blocks[index++] = rightChunk.blocks[Chunk.GetBlockIndex(0, y, z)];
                    }
                }
            }

            // sw.Stop();
            // Debug.Log($"[ChunkManager]CollectNeighborBlocks: {sw.ElapsedMilliseconds}ms");
            
            return blocks;
        }

        private IEnumerator GenerateChunksCoroutine()
        {
            m_isGeneratingChunks = true;
            
            while (m_chunkGeneratingQueue.Count > 0)
            {
                var chunkPosXZ = m_chunkGeneratingQueue.Dequeue();

                var chunks = new Chunk[12];
                // 目前先假定y轴上能有192m 12个chunk，创建Chunk
                for (var chunkY = 0; chunkY < 12; chunkY++)
                {
                    var chunkPos = new Vector3Int(chunkPosXZ.x, chunkY, chunkPosXZ.y);
                    if (!m_chunks.TryGetValue(chunkPos, out var chunk))
                    {
                        chunk = new Chunk(chunkPos);
                        chunk.status = ChunkStatus.BaseData;
                        m_chunks.Add(chunkPos, chunk);
                        chunks[chunkY] = chunk;
                    }
                }
                
                // 生成Base Data
                for (var chunkY = 0; chunkY < 12; chunkY++)
                {
                    if (chunks[chunkY].status == ChunkStatus.BaseData)
                    {
                        yield return StartCoroutine(GenerateBaseDataAsync(chunks[chunkY]));
                    }
                }
                
                // Aquifer阶段
                for (var chunkY = 0; chunkY < 12; chunkY++)
                {
                    if (chunks[chunkY].status == ChunkStatus.Aquifer)
                    {
                        GenerateAquifer(chunks[chunkY]);
                    }
                }
                
                
                // 生成Surface阶段
                var offsetX = chunkPosXZ.x * 32;
                var offsetZ = chunkPosXZ.y * 32;
                var contexts = new SurfaceContext[32 * 32];
                for (var sx = 0; sx < 32; sx++)
                {
                    for (var sz = 0; sz < 32; sz++)
                    {
                        contexts[sx * 32 + sz] = NoiseManager.Instance.SampleSurface(new Vector3(offsetX + sx, 0, offsetZ + sz));
                    }
                }
                for (var chunkY = 11; chunkY >= 0; chunkY--)
                {
                    var chunk = chunks[chunkY];
                    
                    if (chunk.status == ChunkStatus.Surface)
                    {
                        GenerateSurface(chunk, contexts);
                    }
                }

                yield return null;
            }

            m_isGeneratingChunks = false;
        }

        private IEnumerator GenerateBaseDataAsync(Chunk chunk)
        {
            Task.Run(() =>
            {
                try
                {
                    GenerateBaseData(chunk);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Chunk Generation Error: {chunk.chunkPos} {e.Message}\n{e.StackTrace}");  
                }
            });

            while (chunk.status == ChunkStatus.BaseData)
            {
                yield return null;
            }
        }
        
        private void GenerateBaseData(Chunk chunk)
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

            chunk.blocks = blocks;
            chunk.status = ChunkStatus.Aquifer;
            
            sw.Stop();
            // Debug.Log($"[ChunkManager] 生成Chunk {chunk.chunkPos} BaseData耗时 {sw.ElapsedMilliseconds} ms");
        }
        
        private void GenerateAquifer(Chunk chunk)
        {
            var offsetX = chunk.chunkPos.x * 32;
            var offsetY = chunk.chunkPos.y * 32;
            var offsetZ = chunk.chunkPos.z * 32;
            var sw = Stopwatch.StartNew();
            
            
            var index = 0;
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    for (var sy = 0; sy < 32; sy++)
                    {
                        // 简单按海平面判断
                        if (chunk.blocks[index] == BlockType.Air && sy + offsetY < m_seaLevel)
                        {
                            chunk.blocks[index] = BlockType.Water;
                        }

                        index++;
                    }
                }
            }
            
            chunk.status = ChunkStatus.Surface;
            
            sw.Stop();
            Debug.Log($"[SceneManager] 生成Chunk {chunk.chunkPos} Aquifer耗时 {sw.ElapsedMilliseconds} ms");
        }

        private void GenerateSurface(Chunk chunk, SurfaceContext[] contexts)
        {
            // var offsetX = chunk.chunkPos.x * 32;
            // var offsetZ = chunk.chunkPos.z * 32;
            // var offsetY = chunk.chunkPos.y * 32;
            
            var sw = Stopwatch.StartNew();
            
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    var context = contexts[sx * 32 + sz];
                    var index = Chunk.GetBlockIndex(sx, 31, sz);
                    for (var sy = 31; sy >= 0; sy--)
                    {
                        if (chunk.blocks[index] == BlockType.Stone)
                        {
                            chunk.blocks[index] = JudgeSurfaceBlockType(ref context);
                            context.stoneDepthAbove++;
                            context.waterHeight++;
                        }
                        else if (chunk.blocks[index] == BlockType.Air)
                        {
                            context.stoneDepthAbove = 0;
                            context.waterHeight = Int32.MinValue;
                        }
                        else if (chunk.blocks[index] == BlockType.Water)
                        {
                            if (context.waterHeight < 0)
                            {
                                context.waterHeight = 1;
                            }
                            else
                            {
                                context.waterHeight++;
                            }
                        }

                        index--;
                    }

                    contexts[sx * 32 + sz] = context;
                }
            }
            
            chunk.status = ChunkStatus.DataReady;
            
            // 数据完成时，通知邻居更新mesh，如果有的话
            NotifyNeighborUpdateMesh(chunk.chunkPos);
            
            sw.Stop();
            // Debug.Log($"[SceneManager] 生成Chunk {chunk.chunkPos} Surface耗时 {sw.ElapsedMilliseconds} ms");
        }

        private void NotifyNeighborUpdateMesh(Vector3Int chunkPos)
        {
            foreach (var dir in m_directions)
            {
                var chunk = GetChunk(chunkPos + dir);
                if (chunk != null && chunk.status > ChunkStatus.DataReady)
                {
                    SceneManager.Instance.UpdateChunkMeshOnTick(chunk);
                }
            }
        }
        

        private BlockType JudgeSurfaceBlockType(ref SurfaceContext context)
        {
            if (context.surfaceDepth <= 0)
            {
                return BlockType.Stone;
            }
            
            // 上方无水
            if (context.waterHeight < 0)
            {
                // 表层
                if (context.stoneDepthAbove == 0)
                {
                    if (context.biome == BiomeType.Forest || context.biome == BiomeType.Plain)
                    {
                        return BlockType.Grass;
                    }

                    if (context.biome == BiomeType.SnowForest || context.biome == BiomeType.SnowPlain)
                    {
                        return BlockType.Snow;
                    }
            
                    if (context.biome == BiomeType.Beach || context.biome == BiomeType.Desert || context.biome == BiomeType.Ocean || context.biome == BiomeType.BadLand || context.biome == BiomeType.River)
                    {
                        return BlockType.Sand;
                    }
                }
                
                // 非表层
                if (context.stoneDepthAbove < context.surfaceDepth)
                {
                    if (context.biome == BiomeType.Forest || context.biome == BiomeType.Plain)
                    {
                        return BlockType.Dirt;
                    }
                    
                    if (context.biome == BiomeType.SnowForest || context.biome == BiomeType.SnowPlain)
                    {
                        return BlockType.Dirt;
                    }
            
                    if (context.biome == BiomeType.Beach || context.biome == BiomeType.Desert || context.biome == BiomeType.Ocean || context.biome == BiomeType.BadLand || context.biome == BiomeType.River)
                    {
                        return BlockType.Sand;
                    }
                }
            }
            
            // 上方有水
            if (context.stoneDepthAbove < context.surfaceDepth)
            {
                if (context.biome == BiomeType.Forest || context.biome == BiomeType.Plain)
                {
                    return BlockType.Dirt;
                }
                
                if (context.biome == BiomeType.SnowForest || context.biome == BiomeType.SnowPlain)
                {
                    return BlockType.Dirt;
                }
            
                if (context.biome == BiomeType.Beach || context.biome == BiomeType.Desert || context.biome == BiomeType.Ocean || context.biome == BiomeType.BadLand || context.biome == BiomeType.River)
                {
                    return BlockType.Sand;
                }
            }
            
            return BlockType.Stone;
        }
        
        private BlockType JudgeBaseBlockType(float density)
        {
            return density > 0 ? BlockType.Stone : BlockType.Air;
        }

        public BlockType GetBlockType(Vector3Int blockPos)
        {
            var chunk = GetChunk(Chunk.BlockWorldPosToChunkPos(blockPos));
            if (chunk == null || chunk.status < ChunkStatus.DataReady)
            {
                // chunk未生成时，默认返回Stone;
                return BlockType.Stone;
            }
            
            return chunk.blocks[Chunk.GetBlockIndex(Chunk.BlockWorldPosToBlockLocalPos(blockPos))];
        }

        public void PlaceBlock(Vector3Int blockPos, BlockType blockType, bool delayUpdate = false)
        {
            var chunk = GetChunk(Chunk.BlockWorldPosToChunkPos(blockPos));
            if (chunk == null || chunk.status < ChunkStatus.DataReady)
            {
                // chunk未生成时
                Debug.LogError($"[ChunkManager] 无法放置方块, Chunk未生成 {blockPos}");
                return;
            }

            var blockLocalPos = Chunk.BlockWorldPosToBlockLocalPos(blockPos);
            Debug.Log($"[ChunkManager] 放置方块 {chunk.chunkPos}, {blockLocalPos}");
            chunk.ModifyBlock(blockLocalPos, blockType);
            if (delayUpdate)
            {
                SceneManager.Instance.UpdateChunkMeshOnTick(chunk);
            }
            else
            {
                chunk.UpdateMesh();
            }
        }
    }
}