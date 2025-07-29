using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

using RS.Scene;
using RS.Scene.Biome;
using RS.Utils;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class RsDebug
    {
        [MenuItem("RSTest/Sampler Benchmark")]
        public static void SamplerBenchmark()
        {
            Debug.Log("Sample Benchmark");

            RsRandom.Init(20250715);
            RsSamplerManager.Reload();

            var samplerName = "InterTest";
            
            var sampler = RsConfigManager.Instance.GetSamplerConfig(samplerName).BuildRsSampler() as InterpolatedSampler;
            
            var sw = Stopwatch.StartNew();

            var batchSampleResult = sampler.SampleBatch(new Vector3(0, 0, 0));
            
            for (var sx = 0; sx < 32; sx++)
            {
                for (var sz = 0; sz < 32; sz++)
                {
                    for (var sy = 0; sy < 32; sy++)
                    {
                        // var sampleX = sx;
                        // var sampleY = sy;
                        // var sampleZ = sz;
                        // var density = sampler.Sample(new Vector3(sampleX, sampleY, sampleZ));
                        var density = batchSampleResult[sx, sy, sz];
                    }
                }
            }

            sw.Stop();
            Debug.Log($"{samplerName} Sampler Benchmark: {sw.ElapsedMilliseconds}ms");
        }
    }
}