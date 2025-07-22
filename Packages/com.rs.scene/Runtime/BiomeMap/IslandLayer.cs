using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

using RS.Utils;

namespace RS.Scene.BiomeMap
{
    public class IslandLayer : Layer
    {
        public IslandLayer(int width, int height, int blockSize, RsRandom rng) : base(width, height, blockSize)
        {
            // 以1:9的比例生成岛屿
            for (var x = 0; x < m_width; x++)
            {
                for (var y = 0; y < m_height; y++)
                {
                    if (rng.NextInt(0, 10) == 0)
                    {
                        m_data[x, y] = BiomeType.Land;
                    }
                    else
                    {
                        m_data[x, y] = BiomeType.Ocean;
                    }
                }
            }
        }

        public IslandLayer(Layer parent) : base(parent)
        {
            
        }

        /// <summary>
        /// 随机元胞自动机增加岛屿丰富性
        /// </summary>
        /// <param name="rng"></param>
        public void AddIsland(RsRandom rng)
        {
            // 元胞自动机规则
            // 周围不同的类型越多，自己变过去的概率也越大
            // 但是ocean变land的基础概率比land变ocean的概率要大，从而达到增加land的目的
            
            // TODO: 是否所有的变化需要基于初始状态？
            for (var x = 0; x < m_width; x++)
            {
                for (var y = 0; y < m_height; y++)
                {
                    // 忽略边界
                    if (x == 0 || x == m_width - 1 || y == 0 || y == m_height - 1)
                    {
                        continue;
                    }

                    var landCount = GetNeighborCount(x, y, out var mostCommon);
                    
                    if (m_data[x, y] == BiomeType.Ocean)
                    {
                        if (rng.NextFloat() < landCount / 12.0f)
                        {
                            m_data[x, y] = mostCommon;
                        }
                    }
                    else
                    {
                        if (rng.NextFloat() < (8 - landCount) / 24.0f)
                        {
                            m_data[x, y] = BiomeType.Ocean;
                        }
                    }
                }
            }
            
        }

        /// <summary>
        /// 获取周围8格陆地数量，默认没有边界坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="mostCommon">out一个邻居中最常见的Land类型</param>
        /// <returns></returns>
        private int GetNeighborCount(int x, int y, out BiomeType mostCommon)
        {
            int count = 0;
            
            var neighborTypeCount = new Dictionary<BiomeType, int>();
            
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }

                    var biomeType = m_data[x + i, y + j];
                    
                    if (biomeType != BiomeType.Ocean)
                    {
                        if (neighborTypeCount.ContainsKey(biomeType))
                        {
                            neighborTypeCount[biomeType]++;
                        }
                        else
                        {
                            neighborTypeCount.Add(biomeType, 1);
                        }
                        count++;
                    }
                }
            }

            var maxCount = 0;
            mostCommon = BiomeType.Ocean;
            foreach (var kv in neighborTypeCount)
            {
                if (kv.Value > maxCount)
                {
                    mostCommon = kv.Key;
                    maxCount = kv.Value;
                }
            }

            return count;
        }
    }
}