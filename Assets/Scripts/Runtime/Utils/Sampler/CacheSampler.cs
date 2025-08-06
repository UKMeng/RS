using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace RS.Utils
{
    /// <summary>
    /// 仅采样xz坐标并缓存
    /// </summary>
    public class Cache2DSampler : RsSampler
    {
        private NativeArray<float> m_cache;
        private RsSampler m_sampler;
        private int m_startX;
        private int m_startZ;

        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }

            m_cache.Dispose();
        }

        public Cache2DSampler(RsSampler sampler, Vector3Int startPos)
        {
            // 1028 * 1028
            m_cache = new NativeArray<float>(1056784, Allocator.Persistent);
            for (var i = 0; i < m_cache.Length; i++)
            {
                m_cache[i] = 65535.0f;
            }
            m_sampler = sampler;
            m_startX = startPos.x;
            m_startZ = startPos.z;
        }
    
        public override float Sample(Vector3 position)
        {
            var posX = (int)position.x;
            var posZ = (int)position.z;
            var ix = RsMath.Mod(posX, 1024);
            var iz = RsMath.Mod(posZ, 1024);
            
            if (posX >= (m_startX + 1) * 1024)
            {
                ix += 1024;
            }

            if (posZ >= (m_startZ + 1) * 1024)
            {
                iz += 1024;
            }
            
            var index = ix * 1028 + iz;
            
            if (m_cache[index] > 65534.0f)
            {
                var val = m_sampler.Sample(new Vector3(posX, 0, posZ));
                m_cache[index] = val;
            }

            return m_cache[index];
        }

        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            var result = new NativeArray<float>(posList.Length, Allocator.TempJob);
            var sampleList = new List<Vector3>();
            var indexList = new List<int>();

            var dict = new Dictionary<int, List<int>>();
            
            for (var i = 0; i < posList.Length; i++)
            {
                var pos = posList[i];
                var posX = (int)pos.x;
                var posZ = (int)pos.z;
                var ix = RsMath.Mod(posX, 1024);
                var iz = RsMath.Mod(posZ, 1024);
            
                if (posX >= (m_startX + 1) * 1024)
                {
                    ix += 1024;
                }

                if (posZ >= (m_startZ + 1) * 1024)
                {
                    iz += 1024;
                }
            
                var index = ix * 1028 + iz;
            
                if (m_cache[index] > 65534.0f)
                {
                    if (dict.TryGetValue(index, out var toSampleIndex))
                    {
                        toSampleIndex.Add(i);
                    }
                    else
                    {
                        dict[index] = new List<int>() { i };
                        sampleList.Add(new Vector3(posX, 0, posZ));
                        indexList.Add(index);
                    }
                }
                else
                {
                    result[i] = m_cache[index];
                }
            }

            if (sampleList.Count > 0)
            {
                var sampleResult = m_sampler.SampleBatch(sampleList.ToArray());
                for (var i = 0; i < sampleList.Count; i++)
                {
                    var index = indexList[i];
                    var toSampleIndex = dict[index];
                    m_cache[index] = sampleResult[i];
                    for (var j = 0; j < toSampleIndex.Count; j++)
                    {
                        result[toSampleIndex[j]] = sampleResult[i];
                    }
                }
                sampleResult.Dispose();
            }
            
            return result;
        }
    }

    public class CacheOnceSampler : RsSampler
    {
        private Vector3 m_lastPos;
        private float m_lastValue;
        private RsSampler m_sampler;

        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }
        }

        public CacheOnceSampler(RsSampler sampler)
        {
            m_sampler = sampler;
        }

        public override float Sample(Vector3 pos)
        {
            if (pos != m_lastPos)
            {
                m_lastPos = pos;
                m_lastValue = m_sampler.Sample(pos);
            }

            return m_lastValue;
        }

        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            // 批量时这个缓存失效 可能要想想其他Cache手段
            return m_sampler.SampleBatch(posList);
        }
    }
    
    /// <summary>
    /// 4x4的区域仅采样一次y=0处
    /// </summary>
    public class FlatCacheSampler : RsSampler
    {
        private NativeArray<float> m_cache;
        private RsSampler m_sampler;
        private int m_startX;
        private int m_startZ;

        public override void Dispose()
        {
            if (!m_sampler.BuildFromConfig)
            {
                m_sampler.Dispose();
            }

            m_cache.Dispose();
        }

        public FlatCacheSampler(RsSampler sampler, Vector3Int startPos)
        {
            // 66049 = 257 * 257
            m_cache = new NativeArray<float>(66049, Allocator.Persistent);
            for (var i = 0; i < m_cache.Length; i++)
            {
                m_cache[i] = 65535.0f;
            }
            m_sampler = sampler;
            m_startX = startPos.x;
            m_startZ = startPos.z;
        }

        public override float Sample(Vector3 pos)
        {
            var posX = Mathf.FloorToInt(pos.x * 0.25f);
            var posZ = Mathf.FloorToInt(pos.z * 0.25f);

            var ix = RsMath.Mod(posX, 256);
            var iz = RsMath.Mod(posZ, 256);
            
            if (posX >= (m_startX + 1) * 256)
            {
                ix += 256;
            }

            if (posZ >= (m_startZ + 1) * 256)
            {
                iz += 256;
            }
            
            var index = ix * 257 + iz;

            if (m_cache[index] > 65534.0f)
            {
                var val = m_sampler.Sample(new Vector3(posX * 4.0f, 0, posZ * 4.0f));
                m_cache[index] = val;
            }

            return m_cache[index];
        }
        
        public override NativeArray<float> SampleBatch(Vector3[] posList)
        {
            var result = new NativeArray<float>(posList.Length, Allocator.TempJob);
            var sampleList = new List<Vector3>();
            var indexList = new List<int>();

            var dict = new Dictionary<int, List<int>>();
            
            for (var i = 0; i < posList.Length; i++)
            {
                var pos = posList[i];
                var posX = Mathf.FloorToInt(pos.x * 0.25f);
                var posZ = Mathf.FloorToInt(pos.z * 0.25f);

                var ix = RsMath.Mod(posX, 256);
                var iz = RsMath.Mod(posZ, 256);
            
                if (posX >= (m_startX + 1) * 256)
                {
                    ix += 256;
                }

                if (posZ >= (m_startZ + 1) * 256)
                {
                    iz += 256;
                }
            
                var index = ix * 257 + iz;
            
                if (m_cache[index] > 65534.0f)
                {
                    if (dict.TryGetValue(index, out var toSampleIndex))
                    {
                        toSampleIndex.Add(i);
                    }
                    else
                    {
                        dict[index] = new List<int>() { i };
                        sampleList.Add(new Vector3(posX * 4.0f, 0, posZ * 4.0f));
                        indexList.Add(index);
                    }
                }
                else
                {
                    result[i] = m_cache[index];
                }
            }

            if (sampleList.Count > 0)
            {
                var sampleResult = m_sampler.SampleBatch(sampleList.ToArray());
                for (var i = 0; i < sampleList.Count; i++)
                {
                    var index = indexList[i];
                    var toSampleIndex = dict[index];
                    m_cache[index] = sampleResult[i];
                    for (var j = 0; j < toSampleIndex.Count; j++)
                    {
                        result[toSampleIndex[j]] = sampleResult[i];
                    }
                }

                sampleResult.Dispose();
            }
            
            return result;
        }
    }
}