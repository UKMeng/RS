using UnityEngine;
using RS.Utils;

namespace RS.Scene.Sampler
{
    public class RsSampler
    {
        public float Sample(Vector3 pos)
        {
            return RsMath.ClampGradient(pos.y, 1000.0f, 1500.0f, 0.0f, 1.0f);
        }
    }
}