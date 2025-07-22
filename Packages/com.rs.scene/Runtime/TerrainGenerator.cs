using System.Diagnostics;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class TerrainGenerator
    {
        /// <summary>
        /// 柏林噪声生成地形
        /// </summary>
        /// <param name="width">宽</param>
        /// <param name="height">长</param>
        /// <param name="scale">缩放尺寸</param>
        /// <param name="seed">随机种子</param>
        /// <param name="octaves">噪声层数</param>
        /// <param name="topLimit">海拔上限</param>
        /// <param name="exponent">海拔指数</param>
        /// <param name="fudgeFactor">海拔因子</param>
        /// <param name="islandMix">岛屿混合</param>
        /// <returns></returns>
        public static Mesh GenerateTerrain(int width, int height, float scale, int seed, int octaves, 
            float topLimit, float exponent, float fudgeFactor, float islandMix)
        {
            var sw = Stopwatch.StartNew();

            var rand = new System.Random(seed);
            var offsetX = rand.Next(-100000, 100000);
            var offsetZ = rand.Next(-100000, 100000);
            
            var mesh = new Mesh();
            var vertices = new Vector3[(width + 1) * (height + 1)];
            var triangles = new int[width * height * 6];
            
            // 构建Vertices
            var index = 0;
            var frequencyZ = scale / height;
            var frequencyX = scale / width;
            for (var z = 0; z < height + 1; z++)
            {
                for (var x = 0; x < width + 1; x++)
                {
                    var fz = frequencyZ;
                    var fx = frequencyX;
                    var amplitude = 1.0f;
                    var y = 0.0f;
                    
                    // amplitude总和，用于归一化
                    var totalAmplitude = 0.0f;

                    for (var i = 0; i < octaves; i++)
                    {
                        var px = x * fx + offsetX;
                        var pz = z * fz + offsetZ;
                        y += Mathf.PerlinNoise(px, pz) * amplitude;
                        
                        totalAmplitude += amplitude;
                        fx *= 2;
                        fz *= 2;
                        amplitude *= 0.5f;
                    }
                    
                    // 归一化
                    y /= totalAmplitude;
                    
                    // 取幂指数让地形更平坦
                    y = Mathf.Pow(y * fudgeFactor, exponent);
                    
                    // 计算与地图中心的距离，形成海岛
                    var nx = 2.0f * x / width - 1;
                    var nz = 2.0f * z / height - 1;
                    var distance = 1 - (1 - nx * nx) * (1 - nz * nz);
                    
                    // 距离中心时，应该是海拔比较高的位置
                    y = Mathf.Lerp(y, 1 - distance, islandMix);

                    // 以0.5米为间隔取间隔值，就能做出平台了，这一步到后续填充砖块的时候再做就行
                    // y = Mathf.Round(y * topLimit * 2) * 0.5f;
                    
                    y *= topLimit;
                    
                    vertices[index++] = new Vector3(x, y, z);
                }
            }
            
            // 构建Triangles
            // Unity顺时针是正面
            var triIndex = 0;
            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var verIndex = z * (width + 1) + x;
                    
                    triangles[triIndex++] = verIndex;
                    triangles[triIndex++] = verIndex + width + 1;
                    triangles[triIndex++] = verIndex + 1;
                    
                    triangles[triIndex++] = verIndex + 1;
                    triangles[triIndex++] = verIndex + width + 1;
                    triangles[triIndex++] = verIndex + width + 2;
                }
            }
            

            mesh.name = "TerrainData";
            mesh.vertices = vertices;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            sw.Stop();
            
            Debug.Log($"Terrain generated in {sw.ElapsedMilliseconds} ms");
            
            return mesh;
        }
    }
}