using RS.GamePlay;
using UnityEngine;

namespace RS.Item
{
    public class RsItem
    {
        public virtual int Capacity => 1;
        public virtual int Count => 0;
        public virtual string Name => "Unknown";

        public virtual void Interact(RaycastHit hitInfo, Player player)
        {
        }
    }
}