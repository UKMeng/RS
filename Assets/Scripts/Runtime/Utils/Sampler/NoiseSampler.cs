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

    public class WeirdScaledSampler : RsSampler
    {
        private RsSampler m_raritySampler;
        private int m_type;
        public WeirdScaledSampler(RsNoise noise, RsSampler raritySampler, int type)
            : base(noise)
        {
            m_raritySampler = raritySampler;
            m_type = type;
        }

        public override float Sample(Vector3 pos)
        {
            float rarity;
            if (m_type == 1)
            {
                rarity = RarityMapperType1(m_raritySampler.Sample(pos));
            }
            else
            {
                rarity = RarityMapperType2(m_raritySampler.Sample(pos));
            }

            return base.Sample(pos / rarity);
        }

        private float RarityMapperType1(float rarity)
        {
            if (rarity < -0.5f)
            {
                return 0.75f;
            }
            
            if (rarity < 0f)
            {
                return 1.0f;
            }

            if (rarity < 0.5f)
            {
                return 1.5f;
            }

            return 2.0f;
        }
        
        private float RarityMapperType2(float rarity)
        {
            if (rarity < -0.75f)
            {
                return 0.5f;
            }
            
            if (rarity < -0.5f)
            {
                return 0.75f;
            }

            if (rarity < 0.5f)
            {
                return 1.0f;
            }

            if (rarity < 0.75f)
            {
                return 2.0f;
            }

            return 3.0f;
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