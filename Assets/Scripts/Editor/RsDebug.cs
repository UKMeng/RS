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

            var locations = new float[] { 0, 1, 2 };
            var derivatives = new float[] { 0, 2, 4 };
            var values = new RsSampler[] { new ConstantSampler(0), new ConstantSampler(1), new ConstantSampler(4) };
            var coordinates = new XSampler();
            
            var spline = new SplineSampler(coordinates, locations, derivatives, values);

            var t = spline.Sample(new Vector3(1.5f, 0, 0));
            
            Debug.Log(t);
        }
    }
}