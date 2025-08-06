using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Vector3 = UnityEngine.Vector3;

namespace RS.Utils
{
    public class InterpolatedSampler : RsSampler
    {
        private RsSampler m_sampler;
        private const float m_cellWidth = 4.0f;
        private const float m_cellHeight = 4.0f;

        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public InterpolatedSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        /// <summary>
        /// 4x4*4的区域仅采样一次, 需通过插值来获取更细致的曲线
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public override float Sample(Vector3 pos)
        {
            var w = m_cellWidth;
            var h = m_cellHeight;
            var tx = ((pos.x % w + w) % w) / w;
            var ty = ((pos.y % h + h) % h) / h;
            var tz = ((pos.z % w + w) % w) / w;

            var fx = Mathf.Floor(pos.x / w) * w;
            var fy = Mathf.Floor(pos.y / h) * h;
            var fz = Mathf.Floor(pos.z / w) * w;

            var c000 = m_sampler.Sample(new Vector3(fx, fy, fz));
            var c100 = m_sampler.Sample(new Vector3(fx + w, fy, fz));
            var c010 = m_sampler.Sample(new Vector3(fx, fy + h, fz));
            var c110 = m_sampler.Sample(new Vector3(fx + w, fy + h, fz));
            var c001 = m_sampler.Sample(new Vector3(fx, fy, fz + w));
            var c101 = m_sampler.Sample(new Vector3(fx + w, fy, fz + w));
            var c011 = m_sampler.Sample(new Vector3(fx, fy + h, fz + w));
            var c111 = m_sampler.Sample(new Vector3(fx + w, fy + h, fz + w));

            // 三线性插值
            return RsMath.TriLerp(tx, ty, tz, c000, c100, c010, c110, c001, c101, c011, c111);
        }

        public override float[] SampleBatch(Vector3 startPos, int x, int y, int z)
        {
            var w = m_cellWidth;
            var h = m_cellHeight;
            
            // 对所有间隔点先采样, 需各维度多一个间隔
            // var sw = Stopwatch.StartNew();
            
            var posList = new List<Vector3>();
            
            // 全批量采样 JobSystem
            for (var ix = 0; ix < 9; ix++)
            {
                for (var iz = 0; iz < 9; iz++)
                {
                    for (var iy = 0; iy < 9; iy++)
                    {
                        var fx = startPos.x + ix * w;
                        var fy = startPos.y + iy * h;
                        var fz = startPos.z + iz * w;
                        posList.Add(new Vector3(fx, fy, fz));
                    }
                }
            }

            var sampleResult = m_sampler.SampleBatch(posList.ToArray());
            var cache = new NativeArray<float>(sampleResult, Allocator.TempJob);
            
            // 并行单次采样
            // var cache = new NativeArray<float>(9 * 9 * 9, Allocator.TempJob);
            // Parallel.For(0, 81, (index) =>
            // {
            //     var ix = index / 9;
            //     var iz = index % 9;
            //     for (var iy = 0; iy < 9; iy++)
            //     {
            //         var fx = startPos.x + ix * w;
            //         var fy = startPos.y + iy * h;
            //         var fz = startPos.z + iz * w;
            //         cache[ix * 81 + iz * 9 + iy] = m_sampler.Sample(new Vector3(fx, fy, fz));
            //     }
            // });
            
            // 串行单次采样
            // for (var ix = 0; ix < 9; ix++)
            // {
            //     for (var iz = 0; iz < 9; iz++)
            //     {
            //         for (var iy = 0; iy < 9; iy++)
            //         {
            //             var fx = startPos.x + ix * w;
            //             var fy = startPos.y + iy * h;
            //             var fz = startPos.z + iz * w;
            //             cache[ix * 81 + iz * 9 + iy] = m_sampler.Sample(new Vector3(fx, fy, fz));
            //         }
            //     }
            // }

            // sw.Stop();
            // Debug.Log($"SampleBatch: {sw.ElapsedMilliseconds}ms");
            // sw = Stopwatch.StartNew();

            // 对中间点进行插值 JobSystem
            var result = new NativeArray<float>(32 * 32 * 32, Allocator.TempJob);
            var job = new TriLerpJob
            {
                cache = cache,
                w = m_cellWidth,
                h = m_cellHeight,
                result = result
            };

            var handle = job.Schedule(32 * 32 * 32, 64);
            handle.Complete();

            var ret = result.ToArray();

            cache.Dispose();
            result.Dispose();
            
            // sw.Stop();
            // Debug.Log($"Interpolate: {sw.ElapsedMilliseconds}ms");

            return ret;
        }

        [BurstCompile]
        private struct TriLerpJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> cache;
            [ReadOnly] public float w;
            [ReadOnly] public float h;
            [WriteOnly] public NativeArray<float> result;

            public void Execute(int index)
            {
                var ix = index / (32 * 32);
                var iy = index % 32;
                var iz = (index / 32) % 32;
                
                var tx = (ix % w) / w;
                var ty = (iy % h) / h;
                var tz = (iz % w) / w;
                
                var fx = Mathf.FloorToInt(ix / w);
                var fy = Mathf.FloorToInt(iy / h);
                var fz = Mathf.FloorToInt(iz / w);

                var c000 = cache[fx * 81 + fy + fz * 9];
                var c100 = cache[(fx + 1) * 81 + fy + fz * 9];
                var c010 = cache[fx * 81 + (fy + 1) + fz * 9];
                var c110 = cache[(fx + 1) * 81 + (fy + 1) + fz * 9];
                var c001 = cache[fx * 81 + fy + (fz + 1) * 9];
                var c101 = cache[(fx + 1) * 81 + fy + (fz + 1) * 9];
                var c011 = cache[fx * 81 + fy + 1 + (fz + 1) * 9];
                var c111 = cache[(fx + 1) * 81 + fy + 1 + (fz + 1) * 9];
                
                result[index] = RsMath.TriLerp(tx, ty, tz, c000, c100, c010, c110, c001, c101, c011, c111);
            }
        }

    }
}