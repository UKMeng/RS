using UnityEngine;
using UnityEngine.UI;

namespace RS.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private Slider m_stamina;
        [SerializeField] private Image m_staminaFill;

        public void SetStamina(int value)
        {
            if (value == 100)
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
    }
}