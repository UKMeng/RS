using System;
using System.Collections.Generic;
using RS.Scene.Biome;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace RS.Utils
{
    public class RsBiomeInterval
    {
        public float min;
        public float max;

        public RsBiomeInterval(JToken intervalToken)
        {
            if (intervalToken.Type == JTokenType.Float || intervalToken.Type == JTokenType.Integer)
            {
                min = max = intervalToken.ToObject<float>();
            }
            else if (intervalToken.Type == JTokenType.Array)
            {
                var intervalArray = intervalToken.ToObject<float[]>();
                min = intervalArray[0];
                max = intervalArray[1];
            }
        }

        public float GetDistance(float val)
        {
            if (val < min)
            {
                return min - val;
            }

            if (val > max)
            {
                return val - max;
            }
            
            return 0.0f;
        }
    }

    public class RsBiomeConfig : RsConfig
    {
        public BiomeType type;
        private RsBiomeInterval[] m_intervals;
        private float m_offset;

        public RsBiomeConfig(JObject biomeToken)
        {
            var typeStr = biomeToken["type"].ToString();
            type = (BiomeType)Enum.Parse(typeof(BiomeType), typeStr, true);

            m_intervals = new RsBiomeInterval[6];
            
            var args = biomeToken["arguments"].ToObject<JObject>();

            if (args.TryGetValue("continentalness", out var contiToken))
            {
                m_intervals[0] = new RsBiomeInterval(contiToken);
            }

            if (args.TryGetValue("depth", out var depthToken))
            {
                m_intervals[1] = new RsBiomeInterval(depthToken);
            }

            if (args.TryGetValue("erosion", out var erosionToken))
            {
                m_intervals[2] = new RsBiomeInterval(erosionToken);
            }

            if (args.TryGetValue("humidity", out var humidityToken))
            {
                m_intervals[3] = new RsBiomeInterval(humidityToken);
            }
            
            if (args.TryGetValue("temperature", out var temperatureToken))
            {
                m_intervals[4] = new RsBiomeInterval(temperatureToken);
            }

            if (args.TryGetValue("ridges", out var ridgesToken))
            {
                m_intervals[5] = new RsBiomeInterval(ridgesToken);
            }

            if (args.TryGetValue("offset", out var offsetToken))
            {
                var offset = offsetToken.ToObject<float>();
                m_offset = offset * offset;
            }

            
        }

        public float GetDistance(float[] values)
        {
            var result = m_offset;
            for (var i = 0; i < 6; i++)
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