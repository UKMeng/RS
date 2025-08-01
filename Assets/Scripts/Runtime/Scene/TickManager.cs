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
        private List<IUpdateByTick> m_subscribers;
        private List<Chunk> m_toUpdateChunks;

        public void Awake()
        {
            m_subscribers = new List<IUpdateByTick>();
            m_toUpdateChunks = new List<Chunk>();
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
                foreach (var sub in m_subscribers)
                {
                    sub.OnTick();
                }

                // 延迟在本tick更新的chunk mesh
                foreach (var chunk in m_toUpdateChunks)
                {
                    chunk.UpdateMesh();
                }
                m_toUpdateChunks.Clear();
            }
        }
        

        public void Register(IUpdateByTick sub)
        {
            if (!m_subscribers.Contains(sub))
            {
                m_subscribers.Add(sub);
            }
        }

        public void UpdateChunkMeshOnTick(Chunk chunk)
        {
            if (!m_toUpdateChunks.Contains(chunk))
            {
                m_toUpdateChunks.Add(chunk);
            }
        }

        public void Unregister(IUpdateByTick sub)
        {
            m_subscribers.Remove(sub);
        }
    }
}
