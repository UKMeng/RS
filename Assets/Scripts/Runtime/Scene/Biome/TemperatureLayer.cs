// using System.Collections.Generic;
// using RS.Utils;
//
// namespace RS.Scene.Biome
// {
//     public class TemperatureLayer : Layer
//     {
//         public TemperatureLayer(Layer parent, RsRandom rng): base(parent)
//         {
//             // 以4:1:1的比例随机分配Land转变成Warm，Cold，Freezing
//             for (var x = 0; x < m_width; x++)
//             {
//                 for (var y = 0; y < m_height; y++)
//                 {
//                     if (m_data[x, y] == BiomeType.Land)
//                     {
//                         var rand = rng.NextInt(0, 6);
//
//                         if (rand == 0)
//                         {
//                             m_data[x, y] = BiomeType.Freezing;
//                         }
//                         else if (rand == 1)
//                         {
//                             m_data[x, y] = BiomeType.Cold;
//                         }
//                         else
//                         {
//                             m_data[x, y] = BiomeType.Warm;
//                         }
//                     }
//                 }
//             }
//         }
//
//         public TemperatureLayer(Layer parent) : base(parent)
//         {
//             
//         }
//
//         /// <summary>
//         /// 与Cold或Freezing相邻的Warm变成Temperate
//         /// </summary>
//         public void WarmToTemperate()
//         {
//             for (var x = 0; x < m_width; x++)
//             {
//                 for (var y = 0; y < m_height; y++)
//                 {
//                     if (m_data[x, y] == BiomeType.Warm)
//                     {
//                         var dirs = new List<int[]>()  {
//                             new int[] { 0, 1 },
//                             new int[] { 0, -1 },
//                             new int[] { 1, 0 },
//                             new int[] { -1, 0 }
//                         };
//                         
//                         foreach (var dir in dirs)
//                         {
//                             var nx = x + dir[0];
//                             var ny = y + dir[1];
//
//                             if (nx < 0 || nx >= m_width || ny < 0 || ny >= m_height)
//                             {
//                                 continue;
//                             }
//                             
//                             var neighbor = m_data[nx, ny];
//                             
//                             if (neighbor == BiomeType.Cold || neighbor == BiomeType.Freezing)
//                             {
//                                 m_data[x, y] = BiomeType.Temperate;
//                                 break;
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//
//         /// <summary>
//         /// 与Warm或Temperate相邻的Freezing变成Cold
//         /// </summary>
//         public void FreezingToCold()
//         {
//             for (var x = 0; x < m_width; x++)
//             {
//                 for (var y = 0; y < m_height; y++)
//                 {
//                     if (m_data[x, y] == BiomeType.Freezing)
//                     {
//                         var dirs = new List<int[]>()  {
//                             new int[] { 0, 1 },
//                             new int[] { 0, -1 },
//                             new int[] { 1, 0 },
//                             new int[] { -1, 0 }
//                         };
//                         
//                         foreach (var dir in dirs)
//                         {
//                             var nx = x + dir[0];
//                             var ny = y + dir[1];
//
//                             if (nx < 0 || nx >= m_width || ny < 0 || ny >= m_height)
//                             {
//                                 continue;
//                             }
//                             
//                             var neighbor = m_data[nx, ny];
//                             
//                             if (neighbor == BiomeType.Warm || neighbor == BiomeType.Temperate)
//                             {
//                                 m_data[x, y] = BiomeType.Cold;
//                                 break;
//                             }
//                         }
//                     }
//                 }
//             }
//         }
//     }
// }