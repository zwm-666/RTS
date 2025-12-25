// ============================================================
// UnitData.cs - 单位领域数据（纯POCO，无Unity依赖）
// ============================================================

using RTS.Domain.Enums;

namespace RTS.Domain.Entities
{
    /// <summary>
    /// 单位领域数据
    /// 纯 POCO 对象，不依赖 UnityEngine
    /// </summary>
    public class UnitData
    {
        #region 唯一标识
        
        /// <summary>
        /// 单位唯一ID
        /// </summary>
        public string UnitId { get; set; }
        
        #endregion

        #region 基础信息
        
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 阵营
        /// </summary>
        public string Faction { get; set; }
        
        /// <summary>
        /// 类别（Basic/Combat/Special）
        /// </summary>
        public string Category { get; set; }
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        
        #endregion

        #region 战斗属性
        
        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth { get; set; }
        
        /// <summary>
        /// 生命恢复（每秒）
        /// </summary>
        public float HealthRegen { get; set; }
        
        /// <summary>
        /// 攻击力
        /// </summary>
        public int AttackDamage { get; set; }
        
        /// <summary>
        /// 攻击类型
        /// </summary>
        public AttackType AttackType { get; set; }
        
        /// <summary>
        /// 攻击间隔（秒）
        /// </summary>
        public float AttackSpeed { get; set; }
        
        /// <summary>
        /// 攻击范围
        /// </summary>
        public float AttackRange { get; set; }
        
        /// <summary>
        /// 护甲值
        /// </summary>
        public int Armor { get; set; }
        
        /// <summary>
        /// 护甲类型
        /// </summary>
        public ArmorType ArmorType { get; set; }
        
        /// <summary>
        /// 移动速度
        /// </summary>
        public float MoveSpeed { get; set; }
        
        /// <summary>
        /// 视野范围
        /// </summary>
        public float SightRange { get; set; }
        
        #endregion

        #region 造价
        
        /// <summary>
        /// 木材消耗
        /// </summary>
        public int WoodCost { get; set; }
        
        /// <summary>
        /// 粮食消耗
        /// </summary>
        public int FoodCost { get; set; }
        
        /// <summary>
        /// 金币消耗
        /// </summary>
        public int GoldCost { get; set; }
        
        /// <summary>
        /// 建造时间（秒）
        /// </summary>
        public float BuildTime { get; set; }
        
        /// <summary>
        /// 人口消耗
        /// </summary>
        public int PopulationCost { get; set; }
        
        #endregion

        #region 能力标记
        
        /// <summary>
        /// 能否采集资源
        /// </summary>
        public bool CanGather { get; set; }
        
        /// <summary>
        /// 能否建造
        /// </summary>
        public bool CanBuild { get; set; }
        
        /// <summary>
        /// 能否治疗
        /// </summary>
        public bool CanHeal { get; set; }
        
        #endregion

        #region 业务方法
        
        /// <summary>
        /// 计算对目标的实际伤害
        /// </summary>
        public int CalculateDamage(ArmorType targetArmorType, int targetArmor)
        {
            float damageMultiplier = GetDamageMultiplier(AttackType, targetArmorType);
            int rawDamage = (int)(AttackDamage * damageMultiplier);
            
            // 护甲减伤：伤害 - 护甲，最低1点伤害
            int finalDamage = rawDamage - targetArmor;
            return finalDamage < 1 ? 1 : finalDamage;
        }
        
        /// <summary>
        /// 获取攻击类型对护甲类型的伤害倍率
        /// </summary>
        public static float GetDamageMultiplier(AttackType attack, ArmorType armor)
        {
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
                    return 1.0f; // 魔法攻击忽略护甲类型
                case AttackType.Hero:
                    return 1.0f; // 英雄攻击不受护甲影响
            }
            return 1.0f;
        }
        
        /// <summary>
        /// 计算每秒伤害 (DPS)
        /// </summary>
        public float CalculateDPS()
        {
            return AttackSpeed > 0 ? AttackDamage / AttackSpeed : 0;
        }
        
        /// <summary>
        /// 计算总资源消耗
        /// </summary>
        public int CalculateTotalCost()
        {
            return WoodCost + FoodCost + GoldCost;
        }
        
        /// <summary>
        /// 计算性价比（DPS / 总资源）
        /// </summary>
        public float CalculateCostEfficiency()
        {
            int totalCost = CalculateTotalCost();
            return totalCost > 0 ? CalculateDPS() / totalCost : 0;
        }
        
        #endregion
    }
}
