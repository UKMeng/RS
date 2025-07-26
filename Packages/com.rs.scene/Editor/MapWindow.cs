using System;
using System.Diagnostics;
using RS.Scene;
using UnityEditor;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

using RS.Utils;

using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public class MapWindow : EditorWindow
    {
        private Int64 m_seed = 20250715;
        private int m_width = 2048;
        private int m_height = 2048;

        private float[,] m_whiteNoise;

        private Texture2D m_texture;
        // private BiomeMapGenerator m_bmg;

        [MenuItem("RS/Map")]
        private static void ShowWindow()
        {
            GetWindow(typeof(MapWindow), true, "Map");
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

            // 将数据转换成Texture2D显示
            if (GUILayout.Button("Generate New White Noise", buttonStyle))
            {
                m_whiteNoise = RsNoise.GenerateWhiteNoise(m_width, m_height, m_seed);
                m_texture = RsJobs.GenerateTexture(m_whiteNoise, m_width, m_height);
            }
            
            // 测试3D Simplex Noise
            // if (GUILayout.Button("Test Simplex Noise", buttonStyle))
            // {
            //     m_whiteNoise = RsNoise.GenerateSimplexNoise(m_width, m_height, m_seed);
            //     m_texture = RsJobs.GenerateTexture(m_whiteNoise, m_width, m_height);
            // }
            
            // 测试Perlin Noise
            if (GUILayout.Button("Test Perlin Noise", buttonStyle))
            {
                m_whiteNoise = RsNoise.GeneratePerlinNoise(m_width, m_height, m_seed);
                m_texture = RsJobs.GenerateTexture(m_whiteNoise, m_width, m_height);
            }

            // if (GUILayout.Button("Generate Biome Map", buttonStyle))
            // {
            //     m_bmg = new BiomeMapGenerator(m_seed);
            //     m_texture = m_bmg.Generate();
            // }
            //
            // // 各阶段BiomeMap显示按钮
            // if (m_bmg != null)
            // {
            //     EditorGUILayout.BeginHorizontal();
            //     if (GUILayout.Button("Init Island", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(0);
            //     }
            //     
            //     if (GUILayout.Button("Zoom 1024->512", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(1);
            //     }
            //     
            //     if (GUILayout.Button("Add Island 1", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(2);
            //     }
            //     EditorGUILayout.EndHorizontal();
            //     
            //     EditorGUILayout.BeginHorizontal();
            //     if (GUILayout.Button("Zoom 512->256", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(3);
            //     }
            //     
            //     if (GUILayout.Button("Add Island 2", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(4);
            //     }
            //     
            //     if (GUILayout.Button("Add Island 3", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(5);
            //     }
            //     
            //     if (GUILayout.Button("Add Island 4", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(6);
            //     }
            //     EditorGUILayout.EndHorizontal();
            //     
            //     EditorGUILayout.BeginHorizontal();
            //     if (GUILayout.Button("Temperature", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(7);
            //     }
            //     
            //     if (GUILayout.Button("Add Island 5", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(8);
            //     }
            //     
            //     if (GUILayout.Button("Warm To Temperate", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(9);
            //     }
            //     
            //     if (GUILayout.Button("Freezing To Cold", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(10);
            //     }
            //     EditorGUILayout.EndHorizontal();
            //     
            //     EditorGUILayout.BeginHorizontal();
            //     if (GUILayout.Button("256->128", buttonStyle))
            //     {
            //         m_texture = m_bmg.ShowStageMap(11);
            //     }
            //     EditorGUILayout.EndHorizontal();
            // }

            // 显示Texture2D
            if (m_texture != null)
            {
                GUILayout.Label(m_texture, GUILayout.Width(512), GUILayout.Height(512));
            }


            EditorGUILayout.EndVertical();
        }
    }
}