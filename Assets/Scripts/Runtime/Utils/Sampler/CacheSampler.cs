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
        private int m_startX;
        private int m_startZ;

        public Cache2DSampler(RsSampler sampler, Vector3Int startPos)
        {
            m_cache = new float[1028 * 1028];
            Array.Fill(m_cache, 65535.0f);
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
        private int m_startX;
        private int m_startZ;

        public FlatCacheSampler(RsSampler sampler, Vector3Int startPos)
        {
            m_cache = new float[257 * 257];
            Array.Fill(m_cache, 65535.0f);
            m_sampler = sampler;
            m_startX = startPos.x;
            m_startZ = startPos.z;
        }

        public FlatCacheSampler(RsNoise noise)
        {
            m_cache = new float[257 * 257];
            Array.Fill(m_cache, 65535.0f);
            m_sampler = new RsSampler(noise);
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
    }
}