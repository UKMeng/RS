using RS.GamePlay;
using RS.Scene;
using RS.Utils;
using UnityEngine;

namespace RS.Item
{
    public class Shovel : RsItem
    {
        public override int Capacity => 1;
        public override int Count => 1;
        public override string Name => "铲子";

        public override void Interact(RaycastHit hitInfo, Player player)
        {
            var pos = hitInfo.point;
            var normal = hitInfo.normal;
            var blockPos = RsMath.GetBlockMinCorner(pos, normal);
            var chunkPos = new Vector3Int(Mathf.FloorToInt(blockPos.x / 32.0f), Mathf.FloorToInt(blockPos.y / 16.0f),
                Mathf.FloorToInt(blockPos.z / 32.0f));

            var blockWorldPos = Chunk.WorldPosToBlockWorldPos(blockPos);
            var blockType = SceneManager.Instance.GetBlockType(blockWorldPos);

            // 铲子只能用来铲泥土
            if (!IsBreakable(blockType))
            {
                return;
            }
            
            var blockLocalPos = Chunk.WorldPosToBlockLocalPos(blockPos);
            Debug.Log($"Hit Position: {pos}");

            Debug.Log($"方块坐标: {blockPos}, Chunk坐标: {chunkPos}, 方块本地坐标: {blockLocalPos}");

            // 破坏这个block
            // 首先获取chunk
            var chunk = SceneManager.Instance.GetChunk(chunkPos);
            chunk.ModifyBlock(blockLocalPos, BlockType.Air);
            chunk.UpdateMesh();

            if (blockType == BlockType.Dirt || blockType == BlockType.Grass)
            {
                player.TryAddBlock(BlockType.Dirt);
            }
            else if (blockType == BlockType.Sand)
            {
                player.TryAddBlock(BlockType.Sand);
            }
            

            // 如何y小于127，检查邻居是否有水
            if (blockLocalPos.y < 127)
            {
                var upType = SceneManager.Instance.GetBlockType(blockWorldPos + Vector3Int.up);
                if (upType == BlockType.Water)
                {
                    var water = new Water(blockWorldPos + Vector3Int.up);
                    var sub = new Flow(water);
                    SceneManager.Instance.RegisterTickEvent(sub);
                }

                var leftType = SceneManager.Instance.GetBlockType(blockWorldPos + Vector3Int.left);
                if (leftType == BlockType.Water)
                {
                    var water = new Water(blockWorldPos + Vector3Int.left);
                    var sub = new Flow(water);
                    SceneManager.Instance.RegisterTickEvent(sub);
                }

                var rightType = SceneManager.Instance.GetBlockType(blockWorldPos + Vector3Int.right);
                if (rightType == BlockType.Water)
                {
                    var water = new Water(blockWorldPos + Vector3Int.right);
                    var sub = new Flow(water);
                    SceneManager.Instance.RegisterTickEvent(sub);
                }

                var forwardType = SceneManager.Instance.GetBlockType(blockWorldPos + Vector3Int.forward);
                if (forwardType == BlockType.Water)
                {
                    var water = new Water(blockWorldPos + Vector3Int.forward);
                    var sub = new Flow(water);
                    SceneManager.Instance.RegisterTickEvent(sub);
                }

                var backType = SceneManager.Instance.GetBlockType(blockWorldPos + Vector3Int.back);
                if (backType == BlockType.Water)
                {
                    var water = new Water(blockWorldPos + Vector3Int.back);
                    var sub = new Flow(water);
                    SceneManager.Instance.RegisterTickEvent(sub);
                }
            }

            // 检查是否是边界是否需要更新邻居Chunk的mesh
            if (blockLocalPos.y == 0)
            {
                var neighbor = SceneManager.Instance.GetChunk(chunkPos + Vector3Int.down);
                if (neighbor != null)
                {
                    neighbor.UpdateMesh();
                }
            }
            else if (blockLocalPos.y == 31)
            {
                var neighbor = SceneManager.Instance.GetChunk(chunkPos + Vector3Int.up);
                if (neighbor != null)
                {
                    neighbor.UpdateMesh();
                }
            }

            if (blockLocalPos.z == 0)
            {
                var neighbor = SceneManager.Instance.GetChunk(chunkPos + Vector3Int.back);
                if (neighbor != null)
                {
                    neighbor.UpdateMesh();
                }
            }
            else if (blockLocalPos.z == 31)
            {
                var neighbor = SceneManager.Instance.GetChunk(chunkPos + Vector3Int.forward);
                if (neighbor != null)
                {
                    neighbor.UpdateMesh();
                }
            }

            if (blockLocalPos.x == 0)
            {
                var neighbor = SceneManager.Instance.GetChunk(chunkPos + Vector3Int.left);
                if (neighbor != null)
                {
                    neighbor.UpdateMesh();
                }
            }
            else
            {
                var neighbor = SceneManager.Instance.GetChunk(chunkPos + Vector3Int.right);
                if (neighbor != null)
                {
                    neighbor.UpdateMesh();
                }
            }
        }

        private bool IsBreakable(BlockType blockType)
        {
            switch (blockType)
            {
                case BlockType.Dirt:
                case BlockType.Grass:
                case BlockType.Leaf:
                case BlockType.Sand:
                case BlockType.Snow:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
    }
}