using RS.Scene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RS.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private Slider m_stamina;
        [SerializeField] private Image m_staminaFill;
        [SerializeField] private Slider m_health;
        [SerializeField] private Image m_healthFill;
        [SerializeField] private GameObject m_normal;
        [SerializeField] private GameObject m_death;
        [SerializeField] private Button m_deathReturnBtn;
        
        public void Start()
        {
            m_normal.SetActive(true);
            m_death.SetActive(false);
            m_deathReturnBtn.onClick.AddListener(DeathOnClick);
        }
        
        public void ShowDeath()
        {
            m_normal.SetActive(false);
            m_death.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var playerInput = gameObject.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.actions["Attack"].Disable();
                playerInput.actions["Look"].Disable();
                playerInput.actions["Move"].Disable();
            }
        }

        private void DeathOnClick()
        {
            RsSceneManager.Instance.ReturnHome(false);
        }
        
        public void SetStamina(int value)
        {
            if (value >= 100)
            {
                m_stamina.gameObject.SetActive(false);
                return;
            }

            m_stamina.gameObject.SetActive(true);
            
            if (value < 20)
            {
                m_staminaFill.color = Color.red;
            }
            else if (value < 50)
            {
                m_staminaFill.color = Color.yellow;
            }
            else
            {
                m_staminaFill.color = Color.green;
            }

            m_stamina.value = value / 100.0f;
        }

        public void SetHealth(int value)
        {
            if (value >= 100)
            {
                m_health.gameObject.SetActive(false);
                return;
            }

            m_health.gameObject.SetActive(true);

            m_health.value = value / 100.0f;
        }
    }
}