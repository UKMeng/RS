using UnityEngine;

namespace RS.Scene.Sampler
{
    public class ConstantSampler : RsSampler
    {
        private float m_value;

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