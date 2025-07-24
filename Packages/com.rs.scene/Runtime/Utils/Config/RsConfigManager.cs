using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;


namespace RS.Utils
{
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
            "Continentalness",
            "Erosion",
            "Humidity",
            "Offset",
            "Ridge",
            "Temperature"
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
                var config = RsConfig.GetConfig("Noise/" + preset) as RsNoiseConfig;
                m_noiseConfigs.Add(preset, config);
            }
            
            sw.Stop();
            Debug.Log($"[RsConfigManager] Init ConfigManager Cost: {sw.ElapsedMilliseconds} ms");
        }
    }
}