using System.Collections.Generic;
using RS.Scene;
using UnityEngine;

namespace RS.Item
{
    public class Flow : IUpdateByTick
    {
        private List<Liquid> m_liquids;
        private Liquid m_source;

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
                if (liquid.Depth == liquid.MaxDepth)
                {
                    continue;
                }
                
                // 先只检查水平方向的
                var forward = liquid.Position + Vector3Int.forward;
                var back = liquid.Position + Vector3Int.back;
                var left = liquid.Position + Vector3Int.left;
                var right = liquid.Position + Vector3Int.right;

                CheckLiquidFlow(liquid, forward, newLiquids);
                CheckLiquidFlow(liquid, back, newLiquids);
                CheckLiquidFlow(liquid, left, newLiquids);
                CheckLiquidFlow(liquid, right, newLiquids);
            }
            
            m_liquids.AddRange(newLiquids);
        }

        private void CheckLiquidFlow(Liquid liquid, Vector3Int direction, List<Liquid> newLiquids)
        {
            var block = SceneManager.Instance.GetBlockType(direction);
            if (block == BlockType.Air)
            {
                var newLiquid = liquid.Spread(direction, (byte)(liquid.Depth + 1));
                newLiquids.Add(newLiquid);
                SceneManager.Instance.PlaceBlock(direction, liquid.Type);
            }
        }
    }
}