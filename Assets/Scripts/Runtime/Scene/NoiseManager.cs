using System;
using System.Collections.Generic;
using System.Diagnostics;
using RS.Scene.Biome;
using RS.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    /// <summary>
    /// 用于判断地表方块的数据上下文
    /// </summary>
    public struct SurfaceContext
    {
        public BiomeType biome;
        public float humidity;
        public float surfaceNoise;
        public float surfaceDepth;
        public int waterHeight;     // 本方块距离上方最近的液体表面上方一格的距离，如果上方有空气，则不在水下，值为int最小值
        public int stoneDepthAbove; // 本方格距离上方最近空气格之间的非液体格的数量，如果上方直接为空气则为0
        public int stoneDepthBelow; 
        public int minSurfaceLevel;
    }
    
    /// <summary>
    /// 总管噪声,随机数,采样器的类，保持随机数统一
    /// 各种噪声、随机数都通过这里来获取
    /// </summary>
    public class NoiseManager : IDisposable
    {
        private static NoiseManager s_instance;

        public static NoiseManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new NoiseManager(20250715);
                }
                return s_instance;
            }
        }

        public static void Init(Int64 seed)
        {
            if (s_instance != null)
            {
                s_instance.Dispose();
            }
            
            s_instance = new NoiseManager(seed);
            // 需要在单例对象生成后
            s_instance.InitNoiseManager();
        }

        private long m_seed;
        private BiomeSampler m_biomeSampler;

        // private RsSampler m_continents;
        // private RsSampler m_depth;
        // private RsSampler m_erosion;
        // private RsSampler m_humidity;
        // private RsSampler m_temperature;
        // private RsSampler m_ridges;
        // private RsSampler m_surfaceNoise;

        private Dictionary<string, RsNoise> m_noises;
        private Dictionary<string, RsSampler> m_samplers;
        private Dictionary<(string, Vector3Int), RsSampler> m_cacheSampelers;
        
        private NoiseManager(long seed)
        {
            m_seed = seed;
            m_noises = new Dictionary<string, RsNoise>();
            m_samplers = new Dictionary<string, RsSampler>();
            m_cacheSampelers = new Dictionary<(string, Vector3Int), RsSampler>();
        }

        public void Dispose()
        {
            // Sampler和Noise都统一在这里Dispose
            // Sampler里面包含噪声的，不在sampler的Dispose进行
            foreach (var (_, noise) in m_noises)
            {
                noise.Dispose();
            }

            foreach (var (_, sampler) in m_samplers)
            {
                sampler.Dispose();
            }

            s_instance = null;
        }

        private void InitNoiseManager()
        {
            Debug.Log($"[NoiseManager] 初始化中，seed: {m_seed}");
            var sw = Stopwatch.StartNew();
            
            // 理论上只有这里能够重置
            // 重置随机数生成器
            RsRandom.Init(m_seed);
            // 重置配置管理器
            RsConfigManager.Reload();
            // 重置采样器与噪声缓存
            // m_continents = GetOrCreateSampler("Continents");
            // m_depth = GetOrCreateSampler("Depth");
            // m_erosion = GetOrCreateSampler("Erosion");
            // m_humidity = GetOrCreateSampler("Humidity");
            // m_temperature = GetOrCreateSampler("Temperature");
            // m_ridges = GetOrCreateSampler("Ridges");
            // m_surfaceNoise = GetOrCreateSampler("SurfaceNoise");
            m_biomeSampler = new BiomeSampler();
            
            sw.Stop();
            Debug.Log($"[NoiseManager] 初始化完成，耗时: {sw.ElapsedMilliseconds}ms");
        }
        
        public RsSampler GetOrCreateSampler(string samplerName)
        {
            if (!m_samplers.TryGetValue(samplerName, out var sampler))
            {
                // Debug.Log($"[RsSamplerManager]实例化{samplerName}");
                var config = RsConfigManager.Instance.GetSamplerConfig(samplerName);
                sampler = config.BuildRsSampler(Vector3Int.zero);
                m_samplers.Add(samplerName, sampler);
            }

            return sampler;
        }

        public RsSampler GetOrCreateCacheSampler(string samplerName, Vector3Int pos)
        {
            if (!m_cacheSampelers.TryGetValue((samplerName, pos), out var sampler))
            {
                var config = RsConfigManager.Instance.GetSamplerConfig(samplerName);
                sampler = config.BuildRsSampler(pos);
                m_cacheSampelers.Add((samplerName, pos), sampler);
            }

            return sampler;
        }

        public RsNoise GetOrCreateNoise(string noiseName)
        {
            if (!m_noises.TryGetValue(noiseName, out var noise))
            {
                var config = RsConfigManager.Instance.GetNoiseConfig(noiseName);
                noise = new RsNoise(RsRandom.Instance.NextULong(), config);
                m_noises.Add(noiseName, noise);
            }

            return noise;
        }

        public BiomeType SampleBiome(Vector3 pos, out float[] biomeParams)
        {
            var chunkPos = Chunk.WorldPosToChunkPos(pos);
            var samplerX = Mathf.FloorToInt(chunkPos.x / 32.0f);
            var samplerZ = Mathf.FloorToInt(chunkPos.z / 32.0f);
            
            var continents = GetOrCreateCacheSampler("Continents", new Vector3Int(samplerX, 0, samplerZ));
            // var depth = GetOrCreateCacheSampler("Depth", new Vector3Int(samplerX, 0, samplerZ));
            var erosion = GetOrCreateCacheSampler("Erosion", new Vector3Int(samplerX, 0, samplerZ));
            var humidity = GetOrCreateCacheSampler("Humidity", new Vector3Int(samplerX, 0, samplerZ));
            var temperature = GetOrCreateCacheSampler("Temperature", new Vector3Int(samplerX, 0, samplerZ));
            var ridges = GetOrCreateCacheSampler("Ridges", new Vector3Int(samplerX, 0, samplerZ));
            
            
            biomeParams = new float[7];
            biomeParams[0] = continents.Sample(pos);
            // vals[1] = m_depth.Sample(pos);
            biomeParams[2] = erosion.Sample(pos);
            biomeParams[3] = humidity.Sample(pos);
            biomeParams[4] = temperature.Sample(pos);
            biomeParams[5] = ridges.Sample(pos);
            biomeParams[6] = RsMath.RidgesFolded(biomeParams[5]);
            
            return m_biomeSampler.Sample(biomeParams);
        }

        public SurfaceContext SampleSurface(Vector3 pos)
        {
            var chunkPos = Chunk.WorldPosToChunkPos(pos);
            var samplerX = Mathf.FloorToInt(chunkPos.x / 32.0f);
            var samplerZ = Mathf.FloorToInt(chunkPos.y / 32.0f);
            var surfaceNoiseSampler = GetOrCreateCacheSampler("SurfaceNoise", new Vector3Int(samplerX, 0, samplerZ));
            
            var biome = SampleBiome(pos, out var sampleValues);
            var surfaceNoise = surfaceNoiseSampler.Sample(pos);
            var surfaceDepth = Mathf.FloorToInt(6 * surfaceNoise + 6);
            
            return new SurfaceContext
            {
                biome = biome, 
                humidity = sampleValues[3],
                surfaceDepth = surfaceDepth,
                waterHeight = int.MinValue,
                stoneDepthAbove = 0
            };
        }
    }
}