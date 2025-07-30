using System;
using System.Diagnostics;
using RS.Scene.Biome;
using RS.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public struct BiomeMapData
    {
        public BiomeType biome;
        public float[] values;
        public int x;
        public int z;
    }
    
    public class SamplerTestWindow : EditorWindow
    {
        private long m_seed = 20250715;
        
        // 采样起始位置
        private Vector3Int m_startPos = new Vector3Int(0, 100, 0);
        
        // 采样范围
        private int m_samplerWidth = 1024;
        private int m_samplerHeight = 1024;
        
        // 预览纹理分辨率
        private int m_width = 1024;
        private int m_height = 1024;
        
        // 预览视角 0 = side, 1 = top
        private byte m_previewMode = 0;
        private readonly string[] m_previewModeStrs = { "Side", "Top" };

        // 预览配置选择
        private string m_sampler = "Erosion";
        private string[] m_presetSamplerStrs;
        
        private Texture2D m_texture;
        private float[,] m_textureData;
        private Vector3 m_pickData;
        
        private Texture2D m_biomeMap;
        private BiomeMapData[,] m_biomeData;
        private BiomeMapData m_PickBiomeMapData;
        private bool m_showBiomeMap = false;

        private RsConfigManager m_configManager;

        [MenuItem("RS/重载Noise Manager")]
        private static void ReloadNoiseManager()
        {
            NoiseManager.Init(20250715);
        }
        
        [MenuItem("RS/Sampler Test")]
        private static void ShowWindow()
        {
            GetWindow(typeof(SamplerTestWindow), true, "Sampler Test");
        }

        private void OnEnable()
        {
            NoiseManager.Init(m_seed);
            
            m_presetSamplerStrs = RsConfigManager.Instance.GetLoadedSamplerConfigName();
            m_showBiomeMap = false;
        }

        private void OnGUI()
        {
            // GUI Styles
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = new Color32(131, 207, 240, 255);
            buttonStyle.fontSize = 12;
            buttonStyle.fixedHeight = 24;
            // style.fixedWidth = 200;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.margin = new RectOffset(5, 5, 5, 5);
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.margin = new RectOffset(5, 5, 5, 5);
            GUIStyle filedStyle = new GUIStyle(GUI.skin.textField);
            filedStyle.alignment = TextAnchor.MiddleCenter;
            filedStyle.margin = new RectOffset(5, 5, 5, 5);
            filedStyle.fixedHeight = 24;


            EditorGUILayout.BeginVertical();

            // 生成随机种子的按钮和显示
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Seed", labelStyle);
            m_seed = EditorGUILayout.LongField(m_seed, filedStyle);
            if (GUILayout.Button("Generate New Seed", buttonStyle))
            {
                m_seed = RsRandom.GetSeed();
                NoiseManager.Init(m_seed);
            }

            if (GUILayout.Button("Noise Manager Reload", buttonStyle))
            {
                NoiseManager.Init(m_seed);
            }
            EditorGUILayout.EndHorizontal();
            
            // 采样起始点
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("采样起始点", labelStyle, GUILayout.Width(60));
            m_startPos.x = EditorGUILayout.IntField(m_startPos.x, filedStyle);
            m_startPos.y = EditorGUILayout.IntField(m_startPos.y, filedStyle);
            m_startPos.z = EditorGUILayout.IntField(m_startPos.z, filedStyle);
            EditorGUILayout.EndHorizontal();
            
            // 调整采样范围
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("采样宽度", labelStyle, GUILayout.Width(60));
            m_samplerWidth = EditorGUILayout.IntField(m_samplerWidth, filedStyle);
            EditorGUILayout.LabelField("采样高度", labelStyle, GUILayout.Width(60));
            m_samplerHeight = EditorGUILayout.IntField(m_samplerHeight, filedStyle);
            EditorGUILayout.LabelField("预览视角", labelStyle, GUILayout.Width(60));
            m_previewMode = (byte)GUILayout.Toolbar(m_previewMode, m_previewModeStrs);
            EditorGUILayout.EndHorizontal();
            
            // 选择生成的噪声
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预览选择", labelStyle, GUILayout.Width(60));
            m_sampler = EditorGUILayout.TextField(m_sampler, filedStyle);
            if (EditorGUILayout.DropdownButton(new GUIContent("▼"), FocusType.Passive, GUILayout.Width(20)))
            {
                var menu = new GenericMenu();
                foreach (var preset in m_presetSamplerStrs)
                {
                    menu.AddItem(new GUIContent(preset), m_sampler == preset, () => m_sampler = preset);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            // 采样并生成纹理
            if (GUILayout.Button("Generate", buttonStyle))
            {
                var sampler = RsSamplerManager.Instance.GetOrCreateSampler(m_sampler);
                
                m_texture = Sample(sampler);
                m_biomeMap = null;
            }
            
            // 采样并生成Biome Map
            if (GUILayout.Button("Generate Biome Map", buttonStyle))
            {
                m_biomeMap = BiomeMapSample();
                m_texture = null;
            }
            
            // 显示BiomeMap
            if (m_biomeMap != null)
            {
                if (m_showBiomeMap == false)
                {
                    m_showBiomeMap = true;
                    m_PickBiomeMapData = m_biomeData[0, 0];
                }
                
                GUILayout.Label(m_biomeMap, GUILayout.Width(m_width), GUILayout.Height(m_height));
                
                // 鼠标取值
                var textureRect = GUILayoutUtility.GetLastRect();
                var mousePos = Event.current.mousePosition;
                if (textureRect.Contains(mousePos))
                {
                    var u = (mousePos.x - textureRect.x) / textureRect.width;
                    var v = (mousePos.y - textureRect.y) / textureRect.height;
                    var pu = Mathf.Clamp(Mathf.FloorToInt(u * m_samplerWidth), 0, m_samplerWidth - 1);
                    var pv = m_samplerHeight - Mathf.Clamp(Mathf.FloorToInt(v * m_samplerHeight), 0, m_samplerHeight - 1);

                    m_PickBiomeMapData = m_biomeData[pu, pv];
                }
            }

            if (m_showBiomeMap)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("X:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField((m_PickBiomeMapData.x).ToString("F1"), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.LabelField("Z:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField((m_PickBiomeMapData.z).ToString("F1"), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.LabelField("c:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_PickBiomeMapData.values[0].ToString("F3"), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.LabelField("e:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_PickBiomeMapData.values[2].ToString("F3"), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.LabelField("h:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_PickBiomeMapData.values[3].ToString("F3"), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.LabelField("t:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_PickBiomeMapData.values[4].ToString("F3"), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.LabelField("r:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_PickBiomeMapData.values[5].ToString("F3"), labelStyle, GUILayout.Width(60));

                EditorGUILayout.LabelField("pv:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_PickBiomeMapData.values[6].ToString("F3"), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.LabelField("biome:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_PickBiomeMapData.biome.ToString(), labelStyle, GUILayout.Width(60));
                
                EditorGUILayout.EndHorizontal();
                
                Repaint();
            }
            
            
            // 显示Texture2D
            if (m_texture != null)
            {
                GUILayout.Label(m_texture, GUILayout.Width(m_width), GUILayout.Height(m_height));
                
                // 鼠标取值
                var textureRect = GUILayoutUtility.GetLastRect();
                var mousePos = Event.current.mousePosition;
                if (textureRect.Contains(mousePos))
                {
                    var u = (mousePos.x - textureRect.x) / textureRect.width;
                    var v = (mousePos.y - textureRect.y) / textureRect.height;
                    var pu = Mathf.Clamp(Mathf.FloorToInt(u * m_samplerWidth), 0, m_samplerWidth - 1);
                    var pv = m_samplerHeight - Mathf.Clamp(Mathf.FloorToInt(v * m_samplerHeight), 0, m_samplerHeight - 1);

                    var value  = m_textureData[pu, pv];

                    m_pickData = new Vector3(pu, pv, value);
                }
            }
            

            if (m_pickData != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("X:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField((m_pickData.x + m_startPos.x).ToString("F1"), labelStyle, GUILayout.Width(60));
                
                if (m_previewMode == 0)
                {
                    EditorGUILayout.LabelField("Y:", labelStyle, GUILayout.Width(60));
                    EditorGUILayout.LabelField((m_pickData.y + m_startPos.y).ToString("F1"), labelStyle, GUILayout.Width(60));
                }
                else
                {
                    EditorGUILayout.LabelField("Z:", labelStyle, GUILayout.Width(60));
                    EditorGUILayout.LabelField((m_pickData.y + m_startPos.z).ToString("F1"), labelStyle, GUILayout.Width(60));
                }
                
                EditorGUILayout.LabelField("Value:", labelStyle, GUILayout.Width(60));
                EditorGUILayout.LabelField(m_pickData.z.ToString("F3"), labelStyle, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                
                Repaint();
            }
            
            

            EditorGUILayout.EndVertical();
        }
        
        private Texture2D Sample(RsSampler sampler)
        {
            var data = new float[m_samplerWidth, m_samplerHeight];

            var startX = m_startPos.x;
            var startY = m_startPos.y;
            var startZ = m_startPos.z;

            var sw = Stopwatch.StartNew();

            for (int x = 0; x < m_samplerWidth; x++)
            {
                if (m_previewMode == 0)
                {
                    for (int y = 0; y < m_samplerHeight; y++)
                    {
                        data[x, y] = sampler.Sample(new Vector3(startX + x, startY + y, startZ));
                    }
                }
                else
                {
                    for (int z = 0; z < m_samplerHeight; z++)
                    {
                        data[x, z] = sampler.Sample(new Vector3(startX + x, startY, startZ + z));
                    }
                }
            }

            sw.Stop();
            Debug.Log($"Sample Total Time {sw.ElapsedMilliseconds} ms");

            m_textureData = data;
            
            return RsJobs.GenerateTexture(data, m_width, m_height);
        }
        
        private Texture2D BiomeMapSample()
        {
            var data = new BiomeMapData[m_samplerWidth, m_samplerHeight];

            var startX = m_startPos.x;
            var startY = m_startPos.y;
            var startZ = m_startPos.z;


            var sw = Stopwatch.StartNew();
            
            for (int x = 0; x < m_samplerWidth; x++)
            {
                for (int z = 0; z < m_samplerHeight; z++)
                {
                    var pos = new Vector3(startX + x, startY, startZ + z);

                    var biome = NoiseManager.Instance.SampleBiome(pos, out var vals);
                    
                    data[x, z].x = startX + x;
                    data[x, z].z = startZ + z;
                    data[x, z].values = vals;
                    data[x, z].biome = biome;
                }
            }

            sw.Stop();
            Debug.Log($"Sample Total Time {sw.ElapsedMilliseconds} ms");

            m_biomeData = data;
            
            return GenerateBiomeMap(data, m_width, m_height);
        }
        
        private static Texture2D GenerateBiomeMap(BiomeMapData[,] data, int width, int height)
        {
            var sw = Stopwatch.StartNew();

            var dataWidth = data.GetLength(0);
            var dataHeight = data.GetLength(1);
            var colorArray = new Color[width * height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var srcX = Mathf.Clamp(Mathf.RoundToInt((float)x / width * dataWidth), 0, dataWidth - 1);
                    var srcY = Mathf.Clamp(Mathf.RoundToInt((float)y / height * dataHeight), 0, dataHeight - 1);
                    colorArray[x + y * width] = BiomeColor.Colors[(int)data[srcX, srcY].biome];
                }
            }
            
            var texture = new Texture2D(width, height);
            texture.SetPixels(colorArray);
            texture.Apply();

            sw.Stop();
            Debug.Log($"Texture generated in {sw.ElapsedMilliseconds} ms");
            return texture;
        }
    }
}