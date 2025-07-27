using System;
using System.Collections.Generic;
using RS.Scene.Biome;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;

namespace RS.Utils
{
    public class RsBiomeConfig : RsConfig
    {
        public BiomeType type;
        private BiomeInterval[] m_intervals;
        private float m_offset;

        public RsBiomeConfig(JObject biomeToken)
        {
            var typeStr = biomeToken["type"].ToString();
            type = (BiomeType)Enum.Parse(typeof(BiomeType), typeStr);

            m_intervals = new BiomeInterval[7];
            
            var args = biomeToken["arguments"].ToObject<JObject>();

            if (args.TryGetValue("continentalness", out var contiToken))
            {
                m_intervals[0] = new BiomeInterval(contiToken);
            }

            if (args.TryGetValue("depth", out var depthToken))
            {
                m_intervals[1] = new BiomeInterval(depthToken);
            }

            if (args.TryGetValue("erosion", out var erosionToken))
            {
                m_intervals[2] = new BiomeInterval(erosionToken);
            }

            if (args.TryGetValue("humidity", out var humidityToken))
            {
                m_intervals[3] = new BiomeInterval(humidityToken);
            }
            
            if (args.TryGetValue("temperature", out var temperatureToken))
            {
                m_intervals[4] = new BiomeInterval(temperatureToken);
            }

            if (args.TryGetValue("ridges", out var ridgesToken))
            {
                m_intervals[5] = new BiomeInterval(ridgesToken);
            }
            
            // peak & valley
            m_intervals[6] = new BiomeInterval(RsMath.RidgesFolded(m_intervals[5].min));

            if (args.TryGetValue("offset", out var offsetToken))
            {
                var offset = offsetToken.ToObject<float>();
                m_offset = offset * offset;
            }

            
        }

        public float GetDistance(float[] values)
        {
            var result = m_offset;
            for (var i = 0; i < 7; i++)
            {
                // 跳过深度采样
                if (i == 1)
                {
                    continue;
                }
                
                var v = values[i];
                var interval = m_intervals[i];
                var dis = interval.GetDistance(v);
                result += dis * dis;
            }
            return result;
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
    }
}