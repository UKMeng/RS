using System;
using System.Security.Cryptography;
using UnityEngine;
using Random = System.Random;

namespace RS.Utils
{
    public class RsRandom
    {
        private static RsRandom s_instance;

        public static RsRandom Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var seed = GetSeed();
                    Debug.Log($"[Random] RsRandom Instance Init with Seed {seed}");
                    s_instance = new RsRandom(seed);
                }

                return s_instance;
            }
        }

        public static RsRandom Init(long seed)
        {
            Debug.Log($"[Random] RsRandom Instance Init with Seed {seed}");
            s_instance = new RsRandom(seed);
            return s_instance;
        }
        
        private ulong m_seed;
        private ulong a = 2862933555777941756;
        private ulong b = 3037000493;
        private ulong c = 1;
        private ulong mod = 1UL << 63;

        private ulong m_state;
        
        public RsRandom(long seed)
        {
            // 转成无符号数便于计算
            m_seed = (ulong)(seed - long.MinValue);

            m_state = m_seed % mod;
        }

        private void Next()
        {
            // 采用二次同余法(quadratic congruential generator)生成随机数
            m_state = (a * m_state * m_state + b * m_state + c) % mod;
        }

        public ulong NextULong()
        {
            Next();
            return m_state;
        }

        /// <summary>
        /// 生成[0,1)之间的随机浮点数
        /// </summary>
        /// <returns></returns>
        public float NextFloat()
        {
            Next();
            return (float) m_state / mod;
        }

        /// <summary>
        /// 生成[min,max)之间的随机整数
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public int NextInt(int min, int max)
        {
            Next();

            // 暂时不在这里检查min < max
            var range = (ulong)(max - min);
            return (int) (m_state % range) + min;
        }
        
        
        /// <summary>
        /// 获取随机64位种子
        /// </summary>
        /// <returns></returns>
        public static long GetSeed()
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
    }
}