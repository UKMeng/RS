using UnityEngine;

using RS.Item;

namespace RS.GamePlay
{
    public class Player : MonoBehaviour
    {
        private int m_health; // 血条 0~100
        private int m_hungry; // 饥饿值 0~100
        private Transform m_transform;
        private RsItem m_handItem; // 目前手持道具
        private RsItem[] m_items; // 道具栏 暂定为10个栏位
        
        public void Awake()
        {
            m_health = 100;
            m_hungry = 100;
            m_items = new RsItem[10];
            m_handItem = m_items[0];
            m_transform = gameObject.transform;
        }

        public int Health
        {
            get { return m_health; }
        }

        public int Hungry
        {
            get { return m_hungry; }
        }

        public Vector3 Position
        {
            get
            {
                return m_transform.position;
            }
            set
            {
                m_transform.position = value;
            }
        }
    }
}