using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using RS.GamePlay;
using RS.Item;
using TMPro;

namespace RS.UI
{
    public class ItemBar : MonoBehaviour
    {
        [SerializeField] private List<Sprite> m_itemSprites;
        [SerializeField] private List<Sprite> m_blockSprites;
        [SerializeField] private List<Image> m_slots;
        [SerializeField] private List<Image> m_items;
        [SerializeField] private List<TextMeshProUGUI> m_itemCounts;
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
                    m_itemCounts[i].text = "";
                    SetImageAlpha(m_items[i], 0);
                }
                else
                {
                    Sprite sprite = null;
                    var count = 0;
                    if (item is Block block)
                    {
                        foreach (var s in m_blockSprites)
                        {
                            if (s.name == block.Type.ToString())
                            {
                                sprite = s;
                                count = block.Count;
                                break;
                            }
                        }
                    }
                    else
                    {
                        sprite = m_itemSprites[item.Id];
                        count = 0;
                    }

                    if (sprite != null)
                    {
                        m_items[i].sprite = sprite;
                        if (count > 0)
                        {
                            m_itemCounts[i].text = count.ToString();
                        }

                        if (count == 0)
                        {
                            m_itemCounts[i].text = "";
                        }

                        if (i == m_player.HandItemIndex)
                        {
                            SetImageAlpha(m_items[i], 255);
                        }
                        else
                        {
                            SetImageAlpha(m_items[i], 175);
                        }
                    }
                }
            }
        }

        public void UpdateItemCount()
        {
            var items = m_player.Items;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item != null)
                {
                    var count = 0;
                    if (item is Block block)
                    {
                        count = block.Count;
                        m_itemCounts[i].text = count.ToString();
                    }
                }
            }
        }

        public void UpdateSlotHighLight()
        {
            var handIndex = m_player.HandItemIndex;
            for (var i = 0; i < m_slots.Count; i++)
            {
                if (i == handIndex)
                {
                    if (m_player.Items[i] == null)
                    {
                        SetImageAlpha(m_items[i], 0);
                    }
                    else
                    {
                        SetImageAlpha(m_items[i], 255);
                    }
                    SetImageAlpha(m_slots[i], 255);
                }
                else
                {
                    if (m_player.Items[i] == null)
                    {
                        SetImageAlpha(m_items[i], 0);
                    }
                    else
                    {
                        SetImageAlpha(m_items[i], 175);
                    }
                    SetImageAlpha(m_slots[i], 175);
                }
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