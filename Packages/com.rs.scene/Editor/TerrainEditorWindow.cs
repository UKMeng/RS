using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RS.Scene
{
    public class TerrainEditorWindow : EditorWindow
    {
        [MenuItem("RS/Terrain Editor")]
        private static void ShowWindow()
        {
            GetWindow(typeof(TerrainEditorWindow), true, "Terrain Editor");
        }

        private GameObject m_terrain;
        private int m_width = 50;
        private int m_height = 50;
        private int m_seed = 20250715;
        private int m_octaves = 3;
        
        private float m_scale = 20.0f;
        private float m_topLimit = 10.0f;
        private float m_exponent = 4.5f;
        private float m_fudgeFactor = 1.2f;
        private float m_islandMix = 0.5f;
        
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
            
            // Terrain物件
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Terrain", labelStyle);
            m_terrain = EditorGUILayout.ObjectField(m_terrain, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndHorizontal();
            
            // Terrain与噪声参数
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("宽", labelStyle, GUILayout.Width(40));
            m_width = EditorGUILayout.IntField(m_width, filedStyle);
            EditorGUILayout.LabelField("长", labelStyle, GUILayout.Width(40));
            m_height = EditorGUILayout.IntField(m_height, filedStyle);
            EditorGUILayout.LabelField("缩放", labelStyle, GUILayout.Width(40));
            m_scale = EditorGUILayout.FloatField(m_scale, filedStyle);
            EditorGUILayout.LabelField("随机种子", labelStyle, GUILayout.Width(60));
            m_seed = EditorGUILayout.IntField(m_seed, filedStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("噪声层数", labelStyle, GUILayout.Width(60));
            m_octaves = EditorGUILayout.IntField(m_octaves, filedStyle);
            EditorGUILayout.LabelField("海拔上限", labelStyle, GUILayout.Width(60));
            m_topLimit = EditorGUILayout.FloatField(m_topLimit, filedStyle);
            EditorGUILayout.LabelField("海拔指数", labelStyle, GUILayout.Width(60));
            m_exponent = EditorGUILayout.FloatField(m_exponent, filedStyle);
            EditorGUILayout.LabelField("海拔因子", labelStyle, GUILayout.Width(60));
            m_fudgeFactor = EditorGUILayout.FloatField(m_fudgeFactor, filedStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("岛屿混合", labelStyle, GUILayout.Width(60));
            m_islandMix = EditorGUILayout.FloatField(m_islandMix, filedStyle);
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("生成地形", buttonStyle))
            {
                if (m_terrain != null)
                {
                    GenerateTerrainTest();
                }
            }
        }
        
        
        private void GenerateTerrainTest()
        {
            var mesh = TerrainGenerator.GenerateTerrain(m_width, m_height, m_scale, m_seed, m_octaves,
                m_topLimit, m_exponent, m_fudgeFactor, m_islandMix);
            
            var mf = m_terrain.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = m_terrain.AddComponent<MeshFilter>();
            }

            mf.sharedMesh = mesh;
            
            EditorUtility.SetDirty(m_terrain);
            AssetDatabase.SaveAssetIfDirty(m_terrain);
        }
    }
}