using UnityEngine;

namespace RS.Utils
{
    public class NoiseSampler : RsSampler
    {
        private float m_xzScale;
        private float m_yScale;

        public NoiseSampler(RsNoise noise, float xzScale, float yScale)
            : base(noise)
        {
            m_xzScale = xzScale;
            m_yScale = yScale;
        }

        public override float Sample(Vector3 pos)
        {
            return base.Sample(new Vector3(pos.x * m_xzScale, pos.y * m_yScale, pos.z * m_xzScale));
        }
    }
    
    // public class BlendedNoiseSampler : RsSampler
    // {
    //     private float m_xzScale;
    //     private float m_yScale;
    //     private float m_xzFactor;
    //     private float m_yFactor;
    //     private float m_xzMultipler;.
    //     private float m_yMultipler;
    //     
    //     private RsNoise m_minLimitNoise;
    //     private RsNoise m_maxLimitNoise;
    //     private RsNoise m_mainNoise;
    //
    //     public BlendedNoiseSampler(RsNoise mainNoise, RsNoise minLimitNoise, RsNoise maxLimitNoise, float xzScale,
    //         float yScale, float xzFactor, float yFactor)
    //     {
    //         m_mainNoise = mainNoise;
    //         m_minLimitNoise = minLimitNoise;
    //         m_maxLimitNoise = maxLimitNoise;
    //         m_xzScale = xzScale;
    //         m_yScale = yScale;
    //         m_xzFactor = xzFactor;
    //         m_yFactor = yFactor;
    //     }
    //
    //     public override float Sample(Vector3 pos)
    //     {
    //         
    //     }
    // }
}