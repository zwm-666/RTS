// ============================================================
// UnitData.cs
// 单位数据 ScriptableObject
// ============================================================

using UnityEngine;

namespace RTS.Data
{
    /// <summary>
    /// 单位类型
    /// </summary>
    public enum UnitType
    {
        Worker,         // 工人
        Infantry,       // 步兵
        Ranged,         // 远程
        Cavalry,        // 骑兵
        Siege,          // 攻城单位
        Hero            // 英雄
    }

    /// <summary>
    /// 攻击类型
    /// </summary>
    public enum AttackType
    {
        Normal,         // 普通攻击
        Pierce,         // 穿刺（对轻甲有效）
        Siege,          // 攻城（对建筑有效）
        Magic           // 魔法（忽略护甲）
    }

    /// <summary>
    /// 护甲类型
    /// </summary>
    public enum ArmorType
    {
        None,           // 无护甲
        Light,          // 轻甲
        Medium,         // 中甲
        Heavy,          // 重甲
        Fortified       // 城防（建筑）
    }

    /// <summary>
    /// 单位数据 ScriptableObject
    /// 在 Unity 中创建: 右键 -> Create -> RTS -> Unit Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "RTS/Unit Data", order = 1)]
    public class UnitData : EntityData
    {
        [Header("====== 单位属性 ======")]
        
        [Header("类型")]
        public UnitType unitType = UnitType.Infantry;
        
        [Header("生命值")]
        public int maxHealth = 100;
        public float healthRegen = 0f;          // 每秒生命恢复
        
        [Header("攻击")]
        public int attackDamage = 10;
        public AttackType attackType = AttackType.Normal;
        public float attackSpeed = 1.0f;        // 攻击间隔（秒）
        public float attackRange = 1.5f;        // 攻击范围
        
        [Header("防御")]
        public int armor = 0;                   // 护甲值（减少受到的伤害）
        public ArmorType armorType = ArmorType.None;
        
        [Header("移动")]
        public float moveSpeed = 5f;            // 移动速度
        public float turnSpeed = 180f;          // 转向速度（度/秒）
        
        [Header("视野")]
        public float sightRange = 10f;          // 视野范围
        
        [Header("人口")]
        public int populationCost = 1;          // 占用人口数
        
        [Header("特殊能力（预留）")]
        public bool canGather = false;          // 能否采集资源
        public bool canBuild = false;           // 能否建造
        public bool canHeal = false;            // 能否治疗
        
        [Header("预制体")]
        public GameObject unitPrefab;           // 单位预制体
        
        /// <summary>
        /// 计算对目标的实际伤害
        /// </summary>
        public int CalculateDamage(ArmorType targetArmorType, int targetArmor)
        {
            float damageMultiplier = GetDamageMultiplier(attackType, targetArmorType);
            int rawDamage = Mathf.RoundToInt(attackDamage * damageMultiplier);
            
            // 护甲减伤（简单公式：伤害 - 护甲，最低1点伤害）
            int finalDamage = Mathf.Max(1, rawDamage - targetArmor);
            return finalDamage;
        }
        
        /// <summary>
        /// 获取攻击类型对护甲类型的伤害倍率
        /// </summary>
        private float GetDamageMultiplier(AttackType attack, ArmorType armor)
        {
            // 简化的克制关系表
            switch (attack)
            {
                case AttackType.Pierce:
                    if (armor == ArmorType.Light) return 1.5f;
                    if (armor == ArmorType.Heavy) return 0.5f;
                    break;
                case AttackType.Siege:
                    if (armor == ArmorType.Fortified) return 2.0f;
                    if (armor == ArmorType.Light) return 0.5f;
                    break;
                case AttackType.Magic:
                    return 1.0f; // 忽略护甲类型
            }
            return 1.0f;
        }
    }
}
