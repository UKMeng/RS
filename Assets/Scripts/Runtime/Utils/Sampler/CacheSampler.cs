using System;
using System.Collections.Generic;
using UnityEngine;

namespace RS.Utils
{
    /// <summary>
    /// 仅采样xz坐标并缓存
    /// </summary>
    public class Cache2DSampler : RsSampler
    {
        private float[] m_cache;
        private RsSampler m_sampler;

        public Cache2DSampler(RsSampler sampler)
        {
            m_cache = new float[1028 * 1028];
            Array.Fill(m_cache, float.MinValue);
            m_sampler = sampler;
        }
    
        public override float Sample(Vector3 position)
        {
            var posX = (int)position.x;
            var posZ = (int)position.z;
            var ix = RsMath.Mod(posX, 1028);
            var iz = RsMath.Mod(posZ, 1028);
            var index = ix * 1028 + iz;
            
            if (m_cache[index] == float.MinValue)
            {
                var val = m_sampler.Sample(new Vector3(posX, 0, posZ));
                m_cache[index] = val;
            }

            return m_cache[index];
        }
    }

    public class CacheOnceSampler : RsSampler
    {
        private Vector3 m_lastPos;
        private float m_lastValue;
        private RsSampler m_sampler;

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
    }
    
    /// <summary>
    /// 4x4的区域仅采样一次y=0处
    /// </summary>
    public class FlatCacheSampler : RsSampler
    {
        private float[] m_cache;
        private RsSampler m_sampler;

        public FlatCacheSampler(RsSampler sampler)
        {
            m_cache = new float[257 * 257];
            Array.Fill(m_cache, float.MinValue);
            m_sampler = sampler;
        }

        public FlatCacheSampler(RsNoise noise)
        {
            m_cache = new float[257 * 257];
            Array.Fill(m_cache, float.MinValue);
            m_sampler = new RsSampler(noise);
        }

        public override float Sample(Vector3 pos)
        {
            var posX = Mathf.FloorToInt(pos.x * 0.25f);
            var posZ = Mathf.FloorToInt(pos.z * 0.25f);
            var ix = RsMath.Mod(posX, 257);
            var iz = RsMath.Mod(posZ, 257);
            var index = ix * 257 + iz;

            if (m_cache[index] == float.MinValue)
            {
                var val = m_sampler.Sample(new Vector3(posX * 4.0f, 0, posZ * 4.0f));
                m_cache[index] = val;
            }

            return m_cache[index];
        }
    }
}