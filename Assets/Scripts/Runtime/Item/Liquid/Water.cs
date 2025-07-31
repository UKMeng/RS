using UnityEngine;

namespace RS.Item
{
    public class Water: Liquid
    {
        public override string Name => "Water";
        
        protected override Liquid Spread(Vector3Int pos, byte depth, Liquid source)
        {
            return new Water(pos, depth, this);
        }

        public Water(Vector3Int pos, byte depth, Liquid source)
            : base(pos, depth, source)
        {
            m_maxDepth = 7;
        }
        
    }
}