using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

using RS.Scene;

namespace RS.UI
{
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_message;
        [SerializeField] private Button m_yesButton;
        [SerializeField] private Button m_noButton;
        private PlayerInput m_playerInput;
        
        public void Show(string message, Action onYes, Action onNo)
        {
            m_message.text = message;

            m_yesButton.onClick.RemoveAllListeners();
            m_yesButton.onClick.AddListener(() =>
            {
                onYes?.Invoke();
                Close();
            });
            m_noButton.onClick.RemoveAllListeners();
            m_noButton.onClick.AddListener(() =>
            {
                onNo?.Invoke();
                Close();
            });

            if (m_playerInput == null)
            {
                m_playerInput = SceneManager.Instance.PlayerInput;
            }
            
            if (m_playerInput != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                m_playerInput.actions["Attack"].Disable();
                m_playerInput.actions["Look"].Disable();
                m_playerInput.actions["Move"].Disable();
            }
            
            gameObject.SetActive(true);
        }

        private void Close()
        {
            if (m_playerInput != null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                m_playerInput.actions["Attack"].Enable();
                m_playerInput.actions["Look"].Enable();
                m_playerInput.actions["Move"].Enable();
            }
            gameObject.SetActive(false);
        }
    }
}