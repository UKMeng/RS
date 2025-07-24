using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    public class RsSamplerConfig : RsConfig
    {
        public string type;
        public JObject arguments;

        public RsSampler BuildRsSampler()
        {
            RsSampler sampler = null;
            switch (type)
            {
                case "constant":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        sampler = new ConstantSampler(value.Value<float>());
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "add":
                {
                    if (arguments.TryGetValue("argument1", out var arg1)
                        && arguments.TryGetValue("argument2", out var arg2))
                    {
                        var arg1Sampler = arg1.ToObject<RsSamplerConfig>().BuildRsSampler();
                        var arg2Sampler = arg2.ToObject<RsSamplerConfig>().BuildRsSampler();
                        sampler = new AddSampler(arg1Sampler, arg2Sampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }

                    break;
                }
                case "normal":
                {
                    if (arguments.TryGetValue("noise", out var noiseToken))
                    {
                        var noiseName = noiseToken.Value<string>();
                        var noiseConfig = RsConfigManager.Instance.GetNoiseConfig(noiseName);
                        var noise = new RsNoise(RsRandom.Instance.NextUInt64(), noiseConfig);
                        sampler = new RsSampler(noise);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                default:
                {
                    Debug.LogError($"[RsConfig] Unknown Sampler Type: {type}");
                    break;
                }
            }
            return sampler;
        }
    }
    
    
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

            Debug.LogError($"[RsConfig] Unknown config type: {config.type}");
            return null;
        }
    }

    
}