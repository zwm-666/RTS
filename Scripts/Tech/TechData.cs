// ============================================================
// TechData.cs
// 科技数据 - 定义科技和升级效果
// ============================================================

using System;
using System.Collections.Generic;
using RTS.Core;

namespace RTS.Tech
{
    /// <summary>
    /// 属性类型枚举
    /// </summary>
    public enum StatType
    {
        AttackDamage,
        Armor,
        MaxHealth,
        MoveSpeed,
        AttackSpeed,
        AttackRange,
        SightRange
    }

    /// <summary>
    /// 科技效果类型
    /// </summary>
    public enum TechEffectType
    {
        /// <summary>
        /// 固定值加成
        /// </summary>
        FlatBonus,
        
        /// <summary>
        /// 百分比加成
        /// </summary>
        PercentBonus,
        
        /// <summary>
        /// 解锁能力
        /// </summary>
        UnlockAbility
    }

    /// <summary>
    /// 科技效果
    /// </summary>
    [Serializable]
    public class TechEffect
    {
        /// <summary>
        /// 效果类型
        /// </summary>
        public TechEffectType EffectType { get; set; }
        
        /// <summary>
        /// 影响的属性
        /// </summary>
        public StatType AffectedStat { get; set; }
        
        /// <summary>
        /// 效果数值（固定值或百分比）
        /// </summary>
        public float Value { get; set; }
        
        /// <summary>
        /// 适用的单位类别（如 "Infantry", "Cavalry", "All"）
        /// </summary>
        public string ApplicableCategory { get; set; } = "All";
        
        /// <summary>
        /// 适用的单位ID列表（空表示全部适用）
        /// </summary>
        public List<string> ApplicableUnitIds { get; set; } = new List<string>();
        
        public TechEffect()
        {
        }
        
        public TechEffect(StatType stat, float value, TechEffectType type = TechEffectType.FlatBonus)
        {
            AffectedStat = stat;
            Value = value;
            EffectType = type;
        }
    }

    /// <summary>
    /// 科技数据
    /// </summary>
    [Serializable]
    public class TechData
    {
        #region 基本信息
        
        /// <summary>
        /// 科技唯一标识符
        /// </summary>
        public string TechId { get; set; }
        
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// 所属阵营（"Starfire", "Ironforge", "Verdant", "Common"）
        /// </summary>
        public string Faction { get; set; } = "Common";
        
        /// <summary>
        /// 科技层级（1-3，越高越高级）
        /// </summary>
        public int Tier { get; set; } = 1;
        
        #endregion

        #region 研发要求
        
        /// <summary>
        /// 研发费用
        /// </summary>
        public Dictionary<ResourceType, int> Cost { get; set; } = new Dictionary<ResourceType, int>();
        
        /// <summary>
        /// 研发时间（秒）
        /// </summary>
        public float ResearchTime { get; set; } = 30f;
        
        /// <summary>
        /// 前置科技ID列表
        /// </summary>
        public List<string> Prerequisites { get; set; } = new List<string>();
        
        /// <summary>
        /// 需要的建筑ID
        /// </summary>
        public string RequiredBuildingId { get; set; }
        
        #endregion

        #region 科技效果
        
        /// <summary>
        /// 科技效果列表（一个科技可以有多个效果）
        /// </summary>
        public List<TechEffect> Effects { get; set; } = new List<TechEffect>();
        
        #endregion

        #region 构造函数
        
        public TechData()
        {
        }
        
        public TechData(string techId, string displayName, float researchTime)
        {
            TechId = techId;
            DisplayName = displayName;
            ResearchTime = researchTime;
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 添加效果
        /// </summary>
        public TechData AddEffect(StatType stat, float value, TechEffectType type = TechEffectType.FlatBonus)
        {
            Effects.Add(new TechEffect(stat, value, type));
            return this;
        }
        
        /// <summary>
        /// 设置费用
        /// </summary>
        public TechData SetCost(int gold = 0, int wood = 0, int food = 0)
        {
            Cost.Clear();
            if (gold > 0) Cost[ResourceType.Gold] = gold;
            if (wood > 0) Cost[ResourceType.Wood] = wood;
            if (food > 0) Cost[ResourceType.Food] = food;
            return this;
        }
        
        /// <summary>
        /// 添加前置科技
        /// </summary>
        public TechData AddPrerequisite(string techId)
        {
            Prerequisites.Add(techId);
            return this;
        }
        
        #endregion
    }
}
