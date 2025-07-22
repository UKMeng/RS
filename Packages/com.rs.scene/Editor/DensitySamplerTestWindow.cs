using System;
using RS.Scene.BiomeMap;
using RS.Utils;
using UnityEditor;
using UnityEngine;

namespace RS.Scene
{
    public class DensitySamplerTestWindow : EditorWindow
    {
        private Int64 m_seed = 20250715;
        private int m_width = 2048;
        private int m_height = 2048;

        private float[,] m_whiteNoise;

        private Texture2D m_texture;
        private BiomeMapGenerator m_bmg;

        [MenuItem("RS/Density Function Test")]
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
            
            // 显示Texture2D
            if (m_texture != null)
            {
                GUILayout.Label(m_texture, GUILayout.Width(512), GUILayout.Height(512));
            }


            EditorGUILayout.EndVertical();
        }
        
        // TODO: Sampler抽象
        // private Texture2D Sample()
        // {
        //     
        // }
    }
}