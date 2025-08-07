using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RS.Scene
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private GameObject m_mapUI;
        [SerializeField] private RawImage m_map;
        [SerializeField] private List<Sprite> m_marks;

        private List<Image> m_marksOnMap = new List<Image>();

        public void SetMapTexture(Texture2D mapTexture)
        {
            m_map.texture = mapTexture;
        }

        public void Toggle()
        {
            m_mapUI.SetActive(!m_mapUI.activeSelf);
        }

        public void AddMark(int markIndex, Vector2 markPos)
        {
            var mark = new GameObject("Mark");
            mark.transform.SetParent(m_map.transform);
            var markImage = mark.AddComponent<Image>();
            m_marksOnMap.Add(markImage);
            
            markImage.sprite = m_marks[markIndex];
            
            var size = m_map.rectTransform.rect.size;
            markImage.rectTransform.anchoredPosition = new Vector2((markPos.x - 0.5f) * size.x, (markPos.y - 0.5f) * size.y);
        }
    }
}