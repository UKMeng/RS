using System.Collections.Generic;
using UnityEngine;

namespace RS.Utils
{
    public class RsSamplerManager
    {
        private static RsSamplerManager s_instance;

        public static RsSamplerManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new RsSamplerManager();
                }
                return s_instance;
            }
        }

        public static void Reload()
        {
            s_instance = new RsSamplerManager();
        }

        private Dictionary<string, RsSampler> m_samplers;

        public RsSamplerManager()
        {
            Debug.Log($"[RsSamplerManager]初始化");
            m_samplers = new Dictionary<string, RsSampler>();
        }

        public RsSampler GetOrCreateSampler(string samplerName)
        {
            if (!m_samplers.TryGetValue(samplerName, out var sampler))
            {
                // Debug.Log($"[RsSamplerManager]实例化{samplerName}");
                var config = RsConfigManager.Instance.GetSamplerConfig(samplerName);
                sampler = config.BuildRsSampler();
                m_samplers.Add(samplerName, sampler);
            }

            return sampler;
        }

    }
}