using System.Collections.Generic;
using System.Dynamic;
using RS.Utils;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace RS.Scene.Biome
{
    public class BiomeInterval
    {
        public float min;
        public float max;

        public BiomeInterval(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
        
        public BiomeInterval(float val)
        {
            min = max = val;
        }
        
        public BiomeInterval(JToken intervalToken)
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

    
    public class BiomeSourceNode
    {
        private string m_type;
        private List<BiomeSourceNode> m_children;
        private List<BiomeInterval> m_intervals;
        private BiomeType m_biome;

        public BiomeSourceNode(string type, BiomeType biome)
        {
            m_type = type;
            m_biome = biome;
        }

        public BiomeSourceNode(string type, List<BiomeInterval> intervals, List<BiomeSourceNode> children)
        {
            m_type = type;
            m_children = children;
            m_intervals = intervals;
        }
        
        public BiomeSourceNode(string type, List<BiomeInterval> intervals)
        {
            m_type = type;
            m_children = new List<BiomeSourceNode>(intervals.Count);
            for (var i = 0; i < intervals.Count; i++)
            {
                m_children.Add(new BiomeSourceNode("biome", BiomeType.Unknown));
            }
            m_intervals = intervals;
        }
        
        
        private int GetIndex(float val)
        {
            // 二分查找index
            var left = 0;
            var right = m_intervals.Count - 1;

            while (left <= right)
            {
                var mid = left + ((right - left) >> 1);
                var interval = m_intervals[mid];

                if (val < interval.min)
                {
                    right = mid - 1;
                }
                else if (val > interval.max)
                {
                    left = mid + 1;
                }
                else
                {
                    return mid;
                }
            }

            return -1;
        }
        
        public BiomeType GetBiomeType(float[] values)
        {
            var valueIndex = 0;
            
            switch (m_type)
            {
                case "continentalness":
                {
                    valueIndex = 0;
                    break;
                }
                case "erosion":
                {
                    valueIndex = 2;
                    break;
                }
                case "humidity":
                {
                    valueIndex = 3;
                    break;
                }
                case "temperature":
                {
                    valueIndex = 4;
                    break;
                }
                case "ridges":
                {
                    valueIndex = 5;
                    break;
                }
                case "peak & valley":
                {
                    valueIndex = 6;
                    break;
                }
                case "biome":
                {
                    return m_biome;
                }
            }

            var value = values[valueIndex];
            var index = GetIndex(value);
            return m_children[index].GetBiomeType(values);
        }

        public void AddBiome(RsBiomeConfig biome)
        {
            int[] intervals = new int[] {};
            switch (m_type)
            {
                case "continentalness":
                {
                    intervals = biome.continentalness;
                    break;
                }
                case "peak & valley":
                {
                    intervals = biome.pv;
                    break;
                }
                case "erosion":
                {
                    intervals = biome.erosion;
                    break;
                }
                case "temperature":
                {
                    intervals = biome.temperature;
                    break;
                }
                case "humidity":
                {
                    intervals = biome.humidity;
                    break;
                }
                case "ridges":
                {
                    intervals = biome.ridges;
                    break;
                }
                case "biome":
                {
                    m_biome = biome.Type;
                    return;
                }
            }

            foreach (var index in intervals)
            {
                m_children[index].AddBiome(biome);
            }
        }

        public void AddIntervals(string type, List<BiomeInterval> intervals, int level)
        {
            if (level == 0)
            {
                for (var i = 0; i < m_children.Count; i++)
                {
                    m_children[i] = new BiomeSourceNode(type, intervals);
                }
            }
            else
            {
                for (var i = 0; i < m_children.Count; i++)
                {
                    m_children[i].AddIntervals(type, intervals, level - 1);
                }
            }
        }
    }
    
    public class BiomeSourceTree
    {
        private BiomeSourceNode m_root;

        public BiomeSourceTree()
        {
            InitRoot();
        }

        public BiomeSourceTree(RsBiomeSourceConfig sourceConfig)
        {
            InitRoot();

            var biomes = RsBiomeSourceConfig.ParseSourceConfig(sourceConfig);
            
            foreach (var biome in biomes)
            {
                m_root.AddBiome(biome);
            }
        }

        private void InitRoot()
        {
            // 先硬编码区间
            var contiIntervals = new List<BiomeInterval>()
            {
                new BiomeInterval(-1.0f, -0.19f),
                new BiomeInterval(-0.19f, -0.11f),
                new BiomeInterval(-0.11f, 0.03f),
                new BiomeInterval(0.03f, 1.0f),
            };

            var pvIntervals = new List<BiomeInterval>()
            {
                new BiomeInterval(-1.0f, -0.85f),
                new BiomeInterval(-0.85f, -0.2f),
                new BiomeInterval(-0.2f, 0.2f),
                new BiomeInterval(0.2f, 0.7f),
                new BiomeInterval(0.7f, 1.0f)
            };

            var erosionIntervals = new List<BiomeInterval>()
            {
                new BiomeInterval(-1.0f, -0.78f),
                new BiomeInterval(-0.78f, -0.375f),
                new BiomeInterval(-0.375f, -0.2225f),
                new BiomeInterval(-0.2225f, 0.05f),
                new BiomeInterval(0.05f, 0.45f),
                new BiomeInterval(0.45f, 0.55f),
                new BiomeInterval(0.55f, 1.0f)
            };

            var tempIntervals = new List<BiomeInterval>()
            {
                new BiomeInterval(-1.0f, -0.45f),
                new BiomeInterval(-0.45f, -0.15f),
                new BiomeInterval(-0.15f, 0.2f),
                new BiomeInterval(0.2f, 0.55f),
                new BiomeInterval(0.55f, 1.0f),
            };

            var humidityIntervals = new List<BiomeInterval>()
            {
                new BiomeInterval(-1.0f, -0.35f),
                new BiomeInterval(-0.35f, -0.1f),
                new BiomeInterval(-0.1f, 0.1f),
                new BiomeInterval(0.1f, 0.3f),
                new BiomeInterval(0.3f, 1.0f),
            };

            var ridgesIntervals = new List<BiomeInterval>()
            {
                new BiomeInterval(-1.0f, 0.0f),
                new BiomeInterval(0.0f, 1.0f)
            };
            
            m_root = new BiomeSourceNode("continentalness", contiIntervals);
            m_root.AddIntervals("peak & valley", pvIntervals, 0);
            m_root.AddIntervals("erosion", erosionIntervals, 1);
            m_root.AddIntervals("temperature", tempIntervals, 2);
            m_root.AddIntervals("humidity", humidityIntervals, 3);
            m_root.AddIntervals("ridges", ridgesIntervals, 4);
        }
        

        public BiomeType GetBiomeType(float[] values)
        {
            return m_root.GetBiomeType(values);
        }
    }
}