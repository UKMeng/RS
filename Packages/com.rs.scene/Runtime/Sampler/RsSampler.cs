using System;
using UnityEngine;
using RS.Utils;

namespace RS.Scene.Sampler
{
    public class RsSampler
    {
        private RsRandom m_rng;

        public RsSampler(Int64 seed)
        {
            m_rng = new RsRandom(seed);
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
            var amplitudes = new float[] { 1.0f, 1.0f, 1.0f, 0.0f };
            var firstOctave = -3;
            var noise = new RsNoise(m_rng.NextUInt64(), amplitudes, firstOctave);
            
            // ridges
            // var amplitudes = new float[] { 1.0f, 2.0f, 1.0f, 0.0f, 0.0f, 0.0f };
            // var firstOctave = -7;
            return noise.SampleFbm3D(pos);

            // return m_mainNoise.ShiftNoise(pos, 0.25f, 0.0f, firstOctave, amplitudes);
        }

        /// <summary>
        /// 在(x/4, 0, z/4)处采样后乘上4
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        // public float ShiftXZ(Vector3 pos, RsNoise noise)
        // {
        //     // offset
        //     var amplitudes = new float[] { 1.0f, 1.0f, 1.0f, 0.0f };
        //     var firstOctave = -3;
        // }
        
        // public float ShiftNoise(Vector3 samplePosition, float scaleXZ, float scaleY, int firstOctave, float[] amplitudes)
        // {
        //     var scaledPosition = new Vector3(samplePosition.x * scaleXZ, samplePosition.y * scaleY, samplePosition.z  * scaleXZ);
        //     return SampleFbm3D(scaledPosition, firstOctave, amplitudes);
        // }
    }
}