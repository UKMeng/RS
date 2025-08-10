using RS.UI;
using UnityEngine;

namespace RS.GamePlay
{
    public class Treasure
    {
        public GameObject treasurePrefab;
        public string name;
        public string desc;

        public Treasure(GameObject treasurePrefab, string name, string desc)
        {
            this.treasurePrefab = treasurePrefab;
            this.name = name;
            this.desc = desc;
        }
    }
}