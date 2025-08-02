using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

        public float[,,] SampleBatch(Vector3 startPos)
        {
            var w = m_cellWidth;
            var h = m_cellHeight;

            // 对所有间隔点先采样, 需各维度多一个间隔
            var cache = new float[9, 9, 9];

            for (var ix = 0; ix < 9; ix++)
            {
                for (var iz = 0; iz < 9; iz++)
                {
                    for (var iy = 0; iy < 9; iy++)
                    {
                        var fx = startPos.x + ix * w;
                        var fy = startPos.y + iy * h;
                        var fz = startPos.z + iz * w;
                        cache[ix, iy, iz] = m_sampler.Sample(new Vector3(fx, fy, fz));
                    }
                }
            }
            
            // 对中间点进行插值
            var result = new float[32, 32, 32];
            for (var ix = 0; ix < 32; ix++)
            {
                for (var iz = 0; iz < 32; iz++)
                {
                    for (var iy = 0; iy < 32; iy++)
                    {
                        var tx = (ix % w) / w;
                        var ty = (iy % h) / h;
                        var tz = (iz % w) / w;

                        var fx = Mathf.FloorToInt(ix / w);
                        var fy = Mathf.FloorToInt(iy / h);
                        var fz = Mathf.FloorToInt(iz / w);
                        
                        var c000 = cache[fx, fy, fz];
                        var c100 = cache[fx + 1, fy, fz];
                        var c010 = cache[fx, fy + 1, fz];
                        var c110 = cache[fx + 1, fy + 1, fz];
                        var c001 = cache[fx, fy, fz + 1];
                        var c101 = cache[fx + 1, fy, fz + 1];
                        var c011 = cache[fx, fy + 1, fz + 1];
                        var c111 = cache[fx + 1, fy + 1, fz + 1];
                        
                        // 三线性插值
                        result[ix, iy, iz] = RsMath.TriLerp(tx, ty, tz, c000, c100, c010, c110, c001, c101, c011, c111);
                    }
                }
            }

            return result;
        }
        
    }
}