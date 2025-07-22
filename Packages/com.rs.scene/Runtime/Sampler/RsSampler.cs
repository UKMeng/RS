using System;
using UnityEngine;
using RS.Utils;

namespace RS.Scene.Sampler
{
    public class RsSampler
    {
        private RsRandom m_rng;
        private RsNoise m_mainNoise;

        public RsSampler(Int64 seed)
        {
            m_rng = new RsRandom(seed);
            m_mainNoise = new RsNoise(m_rng.NextUInt64());
        }
        
        public float Sample(Vector3 pos)
        {
            // TODO: Use Config
            
            // Temperature
            // var amplitudes = new float[] { 1.5f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f };
            // var firstOctave = -10;
            
            // Continentalness
            // var amplitudes = new float[] { 1.0f, 1.0f, 2.0f, 2.0f, 2.0f, 1.0f, 1.0f, 1.0f, 1.0f };
            // var firstOctave = -9;
            
            // offset
            
            // ridges
            var amplitudes = new float[] { 1.0f, 2.0f, 1.0f, 0.0f, 0.0f, 0.0f };
            var firstOctave = -7;
            // return RsNoise.SampleFbm3D(pos, firstOctave, amplitudes, m_noise);

            return m_mainNoise.ShiftNoise(pos, 0.25f, 0.0f, firstOctave, amplitudes);
        }
    }
}