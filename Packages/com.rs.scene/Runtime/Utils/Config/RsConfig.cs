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

            if (config.type == "sampler")
            {
                return config.arguments.ToObject<RsSamplerConfig>();
            }

            if (config.type == "biome")
            {
                return new RsBiomeSourceConfig(config.arguments as JObject);
            }

            Debug.LogError($"[RsConfig] Unknown config type: {config.type}");
            return null;
        }
    }

    
}