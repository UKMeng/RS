using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RS.GamePlay;
using RS.Item;

namespace RS.UI
{
    public class ItemBar : MonoBehaviour
    {
        [SerializeField] private List<Sprite> m_itemSprites;
        [SerializeField] private List<Sprite> m_blockSprites;
        [SerializeField] private List<Image> m_slots;
        [SerializeField] private List<Image> m_items;
        [SerializeField] private Player m_player;

        public void OnEnable()
        {
            m_player.OnItemsChanged += UpdateItemsUI;
            m_player.OnHandItemIndexChanged += UpdateSlotHighLight;
        }

        public void OnDisable()
        {
            m_player.OnItemsChanged -= UpdateItemsUI;
            m_player.OnHandItemIndexChanged -= UpdateSlotHighLight;
        }
        
        public void UpdateItemsUI()
        {
            var items = m_player.Items;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    SetImageAlpha(m_items[i], 0);
                }
                else
                {
                    Sprite sprite = null;
                    if (item is Block block)
                    {
                        foreach (var s in m_blockSprites)
                        {
                            if (s.name == block.Type.ToString())
                            {
                                sprite = s;
                                break;
                            }
                        }
                    }
                    else
                    {
                        sprite = m_itemSprites[item.Id];
                    }

                    if (sprite != null)
                    {
                        m_items[i].sprite = sprite;
                        SetImageAlpha(m_items[i], 175);
                    }
                    
                }
            }
        }

        public void UpdateSlotHighLight()
        {
            var handIndex = m_player.HandItemIndex;
            for (var i = 0; i < m_slots.Count; i++)
            {
                SetImageAlpha(m_slots[i], i == handIndex ? 255 : 175);
            }
        }

        private void SetImageAlpha(Image img, int alpha)
        {
            if (img == null)
            {
                return;
            }

            var color = img.color;
            color.a = alpha / 255.0f;
            img.color = color;
        }
    }
}