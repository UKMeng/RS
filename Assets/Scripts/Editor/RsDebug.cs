using System;
using System.Diagnostics;
using RS.Item;
using UnityEditor;
using UnityEngine;

using RS.Scene;
using RS.Scene.Biome;
using RS.Utils;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class RsDebug
    {
        [MenuItem("RSTest/Sampler Benchmark 1")]
        public static void SamplerBenchmark()
        {
            Debug.Log("Sample Benchmark");

            NoiseManager.Init(20250715);
            
            var samplerName = "InterTest";
            var sampler = NoiseManager.Instance.GetOrCreateCacheSampler("InterTest", new Vector3Int(0, 0, 0)) as InterpolatedSampler;
            
            var sw = Stopwatch.StartNew();

            GenerateChunks(0, 0, sampler);

            sw.Stop();
            Debug.Log($"{samplerName} Sampler Benchmark: {sw.ElapsedMilliseconds}ms");
        }
        
        [MenuItem("RSTest/Sampler Benchmark 8 x 8")]
        public static void SamplerBenchmark64()
        {
            Debug.Log("Sample Benchmark");

            NoiseManager.Init(20250715);
            
            var samplerName = "InterTest";
            
            var sampler = NoiseManager.Instance.GetOrCreateCacheSampler("InterTest", new Vector3Int(0, 0, 0)) as InterpolatedSampler;
            
            var sw = Stopwatch.StartNew();
            
            for (var x = 0; x < 8; x++)
            {
                for (var z = 0; z < 8; z++)
                {
                    GenerateChunks(x, z, sampler);
                }
            }

            sw.Stop();
            Debug.Log($"{samplerName} Sampler Benchmark: {sw.ElapsedMilliseconds}ms");
        }

        private static void GenerateChunks(int x, int z, InterpolatedSampler sampler)
        {
            var chunks = new Chunk[12];
            
            for (var chunkY = 3; chunkY < 12; chunkY++)
            {
                var chunkPos = new Vector3Int(x, chunkY, z);
                var chunk = new Chunk(chunkPos);
                chunk.status = ChunkStatus.BaseData;
                chunks[chunkY] = chunk;
            }
            
            // Base Data
            // var baseSw = Stopwatch.StartNew();
            for (var chunkY = 3; chunkY < 12; chunkY++)
            {
                if (chunks[chunkY].status == ChunkStatus.BaseData)
                {
                    GenerateBaseData(chunks[chunkY], sampler);
                }
            }
            // baseSw.Stop();
            // Debug.Log($"Base Data: {baseSw.ElapsedMilliseconds}ms");
            
            // Aquifer阶段
            // var aquiferSw = Stopwatch.StartNew();
            for (var chunkY = 3; chunkY < 12; chunkY++)
            {
                if (chunkY == 3)
                {
                    if (chunks[chunkY].status == ChunkStatus.Aquifer)
                    {
                        GenerateAquifer(chunks[chunkY]);
                    }
                }
                else
                {
                    if (chunks[chunkY].status == ChunkStatus.Aquifer)
                    {
                        chunks[chunkY].status = ChunkStatus.Surface;
                    }
                }
            }
            
            // aquiferSw.Stop();
            // Debug.Log($"Aquifer: {aquiferSw.ElapsedMilliseconds}ms");
            
            // 生成Surface阶段
            // var surfaceSw = Stopwatch.StartNew();
            
            var offsetX = x * 32;
            var offsetZ = z * 32;
            var contexts = new SurfaceContext[32 * 32];
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    contexts[sx * 32 + sz] = NoiseManager.Instance.SampleSurface(new Vector3(offsetX + sx, 0, offsetZ + sz));
                }
            }
            for (var chunkY = 11; chunkY >= 3; chunkY--)
            {
                var chunk = chunks[chunkY];
                    
                if (chunk.status == ChunkStatus.Surface)
                {
                    GenerateSurface(chunk, contexts);
                }
            }

            // surfaceSw.Stop();
            // Debug.Log($"Surface: {surfaceSw.ElapsedMilliseconds}ms");
        }
        
        private static void GenerateBaseData(Chunk chunk, InterpolatedSampler sampler)
        {
            var offsetX = chunk.chunkPos.x * 32;
            var offsetZ = chunk.chunkPos.z * 32;
            var offsetY = chunk.chunkPos.y * 32;
            
            var blocks = new BlockType[32 * 32 * 32];
            var finalDensity = new float[32 * 32 * 32];
            var index = 0;
            
            var batchSampleResult = sampler.SampleBatch(new Vector3(offsetX, offsetY, offsetZ));

            // var judgeSw = Stopwatch.StartNew();
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    for (var sy = 0; sy < 32; sy++)
                    {
                        var density = batchSampleResult[sx * 1024 + sy + sz * 32];
                        blocks[index] = density > 0 ? BlockType.Stone : BlockType.Air;
                        finalDensity[index++] = density;
                    }
                }
            }
            // judgeSw.Stop();
            // Debug.Log($"Judge: {judgeSw.Elapsed.TotalMilliseconds * 1000:n3} us");
            // Debug.Log($"Judge: {judgeSw.ElapsedMilliseconds} ms");

            
            chunk.blocks = blocks;
            chunk.density = finalDensity;
            chunk.status = ChunkStatus.Aquifer;
        }
        
        private static void GenerateAquifer(Chunk chunk)
        {
            var offsetX = chunk.chunkPos.x * 32;
            var offsetY = chunk.chunkPos.y * 32;
            var offsetZ = chunk.chunkPos.z * 32;
            var sw = Stopwatch.StartNew();
            
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
                        if (chunk.blocks[index] == BlockType.Air && sy + offsetY < 127)
                        {
                            chunk.blocks[index] = BlockType.Water;
                        }

                        index++;
                    }
                }
            }
            
            chunk.status = ChunkStatus.Surface;
            
            sw.Stop();
            // Debug.Log($"[SceneManager] 生成Chunk {chunk.chunkPos} Aquifer耗时 {sw.ElapsedMilliseconds} ms");
        }
        
        private static void GenerateSurface(Chunk chunk, SurfaceContext[] contexts)
        {
            // var offsetX = chunk.chunkPos.x * 32;
            // var offsetZ = chunk.chunkPos.z * 32;
            // var offsetY = chunk.chunkPos.y * 32;
            
            // var sw = Stopwatch.StartNew();
            
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
            // NotifyNeighborUpdateMesh(chunk.chunkPos);
            
            // sw.Stop();
            // Debug.Log($"[SceneManager] 生成Chunk {chunk.chunkPos} Surface耗时 {sw.ElapsedMilliseconds} ms");
        }
        
        
        private static BlockType JudgeSurfaceBlockType(ref SurfaceContext context)
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
    }
}