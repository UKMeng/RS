using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RS.Scene;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    public class RsSplinePointConfig : RsConfig
    {
        public float location;
        public float derivative;
        public JToken value;
    }
    
    public class RsSamplerConfig : RsConfig
    {
        public string type;
        public JObject arguments;

        public RsSampler BuildRsSampler(Vector3Int startPos)
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
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new AbsSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "square":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new SquareSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "cube":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new CubeSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "halfNegative":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new HalfNegativeSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "quarterNegative":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new QuarterNegativeSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "clamp":
                {
                    if (arguments.TryGetValue("value", out var valueToken) &&
                        arguments.TryGetValue("min", out var minToken) &&
                        arguments.TryGetValue("max", out var maxToken))
                    {
                        var value = ParseJTokenToSampler(valueToken, startPos);
                        var min = minToken.ToObject<float>();
                        var max = maxToken.ToObject<float>();
                        sampler = new ClampSampler(value, min, max);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "yClampedGradient":
                {
                    if (arguments.TryGetValue("min", out var minToken) &&
                        arguments.TryGetValue("max", out var maxToken) &&
                        arguments.TryGetValue("from", out var fromToken) &&
                        arguments.TryGetValue("to", out var toToken))
                    {
                        var min = minToken.ToObject<float>();
                        var max = maxToken.ToObject<float>();
                        var from = fromToken.ToObject<float>();
                        var to = toToken.ToObject<float>();
                        sampler = new YClampedGradientSampler(min, max, from, to);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "squeeze":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new SqueezeSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "interpolated":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new InterpolatedSampler(valueSampler);
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
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new Cache2DSampler(valueSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "cacheOnce":
                {
                    if (arguments.TryGetValue("value", out var value))
                    {
                        var valueSampler = ParseJTokenToSampler(value, startPos);
                        sampler = new CacheOnceSampler(valueSampler);
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
                        var valueSampler = ParseJTokenToSampler(value, startPos);
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
                        var leftSampler = ParseJTokenToSampler(left, startPos);
                        var rightSampler = ParseJTokenToSampler(right, startPos);
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
                        var leftSampler = ParseJTokenToSampler(left, startPos);
                        var rightSampler = ParseJTokenToSampler(right, startPos);
                        sampler = new MulSampler(leftSampler, rightSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }

                    break;
                }
                case "max":
                {
                    if (arguments.TryGetValue("left", out var left)
                        && arguments.TryGetValue("right", out var right))
                    {
                        var leftSampler = ParseJTokenToSampler(left, startPos);
                        var rightSampler = ParseJTokenToSampler(right, startPos);
                        sampler = new MaxSampler(leftSampler, rightSampler);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }

                    break;
                }
                case "min":
                {
                    if (arguments.TryGetValue("left", out var left)
                        && arguments.TryGetValue("right", out var right))
                    {
                        var leftSampler = ParseJTokenToSampler(left, startPos);
                        var rightSampler = ParseJTokenToSampler(right, startPos);
                        sampler = new MinSampler(leftSampler, rightSampler);
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
                        var noise = NoiseManager.Instance.GetOrCreateNoise(noiseName);
                        sampler = new RsSampler(noise);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "noise":
                {
                    if (arguments.TryGetValue("noise", out var noiseToken)
                        && arguments.TryGetValue("xzScale", out var xzScaleToken)
                        && arguments.TryGetValue("yScale", out var yScaleToken))
                    {
                        var noiseName = noiseToken.Value<string>();
                        var noise = NoiseManager.Instance.GetOrCreateNoise(noiseName);
                        
                        var xzScale = xzScaleToken.Value<float>();
                        var yScale = yScaleToken.Value<float>();
                        
                        sampler = new NoiseSampler(noise, xzScale, yScale);
                    }
                    else
                    {
                        Debug.LogError($"[RsConfig] Parse Failed {type}");
                    }
                    
                    break;
                }
                case "weirdScaled":
                {
                    if (arguments.TryGetValue("noise", out var noiseToken)
                        && arguments.TryGetValue("rarity", out var rarityToken)
                        && arguments.TryGetValue("mapper", out var mapperToken))
                    {
                        var noiseName = noiseToken.Value<string>();
                        var noise = NoiseManager.Instance.GetOrCreateNoise(noiseName);
                        var rarity = ParseJTokenToSampler(rarityToken, startPos);
                        var mapper = mapperToken.Value<int>();
                        
                        sampler = new WeirdScaledSampler(noise, rarity, mapper);
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
                        var noise = NoiseManager.Instance.GetOrCreateNoise(noiseName);
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
                        var noise = NoiseManager.Instance.GetOrCreateNoise(noiseName);
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
                        var noise = NoiseManager.Instance.GetOrCreateNoise(noiseName);

                        var samplerX = ParseJTokenToSampler(x, startPos);
                        var samplerY = ParseJTokenToSampler(y, startPos);
                        var samplerZ = ParseJTokenToSampler(z, startPos);
                        
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
                case "rangeChoice":
                {
                    if (arguments.TryGetValue("input", out var inputToken)
                        && arguments.TryGetValue("inRange", out var inRangeToken)
                        && arguments.TryGetValue("outRange", out var outRangeToken)
                        && arguments.TryGetValue("min", out var minToken)
                        && arguments.TryGetValue("max", out var maxToken))
                    {
                        var input = ParseJTokenToSampler(inputToken, startPos);
                        var inRange = ParseJTokenToSampler(inRangeToken, startPos);
                        var outRange = ParseJTokenToSampler(outRangeToken, startPos);
                        var min = minToken.ToObject<float>();
                        var max = maxToken.ToObject<float>();
                        
                        sampler = new RangeChoiceSampler(input, inRange, outRange, min, max);
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
                        var coordinate = ParseJTokenToSampler(coordToken, startPos);
                        var pointList = (points as JArray).ToObject<List<RsSplinePointConfig>>();
                        var pointCount = pointList.Count;
                        var locations = new float[pointCount];
                        var derivatives = new float[pointCount];
                        var values = new RsSampler[pointCount];

                        for (var i = 0; i < pointCount; i++)
                        {
                            locations[i] = pointList[i].location;
                            derivatives[i] = pointList[i].derivative;
                            values[i] = ParseJTokenToSampler(pointList[i].value, startPos);
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

        private RsSampler ParseJTokenToSampler(JToken token, Vector3Int startPos)
        {
            if (token.Type == JTokenType.Object)
            {
                return token.ToObject<RsSamplerConfig>().BuildRsSampler(startPos);
            }
            
            if (token.Type == JTokenType.String)
            {
                var samplerName = token.Value<string>();
                return NoiseManager.Instance.GetOrCreateCacheSampler(samplerName, startPos);
                //
                // var config = RsConfigManager.Instance.GetSamplerConfig(token.ToObject<string>());
                // return config.BuildRsSampler();
            }
            
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            {
                var value = token.ToObject<float>();
                return new ConstantSampler(value);
            }
            
            Debug.LogError($"[Config] Unknown token type: {token.Type}");
            return null;
        }
    }
}