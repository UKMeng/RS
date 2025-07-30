using UnityEngine;

namespace RS.Scene
{
    public class GameTime : IUpdateByTick
    {
        private uint m_time;
        
        // 一天24分钟
        private uint m_oneDay = 24;
        private uint m_totalTick;
        private uint m_hourTick;
        private uint m_minuteTick;

        public GameTime(uint time)
        {
            m_time = time;
            
            // 目前半秒一次tick
            m_totalTick = m_oneDay * 60 * 2;
            m_hourTick = m_totalTick / 24;
            m_minuteTick = m_hourTick / 60;
        }

        public string GetTime()
        {
            var hour = m_time / m_hourTick;
            var minute = (m_time % m_hourTick) / m_minuteTick;
            return $"{hour:D2}:{minute:D2}";
        }
        
        
        public void OnTick()
        {
            m_time++;
            m_time %= m_totalTick;
        }
    }
}