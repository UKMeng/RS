using UnityEngine;

namespace RS.Utils
{
    public class BiomeSampler : RsSampler
    {
        private RsSampler m_continentalness;
        private RsSampler m_depth;
        private RsSampler m_erosion;
        private RsSampler m_humidity;
        private RsSampler m_temperature;
        private RsSampler m_ridges;

        public BiomeSampler(RsSampler continentalness, RsSampler depth, RsSampler erosion, RsSampler humidity,
            RsSampler temperature, RsSampler ridges)
        {
            m_continentalness = continentalness;
            m_depth = depth;
            m_erosion = erosion;
            m_humidity = humidity;
            m_temperature = temperature;
            m_ridges = ridges;
        }

        public override float Sample(Vector3 pos)
        {
            var continentalness = m_continentalness.Sample(pos);
            var depth = m_depth.Sample(pos);
            var erosion = m_erosion.Sample(pos);
            var humidity = m_humidity.Sample(pos);
            var temperature = m_temperature.Sample(pos);
            var ridges = m_ridges.Sample(pos);

            return 1.0f;
        }
    }
}