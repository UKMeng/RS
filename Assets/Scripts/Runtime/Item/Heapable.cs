namespace RS.Item
{
    public class Heapable : RsItem
    {
        protected ushort m_heapCapacity = 64;
        protected ushort m_heapCount;

        protected Heapable()
        {
            m_heapCount = 0;
        }

        protected Heapable(ushort heapCount)
        {
            m_heapCount = heapCount;
        }
    }
}