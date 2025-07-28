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
        private Dictionary<(int, int), float> m_cache;
        private RsSampler m_sampler;
        
        public Cache2DSampler(RsSampler sampler)
        {
            m_cache = new Dictionary<(int, int), float>();
            m_sampler = sampler;
        }
    
        public override float Sample(Vector3 position)
        {
            var posX = (int)position.x;
            var posZ = (int)position.z;
            if (m_cache.TryGetValue((posX, posZ), out var val))
            {
                return val;
            }
            else
            {
                val = m_sampler.Sample(new Vector3(posX, 0, posZ));
                m_cache.Add((posX, posZ), val);
                return val;
            }
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
        private Dictionary<(int, int), float> m_cache;
        private RsSampler m_sampler;

        public FlatCacheSampler(RsSampler sampler)
        {
            m_cache = new Dictionary<(int, int), float>();
            m_sampler = sampler;
        }

        public FlatCacheSampler(RsNoise noise)
        {
            m_cache = new Dictionary<(int, int), float>();
            m_sampler = new RsSampler(noise);
        }

        public override float Sample(Vector3 pos)
        {
            var ix = Mathf.FloorToInt(pos.x * 0.25f);
            var iz = Mathf.FloorToInt(pos.z * 0.25f);

            if (m_cache.TryGetValue((ix, iz), out var val))
            {
                return val;
            }
            else
            {
                val = m_sampler.Sample(new Vector3(ix * 4.0f, 0, iz * 4.0f));
                m_cache.Add((ix, iz), val);
                return val;
            }
        }
    }
}