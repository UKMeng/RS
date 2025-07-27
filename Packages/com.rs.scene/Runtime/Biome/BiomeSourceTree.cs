using System.Collections.Generic;
using System.Dynamic;
using Unity.Plastic.Newtonsoft.Json.Linq;

namespace RS.Scene.Biome
{
    public class BiomeInterval
    {
        public float min;
        public float max;

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
                case "biome":
                {
                    return m_biome;
                }
            }

            var value = values[valueIndex];
            var index = GetIndex(value);
            return m_children[index].GetBiomeType(values);
        }
    }
    
    
    
    public class BiomeSourceTree
    {
        private BiomeSourceNode m_root;

        public BiomeSourceTree()
        {
            // 先做一个硬编码的区间树
            m_root = new BiomeSourceNode("biome", BiomeType.Inland);
        }

        public BiomeType GetBiomeType(float[] values)
        {
            return m_root.GetBiomeType(values);
        }
    }
}