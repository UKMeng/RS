using System;
using UnityEngine;
using RS.Utils;

namespace RS.Scene.Sampler
{
    /// <summary>
    /// 必须从噪声进行采样
    /// 采样在(x/4, 0, z/4)处采样值乘上4
    /// </summary>
    public class ShiftXZSampler : RsSampler
    {
        public ShiftXZSampler(Int64 seed, float[] amplitudes, int firstOctave) 
            : base(seed, amplitudes, firstOctave)
        {
        }

        public override float Sample(Vector3 pos)
        {
            var shiftPos = new Vector3(pos.x * 0.25f, 0, pos.z * 0.25f);
            return m_noise.SampleFbm3D(shiftPos) * 4.0f;
        }
    }
}