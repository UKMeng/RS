using System.Collections.Generic;
using RS.GamePlay;
using RS.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RS.UI
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_menuUI;
        [SerializeField] private PlayerInput m_playerInput;
        [SerializeField] private Button m_exitBtn;
        [SerializeField] private Button m_resumeBtn;
        [SerializeField] private Button m_giveUpBtn;
        [SerializeField] private Player m_player;
        
        private void Start()
        {
            if (m_exitBtn != null)
            {
                m_exitBtn.onClick.AddListener(OnExitBtnClick);
            }

            if (m_resumeBtn != null)
            {
                m_resumeBtn.onClick.AddListener(OnResumeBtnClick);
            }

            if (m_giveUpBtn != null)
            {
                m_giveUpBtn.onClick.AddListener(OnGiveUpBtnClick);
            }
        }

        private void OnExitBtnClick()
        {
            RsSceneManager.Instance.QuitGame();
        }

        private void OnResumeBtnClick()
        {
            Toggle();
        }

        private void OnGiveUpBtnClick()
        {
            RsSceneManager.Instance.ReturnHome(false);
        }
        
        public void Toggle()
        {
            if (m_menuUI.activeSelf)
            {
                m_menuUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                m_playerInput.actions["Attack"].Enable();
                m_playerInput.actions["Look"].Enable();
                m_playerInput.actions["Move"].Enable();
            }
            else
            {
                m_menuUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                m_playerInput.actions["Attack"].Disable();
                m_playerInput.actions["Move"].Disable();
                m_playerInput.actions["Look"].Disable();
                m_playerInput.actions["Move"].Disable();
            }
        }
    }
}