using System;
using UnityEngine;
using RS.Utils;

namespace RS.Scene.Sampler
{
    public class RsSampler
    {
        private RsNoise m_noise;

        public RsSampler(Int64 seed)
        {
            m_noise = new RsNoise(seed);
        }
        
        public float Sample(Vector3 pos)
        {
            var amplitudes = new float[] { 1.5f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f };
            var firstOctave = -10;
            return RsNoise.SampleFbm3D(pos, firstOctave, amplitudes, m_noise);
        }
    }
}