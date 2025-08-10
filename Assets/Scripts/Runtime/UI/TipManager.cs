using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Object = UnityEngine.Object;

using RS.Scene;

namespace RS.UI
{
    public class TipManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_tips;
        private PlayerInput m_playerInput;
        
        public void Show(string tips)
        {
            m_tips.text = tips;
            gameObject.SetActive(true);
            Invoke(nameof(Hide), 10f);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}