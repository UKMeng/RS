using System.Collections.Generic;

namespace RS.Utils
{
    public class RsSamplerManager
    {
        private static RsSamplerManager m_instance;

        public static RsSamplerManager Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = new RsSamplerManager();
                }
                return m_instance;
            }
        }

        public static void Reload()
        {
            m_instance = new RsSamplerManager();
        }

        private Dictionary<string, RsSampler> m_samplers;

        public RsSamplerManager()
        {
            m_samplers = new Dictionary<string, RsSampler>();
        }

        public RsSampler GetOrCreateSampler(string samplerName)
        {
            if (!m_samplers.TryGetValue(samplerName, out var sampler))
            {
                var config = RsConfigManager.Instance.GetSamplerConfig(samplerName);
                sampler = config.BuildRsSampler();
                m_samplers.Add(samplerName, sampler);
            }

            return sampler;
        }

    }
}