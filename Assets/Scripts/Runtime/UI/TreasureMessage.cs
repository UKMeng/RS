using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using RS.Scene;

namespace RS.UI
{
    public class TreasureMessage : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_message;
        [SerializeField] private Button m_yesButton;
        [SerializeField] private Camera m_previewCamera;
        // [SerializeField] private Transform m_previewSpawnTsf;
        [SerializeField] private RawImage m_previewImage;
        private PlayerInput m_playerInput;

        private GameObject m_currentPreview;

        public void Update()
        {
            // 旋转模型
            if (m_currentPreview != null)
            {
                m_currentPreview.transform.Rotate(Vector3.up, 30.0f * Time.deltaTime);
            }
        }
        
        public void Show(string message, Action onYes, GameObject treasure)
        {
            m_message.text = message;

            m_yesButton.onClick.RemoveAllListeners();
            m_yesButton.onClick.AddListener(() =>
            {
                onYes?.Invoke();
                Close();
            });

            if (m_playerInput == null)
            {
                m_playerInput = RsSceneManager.Instance.PlayerInput;
            }

            if (m_playerInput != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                m_playerInput.actions["Attack"].Disable();
                m_playerInput.actions["Look"].Disable();
                m_playerInput.actions["Move"].Disable();
            }
            
            ShowTreasurePreview(treasure);
            
            gameObject.SetActive(true);
        }

        private void ShowTreasurePreview(GameObject treasure)
        {
            if (m_currentPreview != null)
            {
                Destroy(m_currentPreview);
            }
            
            m_currentPreview = Instantiate(treasure, new Vector3(0, 0, 1f), Quaternion.Euler(0, -180, 0));
            m_currentPreview.layer = LayerMask.NameToLayer("Treasure");
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

            Destroy(m_currentPreview);
            gameObject.SetActive(false);
        }
    }
}