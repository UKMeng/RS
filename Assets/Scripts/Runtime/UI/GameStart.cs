using RS.GamePlay;
using RS.Scene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace RS.UI
{
    public class GameStart : MonoBehaviour
    {
        [SerializeField] private GameObject m_gameStartUI;
        [SerializeField] private PlayerInput m_playerInput;
        [SerializeField] private Button m_cancelBtn;
        [SerializeField] private Button m_startBtn;
        [SerializeField] private Button m_seedBtn;
        [SerializeField] private Toggle m_easyToggle;
        [SerializeField] private Toggle m_normalToggle;
        [SerializeField] private Toggle m_hardToggle;
        [SerializeField] private TMP_InputField m_seedInput;
        [SerializeField] private Player m_player;
        
        private long m_seed;
        private int m_difficulty;

        private void Start()
        {
            m_seedBtn.onClick.AddListener(OnSeedBtnClick);
            m_startBtn.onClick.AddListener(OnStartBtnClick);
            m_cancelBtn.onClick.AddListener(OnCancelBtnClick);
            
            m_seedInput.onValueChanged.AddListener(OnValueChanged);
            m_seedInput.onEndEdit.AddListener(OnEndEdit);
            
            m_easyToggle.onValueChanged.AddListener(isOn => 
            {
                if (isOn)
                {
                    SetDifficulty(0);
                }
                else
                {
                    m_easyToggle.isOn = true;
                }
            });
            m_normalToggle.onValueChanged.AddListener(isOn => 
            {
                if (isOn)
                {
                    SetDifficulty(1);
                }
                else
                {
                    m_normalToggle.isOn = true;
                }
            });
            m_hardToggle.onValueChanged.AddListener(isOn => 
            {
                if (isOn)
                {
                    SetDifficulty(2);
                }
                else
                {
                    m_hardToggle.isOn = true;
                }
            });

            if (m_player.Status < PlayerStatus.GetPickaxe)
            {
                m_normalToggle.interactable = false;
                m_hardToggle.interactable = false;
            }
        }

        private void SetDifficulty(int difficulty)
        {
            m_difficulty = difficulty;
            m_easyToggle.isOn = difficulty == 0;
            m_normalToggle.isOn = difficulty == 1;
            m_hardToggle.isOn = difficulty == 2;
        }
        
        private void OnValueChanged(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            
            if (text == "-")
            {
                return;
            }
            
            if (long.TryParse(text, out long value))
            {
                if (value > long.MaxValue)
                {
                    m_seedInput.text = long.MaxValue.ToString();
                }
                else if (value < long.MinValue)
                {
                    m_seedInput.text = long.MinValue.ToString();
                }
            }
            else
            {
                // 非法输入
                m_seedInput.text = m_seed.ToString();
            }
        }

        public void OnEndEdit(string text)
        {
            if (long.TryParse(text, out long value))
            {
                m_seed = value;
            }
        }
        
        private void OnSeedBtnClick()
        {
            m_seed = Random.Range(int.MinValue, int.MaxValue);
            m_seedInput.text = m_seed.ToString();
        }

        private void OnStartBtnClick()
        {
            RsSceneManager.Instance.GameStart(m_seed, m_difficulty);
        }

        private void OnCancelBtnClick()
        {
            Toggle();
        }
        
        
        public void Toggle()
        {
            if (m_gameStartUI.activeSelf)
            {
                m_gameStartUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                m_playerInput.actions["Attack"].Enable();
                m_playerInput.actions["Look"].Enable();
                m_playerInput.actions["Move"].Enable();
            }
            else
            {
                m_gameStartUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                m_playerInput.actions["Attack"].Disable();
                m_playerInput.actions["Move"].Disable();
                m_playerInput.actions["Look"].Disable();
                m_playerInput.actions["Move"].Disable();
                m_seed = Random.Range(int.MinValue, int.MaxValue);
                m_seedInput.text = m_seed.ToString();
            }
        }
    }
}