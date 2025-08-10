using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RS.Scene.Biome;
using RS.Utils;
using RS.Item;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;


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
        
        [SerializeField] private int m_loadDistance = 10;
        [SerializeField] private int m_deactivateDistance = 15;
        [SerializeField] private int m_destroyDistance = 20;
        private int m_maxChunksPerFrame = 1;

        private int m_seaLevel = 127;

        // private InterpolatedSampler m_finalDensity;

        public void Awake()
        {
            s_instance = this;
            m_chunks = new Dictionary<Vector3Int, Chunk>();
            m_chunkGeneratingQueue = new Queue<Vector2Int>();
            m_isGeneratingChunks = false;
            
            // m_finalDensity = NoiseManager.Instance.GetOrCreateSampler("InterTest") as InterpolatedSampler;
            // m_finalDensity = NoiseManager.Instance.GetOrCreateCacheSampler("InterTest", new Vector3Int(0, 0, 0)) as InterpolatedSampler;
        }

        public Chunk GetChunk(Vector3Int chunkPos)
        {
            if (!m_chunks.TryGetValue(chunkPos, out var chunk))
            {
                return null;
            }

            return chunk;
        }

        public Chunk GetOrCreateChunk(Vector3Int chunkPos)
        {
            if (!m_chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk = new Chunk(chunkPos);
                chunk.status = ChunkStatus.BaseData;
                m_chunks.Add(chunkPos, chunk);
            }

            return chunk;
        }

        private IEnumerator GenerateChunksBatchBaseDataAsync(Vector3Int startChunkPos, int xSize, int zSize)
        {
            var samplerX = Mathf.FloorToInt(startChunkPos.x / 32.0f);
            var samplerZ = Mathf.FloorToInt(startChunkPos.z / 32.0f);
            
            var sampler = NoiseManager.Instance.GetOrCreateCacheSampler("InterTest", new Vector3Int(samplerX, 0, samplerZ)) as InterpolatedSampler;
            
            // 批量采样base data
            // var sampleSw = Stopwatch.StartNew();

            // var batchSampleResult = sampler.SampleBatch(startChunkPos, xSize * 32, 288, zSize * 32);
            
            // 外部分步执行协程
            var w = 4;
            var h = 4;
            var txz = xSize * 32 / (int)w + 1;
            var ty = 288 / (int)h + 1;
            var cache = new NativeArray<float>(txz * ty * txz, Allocator.Persistent);
            var startPos = new Vector3(startChunkPos.x * 32, startChunkPos.y * 32, startChunkPos.z * 32);
            yield return StartCoroutine(sampler.SampleAsync(startPos, w, h, txz, ty, cache));
            yield return StartCoroutine(sampler.SampleLerpAsync(startChunkPos, xSize * 32, 288, zSize * 32, cache));
            
            // 对所有间隔点先采样, 需各维度多一个间隔
            // var sw = Stopwatch.StartNew();
            
            
            
            var result = sampler.GetSampleResult();
            var batchSampleResult = new NativeArray<float>(result, Allocator.TempJob);
            
            // sampleSw.Stop();
            // Debug.Log($"batch sample time: {sampleSw.ElapsedMilliseconds}ms");
            
            // var sw = Stopwatch.StartNew();

            var blocksList = new List<NativeArray<BlockType>>();
            var densityList = new List<NativeArray<float>>();
            var jobHandles = new NativeArray<JobHandle>(xSize * zSize * 9, Allocator.TempJob);
            
            var index = 0;
            for (var x = 0; x < xSize; x++)
            {
                for (var z = 0; z < zSize; z++)
                {
                    for (var y = 0; y < 9; y++)
                    {
                        var offsetX = x * 32;
                        var offsetZ = z * 32;
                        var offsetY = y * 32;
                        
                        
                        var blocks = new NativeArray<BlockType>(32768, Allocator.TempJob);
                        var density = new NativeArray<float>(32768, Allocator.TempJob);
                        
                        blocksList.Add(blocks);
                        densityList.Add(density);

                        var job = new JudgeBaseBlockJob()
                        {
                            blocks = blocks,
                            density = density,
                            batchSampleResult = batchSampleResult,
                            offsetX = offsetX,
                            offsetY = offsetY,
                            offsetZ = offsetZ,
                            sizeX = zSize * 32 * 288
                        };
                        
                        jobHandles[index++] = job.Schedule(32768, 64);
                    }
                }
            }

            JobHandle.ScheduleBatchedJobs();
            
            var allDone = false;
            while (!allDone)
            {
                allDone = true;
                foreach (var handle in jobHandles)
                {
                    if (!handle.IsCompleted)
                    {
                        allDone = false;
                        break;
                    }
                }

                if (!allDone)
                {
                    yield return null;
                }
            }
            
            JobHandle.CompleteAll(jobHandles);

            index = 0;
            for (var x = 0; x < xSize; x++)
            {
                for (var z = 0; z < zSize; z++)
                {
                    for (var y = 0; y < 9; y++)
                    {
                        var chunkPos = startChunkPos + new Vector3Int(x, y, z);
                        var chunk = GetOrCreateChunk(chunkPos);
                        
                        chunk.blocks = blocksList[index].ToArray();
                        chunk.density = densityList[index++].ToArray();
                        
                        chunk.status = ChunkStatus.Aquifer;
                    }
                }
            }

            // sw.Stop();
            // Debug.Log($"Deal with sampled data {sw.ElapsedMilliseconds} ms");

            foreach (var block in blocksList)
            {
                block.Dispose();
            }

            foreach (var density in densityList)
            {
                density.Dispose();
            }
            
            jobHandles.Dispose();
            batchSampleResult.Dispose();
        }
        
        public void GenerateChunksBatchBaseData(Vector3Int startChunkPos, int xSize, int zSize)
        {
            var samplerX = Mathf.FloorToInt(startChunkPos.x / 32.0f);
            var samplerZ = Mathf.FloorToInt(startChunkPos.z / 32.0f);
            
            var sampler = NoiseManager.Instance.GetOrCreateCacheSampler("InterTest", new Vector3Int(samplerX, 0, samplerZ)) as InterpolatedSampler;
            
            // 批量采样base data
            // var sampleSw = Stopwatch.StartNew();
            
            var batchSampleResult = sampler.SampleBatch(startChunkPos, xSize * 32, 288, zSize * 32);
            
            // sampleSw.Stop();
            // Debug.Log($"batch sample time: {sampleSw.ElapsedMilliseconds}ms");
            
            // var sw = Stopwatch.StartNew();

            var blocksList = new List<NativeArray<BlockType>>();
            var densityList = new List<NativeArray<float>>();
            var jobHandles = new NativeArray<JobHandle>(xSize * zSize * 9, Allocator.Temp);
            
            var index = 0;
            for (var x = 0; x < xSize; x++)
            {
                for (var z = 0; z < zSize; z++)
                {
                    for (var y = 0; y < 9; y++)
                    {
                        var offsetX = x * 32;
                        var offsetZ = z * 32;
                        var offsetY = y * 32;
                        
                        
                        var blocks = new NativeArray<BlockType>(32768, Allocator.TempJob);
                        var density = new NativeArray<float>(32768, Allocator.TempJob);
                        
                        blocksList.Add(blocks);
                        densityList.Add(density);

                        var job = new JudgeBaseBlockJob()
                        {
                            blocks = blocks,
                            density = density,
                            batchSampleResult = batchSampleResult,
                            offsetX = offsetX,
                            offsetY = offsetY,
                            offsetZ = offsetZ,
                            sizeX = zSize * 32 * 288
                        };
                        
                        jobHandles[index++] = job.Schedule(32768, 64);
                    }
                }
            }
            
            JobHandle.CompleteAll(jobHandles);

            index = 0;
            for (var x = 0; x < xSize; x++)
            {
                for (var z = 0; z < zSize; z++)
                {
                    for (var y = 0; y < 9; y++)
                    {
                        var chunkPos = startChunkPos + new Vector3Int(x, y, z);
                        var chunk = GetOrCreateChunk(chunkPos);
                        
                        chunk.blocks = blocksList[index].ToArray();
                        chunk.density = densityList[index++].ToArray();
                        
                        chunk.status = ChunkStatus.Aquifer;
                    }
                }
            }

            // sw.Stop();
            // Debug.Log($"Deal with sampled data {sw.ElapsedMilliseconds} ms");

            foreach (var block in blocksList)
            {
                block.Dispose();
            }

            foreach (var density in densityList)
            {
                density.Dispose();
            }
            
            jobHandles.Dispose();
            batchSampleResult.Dispose();
        }

        [BurstCompile]
        private struct JudgeBaseBlockJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> batchSampleResult;
            [ReadOnly] public int offsetX;
            [ReadOnly] public int offsetY;
            [ReadOnly] public int offsetZ;
            [ReadOnly] public int sizeX;
            [WriteOnly] public NativeArray<float> density;
            [WriteOnly] public NativeArray<BlockType> blocks;

            public void Execute(int index)
            {
                var sx = index / 1024;
                var sz = index % 1024 / 32;
                var sy = index % 32;
                
                var d = batchSampleResult[(offsetX + sx) * sizeX + offsetY + sy + (offsetZ + sz) * 288];
                blocks[index] = JudgeBaseBlockType(d);
                density[index] = d;
            }
        }

        public void GenerateChunksBatchAquifer(Vector3Int startChunkPos)
        {
            var sw = Stopwatch.StartNew();
            
            for (var x = 0; x < 8; x++)
            {
                for (var z = 0; z < 8; z++)
                {
                    for (var y = 0; y < 9; y++)
                    {
                        var chunkPos = startChunkPos + new Vector3Int(x, y, z);
                        var chunk = m_chunks[chunkPos];
                        
                        if (chunk.status == ChunkStatus.Aquifer)
                        {
                            GenerateAquifer(chunk);
                        }

                        // offset += 32768;
                        chunk.status = ChunkStatus.Surface;
                    }
                }
            }
            
            sw.Stop();
            Debug.Log($"Deal with aquifer {sw.ElapsedMilliseconds} ms");
        }
        
        public void GenerateChunksBatchSurface(Vector3Int startChunkPos)
        {
            var sw = Stopwatch.StartNew();
            
            for (var x = 0; x < 8; x++)
            {
                var offsetX = (startChunkPos.x + x) * 32;
                for (var z = 0; z < 8; z++)
                {
                    var chunks = new Chunk[12];
                    
                    var offsetZ = (startChunkPos.z + z) * 32;
                    var contexts = new SurfaceContext[32 * 32];
                    for (var sx = 0; sx < 32; sx++)
                    {
                        for (var sz = 0; sz < 32; sz++)
                        {
                            contexts[sx * 32 + sz] = NoiseManager.Instance.SampleSurface(new Vector3(offsetX + sx, 0, offsetZ + sz));
                        }
                    }

                    var topBlocks = new BlockType[32 * 32];
                    var topBlockHeights = new int[32 * 32];
                    for (var y = 8; y >= 0; y--)
                    {
                        var chunkPos = startChunkPos + new Vector3Int(x, y, z);
                        var chunk = m_chunks[chunkPos];
                        
                        if (chunk.status == ChunkStatus.Surface)
                        {
                            GenerateSurface(chunk, contexts, topBlocks, topBlockHeights);
                        }

                        // offset += 32768;
                        chunk.status = ChunkStatus.Tree;

                        if (y == 0)
                        {
                            chunk.topBlocks = topBlocks;
                            chunk.topBlockHeights = topBlockHeights;
                        }

                        chunks[y + 3] = chunk;
                    }
                    
                    // 生成树木
                    GenerateTree(chunks, contexts);
                }
            }
            
            sw.Stop();
            Debug.Log($"Deal with Surface {sw.ElapsedMilliseconds} ms");
        }

        public void ApplyDataModify(List<BlockModifyData> modifyDataList)
        {
            foreach (var modifyData in modifyDataList)
            {
                var chunkPos = modifyData.chunkPos;
                var chunk = GetChunk(chunkPos);
                if (chunk != null && chunk.status == ChunkStatus.DataReady)
                {
                    for (var i = 0; i < modifyData.blockIndex.Count; i++)
                    {
                        var index = modifyData.blockIndex[i];
                        var type = modifyData.blockTypes[i];
                        chunk.blocks[index] = type;
                    }
                }
            }
        }
        
        public Texture2D GenerateMap(Vector3Int startChunkPos, int size)
        {
            var sw = Stopwatch.StartNew();

            // 类似于Biome Map的生成方法
            var colorArray = new NativeArray<Color>(size * size, Allocator.TempJob);
            var blockColorArray = new NativeArray<Color>(Block.BlockColors, Allocator.TempJob);
            var chunkSize = size / 32;
            // var chunkStartPos = Chunk.WorldPosToChunkPos(startPos);
            
            var topBlocksArray = new NativeArray<BlockType>(size * size, Allocator.TempJob);
            var topHeightsArray = new NativeArray<int>(size * size, Allocator.TempJob);

            var index = 0;
            for (var x = 0; x < chunkSize; x++)
            {
                for (var z = 0; z < chunkSize; z++)
                {
                    var chunkPos = startChunkPos + new Vector3Int(x, 3, z);
                    var chunk = GetChunk(chunkPos);
                    var topBlocks = chunk.topBlocks;
                    var topHeights = chunk.topBlockHeights;
                    NativeArray<BlockType>.Copy(topBlocks, 0, topBlocksArray, index, 1024);
                    NativeArray<int>.Copy(topHeights, 0, topHeightsArray, index, 1024);
                    index += 1024;
                }
            }
            
            var job = new ColorSampleJob()
            {
                topBlocks = topBlocksArray,
                topHeightsArray = topHeightsArray,
                blockColorArray = blockColorArray,
                size = size,
                chunkSize = chunkSize,
                colorArray = colorArray,
            };
            
            var handle = job.Schedule(topBlocksArray.Length, 256);
            handle.Complete();

            var texture = new Texture2D(size, size);
            texture.SetPixels(colorArray.ToArray());
            texture.Apply();

            colorArray.Dispose();
            blockColorArray.Dispose();
            topBlocksArray.Dispose();
            topHeightsArray.Dispose();
            
            sw.Stop();
            Debug.Log($"Generate Map {sw.ElapsedMilliseconds} ms");
            return texture;
        }

        [BurstCompile]
        private struct ColorSampleJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<BlockType> topBlocks;
            [ReadOnly] public NativeArray<int> topHeightsArray;
            [ReadOnly] public NativeArray<Color> blockColorArray;
            [ReadOnly] public int size;
            [ReadOnly] public int chunkSize;
            [WriteOnly] public NativeArray<Color> colorArray;

            public void Execute(int index)
            {
                // row-major 填充
                // 以左下角为坐标原点
                var x = index % size;
                
                var z = index / size;
                
                var chunkX = x / 32;
                var chunkZ = z / 32;
                var chunkIndex = chunkX * chunkSize + chunkZ;
                
                var ix = x % 32;
                var iz = z % 32;
                
                var type = topBlocks[chunkIndex * 1024 + ix * 32 + iz];

                if (type == BlockType.Grass)
                {
                    var height = topHeightsArray[chunkIndex * 1024 + ix * 32 + iz];
                    var colorOffset = 1.0f + (height - 127) / 50.0f;
                    colorArray[index] = blockColorArray[(int)type] * colorOffset;
                    return;
                }
                
                colorArray[index] = blockColorArray[(int)type]; 

            }
        }

        public Vector3 ChoosePlayerPos(Vector3Int startChunkPos, int mapSize)
        {
            var chunkSize = mapSize / 32;

            var totalChance = 10;
            
            while (totalChance > 0)
            {
                // 这个是用种子固定要随机的数值应该已经取完了，所以后续随便用也没事
                var chunkX = startChunkPos.x + RsRandom.Instance.NextInt(0, chunkSize);
                var chunkZ = startChunkPos.z + RsRandom.Instance.NextInt(0, chunkSize);

                var bottomChunk = GetChunk(new Vector3Int(chunkX, 3, chunkZ));
                
                var topBlocks = bottomChunk.topBlocks;
                var topBlockHeights = bottomChunk.topBlockHeights;
                
                // 5次机会选不中就换Chunk
                var chance = 5;

                while (chance > 0)
                {
                    chance--;
                    var sx = RsRandom.Instance.NextInt(0, 32);
                    var sz = RsRandom.Instance.NextInt(0, 32);
                    var index = sx * 32 + sz;
                    if (topBlocks[index] != BlockType.Water)
                    {
                        var height = topBlockHeights[index];
                        return new Vector3(chunkX * 32 + sx, height / 2.0f, chunkZ * 32 + sz);
                    }
                }

                totalChance--;
            }
            
            Debug.LogError($"[ChunkManager] Can't find a possible player position");
            return Vector3.zero;
        }

        public Vector3 ChooseChestPos(Vector3Int startChunkPos, Vector3 playerPos, int mapSize)
        {
            // 宝箱位置 应该有几个限制条件
            // 1. 不得超出地图范围
            // 2. 离玩家要有chunkSize / 2的距离
            // 3. 不得在水中
            // 4. 暂时先放在xz位置的最顶端

            var playerChunkPos = Chunk.WorldPosToChunkPos(playerPos);
            var chunkSize = mapSize / 32;
            var minChunkDistance = chunkSize / 2;
            
            var totalChance = 10;
            
            while (totalChance > 0)
            {
                var chunkX = startChunkPos.x + RsRandom.Instance.NextInt(0, chunkSize);
                var chunkZ = startChunkPos.z + RsRandom.Instance.NextInt(0, chunkSize);

                if (Mathf.Abs(chunkX - playerChunkPos.x) + Mathf.Abs(chunkZ - playerChunkPos.z) < minChunkDistance)
                {
                    continue;
                }
                
                var bottomChunk = GetChunk(new Vector3Int(chunkX, 3, chunkZ));
                
                var topBlocks = bottomChunk.topBlocks;
                var topBlockHeights = bottomChunk.topBlockHeights;
                
                // 5次机会选不中就换Chunk
                var chance = 5;

                while (chance > 0)
                {
                    chance--;
                    var sx = RsRandom.Instance.NextInt(0, 32);
                    var sz = RsRandom.Instance.NextInt(0, 32);
                    var index = sx * 32 + sz;
                    if (topBlocks[index] != BlockType.Water)
                    {
                        var height = topBlockHeights[index];
                        return new Vector3(chunkX * 32 + sx, height / 2.0f + 0.5f, chunkZ * 32 + sz);
                    }
                }

                totalChance--;
            }
            
            Debug.LogError($"[ChunkManager] Can't find a possible chest position");
            return Vector3.zero;
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
                    var chunkX = playerChunkX + offsetX;
                    var chunkZ = playerChunkZ + offsetZ;
                    
                    // y=0,1,2目前默认不生成，从位置3开始判断
                    var chunkPos = new Vector3Int(chunkX, 3, chunkZ);

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

        public void UpdateChunkStatus(Vector3 playerPos, bool immediate = false)
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
                        
                        Debug.Log($"[RsSceneManager] 触发卸载Mesh {chunkPos}");
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
                        
                        Debug.Log($"[RsSceneManager] 触发删除Mesh {chunkPos}");
                    }
                }
            }

            var toGenerateMesh = new List<Chunk>();
            
            for (var offsetX = -m_loadDistance; offsetX < m_loadDistance + 1; offsetX++)
            {
                for (var offsetZ = -m_loadDistance; offsetZ < m_loadDistance + 1; offsetZ++)
                {
                    var chunkX = playerChunkPos.x + offsetX;
                    var chunkZ = playerChunkPos.z + offsetZ;
                    
                    // 目前先假定y轴上能有192m 12个chunk
                    // 先不生成地下
                    for (var chunkY = 11; chunkY >= 3; chunkY--)
                    {
                        var chunkPos = new Vector3Int(chunkX, chunkY, chunkZ);

                        if (!m_chunks.TryGetValue(chunkPos, out var chunk) || chunk.status == ChunkStatus.Empty)
                        {
                            // chunk生成以xz一组为单位还没有生成
                            break;
                        }
                        
                        if (chunk.status == ChunkStatus.DataReady)
                        {
                            // chunk数据准备完成，还没有生成Mesh, 
                            // 创建Chunk的GameObject
                            InitChunkGameObject(chunk);

                            if (immediate)
                            {
                                // 使用JobSystem立即批量生成Mesh
                                toGenerateMesh.Add(chunk);
                            }
                            else
                            {
                                // 通知tick manager安排更新mesh
                                RsSceneManager.Instance.UpdateChunkMeshOnTick(chunk);
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

            if (immediate && toGenerateMesh.Count > 0)
            {
                Chunk.BuildMeshUsingJobSystem(toGenerateMesh);
            }
        }

        private void InitChunkGameObject(Chunk chunk)
        {
            if (chunk.go == null)
            {
                var chunkTsfPos = Chunk.ChunkPosToWorldPos(chunk.chunkPos);
                var chunkGo = Instantiate(chunkPrefab, chunkTsfPos, Quaternion.identity);
                chunk.go = chunkGo;
            }
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

                // var samplerX = Mathf.FloorToInt(chunkPosXZ.x / 32.0f);
                // var samplerZ = Mathf.FloorToInt(chunkPosXZ.y / 32.0f);
                // var sampler = NoiseManager.Instance.GetOrCreateCacheSampler("InterTest", new Vector3Int(samplerX, 0, samplerZ)) as InterpolatedSampler;
                
                var chunks = new Chunk[12];
                // 目前先假定y轴上能有192m 12个chunk，创建Chunk
                // 先放弃地下的生成
                for (var chunkY = 3; chunkY < 12; chunkY++)
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
                // yield return StartCoroutine(GenerateBaseDataAsync(chunks));
                yield return StartCoroutine(GenerateChunksBatchBaseDataAsync(chunks[3].chunkPos, 1, 1));
                
                // Aquifer阶段
                for (var chunkY = 3; chunkY < 12; chunkY++)
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
                
                var topBlocks = new BlockType[32 * 32];
                var topBlockHeights = new int[32 * 32];
                for (var chunkY = 11; chunkY >= 3; chunkY--)
                {
                    var chunk = chunks[chunkY];
                    
                    if (chunk.status == ChunkStatus.Surface)
                    {
                        GenerateSurface(chunk, contexts, topBlocks, topBlockHeights);
                    }
                    
                    if (chunkY == 3)
                    {
                        chunk.topBlocks = topBlocks;
                        chunk.topBlockHeights = topBlockHeights;
                    }
                }
                
                // 生成树木
                GenerateTree(chunks, contexts);

                // if (chunkPosXZ.x == 0 && chunkPosXZ.y == 0)
                // {
                //     PutATree(new Vector3(2, 64, 11));
                // }
                
                yield return null;
            }

            m_isGeneratingChunks = false;
        }

        private IEnumerator GenerateBaseDataAsync(Chunk[] chunks)
        {
            var isRunning = true;
            
            Task.Run(() =>
            {
                try
                {
                    GenerateChunksBatchBaseData(chunks[3].chunkPos, 1, 1);

                    isRunning = false;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Chunk Generation Error: {chunks[0].chunkPos} {e.Message}\n{e.StackTrace}");  
                }
            });

            while (isRunning)
            {
                yield return null;
            }
        }
        
        private void GenerateAquifer(Chunk chunk)
        {
            // var offsetX = chunk.chunkPos.x * 32;
            var offsetY = chunk.chunkPos.y * 32;
            // var offsetZ = chunk.chunkPos.z * 32;
            // var sw = Stopwatch.StartNew();

            if (offsetY > m_seaLevel)
            {
                chunk.status = ChunkStatus.Surface;
                return;
            }
            
            // 首先判定是否是最底下的岩浆，不过现在还没实现岩浆，先跳过
            // TODO: 不淹没洞穴的含水层判断
            
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
            
            // sw.Stop();
            // Debug.Log($"[RsSceneManager] 生成Chunk {chunk.chunkPos} Aquifer耗时 {sw.ElapsedMilliseconds} ms");
        }

        private void GenerateSurface(Chunk chunk, SurfaceContext[] contexts, BlockType[] topBlocks, int[] topBlockHeights)
        {
            // var offsetX = chunk.chunkPos.x * 32;
            // var offsetZ = chunk.chunkPos.z * 32;
            var offsetY = chunk.chunkPos.y * 32;
            
            var sw = Stopwatch.StartNew();
            
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    var isTop = false;
                    var blockIndex = sx * 32 + sz;
                    if (topBlocks[blockIndex] == BlockType.Air)
                    {
                        isTop = true;
                    }
                    var context = contexts[blockIndex];
                    var index = Chunk.GetBlockIndex(sx, 31, sz);
                    for (var sy = 31; sy >= 0; sy--)
                    {
                        if (chunk.blocks[index] == BlockType.Stone)
                        {
                            var type = JudgeSurfaceBlockType(ref context);
                            chunk.blocks[index] = type;
                            context.stoneDepthAbove++;
                            context.waterHeight++;

                            if (isTop)
                            {
                                topBlocks[blockIndex] = type;
                                topBlockHeights[blockIndex] = offsetY + sy;
                                isTop = false;
                            }
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
                            
                            if (isTop)
                            {
                                topBlocks[blockIndex] = BlockType.Water;
                                topBlockHeights[blockIndex] = offsetY + sy;
                                isTop = false;
                            }
                        }

                        index--;
                    }

                    contexts[sx * 32 + sz] = context;
                }
            }
            
            chunk.status = ChunkStatus.Tree;
            
            // 数据完成时，通知邻居更新mesh，如果有的话
            NotifyNeighborUpdateMesh(chunk.chunkPos);
            
            sw.Stop();
            // Debug.Log($"[RsSceneManager] 生成Chunk {chunk.chunkPos} Surface耗时 {sw.ElapsedMilliseconds} ms");
        }


        private void GenerateTree(Chunk[] chunks, SurfaceContext[] contexts)
        {
            var sx = 2;
            var sz = 2;

            var chunkPos = chunks[3].chunkPos;
            var topBlocks = chunks[3].topBlocks;
            var topBlockHeights = chunks[3].topBlockHeights;
            var offsetX = chunkPos.x * 32;
            var offsetZ = chunkPos.z * 32;
            
            var sampler = NoiseManager.Instance.GetOrCreateSampler("Tree");
            var sampleResult = sampler.SampleBatch(new Vector3(offsetX, 0, offsetZ), 32, 1, 32);

            var blackList = new bool[32 * 32];
            
            while (sx < 30)
            {
                while (sz < 30)
                {
                    // 此处已生成树，直接跳过
                    if (blackList[sx * 32 + sz])
                    {
                        sz++;
                        continue;
                    }
                    
                    // 目前树只能长在草块上
                    if (topBlocks[sx * 32 + sz] != BlockType.Grass)
                    {
                        sz++;
                        continue;
                    }
                    
                    // 测试概率，与噪声、biome设置相关
                    // 相关的还没做
                    if (contexts[sx * 32 + sz].humidity < 0f || sampleResult[sx * 32 + sz] < 0.8f)
                    {
                        sz++;
                        continue;
                    }
                    
                    // 测试通过，生成一棵树
                    // 更新黑名单
                    for (var ix = -2; ix <= 2; ix++)
                    {
                        for (var iz = -2; iz <= 2; iz++)
                        {
                            blackList[(sx + ix) * 32 + sz + iz] = true;
                            topBlocks[(sx + ix) * 32 + sz + iz] = BlockType.Leaf;
                        }
                    }
                    
                    var height = topBlockHeights[sx * 32 + sz];
                    var treePos = new Vector3Int(sx, height + 1, sz);
                    var changeList = Tree.GetChangeList(1, 3);

                    foreach (var change in changeList)
                    {
                        var relativePos = change.Item1;
                        var newPos = treePos + relativePos;
                        var chunk = chunks[newPos.y / 32];
                        newPos.y %= 32;
                        chunk.blocks[Chunk.GetBlockIndex(newPos)] = change.Item2;
                    }

                    sz += 2;
                }

                sz = 2;
                sx++;
            }
            

            for (var chunkY = 3; chunkY < 12; chunkY++)
            {
                chunks[chunkY].status = ChunkStatus.DataReady;
            }

            sampleResult.Dispose();
        }
        
        // public void PutATree(Vector3 pos)
        // {
        //     var blockWorldPos = Chunk.WorldPosToBlockWorldPos(pos);
        //     var tree = new Tree(1, 3);
        //     var changeList = tree.GetChangeList();
        //
        //     foreach (var change in changeList)
        //     {
        //         var relativePos = change.Item1;
        //         var newPos = blockWorldPos + relativePos;
        //         PlaceBlock(newPos, change.Item2, true);
        //     }
        // }
        
        public void NotifyNeighborUpdateMesh(Vector3Int chunkPos)
        {
            foreach (var dir in m_directions)
            {
                var chunk = GetChunk(chunkPos + dir);
                if (chunk != null && chunk.status > ChunkStatus.DataReady)
                {
                    RsSceneManager.Instance.UpdateChunkMeshOnTick(chunk);
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
        
        private static BlockType JudgeBaseBlockType(float density)
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
                RsSceneManager.Instance.UpdateChunkMeshOnTick(chunk);
            }
            else
            {
                chunk.UpdateMesh();
            }
        }
    }
}