using System;
using Unity.Collections;
using UnityEngine;


namespace RS.Scene
{
    public enum BlockType : byte
    {
        Air,
        Stone,
        Dirt,
        Water,
    }
    
    public class Block
    {
        private BlockType m_type;

        public static Vector2[][] uvTable;

        public static NativeArray<Vector2> uvTableArray;
        
        /// <summary>
        /// 在游戏初始化是**必须**初始化的一个步骤
        /// 计算各种Block对应的UV
        /// </summary>
        public static void Init()
        {
            uvTable = new Vector2[Enum.GetValues(typeof(BlockType)).Length][];

            uvTable[(int)BlockType.Water] = CalUVs(0, 0);
            uvTable[(int)BlockType.Stone] = CalUVs(0, 1);
            uvTable[(int)BlockType.Dirt] = CalUVs(0, 2);

            uvTableArray = new NativeArray<Vector2>(uvTable.Length * 4, Allocator.Persistent);

            for (var i = 0; i < uvTable.Length; i++)
            {
                if (uvTable[i] == null)
                {
                    continue;
                }

                var index = i * 4;
                for (var j = 0; j < 4; j++)
                {
                    uvTableArray[index + j] = uvTable[i][j];
                }
            }
        }

        public static void UnInit()
        {
            uvTableArray.Dispose();
        }

        // public static Vector2[] GetUVs(BlockType type)
        // {
        //     Vector2[] uvs;
        //     
        //     switch (type)
        //     {
        //         case BlockType.Stone:
        //             uvs = CalUVs(0, 0);
        //             break;
        //         case BlockType.Dirt:
        //             uvs = CalUVs(0, 1);
        //             break;
        //         default:
        //             uvs = new Vector2[4];
        //             break;
        //     }
        //
        //     return uvs;
        // }

        private static Vector2[] CalUVs(int x, int y)
        {
            var margin = 2;
            var width = 1028;
            var height = 3084;
            var textureSize = 1024;
            
            var uvs = new Vector2[4];
            uvs[0] = new Vector2((float)(x * textureSize + (x + 1) * margin) / width, (float)(y * textureSize + (y + 1) * margin) / height);
            uvs[1] = new Vector2((float)((x + 1) * textureSize + (x + 1) * margin) / width, (float)(y * textureSize + (y + 1) * margin) / height);
            uvs[2] = new Vector2((float)((x + 1) * textureSize + (x + 1) * margin) / width, (float)((y + 1) * textureSize + (y + 1) * margin) / height);
            uvs[3] = new Vector2((float)(x * textureSize + (x + 1) * margin) / width, (float)((y + 1) * textureSize + (y + 1) * margin) / height);
            return uvs;
        }
        
        public Block(BlockType type)
        {
            m_type = type;
        }
    }
}