using RS.Scene.Sampler;

namespace RS.Utils
{
    public struct RsSplineNode
    {
        public float location;
        public float derivative;
        public float value;
        public RsSampler sampler;
        public byte type; // 0 = value, 1 = sampler

        public RsSplineNode(float location, float derivative, float value)
        {
            this.location = location;
            this.derivative = derivative;
            this.value = value;
            this.sampler = null;
            this.type = 0;
        }

        public RsSplineNode(float location, float derivative, RsSampler sampler)
        {
            this.location = location;
            this.derivative = derivative;
            this.value = 0;
            this.sampler = sampler;
            this.type = 1;
        }
    }
    
    public class RsSpline
    {
        private RsSplineNode[] m_nodes;

        public RsSpline(RsSplineNode[] nodes)
        {
            m_nodes = nodes;
        }

        // public float GetValue()
        // {
        //     
        // }
    }
}