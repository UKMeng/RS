using System;
using System.Collections.Generic;
using System.IO;
using RS.GamePlay;
using RS.Item;
using UnityEngine;

namespace RS.Scene
{
    [Serializable]
    public class SaveData
    {
        public List<BlockModifyData> blockModifyData = new List<BlockModifyData>();
        public PlayerData playerData;
        
        public SaveData(List<BlockModifyData> blockModifyData, PlayerData playerData)
        {
            this.blockModifyData = blockModifyData;
            this.playerData = playerData;
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

    [Serializable]
    public class PlayerData
    {
        public PlayerStatus status;
        public Vector3 birthPosition;
        public bool firstNight;
        public bool firstWater;
        public List<int> treasure;

        public PlayerData(PlayerStatus status, Vector3 birthPosition, bool firstNight, bool firstWater,
            List<int> treasure)
        {
            this.status = status;
            this.birthPosition = birthPosition;
            this.firstNight = firstNight;
            this.firstWater = firstWater;
            this.treasure = treasure;
        }
    }
    
    public static class SaveSystem
    {
        private static string m_savePath = Application.persistentDataPath + "/save.json";
        private static string m_initSavePath = $"{Application.streamingAssetsPath}/Config/InitSave.json";

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
                File.Copy(m_initSavePath, m_savePath);
            }

            var json = File.ReadAllText(m_savePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
    }
}