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

        public static List<(Vector3Int, BlockType)> GetTreeChangeList(int size, int height, BlockType leaf)
        {
            var list = new List<(Vector3Int, BlockType)>();
            var pos = new Vector3Int(0, 0, 0); // 相对位置

            for (var i = 0; i < height + 2; i++)
            {
                // 树顶加一个叶子
                if (i == height + 1)
                {
                    list.Add((pos, leaf));
                    break;
                }
                
                if (i > 1)
                {
                    foreach (var dir in m_Directions)
                    {
                        list.Add((pos + dir, leaf));
                        list.Add((pos + Vector3Int.up + dir, leaf));
                    }
                }

                // 树干高度2 * m_height
                if (i < height)
                {
                    list.Add((pos, BlockType.Orc));
                    list.Add((pos + Vector3Int.up, BlockType.Orc));
                }
                
                pos += Vector3Int.up * 2;
            }

            return list;
        }

        public static int GetTreeHeight(float value, float humidity, out BlockType leaf)
        {
            if (humidity > 0.2f && humidity < 0.23f)
            {
                leaf = BlockType.Sakura;
            }
            else
            {
                leaf = BlockType.Leaf;
            }
            
            if (value < 0.9f)
            {
                return 3;
            }
            
            if (value < 0.95f)
            {
                return 4;
            }
            
            return 5;
        }

        // public static List<(Vector3Int, BlockType)> GetCactusChangeList(int size, int height)
        // {
        //     var list = new List<(Vector3Int, BlockType)>();
        //     var pos = new Vector3Int(0, 0, 0); // 相对位置
        //
        //     for (var i = 0; i < height; i++)
        //     {
        //         if (i == height - 1)
        //         {
        //             list.Add((pos, BlockType.Cactus));
        //             list.Add((pos + Vector3Int.up, BlockType.CactusTop));
        //             break;
        //         }
        //         
        //         list.Add((pos, BlockType.Cactus));
        //         list.Add((pos + Vector3Int.up, BlockType.Cactus));
        //         
        //         pos += Vector3Int.up * 2;
        //     }
        //
        //     return list;
        // }
    }
}