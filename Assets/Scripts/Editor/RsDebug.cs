using System;

using UnityEditor;
using UnityEngine;

using RS.Scene;
using RS.Scene.Biome;
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

            var biomeTest = RsConfig.GetConfig("Biome/BiomeSource") as RsBiomeSourceConfig;

            var biomes = RsBiomeSourceConfig.ParseSourceConfig(biomeTest);
            // var tree = new BiomeSourceTree(biomeTest);
            
            Debug.Log("test");
        }
    }
}