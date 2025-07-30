using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RS.Scene
{
    public interface IUpdateByTick
    {
        void OnTick();
    }
    
    public class TickManager : MonoBehaviour
    {
        // 0.5s更新一次
        public float tickInterval = 0.5f;
        private List<IUpdateByTick> subscribers;

        public void Awake()
        {
            subscribers = new List<IUpdateByTick>();
        }

        public void Start()
        {
            StartCoroutine(Tick());
        }

        private IEnumerator Tick()
        {
            while (true)
            {
                yield return new WaitForSeconds(tickInterval);
                foreach (var sub in subscribers)
                {
                    sub.OnTick();
                }
            }
        }
        

        public void Register(IUpdateByTick sub)
        {
            if (!subscribers.Contains(sub))
            {
                subscribers.Add(sub);
            }
        }

        public void Unregister(IUpdateByTick sub)
        {
            subscribers.Remove(sub);
        }
    }
}
