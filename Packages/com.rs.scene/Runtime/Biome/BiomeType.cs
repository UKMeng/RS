using UnityEngine;

using RS.Utils;

namespace RS.Scene.Biome
{
    public enum BiomeType
    {
        Ocean,
        River,
        SnowPlain,
        Plain,
        SnowForest,
        Forest,
        Desert,
        StoneShore,
        Beach,
        Coast,
        Nearland,
        Inland
    }

    public class BiomeColor
    {
        // Biome类型对应颜色数组
        public static readonly Color[] Colors =
        {
            RsColor.Ocean,
            RsColor.River,
            RsColor.SnowPlain,
            RsColor.Plain,
            RsColor.SnowForest,
            RsColor.Forest,
            RsColor.Desert,
            RsColor.StoneShore,
            RsColor.Beach,
            RsColor.Coast,
            RsColor.Nearland,
            RsColor.Inland,
        };
    }
}