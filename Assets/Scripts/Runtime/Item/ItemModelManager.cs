using System.Collections.Generic;
using UnityEngine;

namespace RS.Item
{
    public class ItemModelManager : MonoBehaviour
    {
        [SerializeField] private Transform m_rightHand;
        [SerializeField] private List<GameObject> m_itemPrefabs;

        public void Start()
        {
            Instantiate(m_itemPrefabs[2], m_rightHand);
        }
    }
}