using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    public class RsSplinePointConfig : RsConfig
    {
        public float location;
        public float derivative;
        public RsSamplerConfig value;
    }
    
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
                case "abs":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = value.ToObject<RsSamplerConfig>().BuildRsSampler();
                        sampler = new AbsSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "cache2D":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = value.ToObject<RsSamplerConfig>().BuildRsSampler();
                        sampler = new Cache2DSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "flatCache":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = value.ToObject<RsSamplerConfig>().BuildRsSampler();
                        sampler = new FlatCacheSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "add":
                {
                    if (arguments.TryGetValue("left", out var left)
                        && arguments.TryGetValue("right", out var right))
                    {
                        var leftSampler = left.ToObject<RsSamplerConfig>().BuildRsSampler();
                        var rightSampler = right.ToObject<RsSamplerConfig>().BuildRsSampler();
                        sampler = new AddSampler(leftSampler, rightSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }

                    break;
                }
                case "mul":
                {
                    if (arguments.TryGetValue("left", out var left)
                        && arguments.TryGetValue("right", out var right))
                    {
                        var leftSampler = left.ToObject<RsSamplerConfig>().BuildRsSampler();
                        var rightSampler = right.ToObject<RsSamplerConfig>().BuildRsSampler();
                        sampler = new MulSampler(leftSampler, rightSampler);
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
                case "shiftA":
                {
                    if (arguments.TryGetValue("noise", out var noiseToken))
                    {
                        var noiseName = noiseToken.Value<string>();
                        var noiseConfig = RsConfigManager.Instance.GetNoiseConfig(noiseName);
                        var noise = new RsNoise(RsRandom.Instance.NextUInt64(), noiseConfig);
                        sampler = new ShiftASampler(noise);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "shiftB":
                {
                    if (arguments.TryGetValue("noise", out var noiseToken))
                    {
                        var noiseName = noiseToken.Value<string>();
                        var noiseConfig = RsConfigManager.Instance.GetNoiseConfig(noiseName);
                        var noise = new RsNoise(RsRandom.Instance.NextUInt64(), noiseConfig);
                        sampler = new ShiftBSampler(noise);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "shiftedNoise":
                {
                    if (arguments.TryGetValue("noise", out var noiseToken)
                        && arguments.TryGetValue("x", out var x)
                        && arguments.TryGetValue("y", out var y)
                        && arguments.TryGetValue("z", out var z)
                        && arguments.TryGetValue("xzScale", out var xzScaleValue)
                        && arguments.TryGetValue("yScale", out var yScaleValue))
                    {
                        var noiseName = noiseToken.Value<string>();
                        var noiseConfig = RsConfigManager.Instance.GetNoiseConfig(noiseName);
                        var noise = new RsNoise(RsRandom.Instance.NextUInt64(), noiseConfig);

                        var samplerX = x.ToObject<RsSamplerConfig>().BuildRsSampler();
                        var samplerY = y.ToObject<RsSamplerConfig>().BuildRsSampler();
                        var samplerZ = z.ToObject<RsSamplerConfig>().BuildRsSampler();
                        
                        var xzScale = xzScaleValue.ToObject<float>();
                        var yScale = yScaleValue.ToObject<float>();

                        sampler = new ShiftedNoiseSampler(noise, samplerX, samplerY, samplerZ, xzScale, yScale);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }

                    break;
                }
                case "spline":
                {
                    if (arguments.TryGetValue("coordinate", out var coordToken)
                        && arguments.TryGetValue("points", out var points))
                    {
                        var coordinate = coordToken.ToObject<RsSamplerConfig>().BuildRsSampler();
                        var pointList = (points as JArray).ToObject<List<RsSplinePointConfig>>();
                        var pointCount = pointList.Count;
                        var locations = new float[pointCount];
                        var derivatives = new float[pointCount];
                        var values = new RsSampler[pointCount];

                        for (var i = 0; i < pointCount; i++)
                        {
                            locations[i] = pointList[i].location;
                            derivatives[i] = pointList[i].derivative;
                            values[i] = pointList[i].value.BuildRsSampler();
                        }
                        
                        sampler = new SplineSampler(coordinate, locations, derivatives, values);
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