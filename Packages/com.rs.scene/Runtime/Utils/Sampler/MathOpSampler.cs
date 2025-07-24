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