using RS.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace RS.GamePlay
{
    public class Chest : MonoBehaviour
    {
        [SerializeField] private Animator m_animator;
        [SerializeField] private PlayableDirector m_openTimeline;
        [SerializeField] private TreasureMessage m_message;

        private Treasure m_treasure;

        public void Start()
        {
            m_animator.Play("Idle");
        }

        public void SetTreasure(Treasure treasure)
        {
            m_treasure = treasure;
        }

        public void Open()
        {
            m_openTimeline.Play();
            
            // 等半秒动画播完
            Invoke("TriggerTreasureMessage", 0.5f);
        }

        private void TriggerTreasureMessage()
        {
            m_message.Show(
                $"{m_treasure.name} {m_treasure.desc}",
                () =>
                {
                    // 增加获得物品后处理
                },
                m_treasure.treasurePrefab
            );
        }
    }
}