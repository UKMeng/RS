// using System.Diagnostics;
// using RS.Utils;
// using Unity.Collections;
// using UnityEngine;
// using Debug = UnityEngine.Debug;
//
// namespace RS.Scene.Biome
// {
//     /// <summary>
//     /// BiomeMap Base Layer
//     /// </summary>
//     public class Layer
//     {
//         // TODO: 是不是可能不需要这么多层的子类
//         
//         // 类型对应颜色数组
//         private static readonly Color[] BiomeColors =
//         {
//             RsColor.Ocean,
//             RsColor.Land,
//             RsColor.Warm,
//             RsColor.Temperate,
//             RsColor.Cold,
//             RsColor.Freezing
//         };
//         
//         protected int m_width;
//         protected int m_height;
//         protected BiomeType[,] m_data;
//
//         // layer一个像素代表方块的尺寸（方块数 = size * size)
//         protected int m_blockSize;
//
//         public int Width => m_width;
//         public int Height => m_height;
//         public int BlockSize => m_blockSize;
//         
//         public BiomeType[,] Data => m_data;
//         
//         protected Layer(int width, int height, int blockSize)
//         {
//             m_width = width;
//             m_height = height;
//             m_blockSize = blockSize;
//             m_data = new BiomeType[width, height];
//         }
//
//         protected Layer(Layer other)
//         {
//             m_width = other.m_width;
//             m_height = other.m_height;
//             m_blockSize = other.m_blockSize;
//             m_data = new BiomeType[m_width, m_height];
//             for (var x = 0; x < m_width; x++)
//             {
//                 for (var y = 0; y < m_height; y++)
//                 {
//                     m_data[x, y] = other.m_data[x, y];
//                 }
//             }
//         }
//
//         public BiomeType[,] GetExtendedData()
//         {
//             var extendedData = new BiomeType[m_width * m_blockSize, m_height * m_blockSize];
//             for (var x = 0; x < m_width; x++)
//             {
//                 for (var y = 0; y < m_height; y++)
//                 {
//                     for (var bx = 0; bx < m_blockSize; bx++)
//                     {
//                         for (var by = 0; by < m_blockSize; by++)
//                         {
//                             extendedData[x * m_blockSize + bx, y * m_blockSize + by] = m_data[x, y];
//                         }
//                     }
//                 }
//             }
//
//             return extendedData;
//         }
//
//         public Color[] GetColors()
//         {
//             // TODO: 待优化 3s
//             var sw = Stopwatch.StartNew();
//             
//             var width = m_width * m_blockSize;
//             var height = m_height * m_blockSize;
//             
//             var colors = new Color[width * height];
//             for (var x = 0; x < m_width; x++)
//             {
//                 for (var y = 0; y < m_height; y++)
//                 {
//                     for (var bx = 0; bx < m_blockSize; bx++)
//                     {
//                         for (var by = 0; by < m_blockSize; by++)
//                         {
//                             var index = width * (y * m_blockSize + by) + x * m_blockSize + bx;
//                             colors[index] = BiomeColors[(int)m_data[x, y]];
//                         }
//                     }
//                 }
//             }
//
//             sw.Stop();
//             Debug.Log($"IslandLayer.GetColor: {sw.ElapsedMilliseconds}ms");
//
//             return colors;
//         }
//     }
// }