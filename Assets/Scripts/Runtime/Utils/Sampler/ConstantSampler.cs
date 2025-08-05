using UnityEngine;

namespace RS.Utils
{
    public class ConstantSampler : RsSampler
    {
        private float m_value;

        public override void Dispose()
        {
            base.Dispose();
        }

        public ConstantSampler(float value)
        {
            m_value = value;
        }

        public override float Sample(Vector3 pos)
        {
            return m_value;
        }
    }
}