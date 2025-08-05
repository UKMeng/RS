using System;
using UnityEngine;

namespace RS.Utils
{
    public class SplineSampler : RsSampler
    {
        private RsSampler m_coordinate;
        private float[] m_locations;
        private float[] m_derivatives;
        private RsSampler[] m_values;
        
        public override void Dispose()
        {
            if (!m_coordinate.BuildFromConfig)
            {
                m_coordinate.Dispose();
            }

            foreach (var value in m_values)
            {
                if (!value.BuildFromConfig)
                {
                    value.Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="locations">要求位置从小到大排列</param>
        /// <param name="derivatives"></param>
        /// <param name="values"></param>
        public SplineSampler(RsSampler coordinate, float[] locations, float[] derivatives, RsSampler[] values)
        {
            m_coordinate = coordinate;
            m_locations = locations;
            m_derivatives = derivatives;
            m_values = values;
        }
        
        public override float Sample(Vector3 pos)
        {
            var x = m_coordinate.Sample(pos);
            var pointCount = m_values.Length;
            
            // 二分查找最接近的位置
            var xPos = Array.BinarySearch(m_locations, x);
            xPos = xPos < 0 ? ~xPos - 1 : xPos;
            
            // 三次Hermite插值多项式
            // p(x) = H₀(x) * f(x₀) + H₁(x) * f(x₁) + M₀(x) * f'(x₀) + M₁(x) * f'(x₁)
            // H₀(x) = (1 + 2(x - x₀) / h) * ((x - x₁) / h)²
            // H₁(x) = (1 - 2(x - x₁) / h) * ((x - x₀) / h)²
            // M₀(x) = (x - x₀) * ((x - x₁) / h)²
            // M₁(x) = (x - x₁) * ((x - x₀) / h)²
            // 其中h = x₁ - x₀
            if (xPos < 0)
            {
                return m_values[0].Sample(pos) + m_derivatives[0] * (x - m_locations[0]);
            }
            
            if (xPos >= m_values.Length - 1)
            {
                return m_values[pointCount - 1].Sample(pos) + m_derivatives[pointCount - 1] * (x - m_locations[pointCount - 1]);
            }

            var x0 = m_locations[xPos];
            var x1 = m_locations[xPos + 1];
            var y0 = m_values[xPos].Sample(pos);
            var y1 = m_values[xPos + 1].Sample(pos);
            var dy0 = m_derivatives[xPos];
            var dy1 = m_derivatives[xPos + 1];
            
            var h = x1 - x0;
            var h0 = (1 + 2 * (x - x0) / h) * ((x - x1) / h) * ((x - x1) / h);
            var h1 = (1 - 2 * (x - x1) / h) * ((x - x0) / h) * ((x - x0) / h);
            var m0 = (x - x0) * ((x - x1) / h) * ((x - x1) / h);
            var m1 = (x - x1) * ((x - x0) / h) * ((x - x0) / h);
            
            return y0 * h0 + y1 * h1 + dy0 * m0 + dy1 * m1;
        }
    }
}