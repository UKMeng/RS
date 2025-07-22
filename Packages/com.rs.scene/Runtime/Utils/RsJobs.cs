using System.Diagnostics;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

namespace RS.Utils
{
    [BurstCompile]
    public struct GenerateTextureJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> data;
        [ReadOnly] public NativeArray<Color> viridisLUT;
        [WriteOnly] public NativeArray<Color> colors;
        public int width;
        public int blockSize;
        public float maxValue;
        public float minValue;

        public void Execute(int index)
        {
            var x = index % width;
            var y = index / width;
            var v = data[x + y * width];
            
            v = RsMath.ClampGradient(v, minValue, maxValue,0.0f, 1.0f);
            
            colors[index] = Viridis(v);
        }

        private Color Viridis(float v)
        {
            var index = Mathf.FloorToInt(v * 255.0f);
            return viridisLUT[index];
        }
    }

    public class RsJobs
    {
        public static Texture2D GenerateTexture(float[,] data, int width, int height, int blockSize = 1)
        {
            var sw = Stopwatch.StartNew();
            
            // 使用JobSystem
            var dataArray = new NativeArray<float>(width * height, Allocator.TempJob);
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    dataArray[x + y * width] = data[x, y];
                }
            }
            
            var viridisLUT = new NativeArray<Color>(256, Allocator.TempJob);
            for (var index = 0; index < 256; index++)
            {
                viridisLUT[index] = RsColor.ViridisLUT[index];
            }
            
            var colorArray = new NativeArray<Color>(width * height, Allocator.TempJob);
            
            var job = new GenerateTextureJob
            {
                data = dataArray,
                colors = colorArray,
                width = width,
                blockSize = blockSize,
                maxValue = 1.0f,
                minValue = -1.0f,
                viridisLUT = viridisLUT,
            };
            
            var handle = job.Schedule(width * height, 32);
            handle.Complete();
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(colorArray.ToArray());
            texture.Apply();
            
            dataArray.Dispose();
            colorArray.Dispose();
            viridisLUT.Dispose();

            sw.Stop();
            Debug.Log($"Texture generated in {sw.ElapsedMilliseconds} ms");
            return texture;
        }
    }
}