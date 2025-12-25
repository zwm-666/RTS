// ============================================================
// TileType.cs
// 地形类型枚举
// ============================================================

namespace RTS.Map
{
    /// <summary>
    /// 地形类型枚举
    /// </summary>
    public enum TileType
    {
        Plain,      // 平地 - 所有单位可通行
        Highland,   // 高地 - 所有单位可通行，提供视野和攻击加成
        Lava,       // 熔岩 - 不可通行
        Water,      // 水域 - 仅部分单位可通行（如船只）
        Forest,     // 森林 - 可通行，提供隐蔽
        Mountain    // 山地 - 不可通行
    }

    /// <summary>
    /// 地形属性数据
    /// </summary>
    public static class TileTypeExtensions
    {
        /// <summary>
        /// 获取地形的移动消耗（用于寻路权重）
        /// </summary>
        public static float GetMovementCost(this TileType type)
        {
            switch (type)
            {
                case TileType.Plain:    return 1.0f;
                case TileType.Highland: return 1.5f;
                case TileType.Forest:   return 2.0f;
                case TileType.Water:    return 3.0f;  // 需要特殊单位
                case TileType.Lava:     return float.MaxValue;
                case TileType.Mountain: return float.MaxValue;
                default:                return 1.0f;
            }
        }

        /// <summary>
        /// 判断地形是否默认可通行
        /// </summary>
        public static bool IsDefaultWalkable(this TileType type)
        {
            switch (type)
            {
                case TileType.Plain:
                case TileType.Highland:
                case TileType.Forest:
                    return true;
                case TileType.Water:
                case TileType.Lava:
                case TileType.Mountain:
                    return false;
                default:
                    return true;
            }
        }
    }
}
