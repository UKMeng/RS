using UnityEngine;

namespace RS.Scene
{
    public class GameTime : IUpdateByTick
    {
        private uint m_time;
        
        // 一天12分钟
        private uint m_oneDay = 12;
        private uint m_totalTick;
        private uint m_hourTick;
        private uint m_minuteTick;

        public int TickTimes { get; set; } = -1;

        public GameTime(uint time)
        {
            m_time = time;
            
            // 目前半秒一次tick
            m_totalTick = m_oneDay * 60 * 2;
            m_hourTick = m_totalTick / 24;
            m_minuteTick = m_hourTick / 60;
        }

        public uint GetHour()
        {
            return m_time / m_hourTick;
        }

        public uint GetMinute()
        {
            return (m_time % m_hourTick) / m_minuteTick;
        }

        /// <summary>
        /// 得到一天的进度，0.0f-1.0f
        /// </summary>
        /// <returns></returns>
        public float GetDayProgress()
        {
            return (float)m_time / m_totalTick;
        }
        
        public string GetTime()
        {
            var hour = GetHour();
            var minute = GetMinute();
            return $"{hour:D2}:{minute:D2}";
        }

        public void SetTime(uint hour, uint minute)
        {
            m_time = (hour * m_hourTick + minute * m_minuteTick) % m_totalTick;
        }
        
        
        public void OnTick()
        {
            m_time++;
            m_time %= m_totalTick;
        }
    }
}