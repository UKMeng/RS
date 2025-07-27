using UnityEngine;

using RS.Utils;

namespace RS.Scene.Biome
{
    public enum BiomeType
    {
        Unknown,
        Ocean,
        River,
        SnowPlain,
        Plain,
        SnowForest,
        Forest,
        Desert,
        StoneShore,
        Beach,
        Nearland,
        Inland
    }

    public class BiomeColor
    {
        // Biome类型对应颜色数组
        public static readonly Color[] Colors =
        {
            RsColor.Unknown,
            RsColor.Ocean,
            RsColor.River,
            RsColor.SnowPlain,
            RsColor.Plain,
            RsColor.SnowForest,
            RsColor.Forest,
            RsColor.Desert,
            RsColor.StoneShore,
            RsColor.Beach,
            RsColor.Nearland,
            RsColor.Inland,
        };
    }
}