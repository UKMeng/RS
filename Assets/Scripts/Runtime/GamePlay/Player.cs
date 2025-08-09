using System;
using UnityEngine;

using RS.Item;
using RS.Scene;
using RS.UI;
using UnityEngine.InputSystem;

namespace RS.GamePlay
{
    public class Player : MonoBehaviour
    {
        private int m_health; // 血条 0~100
        private int m_stamina; // 耐力值 0~100
        private int m_staminaRegainSpeed = 10;
        
        private Transform m_transform;
        private RsItem m_handItem; // 目前手持道具
        private int m_handItemIndex = 0;
        private RsItem[] m_items; // 道具栏 暂定为10个栏位
        private PlayerInput m_playerInput;
        // private BlockType m_onBlockType;
        private bool m_isInWater = false;
        private ThirdPersonController m_controller;

        private ConsumeStamina m_consumeStaminaEvent;

        private HUD m_HUD;

        public class ConsumeStamina : IUpdateByTick
        {
            private Player m_player;
            
            public int TickTimes { get; set; } = -1;
            
            public ConsumeStamina(Player player)
            {
                m_player = player;
            }
            
            public void OnTick()
            {
                var stamina = m_player.Stamina;
                if (m_player.Sprint && m_player.Floating)
                {
                    stamina -= 10;
                }
                else if (m_player.Floating || m_player.Sprint)
                {
                    stamina -= 5;
                }
                else
                {
                    stamina += m_player.StaminaRegainSpeed;
                }
                
                m_player.Stamina = Mathf.Clamp(stamina, 0, 100);
            }
        }
        
        public void Awake()
        {
            m_health = 100;
            m_stamina = 100;
            m_items = new RsItem[10];
            m_items[0] = new Shovel();
            m_handItem = m_items[0];
            m_handItemIndex = 0;
            m_transform = gameObject.transform;
            m_playerInput = GetComponent<PlayerInput>();
            m_controller = GetComponent<ThirdPersonController>();
            m_HUD = GetComponent<HUD>();
        }

        public void Start()
        {
            m_consumeStaminaEvent = new ConsumeStamina(this);
            SceneManager.Instance.RegisterTickEvent(m_consumeStaminaEvent);
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
            else
            {
                m_isInWater = false;
            }

            if (m_stamina < 5)
            {
                m_controller.Sprint = false;
            }

            m_HUD.SetStamina(m_stamina);
        }

        
        public void OnItem(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                var index = int.Parse(context.control.name);
                if (index < m_items.Length + 1)
                {
                    m_handItem = m_items[index - 1];
                    m_handItemIndex = index - 1;
                }
            }
        }

        public void DisposeItem(RsItem item)
        {
            m_items[m_handItemIndex] = null;
            
            if (m_handItemIndex == 0)
            {
                m_handItem = null;
            }
            else
            {
                m_handItem = m_items[m_handItemIndex - 1];
            }

            var lastIndex = m_handItemIndex + 1;
            for (; lastIndex < m_items.Length; lastIndex++)
            {
                if (m_items[lastIndex] != null)
                {
                    m_items[lastIndex - 1] = m_items[lastIndex];
                }
                else
                {
                    break;
                }
            }
            m_items[lastIndex - 1] = null;
        }

        public void TryAddBlock(BlockType blockType)
        {
            var index = 0;
            for (; index < m_items.Length; index++)
            {
                var item = m_items[index];
                if (item == null)
                {
                    break;
                }
                if (item is Block block)
                {
                    if (block.Type == blockType)
                    {
                        if (block.Count < block.Capacity)
                        {
                            block.Add();
                        }

                        return;
                    }
                }
            }

            m_items[index] = new Block(blockType, 3);
        }

        // public BlockType OnBlockType
        // {
        //     get { return m_onBlockType; }
        // }

        public int Health
        {
            get { return m_health; }
        }

        public int Stamina
        {
            get { return m_stamina; }
            set { m_stamina = value; }
        }

        public int StaminaRegainSpeed
        {
            get { return m_staminaRegainSpeed; }
        }

        public bool InWater
        {
            get { return m_isInWater; }
        }

        public bool Floating
        {
            get { return m_isInWater && m_transform.position.y < 62.49; }
        }

        public bool Sprint
        {
            get { return m_controller.Sprint; }
        }

        public RsItem HandItem
        {
            get
            {
                return m_handItem;
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