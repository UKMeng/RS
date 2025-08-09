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
        private Player m_player;
        private SceneManager m_sceneManager;
        

        private void Awake()
        {
            var sceneRoot = GameObject.Find("SceneRoot");
            m_sceneManager = sceneRoot.GetComponent<SceneManager>();
            m_player = GetComponent<Player>();
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
                var block = m_sceneManager.GetBlockType(Chunk.WorldPosToBlockWorldPos(blockPos));
                
                // 水无法选中
                if (block == BlockType.Water || block == BlockType.Air)
                {
                    return;
                }

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
        
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = true;
            }
            
            // 从屏幕中心发出射线
            var screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
            var ray = Camera.main.ScreenPointToRay(screenCenter);

            if (Physics.Raycast(ray, out var hitInfo, rayDistance))
            {
                // 优先和场景物互动
                if (hitInfo.collider.gameObject.name == "Chest")
                {
                    var chest = hitInfo.collider.transform.parent.gameObject.GetComponent<Chest>();
                    chest.Open();
                    Debug.Log("Hit a Chest");
                }
                else if (hitInfo.collider.gameObject.name == "ReturnRock")
                {
                    var rock = hitInfo.collider.transform.parent.gameObject.GetComponent<ReturnRock>();
                    rock.Trigger();
                    Debug.Log("Hit a Return Rock");
                }

                if (m_player.HandItem != null)
                {
                    m_player.HandItem.Interact(hitInfo, m_player);
                }
            }
        }

        // public void OnPut(InputAction.CallbackContext context)
        // {
        //     // 从屏幕中心发出射线
        //     var screenCenter = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
        //     var ray = Camera.main.ScreenPointToRay(screenCenter);
        //
        //     if (Physics.Raycast(ray, out var hitInfo, rayDistance))
        //     {
        //         var pos = hitInfo.point;
        //         var normal = hitInfo.normal;
        //         var blockPos = RsMath.GetBlockMinCorner(pos, normal);
        //         
        //         // 根据normal判断放置block的位置
        //         if (normal == Vector3.up)
        //         {
        //             blockPos.y += 0.5f;
        //         }
        //         else if (normal == Vector3.down)
        //         {
        //             blockPos.y -= 0.5f;
        //         }
        //         else if (normal == Vector3.left)
        //         {
        //             blockPos.x -= 1;
        //         }
        //         else if (normal == Vector3.right)
        //         {
        //             blockPos.x += 1;
        //         }
        //         else if (normal == Vector3.forward)
        //         {
        //             blockPos.z += 1;
        //         }
        //         else if (normal == Vector3.back)
        //         {
        //             blockPos.z -= 1;
        //         }
        //
        //         var blockWorldPos = Chunk.WorldPosToBlockWorldPos(blockPos);
        //         // var chunkPos = new Vector3Int(Mathf.FloorToInt(blockPos.x / 32.0f), Mathf.FloorToInt(blockPos.y / 16.0f), Mathf.FloorToInt(blockPos.z / 32.0f));
        //         // var blockLocalPos = Chunk.WorldPosToBlockLocalPos(blockPos);
        //         Debug.Log($"Hit Position: {pos}");
        //         Debug.Log($"Block World Pos: {blockWorldPos}");
        //
        //         var handBlock = m_player.HandItem;
        //         if (handBlock != BlockType.Air)
        //         {
        //             SceneManager.Instance.PlaceBlock(blockWorldPos, handBlock);
        //         }
        //         
        //
        //         
        //         // Debug.Log($"方块坐标: {blockPos}, Chunk坐标: {chunkPos}, 方块本地坐标: {blockLocalPos}");
        //         // SceneManager.Instance.PlaceBlock(blockWorldPos, BlockType.Water);
        //         // SceneManager.Instance.RegisterTickEvent(new Flow(new Water(blockWorldPos)));
        //         
        //         // 放置这个block
        //         // 首先获取chunk
        //         // var chunk = m_sceneManager.GetChunk(chunkPos);
        //         // chunk.ModifyBlock(blockLocalPos, BlockType.Stone);
        //     }
        // }
    }
}