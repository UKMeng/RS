using System.Collections.Generic;
using RS.Item;
using UnityEngine;

namespace RS.Scene
{
    public class Tree
    {
        private static Vector3Int[] m_Directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(-1, 0, 1),
            new Vector3Int(-1, 0, -1)
        };
        
        private int m_height; // 树高 = 格数 / 2
        private int m_size; // 占地格数

        public Tree(int size, int height)
        {
            m_size = size;
            m_height = height;
        }

        public List<(Vector3Int, BlockType)> GetChangeList()
        {
            var list = new List<(Vector3Int, BlockType)>();
            var pos = new Vector3Int(0, 0, 0); // 相对位置

            for (var i = 0; i < m_height + 2; i++)
            {
                // 树顶加一个叶子
                if (i == m_height + 1)
                {
                    list.Add((pos, BlockType.Leaf));
                    break;
                }
                
                if (i > 1)
                {
                    foreach (var dir in m_Directions)
                    {
                        list.Add((pos + dir, BlockType.Leaf));
                        list.Add((pos + Vector3Int.up + dir, BlockType.Leaf));
                    }
                }

                // 树干高度2 * m_height
                if (i < m_height)
                {
                    list.Add((pos, BlockType.Orc));
                    list.Add((pos + Vector3Int.up, BlockType.Orc));
                }
                
                pos += Vector3Int.up * 2;
            }

            return list;
        }
    }
}