using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;


namespace RS.Utils
{
    public class RsConfigManager
    {
        private static RsConfigManager s_instance;
        
        public static RsConfigManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new RsConfigManager();
                }

                return s_instance;
            }
        }

        public static RsConfigManager Reload()
        {
            s_instance = new RsConfigManager();
            return s_instance;
        }

        private static string[] m_presetNoises = new string[]
        {
            "CaveLayer",
            "Cheese",
            "Continentalness",
            "Entrance",
            "Erosion",
            "Humidity",
            "Jagged",
            "Offset",
            "Pillar",
            "PillarRareness",
            "PillarThickness",
            "Ridges",
            "Spaghetti2d",
            "Spaghetti2dElevation",
            "Spaghetti2dModulator",
            "Spaghetti2dThickness",
            "Spaghetti3d1",
            "Spaghetti3d2",
            "Spaghetti3dRarity",
            "Spaghetti3dThickness",
            "SpaghettiRoughness",
            "SpaghettiRoughnessModulator",
            "Surface",
            "Temperature"
        };

        private static string[] m_presetSamplers = new string[]
        {
            "Base3D",
            "CaveEntrances",
            "CavePillars",
            "CaveNoodle",
            "CaveSpaghetti2d",
            "CaveSpaghettiRoughness",
            "Compare",
            "Continents",
            "Depth",
            "Erosion",
            "Factor",
            "FinalDensity",
            "Height",
            "HeightWithCave",
            "Humidity",
            "InterTest",
            "Offset",
            "Ridges",
            "RidgesFolded",
            "ShiftX",
            "ShiftZ",
            "Spaghetti2dThicknessModulator",
            "SurfaceDensity",
            "SurfaceNoise",
            "Temperature",
            "Temp",
            "YLimit",
        };

        private static string[] m_presetBiomeSources = new string[]
        {
            "Beach",
            "BiomeSource",
            "Coast",
            "Inland",
            "InlandValley",
            "LowMountain",
            "NearInland",
            "Valley",
        };

        private Dictionary<string, RsNoiseConfig> m_noiseConfigs;
        private Dictionary<string, RsSamplerConfig> m_samplerConfigs;
        private Dictionary<string, RsBiomeSourceConfig> m_biomeSourceConfigs;
        
        public RsConfigManager()
        {
            Init();
        }

        public RsNoiseConfig GetNoiseConfig(string name)
        {
            if (!m_noiseConfigs.TryGetValue(name, out var config))
            {
                config = RsConfig.GetConfig("Noise/" + name) as RsNoiseConfig;
                m_noiseConfigs.Add(name, config);
            }
            
            return config;
        }

        public RsSamplerConfig GetSamplerConfig(string name)
        {
            if (!m_samplerConfigs.TryGetValue(name, out var config))
            {
                config = RsConfig.GetConfig("Sampler/" + name) as RsSamplerConfig;
                m_samplerConfigs.Add(name, config);
            }
            
            return config;
        }

        public RsBiomeSourceConfig GetBiomeSource(string name)
        {
            if (!m_biomeSourceConfigs.TryGetValue(name, out var config))
            {
                config = RsConfig.GetConfig("Biome/" + name) as RsBiomeSourceConfig;
                m_biomeSourceConfigs.Add(name, config);
            }

            return config;
        }
        

        public string[] GetLoadedSamplerConfigName()
        {
            return m_samplerConfigs.Keys.ToArray();
        }

        private void Init()
        {
            var sw = Stopwatch.StartNew();
            
            m_noiseConfigs = new Dictionary<string, RsNoiseConfig>();
            m_samplerConfigs = new Dictionary<string, RsSamplerConfig>();
            m_biomeSourceConfigs = new Dictionary<string, RsBiomeSourceConfig>();

            foreach (var preset in m_presetNoises)
            {
                var config = RsConfig.GetConfig("Noise/" + preset) as RsNoiseConfig;
                m_noiseConfigs.Add(preset, config);
            }

            foreach (var preset in m_presetSamplers)
            {
                var config = RsConfig.GetConfig("Sampler/" + preset) as RsSamplerConfig;
                m_samplerConfigs.Add(preset, config);
            }

            foreach (var preset in m_presetBiomeSources)
            {
                var config = RsConfig.GetConfig("Biome/" + preset) as RsBiomeSourceConfig;
                m_biomeSourceConfigs.Add(preset, config);
            }
            
            sw.Stop();
            Debug.Log($"[RsConfigManager] Init ConfigManager Cost: {sw.ElapsedMilliseconds} ms");
        }
    }
}