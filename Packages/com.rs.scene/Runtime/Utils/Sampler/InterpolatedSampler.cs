using UnityEngine;

namespace RS.Utils
{
    public class InterpolatedSampler : RsSampler
    {
        private RsSampler m_sampler;
        private const int m_cellWidth = 4;
        private const int m_cellHeight = 4;

        public InterpolatedSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        /// <summary>
        /// 4x4*4的区域仅采样一次, 需通过插值来获取更细致的曲线
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public override float Sample(Vector3 pos)
        {
            var w = m_cellWidth;
            var h = m_cellHeight;
            var x = ((pos.x % w + w) % w) / w;

            return 1.0f;
        }
        
    }
}