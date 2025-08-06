using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace RS.Utils
{
    public class AddSampler : RsSampler
    {
        private RsSampler m_left;
        private RsSampler m_right;

        public override void Dispose()
        {
            if (!m_left.BuildFromConfig)
            {
                m_left.Dispose();
            }

            if (!m_right.BuildFromConfig)
            {
                m_right.Dispose();
            }
        }

        public AddSampler(RsSampler left, RsSampler right)
        {
            m_left = left;
            m_right = right;
        }

        public override float Sample(Vector3 pos)
        {
            return m_left.Sample(pos) + m_right.Sample(pos);
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var leftList = m_left.SampleBatch(posList);
            var rightList = m_right.SampleBatch(posList);
            
            var left = new NativeArray<float>(leftList, Allocator.TempJob);
            var right = new NativeArray<float>(rightList, Allocator.TempJob);
            var result = new NativeArray<float>(posList.Length, Allocator.TempJob);

            var job = new AddJob()
            {
                left = left,
                right = right,
                result = result
            };
            
            var handle = job.Schedule(posList.Length, 64);
            handle.Complete();
            
            var ret = result.ToArray();
            
            left.Dispose();
            right.Dispose();
            result.Dispose();
            
            // for (int i = 0; i < leftList.Length; i++)
            // {
            //     result[i] = leftList[i] + rightList[i];
            // }
            
            return ret;
        }

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> left;
            [ReadOnly] public NativeArray<float> right;
            [WriteOnly] public NativeArray<float> result;
            
            public void Execute(int index)
            {
                result[index] = left[index] + right[index];
            }
        }
    }

    public class MulSampler : RsSampler
    {
        private RsSampler m_left;
        private RsSampler m_right;

        public override void Dispose()
        {
            if (!m_left.BuildFromConfig)
            {
                m_left.Dispose();
            }

            if (!m_right.BuildFromConfig)
            {
                m_right.Dispose();
            }
        }
        
        public MulSampler(RsSampler left, RsSampler right)
        {
            m_left = left;
            m_right = right;
        }

        public override float Sample(Vector3 pos)
        {
            return m_left.Sample(pos) * m_right.Sample(pos);
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var leftList = m_left.SampleBatch(posList);
            var rightList = m_right.SampleBatch(posList);
            var result = new float[leftList.Length];
            for (int i = 0; i < leftList.Length; i++)
            {
                result[i] = leftList[i] * rightList[i];
            }

            return result;
        }
    }
    public class MaxSampler : RsSampler
    {
        private RsSampler m_left;
        private RsSampler m_right;

        public override void Dispose()
        {
            if (!m_left.BuildFromConfig)
            {
                m_left.Dispose();
            }

            if (!m_right.BuildFromConfig)
            {
                m_right.Dispose();
            }
        }
        
        public MaxSampler(RsSampler left, RsSampler right)
        {
            m_left = left;
            m_right = right;
        }

        public override float Sample(Vector3 pos)
        {
            return Mathf.Max(m_left.Sample(pos), m_right.Sample(pos));
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var leftList = m_left.SampleBatch(posList);
            var rightList = m_right.SampleBatch(posList);
            var result = new float[leftList.Length];
            for (int i = 0; i < leftList.Length; i++)
            {
                result[i] = Mathf.Max(leftList[i], rightList[i]);
            }

            return result;
        }
    }

    public class MinSampler : RsSampler
    {
        private RsSampler m_left;
        private RsSampler m_right;

        public override void Dispose()
        {
            if (!m_left.BuildFromConfig)
            {
                m_left.Dispose();
            }

            if (!m_right.BuildFromConfig)
            {
                m_right.Dispose();
            }
        }
        
        public MinSampler(RsSampler left, RsSampler right)
        {
            m_left = left;
            m_right = right;
        }

        public override float Sample(Vector3 pos)
        {
            return Mathf.Min(m_left.Sample(pos), m_right.Sample(pos));
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var leftList = m_left.SampleBatch(posList);
            var rightList = m_right.SampleBatch(posList);
            var result = new float[leftList.Length];
            for (int i = 0; i < leftList.Length; i++)
            {
                result[i] = Mathf.Min(leftList[i], rightList[i]);
            }

            return result;
        }
    }

    public class AbsSampler : RsSampler
    {
        private RsSampler m_sampler;
        
        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }
        
        public AbsSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            return Mathf.Abs(m_sampler.Sample(pos));
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var list = m_sampler.SampleBatch(posList);
            var result = new float[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                result[i] = Mathf.Abs(list[i]);
            }

            return result;
        }
    }

    public class SquareSampler : RsSampler
    {
        private RsSampler m_sampler;
        
        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public SquareSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);
            return value * value;
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var list = m_sampler.SampleBatch(posList);
            var result = new float[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                result[i] = list[i] * list[i];
            }

            return result;
        }
    }

    public class CubeSampler : RsSampler
    {
        private RsSampler m_sampler;
        
        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public CubeSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);
            return value * value * value;
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var list = m_sampler.SampleBatch(posList);
            var result = new float[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                result[i] = list[i] * list[i] * list[i];
            }

            return result;
        }
    }
    
    public class HalfNegativeSampler : RsSampler
    {
        private RsSampler m_sampler;
        
        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public HalfNegativeSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);

            return value < 0 ? value * 0.5f : value;
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var list = m_sampler.SampleBatch(posList);
            var result = new float[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                result[i] = list[i] < 0 ? list[i] * 0.5f : list[i];
            }

            return result;
        }
    }

    public class QuarterNegativeSampler : RsSampler
    {
        private RsSampler m_sampler;
        
        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public QuarterNegativeSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);

            return value < 0 ? value * 0.25f : value;
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var list = m_sampler.SampleBatch(posList);
            var result = new float[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                result[i] = list[i] < 0 ? list[i] * 0.25f : list[i];
            }

            return result;
        }
    }

    public class ClampSampler : RsSampler
    {
        private RsSampler m_sampler;
        private float m_min;
        private float m_max;
        
        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public ClampSampler(RsSampler sampler, float min, float max)
        {
            m_sampler = sampler;
            m_min = min;
            m_max = max;
        }

        public override float Sample(Vector3 pos)
        {
            var value = m_sampler.Sample(pos);
            return RsMath.Clamp(value, m_min, m_max);
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var list = m_sampler.SampleBatch(posList);
            var result = new float[list.Length];
            for (int i = 0; i < list.Length; i++)
            {
                result[i] = RsMath.Clamp(list[i], m_min, m_max);
            }

            return result;
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

        public override float[] SampleBatch(Vector3[] posList)
        {
            var result = new float[posList.Length];
            for (int i = 0; i < posList.Length; i++)
            {
                result[i] = RsMath.ClampedMap(posList[i].y, m_min, m_max, m_from, m_to);
            }

            return result;
        }
    }

    public class SqueezeSampler : RsSampler
    {
        private RsSampler m_sampler;
        
        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public SqueezeSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            var val = m_sampler.Sample(pos);
            val = RsMath.Clamp(val, -1.0f, 1.0f);
            
            return val * 0.5f - val * val * val / 24.0f;
        }

        public override float[] SampleBatch(Vector3[] posList)
        {
            var result = new float[posList.Length];
            for (int i = 0; i < posList.Length; i++)
            {
                var val = m_sampler.Sample(posList[i]);
                val = RsMath.Clamp(val, -1.0f, 1.0f);
                
                result[i] = val * 0.5f - val * val * val / 24.0f;
            }

            return result;
        }
    }

    // public class XSampler : RsSampler
    // {
    //     public XSampler()
    //     {
    //         
    //     }
    //
    //     public override float Sample(Vector3 pos)
    //     {
    //         return pos.x;
    //     }
    // }
}