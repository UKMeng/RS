using UnityEngine;

using RS.Item;

namespace RS.GamePlay
{
    public class Player : MonoBehaviour
    {
        private int m_health; // 血条 0~100
        private int m_hungry; // 饥饿值 0~100

        private RsItem m_handItem; // 目前手持道具
        private RsItem[] m_items; // 道具栏 暂定为10个栏位
        
        public void Start()
        {
            m_health = 100;
            m_hungry = 100;
            m_items = new RsItem[10];
            m_handItem = m_items[0];
        }

        public int Health
        {
            get { return m_health; }
        }

        public int Hungry
        {
            get { return m_hungry; }
        }
    }
}