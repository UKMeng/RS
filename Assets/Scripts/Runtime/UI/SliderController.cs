using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RS.UI
{
    public class SliderController : MonoBehaviour
    {
        [SerializeField] private Slider m_slider;
        [SerializeField] private TextMeshProUGUI m_sliderValue;

        void Start()
        {
            m_slider.onValueChanged.AddListener((v) =>
            {
                m_sliderValue.text = $"地图生成中 {v:P}";
            });
        }
    }
}