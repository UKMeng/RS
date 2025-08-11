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
        private RsSceneManager m_sceneManager;
        

        private void Awake()
        {
            var sceneRoot = GameObject.Find("SceneRoot");
            m_sceneManager = sceneRoot.GetComponent<RsSceneManager>();
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

                    if (m_player.Status == PlayerStatus.FirstTime)
                    {
                        m_player.InvokeTips("恭喜你找到第一个宝箱！获得了一把铲子,它能铲出泥块和沙块。接下去去找R标记的传送点。");
                        m_player.StatusChange(PlayerStatus.GetShovel);
                        chest.Open(true);
                        return;
                    }

                    if (m_player.Status == PlayerStatus.OnceMainGame)
                    {
                        m_player.InvokeTips("恭喜你获得斧头，但需要在日落前回到传送点才能带回去。");
                        m_player.StatusChange(PlayerStatus.GetAxe);
                        chest.Open(true);
                        return;
                    }

                    if (m_player.Status == PlayerStatus.GetAxe)
                    {
                        m_player.InvokeTips("恭喜你获得镐子，现在可以破坏普通石头了，记得回去。");
                        m_player.StatusChange(PlayerStatus.GetPickaxe);
                        chest.Open(true);
                        return;
                    }
                    
                    
                    chest.Open(false, m_player);
                    // Debug.Log("Hit a Chest");
                }
                else if (hitInfo.collider.gameObject.name == "ReturnRock")
                {
                    if (m_player.Status == PlayerStatus.FirstTime)
                    {
                        m_player.InvokeTips("你需要先找到宝箱。按Q打开地图观察位置。");
                        return;
                    }

                    if (RsSceneManager.Instance.InHome)
                    {
                        // Home弹出开始游戏的UI
                        RsSceneManager.Instance.ToggleGameStartUI();
                        return;
                    }
                    
                    var rock = hitInfo.collider.transform.parent.gameObject.GetComponent<ReturnRock>();
                    rock.Trigger();
                    // Debug.Log("Hit a Return Rock");
                }

                if (m_player.HandItem != null)
                {
                    m_player.HandItem.Interact(hitInfo, m_player);
                }
            }
        }
    }
}