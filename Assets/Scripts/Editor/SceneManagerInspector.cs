using System;
using RS.Scene;
using UnityEngine;
using UnityEditor;

namespace RS
{
    [CustomEditor(typeof(RsSceneManager))]
    public class SceneManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认Inspector
            DrawDefaultInspector();
            
            // 获取当前 Inspector 选中的 RsSceneManager 实例
            RsSceneManager sceneManager = (RsSceneManager)target;

            // 添加按钮
            // if (GUILayout.Button("Generate Scene"))
            // {
            //     sceneManager.GenerateScene();
            // }
            
        }
    }
}