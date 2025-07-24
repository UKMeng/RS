using System;
using System.Collections.Generic;
using UnityEngine;

using RS.Utils;

namespace RS.Utils
{
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