using RS.Scene;
using RS.Scene.Biome;
using RS.GamePlay;
using RS.Item;
using RS.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace RS.GMTool
{
    public struct DebugData
    {
        public BiomeType biomeType;
        public string gameTime;
    }
    
    public class GMToolWindow : MonoBehaviour
    {
        private GUIStyle m_labelStyle;
        private GUIStyle m_textFieldStyle;
        
        private bool m_showWindow = false;
        
        private PlayerInput m_playerInput;

        private SceneManager m_sceneManager;

        private string m_commandLine;
        
        public Player player;

        private DebugData m_debugData;

        private bool m_cmdSwitch = false;

        private string m_rayCastText;
        

        private void Awake()
        {
            m_playerInput = GetComponent<PlayerInput>();
            m_sceneManager = GetComponent<SceneManager>();
            m_debugData = new DebugData();
            
            
            // m_labelStyle.normal.textColor = Color.white;
        }

        public void OnToggleGMToolWindow(InputValue value)
        {
            m_showWindow = !m_showWindow;
        }

        public void OnEnter(InputValue value)
        {
            if (!m_cmdSwitch)
            {
                m_cmdSwitch = true;
            }
            else
            {
                m_cmdSwitch = false;
                if (!string.IsNullOrEmpty(m_commandLine))
                {
                    var args = m_commandLine.Split(" ");
                
                    switch (args[0])
                    {
                        case "time":
                        {
                            var hour = uint.Parse(args[1]);
                            var minute = uint.Parse(args[2]);
                            SceneManager.Instance.SetGameTime(hour, minute);
                            break;
                        }
                        default:
                        {
                            Debug.LogError($"Unknown command: {m_commandLine}");
                            break;
                        }
                    }

                    m_commandLine = "";
                }
            }
        }

        private void RayCastTest()
        {
            var screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
            var ray = Camera.main.ScreenPointToRay(screenCenter);
            var rayDistance = 10.0f;

            if (Physics.Raycast(ray, out var hitInfo, rayDistance))
            {
                var pos = hitInfo.point;
                var normal = hitInfo.normal;
                var blockPos = RsMath.GetBlockMinCorner(pos, normal);
                var blockType = m_sceneManager.GetBlockType(Chunk.WorldPosToBlockWorldPos(blockPos));
                m_rayCastText = $"{blockType}";
            }
            else
            {
                m_rayCastText = "null";
            }
        }
        
        public void Update()
        {
            if (m_showWindow)
            {
                UpdateDebugData();
                RayCastTest();
            }
        }

        public void OnGUI()
        {
            if (!m_showWindow)
            {
                return;
            }

            m_labelStyle = new GUIStyle(GUI.skin.label);
            m_labelStyle.fontSize = 30;
            m_textFieldStyle = new GUIStyle(GUI.skin.textField);
            m_textFieldStyle.fontSize = 30;
            var windowSize = new Rect(10, 10, 500, 500);
            GUI.Window(0, windowSize, DrawWindow, "GM Tool Window");
        }

        private void DrawWindow(int windowId)
        {
            if (player != null)
            {
                GUILayout.Label("游戏时间:" + m_debugData.gameTime, m_labelStyle);
                
                GUILayout.Label("玩家坐标:" + player.Position, m_labelStyle);
                GUILayout.Label("玩家信息:", m_labelStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label("血量:" + player.Health, m_labelStyle);
                GUILayout.Label("耐力:" + player.Stamina, m_labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("手持道具:" + player.HandItem.Name, m_labelStyle);
                GUILayout.Label("道具数量:" + player.HandItem.Count + "/" + player.HandItem.Capacity, m_labelStyle);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                // GUILayout.Label("脚底上方块" + player.OnBlockType, m_labelStyle);
                GUILayout.Label("在水中:" + player.InWater, m_labelStyle);
                GUILayout.Label("漂浮:" + player.Floating, m_labelStyle);
                GUILayout.Label("冲刺:" + player.Sprint, m_labelStyle);
                GUILayout.EndHorizontal();
                
                GUILayout.Label("Biome:" + m_debugData.biomeType, m_labelStyle);
                
                GUILayout.Label("RayCast Block:" + m_rayCastText, m_labelStyle);
            }
            
            GUILayout.Space(10);
            GUI.SetNextControlName("CommandTextField");
            m_commandLine = GUILayout.TextField(m_commandLine, m_textFieldStyle);
            if (m_cmdSwitch)
            {
                GUI.FocusControl("CommandTextField");
            }
        }

        private void UpdateDebugData()
        {
            var pos = player.Position;
            m_debugData.biomeType = NoiseManager.Instance.SampleBiome(pos, out _);
            m_debugData.gameTime = m_sceneManager.GetGameTime();
        }
    }
}