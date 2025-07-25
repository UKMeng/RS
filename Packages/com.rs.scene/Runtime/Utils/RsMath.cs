using System;
using UnityEngine;

namespace RS.Utils
{
    public class RsMath
    {
        public static Vector3 GetBlockMinCorner(Vector3 hitPos, Vector3 hitNormal)
        {
            var corner = hitPos;

            if (hitNormal.x > 0) corner.x -= 1;
            if (hitNormal.y > 0) corner.y -= 0.5f;
            if (hitNormal.z > 0) corner.z -= 1;
            
            return new Vector3(Mathf.Floor(corner.x), FloorToHalfInt(corner.y),  Mathf.Floor(corner.z));
        }

        public static int Mod(int a, int b)
        {
            return (a % b + b) % b;
        }
        
        public static float FloorToHalfInt(float value)
        {
            var floor = Mathf.Floor(value);
            var decimalPart = value - floor;
            
            if (decimalPart < 0.5f)
            {
                return floor;
            }
            else
            {
                return floor + 0.5f;
            }
        }

        public static float Clamp(float value, float min, float max)
        {
            return Mathf.Clamp(value, min, max);
        }

        public static float ClampedLerp(float min, float max, float t)
        {
            if (t < 0.0f)
            {
                return min;
            }

            if (t > 1.0f)
            {
                return max;
            }

            return Mathf.Lerp(min, max, t);
        }
        
        /// <summary>
        /// 将值钳制到[min, max]的区间，然后再映射到[from, to]的范围
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static float ClampedMap(float value, float min, float max, float from, float to)
        {
            return ClampedLerp(from, to, Mathf.InverseLerp(min, max, value));
        }
        

        /// <summary>
        /// https://minecraft.wiki/w/Density_function#squeeze
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float Squeeze(float value)
        {
            var t = Clamp(value, -1.0f, 1.0f);
            return t * 0.5f - t * t * t / 24.0f;
        }
    }
}