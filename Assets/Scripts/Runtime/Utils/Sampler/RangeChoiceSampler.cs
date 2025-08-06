using System.Collections.Generic;
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
        
        public override void Dispose()
        {
            if (!m_input.BuildFromConfig)
            {
                m_input.Dispose();
            }

            if (!m_inRange.BuildFromConfig)
            {
                m_inRange.Dispose();
            }

            if (!m_outRange.BuildFromConfig)
            {
                m_outRange.Dispose();
            }
        }

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

        public override float[] SampleBatch(Vector3[] posList)
        {
            var inputResult = m_input.SampleBatch(posList);
            var inRangeIndex = new List<int>();
            var inRangePosList = new List<Vector3>();
            var outRangeIndex = new List<int>();
            var outRangePosList = new List<Vector3>();

            for (var i = 0; i < inputResult.Length; i++)
            {
                var t = inputResult[i];
                if (t < m_min || t >= m_max)
                {
                    outRangeIndex.Add(i);
                    outRangePosList.Add(posList[i]);
                }
                else
                {
                    inRangeIndex.Add(i);
                    inRangePosList.Add(posList[i]);
                }
            }

            var inRangeResult = m_inRange.SampleBatch(inRangePosList.ToArray());
            var outRangeResult = m_outRange.SampleBatch(outRangePosList.ToArray());
            var result = new float[inputResult.Length];
            for (var i = 0; i < inRangeIndex.Count; i++)
            {
                result[inRangeIndex[i]] = inRangeResult[i];
            }

            for (var i = 0; i < outRangeIndex.Count; i++)
            {
                result[outRangeIndex[i]] = outRangeResult[i];
            }

            return result;
        }
    }
}