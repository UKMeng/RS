using System.Collections.Generic;
using RS.Scene;
using UnityEngine;

namespace RS.Item
{
    public class Flow : IUpdateByTick
    {
        private List<Liquid> m_liquids;
        private Liquid m_source;
        private int m_tickTimes = 7;

        public int TickTimes
        {
            get => m_tickTimes;
            set => m_tickTimes = value;
        }
        
        public Flow(Liquid source)
        {
            m_liquids = new List<Liquid>();
            m_liquids.Add(source);
            m_source = source;
        }
        
        public void OnTick()
        {
            var newLiquids = new List<Liquid>();
            foreach (var liquid in m_liquids)
            {
                var forward = liquid.Position + Vector3Int.forward;
                var back = liquid.Position + Vector3Int.back;
                var left = liquid.Position + Vector3Int.left;
                var right = liquid.Position + Vector3Int.right;
                var down = liquid.Position + Vector3Int.down;

                if (liquid == m_source)
                {
                    CheckLiquidFlow(liquid, forward, newLiquids);
                    CheckLiquidFlow(liquid, back, newLiquids);
                    CheckLiquidFlow(liquid, left, newLiquids);
                    CheckLiquidFlow(liquid, right, newLiquids);
                    CheckLiquidFlow(liquid, down, newLiquids, true);
                }
                else
                {
                    var isFloat = CheckLiquidFlow(liquid, down, newLiquids, true);

                    if (!isFloat && liquid.Depth != liquid.MaxDepth)
                    {
                        CheckLiquidFlow(liquid, forward, newLiquids);
                        CheckLiquidFlow(liquid, back, newLiquids);
                        CheckLiquidFlow(liquid, left, newLiquids);
                        CheckLiquidFlow(liquid, right, newLiquids);
                    }
                }
            }
            
            m_liquids.AddRange(newLiquids);
        }

        private bool CheckLiquidFlow(Liquid liquid, Vector3Int direction, List<Liquid> newLiquids, bool isDown = false)
        {
            var block = RsSceneManager.Instance.GetBlockType(direction);
            if (block == BlockType.Air)
            {
                var newLiquid = liquid.Spread(direction, isDown ? liquid.Depth : (byte)(liquid.Depth + 1));
                newLiquids.Add(newLiquid);
                RsSceneManager.Instance.PlaceBlock(direction, liquid.Type);

                return isDown;
            }

            if (block == BlockType.Water)
            {
                return isDown;
            }

            return false;
        }
    }
}