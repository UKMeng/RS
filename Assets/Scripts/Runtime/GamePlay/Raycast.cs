using UnityEngine;
using UnityEngine.InputSystem;

using RS.Utils;
using RS.Scene;
using RS.Item;
using RS.GamePlay;

namespace RS.GamePlay
{
    public class Raycast : MonoBehaviour
    {
        public float rayDistance = 10.0f;
        public GameObject outlinePrefab;

        private GameObject m_outline;
        private SceneManager m_sceneManager;
        private PlayerInput m_PlayerInput;
        

        private void Awake()
        {
            var sceneRoot = GameObject.Find("SceneRoot");
            m_sceneManager = sceneRoot.GetComponent<SceneManager>();
            m_PlayerInput = GetComponent<PlayerInput>();
            m_outline = Instantiate(outlinePrefab, Vector3.down, Quaternion.identity);
            m_outline.SetActive(false);
        }

        public void Update()
        {
            // 从屏幕中心发出射线
            var screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
            var ray = Camera.main.ScreenPointToRay(screenCenter);

            if (Physics.Raycast(ray, out var hitInfo, rayDistance))
            {
                var pos = hitInfo.point;
                var normal = hitInfo.normal;
                var blockPos = RsMath.GetBlockMinCorner(pos, normal);

                // 命中的block套上黑框
                // TODO: 收到实际aabb大小影响
                m_outline.transform.position = blockPos;
                m_outline.SetActive(true);
            }
            else
            {
                m_outline.SetActive(false);
            }
        }
        
        public void OnAttack(InputValue value)
        {
            // 从屏幕中心发出射线
            var screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
            var ray = Camera.main.ScreenPointToRay(screenCenter);

            if (Physics.Raycast(ray, out var hitInfo, rayDistance))
            {
                var pos = hitInfo.point;
                var normal = hitInfo.normal;
                var blockPos = RsMath.GetBlockMinCorner(pos, normal);
                var chunkPos = new Vector3Int(Mathf.FloorToInt(blockPos.x / 32.0f), Mathf.FloorToInt(blockPos.y / 16.0f), Mathf.FloorToInt(blockPos.z / 32.0f));
                var blockLocalPos = Chunk.WorldPosToBlockLocalPos(blockPos);
                Debug.Log($"Hit Position: {pos}");

                Debug.Log($"方块坐标: {blockPos}, Chunk坐标: {chunkPos}, 方块本地坐标: {blockLocalPos}");
                
                // 破坏这个block
                // 首先获取chunk
                var chunk = m_sceneManager.GetChunk(chunkPos);
                chunk.ModifyBlock(blockLocalPos, BlockType.Air);
                chunk.UpdateMesh();
                
                // 检查是否是边界是否需要更新邻居Chunk的mesh
                if (blockLocalPos.y == 0)
                {
                    var neighbor = m_sceneManager.GetChunk(chunkPos + Vector3Int.down);
                    if (neighbor != null)
                    {
                        neighbor.UpdateMesh();
                    }
                }
                else if (blockLocalPos.y == 31)
                {
                    var neighbor = m_sceneManager.GetChunk(chunkPos + Vector3Int.up);
                    if (neighbor != null)
                    {
                        neighbor.UpdateMesh();
                    }
                }

                if (blockLocalPos.z == 0)
                {
                    var neighbor = m_sceneManager.GetChunk(chunkPos + Vector3Int.back);
                    if (neighbor != null)
                    {
                        neighbor.UpdateMesh();
                    }
                }
                else if (blockLocalPos.z == 31)
                {
                    var neighbor = m_sceneManager.GetChunk(chunkPos + Vector3Int.forward);
                    if (neighbor != null)
                    {
                        neighbor.UpdateMesh();
                    }
                }

                if (blockLocalPos.x == 0)
                {
                    var neighbor = m_sceneManager.GetChunk(chunkPos + Vector3Int.left);
                    if (neighbor != null)
                    {
                        neighbor.UpdateMesh();
                    }
                }
                else
                {
                    var neighbor = m_sceneManager.GetChunk(chunkPos + Vector3Int.right);
                    if (neighbor != null)
                    {
                        neighbor.UpdateMesh();
                    }
                }
            }
        }

        public void OnPut(InputValue value)
        {
            // 从屏幕中心发出射线
            var screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
            var ray = Camera.main.ScreenPointToRay(screenCenter);

            if (Physics.Raycast(ray, out var hitInfo, rayDistance))
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
                // Debug.Log($"方块坐标: {blockPos}, Chunk坐标: {chunkPos}, 方块本地坐标: {blockLocalPos}");
                SceneManager.Instance.PlaceBlock(blockWorldPos, BlockType.Water);
                SceneManager.Instance.RegisterTickEvent(new Flow(new Water(blockWorldPos)));
                
                // 放置这个block
                // 首先获取chunk
                // var chunk = m_sceneManager.GetChunk(chunkPos);
                // chunk.ModifyBlock(blockLocalPos, BlockType.Stone);
            }
        }
    }
}