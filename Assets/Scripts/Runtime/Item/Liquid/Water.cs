using UnityEngine;

namespace RS.Item
{
    public class Water: Liquid
    {
        public override string Name => "Water";
        public override int MaxDepth => 7;
        public override BlockType Type => BlockType.Water;
        
        public override Liquid Spread(Vector3Int pos, byte depth)
        {
            return new Water(pos, depth);
        }

        public Water(Vector3Int pos, byte depth = 0)
            : base(pos, depth)
        {
        }
    }
}