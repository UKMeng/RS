using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RS.Scene
{
    public interface IUpdateByTick
    {
        int TickTimes { get; set; }
        void OnTick();
    }
    
    public class TickManager : MonoBehaviour
    {
        // 0.5s更新一次
        public float tickInterval = 0.5f;
        private List<IUpdateByTick> m_subscribers;
        private List<IUpdateByTick> m_toRemove;
        private List<Chunk> m_toUpdateChunks;

        public void Awake()
        {
            m_subscribers = new List<IUpdateByTick>();
            m_toRemove = new List<IUpdateByTick>();
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
                    if (sub.TickTimes == 0)
                    {
                        m_toRemove.Add(sub);
                        continue;
                    }

                    if (sub.TickTimes != -1)
                    {
                        sub.TickTimes--;
                    }
                    
                    sub.OnTick();
                }

                if (m_toRemove.Count > 0)
                {
                    foreach (var sub in m_toRemove)
                    {
                        m_subscribers.Remove(sub);
                    }
                    m_toRemove.Clear();
                }

                // 延迟在本tick更新的chunk mesh
                if (m_toUpdateChunks.Count > 0)
                {
                    Chunk.BuildMeshUsingJobSystem(m_toUpdateChunks);
                    m_toUpdateChunks.Clear();
                    
                    
                    // if (m_toUpdateChunks.Count < 51)
                    // {
                    //     Chunk.BuildMeshUsingJobSystem(m_toUpdateChunks);
                    //     m_toUpdateChunks.Clear();
                    //     continue;
                    // }
                    //
                    // // 分帧生成，每次只处理距离最近的10个Chunk mesh
                    // // top k
                    // var pos = RsSceneManager.Instance.GetPlayerPos();
                    // var k = 50;
                    // var maxHeap = new SortedList<float, Chunk>(new DescComparer());
                    //
                    // foreach (var chunk in m_toUpdateChunks)
                    // {
                    //     var chunkPos = Chunk.ChunkPosToWorldPos(chunk.chunkPos);
                    //     var dist = Vector3.Distance(pos, chunkPos);
                    //     if (maxHeap.ContainsKey(dist))
                    //     {
                    //         dist += 0.001f;
                    //     }
                    //
                    //     if (maxHeap.Count < k)
                    //     {
                    //         maxHeap.Add(dist, chunk);
                    //     }
                    //     else if (dist < maxHeap.Keys[0])
                    //     {
                    //         maxHeap.RemoveAt(0);
                    //         maxHeap.Add(dist, chunk);
                    //     }
                    // }
                    //
                    // var topK = new List<Chunk>(maxHeap.Values);
                    // Chunk.BuildMeshUsingJobSystem(topK);
                    //
                    // foreach (var chunk in topK)
                    // {
                    //     m_toUpdateChunks.Remove(chunk);
                    // }
                }
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
        
        class DescComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                // 降序排列
                return y.CompareTo(x);
            }
        }
    }
}
