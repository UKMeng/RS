using UnityEngine;
using RS.UI;
using RS.Scene;

namespace RS.GamePlay
{
    public class ReturnRock : MonoBehaviour
    {
        [SerializeField] private ConfirmDialog m_dialog;

        public void Trigger()
        {
            m_dialog.Show("是否返回主城？",
                () => { RsSceneManager.Instance.ReturnHome(true); },
                () => { Debug.Log("取消"); }
            );
        }
    }
}