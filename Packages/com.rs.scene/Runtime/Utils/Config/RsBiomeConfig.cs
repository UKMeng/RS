using System;
using System.Collections.Generic;
using RS.Scene.Biome;
using Unity.Plastic.Newtonsoft.Json.Linq;
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

                        if (deriveBiome.erosion[0] == -1)
                        {
                            deriveBiome.erosion = biome.erosion;
                        }

                        if (deriveBiome.humidity[0] == -1)
                        {
                            deriveBiome.humidity = biome.humidity;
                        }

                        if (deriveBiome.temperature[0] == -1)
                        {
                            deriveBiome.temperature = biome.temperature;
                        }

                        if (deriveBiome.pv[0] == -1)
                        {
                            deriveBiome.pv = biome.pv;
                        }

                        if (deriveBiome.ridges[0] == -1)
                        {
                            deriveBiome.ridges = biome.ridges;
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
    }
}