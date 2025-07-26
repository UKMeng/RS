using System.Collections.Generic;
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
    }

    public class RsBiomeConfig : RsConfig
    {
        public string type;
        public RsBiomeInterval continentalness;
        public RsBiomeInterval depth;
        public RsBiomeInterval erosion;
        public RsBiomeInterval humidity;
        public RsBiomeInterval offset;
        public RsBiomeInterval temperature;
        public RsBiomeInterval ridges;

        public RsBiomeConfig(JObject biomeToken)
        {
            type = biomeToken["type"].ToString();
            var args = biomeToken["arguments"].ToObject<JObject>();

            if (args.TryGetValue("continentalness", out var contiToken))
            {
                continentalness = new RsBiomeInterval(contiToken);
            }

            if (args.TryGetValue("depth", out var depthToken))
            {
                depth = new RsBiomeInterval(depthToken);
            }

            if (args.TryGetValue("erosion", out var erosionToken))
            {
                erosion = new RsBiomeInterval(erosionToken);
            }

            if (args.TryGetValue("humidity", out var humidityToken))
            {
                humidity = new RsBiomeInterval(humidityToken);
            }

            if (args.TryGetValue("offset", out var offsetToken))
            {
                offset = new RsBiomeInterval(offsetToken);
            }

            if (args.TryGetValue("temperature", out var temperatureToken))
            {
                temperature = new RsBiomeInterval(temperatureToken);
            }

            if (args.TryGetValue("ridges", out var ridgesToken))
            {
                ridges = new RsBiomeInterval(ridgesToken);
            }
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