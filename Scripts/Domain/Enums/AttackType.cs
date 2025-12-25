// ============================================================
// AttackType.cs - 攻击类型枚举（领域层）
// ============================================================

namespace RTS.Domain.Enums
{
    /// <summary>
    /// 攻击类型
    /// </summary>
    public enum AttackType
    {
        Normal,     // 普通攻击
        Pierce,     // 穿刺（对轻甲有效）
        Magic,      // 魔法（对重甲有效）
        Siege,      // 攻城（对建筑有效）
        Hero        // 英雄（不受护甲影响）
    }
}
