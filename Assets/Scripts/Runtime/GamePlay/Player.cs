using UnityEngine;

using RS.Item;
using RS.Scene;
using UnityEngine.InputSystem;

namespace RS.GamePlay
{
    public class Player : MonoBehaviour
    {
        private int m_health; // 血条 0~100
        private int m_hungry; // 饥饿值 0~100
        private Transform m_transform;
        private RsItem m_handItem; // 目前手持道具
        private RsItem[] m_items; // 道具栏 暂定为10个栏位
        private PlayerInput m_playerInput;
        // private BlockType m_onBlockType;
        private bool m_isInWater = false;
        
        public void Awake()
        {
            m_health = 100;
            m_hungry = 100;
            m_items = new RsItem[10];
            m_items[0] = new Block(BlockType.Orc);
            m_items[1] = new Block(BlockType.Leaf);
            m_handItem = m_items[0];
            m_transform = gameObject.transform;
            m_playerInput = GetComponent<PlayerInput>();
        }

        public void Update()
        {
            var pos = m_transform.position;
            // var blockPos = Chunk.WorldPosToBlockWorldPos(pos);
            var onBlockType = SceneManager.Instance.GetBlockType(Chunk.WorldPosToBlockWorldPos(pos) + Vector3Int.up);
            if (onBlockType == BlockType.Water)
            {
                m_isInWater = true;
            }
        }

        public void OnItem1(InputValue value)
        {
            m_handItem = m_items[0];
        }

        public void OnItem2(InputValue value)
        {
            m_handItem = m_items[1];
        }

        // public BlockType OnBlockType
        // {
        //     get { return m_onBlockType; }
        // }

        public int Health
        {
            get { return m_health; }
        }

        public int Hungry
        {
            get { return m_hungry; }
        }

        public bool InWater
        {
            get { return m_isInWater; }
        }

        public bool Floating
        {
            get { return m_isInWater && m_transform.position.y < 62.49; }
        }

        public BlockType HandItem
        {
            get
            {
                if (m_handItem is Block block)
                {
                    return block.Type;
                }
                else
                {
                    return BlockType.Air;
                }
            }
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