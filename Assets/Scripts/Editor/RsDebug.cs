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


            var value = RsMath.ClampedMap(0f, -1, 1, 1, 0);
            Debug.Log(value);
            
            // var samplerConfig = RsConfig.GetConfig("Sampler/SingleArg") as RsSamplerConfig;
            // var sampler = samplerConfig.BuildRsSampler();
            //
            // var samplePoint = new Vector3(0, 0, 0);
            // Debug.Log(sampler.Sample(samplePoint));

        }
    }
}