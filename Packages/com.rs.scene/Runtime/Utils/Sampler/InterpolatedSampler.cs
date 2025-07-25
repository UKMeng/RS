using System;
using UnityEngine;

namespace RS.Utils
{
    public class InterpolatedSampler : RsSampler
    {
        private RsSampler m_sampler;
        private const float m_cellWidth = 4.0f;
        private const float m_cellHeight = 4.0f;

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
            var tx = ((pos.x % w + w) % w) / w;
            var ty = ((pos.y % h + h) % h) / h;
            var tz = ((pos.z % w + w) % w) / w;

            var fx = Mathf.Floor(pos.x / w) * w;
            var fy = Mathf.Floor(pos.y / h) * h;
            var fz = Mathf.Floor(pos.z / w) * w;

            var c000 = m_sampler.Sample(new Vector3(fx, fy, fz));
            var c100 = m_sampler.Sample(new Vector3(fx + w, fy, fz));
            var c010 = m_sampler.Sample(new Vector3(fx, fy + h, fz));
            var c110 = m_sampler.Sample(new Vector3(fx + w, fy + h, fz));
            var c001 = m_sampler.Sample(new Vector3(fx, fy, fz + w));
            var c101 = m_sampler.Sample(new Vector3(fx + w, fy, fz + w));
            var c011 = m_sampler.Sample(new Vector3(fx, fy + h, fz + w));
            var c111 = m_sampler.Sample(new Vector3(fx + w, fy + h, fz + w));
            
            // 三线性插值
            return RsMath.TriLerp(tx, ty, tz, c000, c100, c010, c110, c001, c101, c011, c111);
        }
        
    }
}