using System;
using System.Diagnostics;
using RS.Scene.Biome;
using RS.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class SamplerTestWindow : EditorWindow
    {
        private Int64 m_seed = 1882775509054175955;
        
        // Biome类型对应颜色数组
        private static readonly Color[] BiomeColors =
        {
            RsColor.Ocean,
            RsColor.River,
            RsColor.Plains
        };
        
        // 采样起始位置
        private Vector3 m_startPos = new Vector3(0.0f, 100.0f, 0.0f);
        
        // 采样范围
        private int m_samplerWidth = 512;
        private int m_samplerHeight = 512;
        
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

        private RsConfigManager m_configManager;

        [MenuItem("RS/重载Config Manager")]
        private static void ReloadConfigManager()
        {
            RsConfigManager.Reload();
        }
        
        [MenuItem("RS/Sampler Test")]
        private static void ShowWindow()
        {
            GetWindow(typeof(SamplerTestWindow), true, "Sampler Test");
        }

        private void OnEnable()
        {
            m_configManager = RsConfigManager.Instance;
            m_presetSamplerStrs = m_configManager.GetLoadedSamplerConfigName();
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
            }
            EditorGUILayout.EndHorizontal();
            
            // 采样起始点
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("采样起始点", labelStyle, GUILayout.Width(60));
            m_startPos.x = EditorGUILayout.FloatField(m_startPos.x, filedStyle);
            m_startPos.y = EditorGUILayout.FloatField(m_startPos.y, filedStyle);
            m_startPos.z = EditorGUILayout.FloatField(m_startPos.z, filedStyle);
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
                // 重置随机数生成器，保持生成结果确定
                var rng = RsRandom.Init(m_seed);

                var samplerConfig = m_configManager.GetSamplerConfig(m_sampler);
                var sampler = samplerConfig.BuildRsSampler();
                
                m_texture = Sample(sampler);
                m_biomeMap = null;
            }
            
            // 采样并生成Biome Map
            if (GUILayout.Button("Generate Biome Map", buttonStyle))
            {
                // 重置随机数生成器，保持生成结果确定
                var rng = RsRandom.Init(m_seed);

                var biomeSampler = new BiomeSampler();

                m_biomeMap = BiomeMapSample(biomeSampler);
                m_texture = null;
            }
            
            // 显示BiomeMap
            if (m_biomeMap != null)
            {
                GUILayout.Label(m_biomeMap, GUILayout.Width(m_width), GUILayout.Height(m_height));
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
        
        private Texture2D BiomeMapSample(BiomeSampler sampler)
        {
            var data = new BiomeType[m_samplerWidth, m_samplerHeight];

            var startX = m_startPos.x;
            var startY = m_startPos.y;
            var startZ = m_startPos.z;


            var sw = Stopwatch.StartNew();

            for (int x = 0; x < m_samplerWidth; x++)
            {
                for (int z = 0; z < m_samplerHeight; z++)
                {
                    data[x, z] = sampler.Sample(new Vector3(startX + x, startY, startZ + z));
                }
            }

            sw.Stop();
            Debug.Log($"Sample Total Time {sw.ElapsedMilliseconds} ms");

            // m_textureData = data;
            
            return GenerateBiomeMap(data, m_width, m_height);
        }
        
        private static Texture2D GenerateBiomeMap(BiomeType[,] data, int width, int height)
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
                    colorArray[x + y * width] = BiomeColors[(int)data[srcX, srcY]];
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