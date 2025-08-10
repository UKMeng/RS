using System.Collections.Generic;
using RS.Item;
using UnityEngine;

namespace RS.Scene
{
    public class BlockModifyRecorder
    {
        private Dictionary<Vector3Int, BlockModifyData> m_modifyData;
        
        public void Init(SaveData save)
        {
            m_modifyData = new Dictionary<Vector3Int, BlockModifyData>();
            
            if (save != null)
            {
                foreach (var data in save.blockModifyData)
                {
                    m_modifyData.Add(data.chunkPos, data);
                }
            }
        }

        public void AddModifyData(Vector3Int chunkPos, int blockIndex, BlockType blockType)
        {
            if (m_modifyData.ContainsKey(chunkPos))
            {
                m_modifyData[chunkPos].AddModify(blockIndex, blockType);
            }
            else
            {
                var newData = new BlockModifyData(chunkPos);
                newData.AddModify(blockIndex, blockType);
                m_modifyData.Add(chunkPos, newData);
            }
        }

        public BlockModifyData GetModifyData(Vector3Int chunkPos)
        {
            if (m_modifyData.ContainsKey(chunkPos))
            {
                return m_modifyData[chunkPos];
            }

            return null;
        }

        public List<BlockModifyData> GetModifyDataList()
        {
            return new List<BlockModifyData>(m_modifyData.Values);
        }
    }
}