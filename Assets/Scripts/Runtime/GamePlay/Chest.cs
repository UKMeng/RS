using UnityEngine;
using UnityEngine.Playables;

namespace RS.GamePlay
{
    public class Chest : MonoBehaviour
    {
        [SerializeField] private Animator m_animator;
        [SerializeField] private PlayableDirector m_openTimeline;

        public void Start()
        {
            m_animator.Play("Idle");
        }

        public void Open()
        {
            m_openTimeline.Play();
        }
    }
}