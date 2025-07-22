using UnityEngine;
using UnityEngine.InputSystem;

namespace RS.GMTool
{
    public class GMToolWindow : MonoBehaviour
    {
        private bool m_showWindow = false;
        
        public Transform player;

        private PlayerInput m_playerInput;

        private void Awake()
        {
            m_playerInput = GetComponent<PlayerInput>();
        }

        public void OnToggleGMToolWindow(InputValue value)
        {
            m_showWindow = !m_showWindow;
        }
        
        public void Update()
        {
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
            }
        }
    }
}