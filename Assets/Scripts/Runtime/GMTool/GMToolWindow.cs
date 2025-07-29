using RS.Scene;
using RS.Scene.Biome;
using UnityEngine;
using UnityEngine.InputSystem;

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
        
        public Transform player;

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

            var windowSize = new Rect(10, 10, 300, 120);
            GUI.Window(0, windowSize, DrawWindow, "GM Tool Window");
        }

        private void DrawWindow(int windowId)
        {
            if (player != null)
            {
                GUILayout.Label("玩家坐标:" + player.position);
                GUILayout.Label("Biome:" + m_debugData.biomeType);
            }
        }

        private void UpdateDebugData()
        {
            var pos = player.position;
            m_debugData.biomeType = NoiseManager.Instance.SampleBiome(pos, out _);
        }
        
    }
}