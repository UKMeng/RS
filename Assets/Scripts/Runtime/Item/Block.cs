using System;
using System.Collections.Generic;
using System.Linq;
using RS.GamePlay;
using RS.Scene;
using RS.Utils;
using Unity.Collections;
using UnityEngine;


namespace RS.Item
{
    public enum BlockType
    {
        Air,
        Stone,
        Dirt,
        Grass,
        Snow,
        Leaf,
        Orc,
        Sand,
        Water,
        BedRock,
    }
    
    public class Block : RsItem
    {
        private int m_capacity = 3;
        private int m_count = 0;

        public override int Id => -1;
        
        public override int Capacity
        {
            get => m_capacity;
        }

        public override int Count
        {
            get => m_count;
        }

        public override string Name
        {
            get => m_type.ToString();
        }

        public void ExtentCapacity(int newCapacity)
        {
            m_capacity = newCapacity;
        }

        public static readonly Color[] BlockColors =
        {
            RsColor.Unknown,
            RsColor.Stone,
            RsColor.Dirt,
            RsColor.Grass,
            RsColor.Snow,
            RsColor.Leaf,
            RsColor.Orc,
            RsColor.Sand,
            RsColor.Water,
            RsColor.BedRock,
        };
        
        private BlockType m_type;
        
        public BlockType Type => m_type;
        
        public Block(BlockType type)
        {
            m_type = type;
            m_count = 1;
        }

        public Block(BlockType type, int capacity)
        {
            m_type = type;
            m_capacity = capacity;
            m_count = 1;
        }

        public override void Interact(RaycastHit hitInfo, Player player)
        {
            var pos = hitInfo.point;
            var normal = hitInfo.normal;
            var blockPos = RsMath.GetBlockMinCorner(pos, normal);
            
            // 根据normal判断放置block的位置
            if (normal == Vector3.up)
            {
                blockPos.y += 0.5f;
            }
            else if (normal == Vector3.down)
            {
                blockPos.y -= 0.5f;
            }
            else if (normal == Vector3.left)
            {
                blockPos.x -= 1;
            }
            else if (normal == Vector3.right)
            {
                blockPos.x += 1;
            }
            else if (normal == Vector3.forward)
            {
                blockPos.z += 1;
            }
            else if (normal == Vector3.back)
            {
                blockPos.z -= 1;
            }
        
            var blockWorldPos = Chunk.WorldPosToBlockWorldPos(blockPos);
            // var chunkPos = new Vector3Int(Mathf.FloorToInt(blockPos.x / 32.0f), Mathf.FloorToInt(blockPos.y / 16.0f), Mathf.FloorToInt(blockPos.z / 32.0f));
            // var blockLocalPos = Chunk.WorldPosToBlockLocalPos(blockPos);
            Debug.Log($"Hit Position: {pos}");
            Debug.Log($"Block World Pos: {blockWorldPos}");
        
            
            RsSceneManager.Instance.PlaceBlock(blockWorldPos, m_type);

            m_count--;
            player.UseItem();
            player.PlayMining();

            if (m_count == 0)
            {
                player.DisposeItem(this);
            }
            // Debug.Log($"方块坐标: {blockPos}, Chunk坐标: {chunkPos}, 方块本地坐标: {blockLocalPos}");
            // RsSceneManager.Instance.PlaceBlock(blockWorldPos, BlockType.Water);
            // RsSceneManager.Instance.RegisterTickEvent(new Flow(new Water(blockWorldPos)));

            // 放置这个block
            // 首先获取chunk
            // var chunk = m_sceneManager.GetChunk(chunkPos);
            // chunk.ModifyBlock(blockLocalPos, BlockType.Stone);
        }

        public void Add()
        {
            m_count++;
        }
        
        /// <summary>
        /// 一些用于渲染的静态方法
        /// </summary>
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

            uvTable[(int)BlockType.Stone] = CalUVs((0, 0), (0, 0),(0, 0));
            uvTable[(int)BlockType.Dirt] = CalUVs((1, 0), (1, 0), (1, 0));
            uvTable[(int)BlockType.Grass] = CalUVs((3, 1), (1, 0), (3, 0), true);
            uvTable[(int)BlockType.Snow] = CalUVs((2, 1), (1, 0), (2, 0), true);
            uvTable[(int)BlockType.Orc] = CalUVs((1, 2), (1, 2), (1, 1));
            uvTable[(int)BlockType.Leaf] = CalUVs((0, 2), (0, 2), (0, 2));
            uvTable[(int)BlockType.Sand] = CalUVs((0, 1), (0, 1),(0, 1));
            uvTable[(int)BlockType.BedRock] = CalUVs((2, 2), (2, 2), (2, 2));

            uvTableArray = new NativeArray<Vector2>(uvTable.Length * 16, Allocator.Persistent);

            for (var i = 0; i < uvTable.Length; i++)
            {
                if (uvTable[i] == null)
                {
                    continue;
                }

                var index = i * 16;
                for (var j = 0; j < 16; j++)
                {
                    uvTableArray[index + j] = uvTable[i][j];
                }
            }
        }

        public static void UnInit()
        {
            uvTableArray.Dispose();
        }

        /// <summary>
        /// 计算4组uv，1组top，1组bottom，2组side
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        private static Vector2[] CalUVs((int, int) top, (int, int) bottom, (int, int) side, bool onlyHalf = false)
        {
            var uvs = new List<Vector2>();
            
            uvs.AddRange(CalUVs(top.Item1, top.Item2, 0));
            uvs.AddRange(CalUVs(bottom.Item1, bottom.Item2, 0));
            uvs.AddRange(CalUVs(side.Item1, side.Item2, onlyHalf ? 2 : 1));
            uvs.AddRange(CalUVs(side.Item1, side.Item2, 2));
            
            
            return uvs.ToArray();
        }

        private static Vector2[] CalUVs(int x, int y, int half)
        {
            var margin = 0;
            var width = 4096;
            var height = 4096;
            var textureSize = 1024;

            var uvs = new Vector2[4];
            if (half == 0)
            {
                uvs[0] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)(y * textureSize + (2 * y + 1) * margin) / height);
                uvs[1] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)(y * textureSize + (2 * y + 1) * margin) / height);
                uvs[2] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)((y + 1) * textureSize + (2 * y + 1) * margin) / height);
                uvs[3] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)((y + 1) * textureSize + (2 * y + 1) * margin) / height);
            }
            else if (half == 1)
            {
                uvs[0] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)(y * textureSize + (2 * y + 1) * margin) / height);
                uvs[1] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)(y * textureSize + (2 * y + 1) * margin) / height);
                uvs[2] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)((y + 0.5f) * textureSize + (2 * y + 1) * margin) / height);
                uvs[3] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)((y + 0.5f) * textureSize + (2 * y + 1) * margin) / height);
            }
            else if (half == 2)
            {
                uvs[0] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)((y + 0.5f) * textureSize + (2 * y + 1) * margin) / height);
                uvs[1] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)((y + 0.5f) * textureSize + (2 * y + 1) * margin) / height);
                uvs[2] = new Vector2((float)((x + 1) * textureSize + (2 * x + 1) * margin) / width, (float)((y + 1f) * textureSize + (2 * y + 1) * margin) / height);
                uvs[3] = new Vector2((float)(x * textureSize + (2 * x + 1) * margin) / width, (float)((y + 1f) * textureSize + (2 * y + 1) * margin) / height);
            }
            return uvs;
        }
    }
}