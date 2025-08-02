using System;
using System.Collections.Generic;
using System.Linq;
using RS.Scene.Biome;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RS.Utils
{
    public class RsBiomeConfig : RsConfig
    {
        public string type;
        public string configName;
        public int[] continentalness;
        public int[] erosion;
        public int[] humidity;
        public int[] temperature;
        public int[] pv;
        public int[] ridges;
        public int[] depth;
        public float offset;

        public RsBiomeConfig(RsBiomeConfig other)
        {
            type = other.type;
            configName = other.configName;
            continentalness = other.continentalness;
            erosion = other.erosion;
            humidity = other.humidity;
            temperature = other.temperature;
            pv = other.pv;
            ridges = other.ridges;
            depth = other.depth;
            offset = other.offset;
        }
        
        public RsBiomeConfig(JObject biomeToken)
        {
            type = biomeToken["type"].ToString();
            var args = biomeToken["arguments"].ToObject<JObject>();

            if (args.TryGetValue("name", out var nameToken))
            {
                configName = nameToken.ToString();
            }

            if (args.TryGetValue("continentalness", out var contiToken))
            {
                continentalness = GetInterval(contiToken);
            }

            if (args.TryGetValue("depth", out var depthToken))
            {
                depth = GetInterval(depthToken);
            }

            if (args.TryGetValue("erosion", out var erosionToken))
            {
                erosion = GetInterval(erosionToken);
            }

            if (args.TryGetValue("humidity", out var humidityToken))
            {
                humidity = GetInterval(humidityToken);
            }
            
            if (args.TryGetValue("temperature", out var temperatureToken))
            {
                temperature = GetInterval(temperatureToken);
            }

            if (args.TryGetValue("pv", out var pvToken))
            {
                pv = GetInterval(pvToken);
            }

            if (args.TryGetValue("ridges", out var ridgesToken))
            {
                ridges = GetInterval(ridgesToken);
            }
        }

        public BiomeType Type
        {
            get { return (BiomeType)Enum.Parse(typeof(BiomeType), type); }
        }

        private int[] GetInterval(JToken intervalToken)
        {
            if (intervalToken.Type == JTokenType.Integer)
            {
                return new int[] {intervalToken.ToObject<int>()};
            }
            
            if (intervalToken.Type == JTokenType.Array)
            {
                return intervalToken.ToObject<int[]>();
            }

            return new int[] { };
        }
    }
    
    public class RsBiomeSourceConfig : RsConfig
    {
        public List<RsBiomeConfig> biomes;

        public RsBiomeSourceConfig(JObject biomesToken)
        {
            biomes = new List<RsBiomeConfig>();
            var biomesArray = biomesToken["biomes"].ToObject<JArray>();
            foreach (var biomeToken in biomesArray)
            {
                biomes.Add(new RsBiomeConfig(biomeToken as JObject));
            }
        }
        
        public static List<RsBiomeConfig> ParseSourceConfig(RsBiomeSourceConfig config)
        {
            var result = new List<RsBiomeConfig>();

            foreach (var biome in config.biomes)
            {
                if (biome.type == "Derive")
                {
                    var deriveConfig = RsConfigManager.Instance.GetBiomeSource(biome.configName);
                    var deriveBiomes = ParseSourceConfig(deriveConfig);
                    foreach (var tempBiome in deriveBiomes)
                    {
                        var deriveBiome = new RsBiomeConfig(tempBiome);

                        if (deriveBiome.continentalness[0] == -1)
                        {
                            deriveBiome.continentalness = biome.continentalness;
                        }
                        else if (biome.continentalness[0] != -2)
                        {
                            if (Intersect(deriveBiome.continentalness, biome.continentalness, out var intersection))
                            {
                                deriveBiome.continentalness = intersection;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (deriveBiome.erosion[0] == -1)
                        {
                            deriveBiome.erosion = biome.erosion;
                        }
                        else if (biome.erosion[0] != -2)
                        {
                            if (Intersect(deriveBiome.erosion, biome.erosion, out var intersection))
                            {
                                deriveBiome.erosion = intersection;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (deriveBiome.humidity[0] == -1)
                        {
                            deriveBiome.humidity = biome.humidity;
                        }
                        else if (biome.humidity[0] != -2)
                        {
                            if (Intersect(deriveBiome.humidity, biome.humidity, out var intersection))
                            {
                                deriveBiome.humidity = intersection;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (deriveBiome.temperature[0] == -1)
                        {
                            deriveBiome.temperature = biome.temperature;
                        }
                        else if (biome.temperature[0] != -2)
                        {
                            if (Intersect(deriveBiome.temperature, biome.temperature, out var intersection))
                            {
                                deriveBiome.temperature = intersection;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (deriveBiome.pv[0] == -1)
                        {
                            deriveBiome.pv = biome.pv;
                        }
                        else if (biome.pv[0] != -2)
                        {
                            if (Intersect(deriveBiome.pv, biome.pv, out var intersection))
                            {
                                deriveBiome.pv = intersection;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (deriveBiome.ridges[0] == -1)
                        {
                            deriveBiome.ridges = biome.ridges;
                        }
                        else if (biome.ridges[0] != -2)
                        {
                            if (Intersect(deriveBiome.ridges, biome.ridges, out var intersection))
                            {
                                deriveBiome.ridges = intersection;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        
                        result.Add(deriveBiome);
                    }
                }
                else
                {
                    result.Add(biome);
                }
            }

            return result;
        }

        private static bool Intersect(int[] derive, int[] origin, out int[] intersection)
        {
            var res = new List<int>();

            for (var i = 0; i < origin.Length; i++)
            {
                if (derive.Contains(origin[i]))
                {
                    res.Add(origin[i]);
                }
            }
            
            intersection = res.ToArray();
            return res.Count != 0;
        }
    }
}