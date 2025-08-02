// using System.Collections.Generic;
//
// using RS.Utils;
//
// namespace RS.Scene.Biome
// {
//     /// <summary>
//     /// 从父层进行放大
//     /// </summary>
//     public class ZoomLayer : Layer
//     {
//         public ZoomLayer(Layer parent, int zoom, RsRandom rng)
//             : base(parent.Width * zoom, parent.Height * zoom, parent.BlockSize / zoom)
//         {
//             // 从父层复制数据并放大，并非完美放大，而是带点变化
//             // 先实现完美复制
//             var parentData = parent.Data;
//             
//             for (var x = 0; x < Width; x++)
//             {
//                 for (var y = 0; y < Height; y++)
//                 {
//                     m_data[x, y] = parentData[x / zoom, y / zoom];
//                 }
//             }
//             
//             // 添加些许变化，当陆海边界时，会有10%的概率变成对方
//             for (var x = 0; x < Width; x++)
//             {
//                 for (var y = 0; y < Height; y++)
//                 {
//                     // 边界不触发变换
//                     if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
//                     {
//                         continue;
//                     }
//                     
//                     var dirs = new List<int[]>()  {
//                         new int[] { 0, 1 },
//                         new int[] { 0, -1 },
//                         new int[] { 1, 0 },
//                         new int[] { -1, 0 }
//                     };
//
//                     var value = m_data[x, y];
//
//                     var diff = new List<BiomeType>();
//
//                     foreach (var dir in dirs)
//                     {
//                         var nx = x + dir[0];
//                         var ny = y + dir[1];
//
//                         if (m_data[nx, ny] != value)
//                         {
//                             diff.Add(m_data[nx, ny]);
//                         }
//                     }
//
//                     if (diff.Count > 0)
//                     {
//                         var p = rng.NextFloat();
//                         if (p < 0.1)
//                         {
//                             // TODO: 增加互斥判断
//                             var randomIndex = rng.NextInt(0, diff.Count);
//                             m_data[x, y] = diff[randomIndex];
//                         }
//                     }
//                 }
//             }
//         }
//     }
// }