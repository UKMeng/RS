using UnityEngine;

namespace RS.Utils
{
    public class RangeChoiceSampler : RsSampler
    {
        private RsSampler m_input;
        private RsSampler m_inRange;
        private RsSampler m_outRange;
        private float m_min; // 闭
        private float m_max; // 开

        public RangeChoiceSampler(RsSampler input, RsSampler inRange, RsSampler outRange, float min, float max)
        {
            m_input = input;
            m_inRange = inRange;
            m_outRange = outRange;
            m_min = min;
            m_max = max;
        }

        public override float Sample(Vector3 pos)
        {
            var t = m_input.Sample(pos);
            if (t < m_min || t >= m_max)
            {
                return m_outRange.Sample(pos);
            }

            return m_inRange.Sample(pos);
        }

    }
}