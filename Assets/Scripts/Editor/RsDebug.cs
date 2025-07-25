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
            // var rng = RsRandom.Init(20250715);
            //
            // var samplerConfig = RsConfigManager.Instance.GetSamplerConfig("Offset");
            // var sampler = samplerConfig.BuildRsSampler();
            //
            //
            // var value = sampler.Sample(new Vector3(564.0f, 0, 906.0f));
            var w = 4;
            var x = -1.0f;
            var value = ((x % w + w) % w) / w;
            Debug.Log(value);
            
            // var samplerConfig = RsConfig.GetConfig("Sampler/SingleArg") as RsSamplerConfig;
            // var sampler = samplerConfig.BuildRsSampler();
            //
            // var samplePoint = new Vector3(0, 0, 0);
            // Debug.Log(sampler.Sample(samplePoint));

        }
    }
}