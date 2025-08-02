// using System;
// using System.Collections.Generic;
// using RS.Utils;
// using UnityEngine;
//
// namespace RS.Scene.Biome
// {
//     public class BiomeMapGenerator
//     {
//         private RsRandom m_rng;
//         private Int64 m_seed;
//
//         private List<Layer> m_layers;
//
//         public BiomeMapGenerator(Int64 seed)
//         {
//             m_rng = new RsRandom(seed);
//             m_seed = seed;
//         }
//
//         public Texture2D Generate()
//         {
//             m_layers = new List<Layer>();
//             
//             // 先生成一张最大的IslandMap
//             var initIslandLayer = new IslandLayer(10, 10, 1024, m_rng);
//             m_layers.Add(initIslandLayer);
//             
//             // 放大一倍
//             var zoomLayer = new ZoomLayer(initIslandLayer, 2, m_rng);
//             m_layers.Add(zoomLayer);
//             
//             // Add Island
//             var addIslandLayer = new IslandLayer(zoomLayer);
//             addIslandLayer.AddIsland(m_rng);
//             m_layers.Add(addIslandLayer);
//             
//             // 再放大一倍
//             var zoomLayer2 = new ZoomLayer(addIslandLayer, 2, m_rng);
//             m_layers.Add(zoomLayer2);
//             
//             // Add Island 3次
//             var addIslandLayer2 = new IslandLayer(zoomLayer2);
//             addIslandLayer2.AddIsland(m_rng);
//             m_layers.Add(addIslandLayer2);
//
//             var addIslandLayer3 = new IslandLayer(addIslandLayer2);
//             addIslandLayer3.AddIsland(m_rng);
//             m_layers.Add(addIslandLayer3);
//
//             var addIslandLayer4 = new IslandLayer(addIslandLayer3);
//             addIslandLayer4.AddIsland(m_rng);
//             m_layers.Add(addIslandLayer4);
//             
//             // 添加温度
//             var temperatureLayer = new TemperatureLayer(addIslandLayer4, m_rng);
//             m_layers.Add(temperatureLayer);
//             
//             // Add Island 这是不再是land，需要修改AddIsland的规则
//             var addIslandLayer5 = new IslandLayer(temperatureLayer);
//             addIslandLayer5.AddIsland(m_rng);
//             m_layers.Add(addIslandLayer5);
//             
//             // WarmToTemperate
//             var warmToTemperateLayer = new TemperatureLayer(addIslandLayer5);
//             warmToTemperateLayer.WarmToTemperate();
//             m_layers.Add(warmToTemperateLayer);
//             
//             // FreezingToCold
//             var freezingToColdLayer = new TemperatureLayer(warmToTemperateLayer);
//             freezingToColdLayer.FreezingToCold();
//             m_layers.Add(freezingToColdLayer);
//             
//             // 256->128
//             var zoomLayer3 = new ZoomLayer(freezingToColdLayer, 2, m_rng);
//             m_layers.Add(zoomLayer3);
//
//             var layerCount = m_layers.Count;
//             var colors = m_layers[layerCount - 1].GetColors();
//             var texture = new Texture2D(10240, 10240);
//             texture.SetPixels(colors);
//             texture.Apply();
//
//             return texture;
//         }
//
//         public Texture2D ShowStageMap(int stage)
//         {
//             if (stage >= m_layers.Count)
//             {
//                 Debug.LogError($"Stage {stage} is out of range");
//             }
//             
//             var layer = m_layers[stage];
//             var colors = layer.GetColors();
//             var texture = new Texture2D(10240, 10240);
//             texture.SetPixels(colors);
//             texture.Apply();
//             
//             return texture;
//         }
//     }
// }