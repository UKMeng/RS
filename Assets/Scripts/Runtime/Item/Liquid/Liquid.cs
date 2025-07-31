using RS.Scene;
using UnityEngine;

namespace RS.Item
{
    public abstract class Liquid : RsItem, IUpdateByTick
    {
        // 深度，用来形容液体方块的有多少空的部分
        // 源始终是0，最大深度是7，垂直下落形成的液体深度则+8从而特殊处理
        protected byte m_depth;
        protected byte m_maxDepth;

        protected Vector3Int m_pos;
        protected Liquid m_source;

        protected abstract Liquid Spread(Vector3Int pos, byte depth, Liquid source);

        protected Liquid(Vector3Int pos, byte depth, Liquid source)
        {
            m_pos = pos;
            m_depth = depth;
            m_source = source;
        }
        
        public void OnTick()
        {
            // 每次tick，尝试向外扩散
            var bottomPos = m_pos + Vector3Int.down;
            var bottomBlock = SceneManager.Instance.GetBlockType(bottomPos);
            if (bottomBlock == BlockType.Air)
            {
                
            }
        }
    }
}