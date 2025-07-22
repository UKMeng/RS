using System;
using RS.Scene;
using UnityEngine;
using UnityEditor;

namespace RS
{
    [CustomEditor(typeof(SceneManager))]
    public class SceneManagerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认Inspector
            DrawDefaultInspector();
            
            // 获取当前 Inspector 选中的 SceneManager 实例
            SceneManager sceneManager = (SceneManager)target;

            // 添加按钮
            // if (GUILayout.Button("Generate Scene"))
            // {
            //     sceneManager.GenerateScene();
            // }
            
        }
    }
}