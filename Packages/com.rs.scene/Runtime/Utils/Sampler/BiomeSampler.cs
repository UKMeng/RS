using System.Diagnostics;
using UnityEngine;
using RS.Scene;
using RS.Scene.Biome;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    public class BiomeSampler : RsSampler
    {
        private RsSampler[] m_samplers;
        private RsBiomeSourceConfig m_source;
        private BiomeSourceTree m_sourceTree;

        public BiomeSampler()
        {
            var configManager = RsConfigManager.Instance;
            // m_source = configManager.GetBiomeSource();
            m_sourceTree = new BiomeSourceTree();

            m_samplers = new RsSampler[6];
            m_samplers[0] = configManager.GetSamplerConfig("Continents").BuildRsSampler();
            // m_samplers[1] = configManager.GetSamplerConfig("Depth").BuildRsSampler();
            m_samplers[2] = configManager.GetSamplerConfig("Erosion").BuildRsSampler();
            m_samplers[3] = configManager.GetSamplerConfig("BiomeHumidity").BuildRsSampler();
            m_samplers[4] = configManager.GetSamplerConfig("BiomeTemperature").BuildRsSampler();
            m_samplers[5] = configManager.GetSamplerConfig("Ridges").BuildRsSampler();
        }
        
        
        public BiomeSampler(RsBiomeSourceConfig source, RsSampler continentalness, RsSampler depth, RsSampler erosion, RsSampler humidity,
            RsSampler temperature, RsSampler ridges)
        {
            m_source = source;
            
            m_samplers = new RsSampler[6];
            m_samplers[0] = continentalness;
            m_samplers[1] = depth;
            m_samplers[2] = erosion;
            m_samplers[3] = humidity;
            m_samplers[4] = temperature;
            m_samplers[5] = ridges;
        }

        public BiomeType Sample(Vector3 pos)
        {
            var values = new float[7];
            for (var i = 0; i < 6; i++)
            {
                // 跳过深度采样
                if (i == 1)
                {
                    continue;
                }
                
                values[i] = m_samplers[i].Sample(pos);
            }
            
            // PV
            values[6] = RsMath.RidgesFolded(values[5]);

            return m_sourceTree.GetBiomeType(values);

            // var minDis = float.MaxValue;
            // BiomeType minType = BiomeType.Ocean;

            // var sw = Stopwatch.StartNew();

            // foreach (var biome in m_source.biomes)
            // {
            //     var dis = biome.GetDistance(values);
            //     if (dis < minDis)
            //     {
            //         minDis = dis;
            //         minType = biome.type;
            //     }
            // }

            // sw.Stop();
            // Debug.Log($"获取一次类型耗费时间{sw.ElapsedMilliseconds} ms");

            // return minType;
        }
    }
}