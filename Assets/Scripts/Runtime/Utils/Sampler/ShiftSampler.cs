using System;
using UnityEngine;
using RS.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace RS.Utils
{
    /// <summary>
    /// 必须从噪声进行采样
    /// 采样在(x/4, 0, z/4)处采样值乘上4
    /// </summary>
    public class ShiftASampler : RsSampler
    {
        public ShiftASampler(RsNoise noise) 
            : base(noise)
        {
        }

        public override float Sample(Vector3 pos)
        {
            var shiftPos = new Vector3(pos.x * 0.25f, 0, pos.z * 0.25f);
            return m_noise.SampleFbm3D(shiftPos) * 4.0f;
        }

        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            // var input = new NativeArray<Vector3>(posList, Allocator.TempJob);
            //
            // var shiftPosList = new NativeArray<Vector3>(posList.Length, Allocator.TempJob);
            //
            // var job = new shiftPosJob()
            // {
            //     posList = input,
            //     shiftPosList = shiftPosList
            // };
            //
            // var handler = job.Schedule(posList.Length, 256);
            // handler.Complete();
            
            var shiftPosList = new Vector3[posList.Length];
            
            for (int i = 0; i < posList.Length; i++)
            {
                var pos = posList[i];
                shiftPosList[i] = new Vector3(pos.x * 0.25f, 0, pos.z * 0.25f);
            }
            
            var sampleResult = base.SampleBatch(shiftPosList);
            
            for (var i = 0; i < sampleResult.Length; i++)
            {
                sampleResult[i] *= 4.0f;
            }

            // shiftPosList.Dispose();
            // input.Dispose();
            
            return sampleResult;
        }

        [BurstCompile]
        private struct shiftPosJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> posList;
            [WriteOnly] public NativeArray<Vector3> shiftPosList;

            public void Execute(int index)
            {
                var pos = posList[index];
                shiftPosList[index] = new Vector3(pos.x * 0.25f, 0, pos.z * 0.25f);
            }
        }
    }
    
    /// <summary>
    /// 必须从噪声进行采样
    /// 采样在(z/4, x/4, 0)处采样值乘上4
    /// </summary>
    public class ShiftBSampler : RsSampler
    {
        public ShiftBSampler(RsNoise noise) 
            : base(noise)
        {
        }

        public override float Sample(Vector3 pos)
        {
            var shiftPos = new Vector3(pos.z * 0.25f, pos.x * 0.25f, 0);
            return m_noise.SampleFbm3D(shiftPos) * 4.0f;
        }
        
        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            var shiftPosList = new Vector3[posList.Length];
            for (int i = 0; i < posList.Length; i++)
            {
                var pos = posList[i];
                shiftPosList[i] = new Vector3(pos.z * 0.25f, pos.x * 0.25f, 0);
            }
            
            // var input = new NativeArray<Vector3>(posList, Allocator.TempJob);
            //
            // var shiftPosList = new NativeArray<Vector3>(posList.Length, Allocator.TempJob);
            //
            // var job = new shiftPosJob()
            // {
            //     posList = input,
            //     shiftPosList = shiftPosList
            // };
            //
            // var handler = job.Schedule(posList.Length, 256);
            // handler.Complete();
            
            var sampleResult = base.SampleBatch(shiftPosList);
            
            for (var i = 0; i < sampleResult.Length; i++)
            {
                sampleResult[i] *= 4.0f;
            }
            
            // shiftPosList.Dispose();
            // input.Dispose();

            return sampleResult;
        }
        
        [BurstCompile]
        private struct shiftPosJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> posList;
            [WriteOnly] public NativeArray<Vector3> shiftPosList;

            public void Execute(int index)
            {
                var pos = posList[index];
                shiftPosList[index] = new Vector3(pos.x * 0.25f, 0, pos.z * 0.25f);
            }
        }
    }

    /// <summary>
    /// 必须给定一个Noise, 以及三个Sampler(作为坐标三个轴的偏移)
    /// </summary>
    public class ShiftedNoiseSampler : RsSampler
    {
        private RsSampler m_samplerX;
        private RsSampler m_samplerY;
        private RsSampler m_samplerZ;

        private float m_xzScale;
        private float m_yScale;
        
        public override void Dispose()
        {
            if (!m_samplerX.BuildFromConfig)
            {
                m_samplerX.Dispose();
            }

            if (!m_samplerY.BuildFromConfig)
            {
                m_samplerY.Dispose();
            }

            if (!m_samplerZ.BuildFromConfig)
            {
                m_samplerZ.Dispose();
            }
        }

        public ShiftedNoiseSampler(RsNoise noise, RsSampler samplerX, RsSampler samplerY, RsSampler samplerZ, float xzScale, float yScale)
            : base(noise)
        {
            m_samplerX = samplerX;
            m_samplerY = samplerY;
            m_samplerZ = samplerZ;
            m_xzScale = xzScale;
            m_yScale = yScale;
        }

        public override float Sample(Vector3 pos)
        {
            var offsetX = m_samplerX.Sample(pos);
            var offsetY = m_samplerY.Sample(pos);
            var offsetZ = m_samplerZ.Sample(pos);

            return m_noise.SampleFbm3D(
                new Vector3(pos.x * m_xzScale + offsetX , 
                                        pos.y * m_yScale + offsetY,
                                        pos.z * m_xzScale + offsetZ));
        }
        
        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            var offsetX = m_samplerX.SampleBatch(posList);
            var offsetY = m_samplerY.SampleBatch(posList);
            var offsetZ = m_samplerZ.SampleBatch(posList);
            
            var shiftPosList = new Vector3[posList.Length];
            for (int i = 0; i < posList.Length; i++)
            {
                var pos = posList[i];
                shiftPosList[i] = new Vector3(pos.x * m_xzScale + offsetX[i], 
                                              pos.y * m_yScale + offsetY[i],
                                              pos.z * m_xzScale + offsetZ[i]);
            }
            
            return base.SampleBatch(shiftPosList);
        }
    }
}