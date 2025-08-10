using System;
using System.Collections.Generic;
using System.IO;
using RS.Item;
using UnityEngine;

namespace RS.Scene
{
    [Serializable]
    public class SaveData
    {
        public List<BlockModifyData> blockModifyData = new List<BlockModifyData>();

        public SaveData(List<BlockModifyData> blockModifyData)
        {
            this.blockModifyData = blockModifyData;
        }
        
    }

    [Serializable]
    public class BlockModifyData
    {
        public Vector3Int chunkPos;
        public List<int> blockIndex;
        public List<BlockType> blockTypes;

        public BlockModifyData(Vector3Int chunkPos)
        {
            this.chunkPos = chunkPos;
            blockIndex = new List<int>();
            blockTypes = new List<BlockType>();
        }
        
        public void AddModify(int index, BlockType blockType)
        {
            for (var i = 0; i < blockIndex.Count; i++)
            {
                if (blockIndex[i] == index)
                {
                    blockTypes[i] = blockType;
                    return;
                }
            }
            
            blockIndex.Add(index);
            blockTypes.Add(blockType);
        }
    }
    
    public static class SaveSystem
    {
        private static string m_savePath = Application.persistentDataPath + "/save.json";

        public static void SaveGame(SaveData data)
        {
            var json = JsonUtility.ToJson(data);
            File.WriteAllText(m_savePath, json);
            Debug.Log($"[Save System] Save game to {m_savePath}");
        }

        public static SaveData LoadGame()
        {
            if (!File.Exists(m_savePath))
            {
                return null;
            }

            var json = File.ReadAllText(m_savePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}