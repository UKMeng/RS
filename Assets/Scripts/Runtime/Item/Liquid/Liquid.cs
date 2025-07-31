using RS.Scene;
using UnityEngine;

namespace RS.Item
{
    public abstract class Liquid : RsItem
    {
        // 深度，用来形容液体方块的有多少空的部分
        // 源始终是0，最大深度是7，垂直下落形成的液体深度则+8从而特殊处理
        protected byte m_depth;

        protected Vector3Int m_pos;

        public virtual BlockType Type => BlockType.Water;

        public byte Depth
        {
            get { return m_depth; }
        }

        public Vector3Int Position
        {
            get { return m_pos; }
        }

        public virtual int MaxDepth => 7;
        
        public abstract Liquid Spread(Vector3Int pos, byte depth);

        protected Liquid(Vector3Int pos, byte depth)
        {
            m_pos = pos;
            m_depth = depth;
        }
    }
}