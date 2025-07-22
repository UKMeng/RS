using System;
using UnityEngine;
using RS.Utils;

namespace RS.Scene.Sampler
{
    public class RsSampler
    {
        protected RsRandom m_rng;
        protected RsNoise m_noise;

        public RsSampler(Int64 seed, float[] amplitudes, int firstOctave)
        {
            m_rng = new RsRandom(seed);
            m_noise = new RsNoise(m_rng.NextUInt64(), amplitudes, firstOctave);
        }

        protected RsSampler()
        {
        }

        public virtual float Sample(Vector3 pos)
        {
            return m_noise.SampleFbm3D(pos);
        }
        
        
        // public float ShiftNoise(Vector3 samplePosition, float scaleXZ, float scaleY, int firstOctave, float[] amplitudes)
        // {
        //     var scaledPosition = new Vector3(samplePosition.x * scaleXZ, samplePosition.y * scaleY, samplePosition.z  * scaleXZ);
        //     return SampleFbm3D(scaledPosition, firstOctave, amplitudes);
        // }
    }
}