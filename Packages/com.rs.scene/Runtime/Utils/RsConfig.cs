using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    public class RsNoiseConfig : RsConfig
    {
        public int firstOctave;
        public float[] amplitudes;
    }
    
    public class RsConfigWrapper
    {
        public string type;
        public JObject arguments;
    }
    
    public class RsConfig
    {
        private static string m_configPath = "Assets/Scripts/Runtime/Config/{0}.json";

        public static RsConfig GetConfig(string name)
        {
            var json = File.ReadAllText(string.Format(m_configPath, name));
            var config = JsonConvert.DeserializeObject<RsConfigWrapper>(json);
            if (config.type == "noise")
            {
                return config.arguments.ToObject<RsNoiseConfig>();
            }

            Debug.LogError($"[RsConfig] Unknown config type: {config.type}");
            return null;
        }
    }

    public class RsConfigManager
    {
        private static RsConfigManager m_instance;
        
        public static RsConfigManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new RsConfigManager();
                }

                return m_instance;
            }
        }

        private static string[] m_presetNoises = new string[]
        {
            "Erosion",
        };

        private Dictionary<string, RsNoiseConfig> m_noiseConfigs;

        public RsConfigManager()
        {
            Init();
        }

        public RsNoiseConfig GetNoiseConfig(string name)
        {
            return m_noiseConfigs[name];
        }

        private void Init()
        {
            var sw = Stopwatch.StartNew();

            m_noiseConfigs = new Dictionary<string, RsNoiseConfig>();

            foreach (var preset in m_presetNoises)
            {
                var config = RsConfig.GetConfig(preset) as RsNoiseConfig;
                m_noiseConfigs.Add(preset, config);
            }
            
            sw.Stop();
            Debug.Log($"[RsConfigManager] Init ConfigManager Cost: {sw.ElapsedMilliseconds} ms");
        }
    }
}