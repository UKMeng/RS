using System;
using System.Diagnostics;
using RS.Scene.BiomeMap;
using RS.Utils;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class SamplerTestWindow : EditorWindow
    {
        private Int64 m_seed = 20250715;
        
        // 采样起始位置
        private Vector3 m_startPos = new Vector3(0.0f, 0.0f, 0.0f);
        
        // 采样范围
        private int m_samplerWidth = 2048;
        private int m_samplerHeight = 2048;
        
        // 预览纹理分辨率
        private int m_width = 2048;
        private int m_height = 2048;
        
        // 预览视角 0 = side, 1 = top
        private byte m_previewMode = 0;
        private readonly string[] m_previewModeStrs = { "Side", "Top" };

        // 预览噪声 0 = Continentalness, 1 = Erosion, 2 = Peak & Valleys, 3 = Temperature, 4 = Humidity
        private byte m_samplerMode = 0;
        private readonly string[] m_samplerModeStrs =
            { "Continentalness", "Erosion", "Peaks & Valleys", "Temperature", "Humidity", "Offset" };
        
        private Texture2D m_texture;

        private RsConfigManager m_configManager;

        [MenuItem("RS/Sampler Test")]
        private static void ShowWindow()
        {
            GetWindow(typeof(SamplerTestWindow), true, "Sampler Test");
        }

        private void OnEnable()
        {
            m_configManager = RsConfigManager.Instance;
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
            EditorGUILayout.LabelField("预览噪声", labelStyle, GUILayout.Width(60));
            m_samplerMode = (byte)GUILayout.Toolbar(m_samplerMode, m_samplerModeStrs);
            EditorGUILayout.EndHorizontal();

            // 采样并生成纹理
            if (GUILayout.Button("Generate", buttonStyle))
            {
                var rng = new RsRandom(m_seed);
                
                // offset
                var offsetNoiseConfig = m_configManager.GetNoiseConfig("Offset");
                // shiftX
                var offsetNoise1 = new RsNoise(rng.NextUInt64(), offsetNoiseConfig.amplitudes, offsetNoiseConfig.firstOctave);
                var shiftXSampler = new FlatCacheSampler(new Cache2DSampler(new ShiftASampler(offsetNoise1)));
                
                // shiftX
                var offsetNoise2 = new RsNoise(rng.NextUInt64(), offsetNoiseConfig.amplitudes, offsetNoiseConfig.firstOctave);
                var shiftZSampler = new FlatCacheSampler(new Cache2DSampler(new ShiftBSampler(offsetNoise2)));
                
                // erosion
                var erosionNoiseConfig = m_configManager.GetNoiseConfig("Erosion");
                var erosionNoise = new RsNoise(rng.NextUInt64(), erosionNoiseConfig.amplitudes, erosionNoiseConfig.firstOctave);
                var erosionSampler = new FlatCacheSampler(new ShiftedNoiseSampler(erosionNoise, shiftXSampler,
                    new ConstantSampler(0.0f), shiftZSampler, 0.25f, 0.0f));
                    
                // Temperature
                var tempConfig = m_configManager.GetNoiseConfig("Temperature");
                var temperatureNoise = new RsNoise(rng.NextUInt64(), tempConfig.amplitudes, tempConfig.firstOctave);
                var temperatureSampler = new RsSampler(temperatureNoise);
                
                // Humidity
                var humidityConfig = m_configManager.GetNoiseConfig("Humidity");
                var humidityNoise = new RsNoise(rng.NextUInt64(), humidityConfig.amplitudes, humidityConfig.firstOctave);
                var humiditySampler = new RsSampler(humidityNoise);
                
                // ridges
                var ridgeConfig = m_configManager.GetNoiseConfig("Ridge");
                var ridgeNoise = new RsNoise(rng.NextUInt64(), ridgeConfig.amplitudes, ridgeConfig.firstOctave);

                var ridgesSampler = new FlatCacheSampler(new ShiftedNoiseSampler(ridgeNoise, shiftXSampler,
                    new ConstantSampler(0.0f), shiftZSampler, 0.25f, 0.0f));
                var ridgesFoldedSampler = new MulSampler(new ConstantSampler(-3.0f),
                    new AddSampler(new ConstantSampler(-0.33333f),
                        new AbsSampler(new AddSampler(new ConstantSampler(-0.66666f), new AbsSampler(ridgesSampler)))));
                
                // Continentalness
                var contiConfig = m_configManager.GetNoiseConfig("Continentalness");
                var continentNoise = new RsNoise(rng.NextUInt64(), contiConfig.amplitudes, contiConfig.firstOctave);
                var continentsSampler = new FlatCacheSampler(new ShiftedNoiseSampler(continentNoise, shiftXSampler,
                    new ConstantSampler(0.0f), shiftZSampler, 0.25f, 0.0f));
                
                // Depth Offset
                var loc1 = new float[] {-1.0f, -0.4f, 0.0f, 0.4f, 1.0f };
                var der1 = new float[] { 0.5f, 0.0f, 0.0f, 0.0f, 0.007f };
                var val1 = new RsSampler[]
                {
                    new ConstantSampler(-0.3f),
                    new ConstantSampler(0.05f),
                    new ConstantSampler(0.05f),
                    new ConstantSampler(0.05f),
                    new ConstantSampler(0.06f)
                };
                var spline = new SplineSampler(ridgesFoldedSampler, loc1, der1, val1);
                
                switch (m_samplerMode)
                {
                    case 0:
                    {
                        m_texture = Sample(continentsSampler);
                        break;
                    }
                    case 1:
                    {
                        m_texture = Sample(erosionSampler);
                        break;
                    }
                    case 2:
                    {
                        m_texture = Sample(ridgesFoldedSampler);
                        break;
                    }
                    case 3:
                    {
                        m_texture = Sample(temperatureSampler);
                        break;
                    }
                    case 4:
                    {
                        m_texture = Sample(humiditySampler);
                        break;
                    }
                    case 5:
                    {
                        m_texture = Sample(spline);
                        break;
                    }
                }
                // m_texture = Sample(continentsSampler);
            }
            
            
            
            // 显示Texture2D
            if (m_texture != null)
            {
                GUILayout.Label(m_texture, GUILayout.Width(512), GUILayout.Height(512));
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
            
            return RsJobs.GenerateTexture(data, m_width, m_height);
        }
    }
}