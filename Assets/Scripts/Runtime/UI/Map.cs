using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RS.UI
{
    public class Map : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject m_mapUI;
        [SerializeField] private RawImage m_map;
        [SerializeField] private List<Sprite> m_marks;
        [SerializeField] private PlayerInput m_playerInput;
        
        private List<Image> m_marksOnMap = new List<Image>();
        
        
        public void OnPointerClick(PointerEventData data)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_map.rectTransform,
                    data.position, data.pressEventCamera, out var localPos))
            {
                return;
            }

            var normalizedPos = new Vector2(
                (localPos.x / m_map.rectTransform.rect.width) + 0.5f,
                (localPos.y / m_map.rectTransform.rect.height) + 0.5f
            );

            // 超出边界不处理
            if (normalizedPos.x < 0.0f || normalizedPos.x > 1.0f || normalizedPos.y < 0.0f || normalizedPos.y > 1.0f)
            {
                return;
            }

            var existMark = FindMark(localPos);
            
            if (existMark != null)
            {
                m_marksOnMap.Remove(existMark);
                Destroy(existMark.gameObject);
            }
            else
            {
                AddMark(1, normalizedPos);
            }
        }

        private Image FindMark(Vector2 localPos)
        {
            foreach (var mark in m_marksOnMap)
            {
                var dis = Vector2.Distance(localPos, mark.rectTransform.anchoredPosition);
                if (dis < 20.0f)
                {
                    return mark;
                }
            }

            return null;
        }
        
        public void SetMapTexture(Texture2D mapTexture)
        {
            m_map.texture = mapTexture;
        }

        public void Toggle()
        {
            // if (m_playerMap == null)
            // {
            //     m_playerMap = m_inputActions.FindActionMap("Player");
            // }
            
            if (m_mapUI.activeSelf)
            {
                m_mapUI.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                m_playerInput.actions["Attack"].Enable();
            }
            else
            {
                m_mapUI.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                m_playerInput.actions["Attack"].Disable();
            }
        }

        public void AddMark(int markIndex, Vector2 markPos)
        {
            var mark = new GameObject("Mark");
            mark.transform.SetParent(m_map.transform);
            var markImage = mark.AddComponent<Image>();

            markImage.sprite = m_marks[markIndex];
            
            if (markIndex == 1)
            {
                m_marksOnMap.Add(markImage);
                markImage.rectTransform.sizeDelta = new Vector2(50, 50);
            }
            
            var size = m_map.rectTransform.rect.size;
            markImage.rectTransform.anchoredPosition = new Vector2((markPos.x - 0.5f) * size.x, (markPos.y - 0.5f) * size.y);
        }
    }
}