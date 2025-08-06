using System;
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

        public override float[] SampleBatch(Vector3[] posList)
        {
            var result = new float[posList.Length];
            Array.Fill(result, m_value);
            return result;
        }
    }
}