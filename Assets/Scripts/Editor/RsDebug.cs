using System;

using UnityEditor;
using UnityEngine;

using RS.Scene;
using RS.Utils;

namespace RS.Scene
{
    public class RsDebug
    {
        [MenuItem("RS/Test")]
        public static void Test()
        {
            // 测试下Spline
            Debug.Log("Test");
            
            var samplerConfig = RsConfig.GetConfig("Sampler/AddTest") as RsSamplerConfig;
            var sampler = samplerConfig.BuildRsSampler();
            
            var samplePoint = new Vector3(0, 0, 0);
            Debug.Log(sampler.Sample(samplePoint));

        }
    }
}