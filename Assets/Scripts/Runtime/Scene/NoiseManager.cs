using System;
using System.Diagnostics;
using RS.Scene.Biome;
using RS.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    /// <summary>
    /// 总管噪声,随机数,采样器的类，保持随机数统一
    /// 各种噪声、随机数都通过这里来获取
    /// </summary>
    public class NoiseManager
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
            s_instance = new NoiseManager(seed);
        }

        private Int64 m_seed;
        private BiomeSampler m_biomeSampler;

        private RsSampler m_continents;
        private RsSampler m_depth;
        private RsSampler m_erosion;
        private RsSampler m_biomeHumidity;
        private RsSampler m_biomeTemperature;
        private RsSampler m_Ridges;
        
        private NoiseManager(Int64 seed)
        {
            Debug.Log($"[NoiseManager] 初始化中，seed: {seed}");
            var sw = Stopwatch.StartNew();
            
            m_seed = seed;
            
            // 理论上只有这里能够重置
            // 重置随机数生成器
            RsRandom.Init(seed);
            // 重置配置管理器
            RsConfigManager.Reload();
            // 重置采样器缓存
            RsSamplerManager.Reload();

            m_biomeSampler = new BiomeSampler();
            
            m_continents = RsSamplerManager.Instance.GetOrCreateSampler("Continents");
            m_depth = RsSamplerManager.Instance.GetOrCreateSampler("Depth");
            m_erosion = RsSamplerManager.Instance.GetOrCreateSampler("Erosion");
            m_biomeHumidity = RsSamplerManager.Instance.GetOrCreateSampler("BiomeHumidity");
            m_biomeTemperature = RsSamplerManager.Instance.GetOrCreateSampler("BiomeTemperature");
            m_Ridges = RsSamplerManager.Instance.GetOrCreateSampler("Ridges");
            
            sw.Stop();
            Debug.Log($"[NoiseManager] 初始化完成，耗时: {sw.ElapsedMilliseconds}ms");
        }

        public BiomeType SampleBiome(Vector3 pos, out float[] biomeParams)
        {
            biomeParams = new float[7];
            biomeParams[0] = m_continents.Sample(pos);
            // vals[1] = m_depth.Sample(pos);
            biomeParams[2] = m_erosion.Sample(pos);
            biomeParams[3] = m_biomeHumidity.Sample(pos);
            biomeParams[4] = m_biomeTemperature.Sample(pos);
            biomeParams[5] = m_Ridges.Sample(pos);
            biomeParams[6] = RsMath.RidgesFolded(biomeParams[5]);
            
            return m_biomeSampler.Sample(biomeParams);
        }
    }
}