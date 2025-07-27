using System.Diagnostics;
using UnityEngine;
using RS.Scene;
using RS.Scene.Biome;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    public class BiomeSampler : RsSampler
    {
        // private RsSampler[] m_samplers;
        private RsBiomeSourceConfig m_source;
        private BiomeSourceTree m_sourceTree;

        public BiomeSampler()
        {
            var configManager = RsConfigManager.Instance;
            m_sourceTree = new BiomeSourceTree(configManager.GetBiomeSource("BiomeSource"));
        }

        public BiomeType Sample(float[] vals)
        {
            return m_sourceTree.GetBiomeType(vals);
        }
    }
}