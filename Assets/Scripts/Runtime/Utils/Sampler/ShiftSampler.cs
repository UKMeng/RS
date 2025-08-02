using System;
using UnityEngine;
using RS.Utils;

namespace RS.Utils
{
    /// <summary>
    /// 必须从噪声进行采样
    /// 采样在(x/4, 0, z/4)处采样值乘上4
    /// </summary>
    public class ShiftASampler : RsSampler
    {
        public ShiftASampler(RsNoise noise) 
            : base(noise)
        {
        }

        public override float Sample(Vector3 pos)
        {
            var shiftPos = new Vector3(pos.x * 0.25f, 0, pos.z * 0.25f);
            return m_noise.SampleFbm3D(shiftPos) * 4.0f;
        }
    }
    
    /// <summary>
    /// 必须从噪声进行采样
    /// 采样在(z/4, x/4, 0)处采样值乘上4
    /// </summary>
    public class ShiftBSampler : RsSampler
    {
        public ShiftBSampler(RsNoise noise) 
            : base(noise)
        {
        }

        public override float Sample(Vector3 pos)
        {
            var shiftPos = new Vector3(pos.z * 0.25f, pos.x * 0.25f, 0);
            return m_noise.SampleFbm3D(shiftPos) * 4.0f;
        }
    }

    /// <summary>
    /// 必须给定一个Noise, 以及三个Sampler(作为坐标三个轴的偏移)
    /// </summary>
    public class ShiftedNoiseSampler : RsSampler
    {
        private RsSampler m_samplerX;
        private RsSampler m_samplerY;
        private RsSampler m_samplerZ;

        private float m_xzScale;
        private float m_yScale;

        public ShiftedNoiseSampler(RsNoise noise, RsSampler samplerX, RsSampler samplerY, RsSampler samplerZ, float xzScale, float yScale)
            : base(noise)
        {
            m_samplerX = samplerX;
            m_samplerY = samplerY;
            m_samplerZ = samplerZ;
            m_xzScale = xzScale;
            m_yScale = yScale;
        }

        public override float Sample(Vector3 pos)
        {
            var offsetX = m_samplerX.Sample(pos);
            var offsetY = m_samplerY.Sample(pos);
            var offsetZ = m_samplerZ.Sample(pos);

            return m_noise.SampleFbm3D(
                new Vector3(pos.x * m_xzScale + offsetX , 
                                        pos.y * m_yScale + offsetY,
                                        pos.z * m_xzScale + offsetZ));
        }
    }
}