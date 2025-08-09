using System.Collections.Generic;
using RS.GamePlay;
using UnityEngine;

namespace RS.Item
{
    public class ItemModelManager : MonoBehaviour
    {
        [SerializeField] private Transform m_rightHand;
        [SerializeField] private List<GameObject> m_itemPrefabs;
        [SerializeField] private GameObject m_blockPrefab;
        [SerializeField] private Player m_player;

        private List<GameObject> m_items;
        private GameObject m_block;
        private GameObject m_currentItem;
        
        public void Awake()
        {
            m_items = new List<GameObject>();
            foreach (var prefab in m_itemPrefabs)
            {
                var go = Instantiate(prefab, m_rightHand);
                go.SetActive(false);
                m_items.Add(go);
            }
            
            m_block = Instantiate(m_blockPrefab, m_rightHand);
            m_block.SetActive(false);
        }

        public void OnEnable()
        {
            m_player.OnHandItemIndexChanged += UpdateHandItem;
        }

        public void OnDisable()
        {
            m_player.OnHandItemIndexChanged -= UpdateHandItem;
        }

        public void UpdateHandItem()
        {
            var handIndex = m_player.HandItemIndex;
            var items = m_player.Items;

            if (m_currentItem != null)
            {
                m_currentItem.SetActive(false);
            }
            
            if (items[handIndex] != null)
            {
                var item = items[handIndex];
                if (item is Block block)
                {
                    var mat = m_block.GetComponent<MeshRenderer>().sharedMaterial;
                    if (mat != null)
                    {
                        mat.color = Block.BlockColors[(int)block.Type];
                    }
                    m_currentItem = m_block;
                    m_block.SetActive(true);
                }
                else
                {
                    m_currentItem = m_items[item.Id];
                    m_currentItem.SetActive(true);
                }
            }
        }
        
        
    }
}