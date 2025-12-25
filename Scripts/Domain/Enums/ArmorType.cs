// ============================================================
// ArmorType.cs - 护甲类型枚举（领域层）
// ============================================================

namespace RTS.Domain.Enums
{
    /// <summary>
    /// 护甲类型
    /// </summary>
    public enum ArmorType
    {
        None,       // 无护甲
        Light,      // 轻甲
        Medium,     // 中甲
        Heavy,      // 重甲
        Fortified,  // 城防（建筑）
        Divine      // 神圣
    }
}
