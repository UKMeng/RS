using System;
using System.Collections.Generic;
using UnityEngine;

namespace RS.Scene.Sampler
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
}