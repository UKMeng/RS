using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    public class SplineSampler : RsSampler
    {
        private RsSampler m_coordinate;
        private NativeArray<float> m_locations;
        private NativeArray<float> m_derivatives;
        private RsSampler[] m_values;
        
        public override void Dispose()
        {
            if (!m_coordinate.BuildFromConfig)
            {
                m_coordinate.Dispose();
            }

            foreach (var value in m_values)
            {
                if (!value.BuildFromConfig)
                {
                    value.Dispose();
                }
            }
            m_locations.Dispose();
            m_derivatives.Dispose();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="locations">要求位置从小到大排列</param>
        /// <param name="derivatives"></param>
        /// <param name="values"></param>
        public SplineSampler(RsSampler coordinate, float[] locations, float[] derivatives, RsSampler[] values)
        {
            m_coordinate = coordinate;
            m_locations = new NativeArray<float>(locations, Allocator.Persistent);
            m_derivatives = new NativeArray<float>(derivatives, Allocator.Persistent);
            m_values = values;
        }
        
        public override float Sample(Vector3 pos)
        {
            var x = m_coordinate.Sample(pos);
            var pointCount = m_values.Length;
            
            // 二分查找最接近的位置
            var xPos = BinarySearch(m_locations, x);

            // 三次Hermite插值多项式
            // p(x) = H₀(x) * f(x₀) + H₁(x) * f(x₁) + M₀(x) * f'(x₀) + M₁(x) * f'(x₁)
            // H₀(x) = (1 + 2(x - x₀) / h) * ((x - x₁) / h)²
            // H₁(x) = (1 - 2(x - x₁) / h) * ((x - x₀) / h)²
            // M₀(x) = (x - x₀) * ((x - x₁) / h)²
            // M₁(x) = (x - x₁) * ((x - x₀) / h)²
            // 其中h = x₁ - x₀
            if (xPos < 0)
            {
                return m_values[0].Sample(pos) + m_derivatives[0] * (x - m_locations[0]);
            }
            
            if (xPos >= m_values.Length - 1)
            {
                return m_values[pointCount - 1].Sample(pos) + m_derivatives[pointCount - 1] * (x - m_locations[pointCount - 1]);
            }

            var x0 = m_locations[xPos];
            var x1 = m_locations[xPos + 1];
            var y0 = m_values[xPos].Sample(pos);
            var y1 = m_values[xPos + 1].Sample(pos);
            var dy0 = m_derivatives[xPos];
            var dy1 = m_derivatives[xPos + 1];
            
            var h = x1 - x0;
            var h0 = (1 + 2 * (x - x0) / h) * ((x - x1) / h) * ((x - x1) / h);
            var h1 = (1 - 2 * (x - x1) / h) * ((x - x0) / h) * ((x - x0) / h);
            var m0 = (x - x0) * ((x - x1) / h) * ((x - x1) / h);
            var m1 = (x - x1) * ((x - x0) / h) * ((x - x0) / h);
            
            return y0 * h0 + y1 * h1 + dy0 * m0 + dy1 * m1;
        }
        
        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            // Debug.Log($"posList.Length {posList.Length}");
            
            // 全部单采样 3896ms
            // var result = new NativeArray<float>(posList.Length, Allocator.TempJob);
            // for (var i = 0; i < posList.Length; i++)
            // {
            //     result[i] = Sample(posList[i]);
            // }
            //
            // return result;

            // coords批量采 3876ms
            // var coords = m_coordinate.SampleBatch(posList);
            //
            // var result = new NativeArray<float>(posList.Length, Allocator.TempJob);
            // for (var i = 0; i < posList.Length; i++)
            // {
            //     result[i] = SampleSingle(posList[i], coords[i]);
            // }
            //
            // coords.Dispose();
            // return result;
            
            // 全部批采样 3551ms
            var coords = m_coordinate.SampleBatch(posList);
            var result = new NativeArray<float>(posList.Length, Allocator.TempJob);
            
            var pointCount = m_values.Length;
            
            var front = new NativeArray<int>(posList.Length, Allocator.TempJob);
            var back = new NativeArray<int>(posList.Length, Allocator.TempJob);
            
            var sampleIndices = new NativeArray<int2>(posList.Length, Allocator.TempJob);
            
            // 用来存每个value采样器需要采的坐标
            var indexToSample = new List<Vector3>[pointCount];
            for (var i = 0; i < pointCount; i++)
            {
                indexToSample[i] = new List<Vector3>();
            }
            
            var sampleCount = new int[pointCount];
            Array.Fill(sampleCount, 0);
            
            for (var i = 0; i < posList.Length; i++)
            {
                // 二分查找最接近的位置
                var xPos = BinarySearch(m_locations, coords[i]);
            
                if (xPos < 0)
                {
                    front[i] = 0;
                    back[i] = 0;
                    indexToSample[0].Add(posList[i]);
                    sampleIndices[i] = new int2(sampleCount[0], sampleCount[0]);
                    sampleCount[0]++;
                }
                else if (xPos >= m_values.Length - 1)
                {
                    front[i] = pointCount - 1;
                    back[i] = pointCount - 1;
                    indexToSample[pointCount - 1].Add(posList[i]);
                    sampleIndices[i] = new int2(sampleCount[pointCount - 1], sampleCount[pointCount - 1]);
                    sampleCount[pointCount - 1]++;
                }
                else
                {
                    front[i] = xPos;
                    back[i] = xPos + 1;
                    indexToSample[xPos].Add(posList[i]);
                    indexToSample[xPos + 1].Add(posList[i]);
                    sampleIndices[i] = new int2(sampleCount[xPos], sampleCount[xPos + 1]);
                    sampleCount[xPos]++;
                    sampleCount[xPos+1]++;
                }
            }
            
            var sampleOffset = new NativeArray<int>(pointCount, Allocator.TempJob);
            var offset = 0;
            
            
            for (var i = 0; i < pointCount; i++)
            {
                sampleOffset[i] = offset;
                offset += indexToSample[i].Count;
            }
            
            var sampleResult = new NativeArray<float>(offset, Allocator.TempJob);
            for (var i = 0; i < pointCount; i++)
            {
                if (indexToSample[i].Count == 0)
                {
                    continue;
                }
                var res = m_values[i].SampleBatch(indexToSample[i].ToArray());
                for (var j = 0; j < indexToSample[i].Count; j++)
                {
                    var index = sampleOffset[i] + j;
                    sampleResult[index] = res[j];
                }
                res.Dispose();
            }
            
            var hermiteJob = new HermiteJob()
            {
                coords = coords,
                locations = m_locations,
                derivatives = m_derivatives,
                front = front,
                back = back,
                sampleIndices = sampleIndices,
                sampleOffset = sampleOffset,
                sampleResult = sampleResult,
                result = result,
            };

            var handle = hermiteJob.Schedule(posList.Length, 64);
            handle.Complete();
            
            // var sampleCount = new int[pointCount];
            // Array.Fill(sampleCount, 0);
            // for (var i = 0; i < posList.Length; i++)
            // {
            //     if (front[i] == back[i])
            //     {
            //         if (front[i] == 0)
            //         {
            //             var sampleIndex = sampleCount[0]++;
            //             var val = sampleResult[0][sampleIndex];
            //             result[i] = val + m_derivatives[0] * (coords[i] - m_locations[0]);
            //         }
            //         else
            //         {
            //             var sampleIndex = sampleCount[pointCount - 1]++;
            //             var val = sampleResult[pointCount - 1][sampleIndex];
            //             result[i] = val + m_derivatives[pointCount - 1] * (coords[i] - m_locations[pointCount - 1]);
            //         }
            //     }
            //     else
            //     {
            //         var f = front[i];
            //         var b = back[i];
            //         var x = coords[i];
            //         var x0 = m_locations[f];
            //         var x1 = m_locations[b];
            //         var frontIndex = sampleCount[f]++;
            //         var backIndex = sampleCount[b]++;
            //         var y0 = sampleResult[f][frontIndex];
            //         var y1 = sampleResult[b][backIndex];
            //         var dy0 = m_derivatives[f];
            //         var dy1 = m_derivatives[b];
            //
            //         var h = x1 - x0;
            //         var h0 = (1 + 2 * (x - x0) / h) * ((x - x1) / h) * ((x - x1) / h);
            //         var h1 = (1 - 2 * (x - x1) / h) * ((x - x0) / h) * ((x - x0) / h);
            //         var m0 = (x - x0) * ((x - x1) / h) * ((x - x1) / h);
            //         var m1 = (x - x1) * ((x - x0) / h) * ((x - x0) / h);
            //
            //         result[i] = y0 * h0 + y1 * h1 + dy0 * m0 + dy1 * m1;
            //     }
            // }
            
            // foreach (var res in sampleResult)
            // {
            //     res.Dispose();
            // }

            sampleResult.Dispose();
            sampleOffset.Dispose();
            sampleIndices.Dispose();
            front.Dispose();
            back.Dispose();
            coords.Dispose();
            
            return result;
        }

        private int BinarySearch(NativeArray<float> locations, float target)
        {
            var low = 0;
            var high = locations.Length - 1;
            while (low <= high)
            {
                var mid = (low + high) >> 1;
                if (locations[mid] < target)
                {
                    low = mid + 1;
                }
                else if (locations[mid] > target)
                {
                    high = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            return low - 1;
        }

        [BurstCompile]
        private struct HermiteJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> coords;
            [ReadOnly] public NativeArray<float> locations;
            [ReadOnly] public NativeArray<float> derivatives;
            [ReadOnly] public NativeArray<int> front;
            [ReadOnly] public NativeArray<int> back;
            [ReadOnly] public NativeArray<int2> sampleIndices;
            [ReadOnly] public NativeArray<int> sampleOffset;
            [ReadOnly] public NativeArray<float> sampleResult;
            [WriteOnly] public NativeArray<float> result;
            
            public void Execute(int index)
            {
                var f = front[index];
                var b = back[index];
                var x = coords[index];
                var x0 = locations[f];
                var x1 = locations[b];
                var frontIndex = sampleIndices[index].x;
                var backIndex = sampleIndices[index].y;
                var y0 = sampleResult[sampleOffset[f] + frontIndex];
                var y1 = sampleResult[sampleOffset[b] + backIndex];
                var dy0 = derivatives[f];
                var dy1 = derivatives[b];

                if (f == b)
                {
                    result[index] = y0 + dy0 * (x - x0);
                }
                else
                {
                    var h = x1 - x0;
                    var h0 = (1 + 2 * (x - x0) / h) * ((x - x1) / h) * ((x - x1) / h);
                    var h1 = (1 - 2 * (x - x1) / h) * ((x - x0) / h) * ((x - x0) / h);
                    var m0 = (x - x0) * ((x - x1) / h) * ((x - x1) / h);
                    var m1 = (x - x1) * ((x - x0) / h) * ((x - x0) / h);
            
                    result[index] = y0 * h0 + y1 * h1 + dy0 * m0 + dy1 * m1;
                }
            }
        }
    }
}