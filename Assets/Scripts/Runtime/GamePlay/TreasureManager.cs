using System.Collections.Generic;
using UnityEngine;

namespace RS.GamePlay
{
    public class TreasureManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> m_treasurePrefabs;
        private List<Treasure> m_treasures;
        
        // 先硬编码了
        private List<string> m_descs;
        private List<Vector3> m_positions;

        public void Start()
        {
            var count = m_treasurePrefabs.Count;
            m_treasures = new List<Treasure>(count);
            m_descs = new List<string>()
            {
                "神秘的企鹅模型，拥有时最大耐力+10",
                "神秘的企鹅模型，拥有时最大生命+10",
                "神秘的企鹅模型，拥有时方块最大持有量+2"
            };
            m_positions = new List<Vector3>()
            {
                new Vector3(-13632.5f, 68.513f, 52.63f),
                new Vector3(-13631.53f, 69.011f, 52.63f),
                new Vector3(-13630.44f, 68.563f, 52.63f)
            };
            for (var id = 0; id < count; id++)
            {
                m_treasures.Add(new Treasure(m_treasurePrefabs[id], id, m_descs[id]));
            }
        }

        public Vector3 GetPosition(int id)
        {
            return m_positions[id];
        }
        
        public Treasure GetTreasure(int id)
        {
            return m_treasures[id];
        }
    }
    
    public class Treasure
    {
        public GameObject treasurePrefab;
        public int id;
        public string desc;

        public Treasure(GameObject treasurePrefab, int id, string desc)
        {
            this.treasurePrefab = treasurePrefab;
            this.id = id;
            this.desc = desc;
        }
    }
}