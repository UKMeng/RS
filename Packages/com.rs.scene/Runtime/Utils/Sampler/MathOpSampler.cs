using UnityEngine;

namespace RS.Utils
{
    public class AddSampler : RsSampler
    {
        private RsSampler m_left;
        private RsSampler m_right;

        public AddSampler(RsSampler left, RsSampler right)
        {
            m_left = left;
            m_right = right;
        }

        public override float Sample(Vector3 pos)
        {
            return m_left.Sample(pos) + m_right.Sample(pos);
        }
    }

    public class MulSampler : RsSampler
    {
        private RsSampler m_left;
        private RsSampler m_right;

        public MulSampler(RsSampler left, RsSampler right)
        {
            m_left = left;
            m_right = right;
        }

        public override float Sample(Vector3 pos)
        {
            return m_left.Sample(pos) * m_right.Sample(pos);
        }
    }

    public class AbsSampler : RsSampler
    {
        private RsSampler m_sampler;

        public AbsSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            return Mathf.Abs(m_sampler.Sample(pos));
        }
    }

    public class SquareSampler : RsSampler
    {
        private RsSampler m_sampler;

        public SquareSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);
            return value * value;
        }
    }

    public class CubeSampler : RsSampler
    {
        private RsSampler m_sampler;

        public CubeSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);
            return value * value * value;
        }
    }

    public class HalfNegativeSampler : RsSampler
    {
        private RsSampler m_sampler;

        public HalfNegativeSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);

            return value < 0 ? value * 0.5f : value;
        }
    }

    public class QuarterNegativeSampler : RsSampler
    {
        private RsSampler m_sampler;

        public QuarterNegativeSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);

            return value < 0 ? value * 0.25f : value;
        }
    }

    public class YClampedGradientSampler : RsSampler
    {
        private float m_min;
        private float m_max;
        private float m_from;
        private float m_to;

        public YClampedGradientSampler(float min, float max, float from, float to)
        {
            m_min = min;
            m_max = max;
            m_from = from;
            m_to = to;
        }

        public override float Sample(Vector3 pos)
        {
            return RsMath.ClampedMap(pos.y, m_min, m_max, m_from, m_to);
        }
    }

    public class XSampler : RsSampler
    {
        public XSampler()
        {
            
        }

        public override float Sample(Vector3 pos)
        {
            return pos.x;
        }
    }
}