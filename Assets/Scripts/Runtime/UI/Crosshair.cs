using UnityEngine;

namespace RS.UI
{
    public class Crosshair : MonoBehaviour
    {
        void OnGUI()
        {
            var size = 20;
            var posX = Screen.width / 2 - size / 2;
            var posY = Screen.height / 2 - size / 2;

            GUI.Label(new Rect(posX, posY, size, size), "+");
        }
    }
}