using System;
using Unity.Collections;
using UnityEngine;


namespace RS.Item
{
    public class Block: Heapable
    {
        
        
        protected Block()
        {
        }

        protected Block(ushort heapCount)
            : base(heapCount)
        {
            
        }
        
        
        public static Vector2[][] uvTable;
        public static NativeArray<Vector2> uvTableArray;
        /// <summary>
        /// 在游戏初始化是**必须**初始化的一个步骤
        /// 计算各种Block对应的UV
        /// 后期可能会改成动态合并图集和生成UV
        /// </summary>
        public static void Init()
        {
            uvTable = new Vector2[Enum.GetValues(typeof(BlockType)).Length][];

            uvTable[(int)BlockType.Stone] = CalUVs(0, 0);
            uvTable[(int)BlockType.Dirt] = CalUVs(1, 0);
            uvTable[(int)BlockType.Sand] = CalUVs(0, 1);

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

        private static Vector2[] CalUVs(int x, int y)
        {
            var margin = 0;
            var width = 2048;
            var height = 2048;
            var textureSize = 1024;
            
            var uvs = new Vector2[4];
            uvs[0] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)(y * textureSize + (2 * y + 1) * margin) / height);
            uvs[1] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)(y * textureSize + (2 * y + 1) * margin) / height);
            uvs[2] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)((y + 1) * textureSize + (2 * y + 1) * margin) / height);
            uvs[3] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)((y + 1) * textureSize + (2 * y + 1) * margin) / height);
            return uvs;
        }
    }
}