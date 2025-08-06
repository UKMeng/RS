using System;
using Unity.Collections;
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

        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            var temp = new float[posList.Length];
            Array.Fill(temp, m_value);
            return new NativeArray<float>(temp, Allocator.TempJob);
        }
    }
}