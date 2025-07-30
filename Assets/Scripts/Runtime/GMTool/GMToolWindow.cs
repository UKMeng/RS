using RS.Scene;
using RS.Scene.Biome;
using RS.GamePlay;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RS.GMTool
{
    public struct DebugData
    {
        public BiomeType biomeType;
    }
    
    public class GMToolWindow : MonoBehaviour
    {
        private bool m_showWindow = false;
        
        private PlayerInput m_playerInput;
        
        public Player player;

        private DebugData m_debugData;
        

        private void Awake()
        {
            m_playerInput = GetComponent<PlayerInput>();
            m_debugData = new DebugData();
        }

        public void OnToggleGMToolWindow(InputValue value)
        {
            m_showWindow = !m_showWindow;
        }
        
        public void Update()
        {
            if (m_showWindow)
            {
                UpdateDebugData();
            }
        }

        public void OnGUI()
        {
            if (!m_showWindow)
            {
                return;
            }

            var windowSize = new Rect(10, 10, 300, 300);
            GUI.Window(0, windowSize, DrawWindow, "GM Tool Window");
        }

        private void DrawWindow(int windowId)
        {
            if (player != null)
            {
                GUILayout.Label("玩家坐标:" + player.Position);
                GUILayout.Label("玩家信息:");
                GUILayout.BeginHorizontal();
                GUILayout.Label("血量:" + player.Health);
                GUILayout.Label("饥饿:" + player.Hungry);
                GUILayout.EndHorizontal();
                
                GUILayout.Label("Biome:" + m_debugData.biomeType);
            }
        }

        private void UpdateDebugData()
        {
            var pos = player.Position;
            m_debugData.biomeType = NoiseManager.Instance.SampleBiome(pos, out _);
        }
        
    }
}