// ============================================================
// DamageCalculator.cs
// 伤害计算器 - 处理攻击类型对护甲类型的克制关系
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Domain.Enums;

namespace RTS.Domain
{
    /// <summary>
    /// 伤害计算器（静态类）
    /// 从配置加载的伤害克制表中查询倍率
    /// </summary>
    public static class DamageCalculator
    {
        #region 克制表数据
        
        // 克制表: damageMultipliers[AttackType][ArmorType] = 倍率
        private static Dictionary<AttackType, Dictionary<ArmorType, float>> _multiplierTable;
        
        // 是否已初始化
        private static bool _isInitialized = false;
        
        #endregion

        #region 属性
        
        public static bool IsInitialized => _isInitialized;
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化克制表（由 GameBootstrap 调用）
        /// </summary>
        /// <param name="multipliers">从配置加载的克制表数据</param>
        public static void Initialize(Dictionary<string, Dictionary<string, float>> multipliers)
        {
            _multiplierTable = new Dictionary<AttackType, Dictionary<ArmorType, float>>();
            
            if (multipliers == null || multipliers.Count == 0)
            {
                Debug.LogWarning("[DamageCalculator] 未提供克制表数据，使用默认值");
                InitializeDefaults();
                _isInitialized = true;
                return;
            }
            
            // 解析字符串键到枚举
            foreach (var attackKvp in multipliers)
            {
                AttackType attackType = ParseAttackType(attackKvp.Key);
                var armorDict = new Dictionary<ArmorType, float>();
                
                foreach (var armorKvp in attackKvp.Value)
                {
                    ArmorType armorType = ParseArmorType(armorKvp.Key);
                    armorDict[armorType] = armorKvp.Value;
                }
                
                _multiplierTable[attackType] = armorDict;
            }
            
            _isInitialized = true;
            Debug.Log($"[DamageCalculator] 克制表已加载，包含 {_multiplierTable.Count} 种攻击类型");
        }
        
        /// <summary>
        /// 使用默认硬编码值初始化（当配置不可用时）
        /// </summary>
        public static void InitializeDefaults()
        {
            _multiplierTable = new Dictionary<AttackType, Dictionary<ArmorType, float>>
            {
                [AttackType.Normal] = new Dictionary<ArmorType, float>
                {
                    { ArmorType.None, 1.0f }, { ArmorType.Light, 1.0f }, { ArmorType.Medium, 1.2f },
                    { ArmorType.Heavy, 1.0f }, { ArmorType.Fortified, 0.7f }, { ArmorType.Divine, 0.5f }
                },
                [AttackType.Pierce] = new Dictionary<ArmorType, float>
                {
                    { ArmorType.None, 1.0f }, { ArmorType.Light, 1.5f }, { ArmorType.Medium, 1.0f },
                    { ArmorType.Heavy, 0.5f }, { ArmorType.Fortified, 0.5f }, { ArmorType.Divine, 0.5f }
                },
                [AttackType.Magic] = new Dictionary<ArmorType, float>
                {
                    { ArmorType.None, 1.0f }, { ArmorType.Light, 0.75f }, { ArmorType.Medium, 1.0f },
                    { ArmorType.Heavy, 1.4f }, { ArmorType.Fortified, 0.5f }, { ArmorType.Divine, 1.0f }
                },
                [AttackType.Siege] = new Dictionary<ArmorType, float>
                {
                    { ArmorType.None, 1.0f }, { ArmorType.Light, 0.5f }, { ArmorType.Medium, 0.5f },
                    { ArmorType.Heavy, 1.0f }, { ArmorType.Fortified, 2.0f }, { ArmorType.Divine, 0.5f }
                },
                [AttackType.Hero] = new Dictionary<ArmorType, float>
                {
                    { ArmorType.None, 1.0f }, { ArmorType.Light, 1.0f }, { ArmorType.Medium, 1.0f },
                    { ArmorType.Heavy, 1.0f }, { ArmorType.Fortified, 1.0f }, { ArmorType.Divine, 1.0f }
                }
            };
            
            Debug.Log("[DamageCalculator] 使用默认克制表");
        }
        
        #endregion

        #region 查询方法
        
        /// <summary>
        /// 获取伤害倍率
        /// </summary>
        /// <param name="attackType">攻击类型</param>
        /// <param name="armorType">护甲类型</param>
        /// <returns>伤害倍率，默认1.0</returns>
        public static float GetMultiplier(AttackType attackType, ArmorType armorType)
        {
            // 确保已初始化
            if (!_isInitialized)
            {
                InitializeDefaults();
                _isInitialized = true;
            }
            
            if (_multiplierTable.TryGetValue(attackType, out var armorDict))
            {
                if (armorDict.TryGetValue(armorType, out float multiplier))
                {
                    return multiplier;
                }
            }
            
            // 未找到则返回 1.0
            return 1.0f;
        }
        
        /// <summary>
        /// 计算最终伤害
        /// </summary>
        /// <param name="baseDamage">基础伤害</param>
        /// <param name="attackType">攻击类型</param>
        /// <param name="armorType">护甲类型</param>
        /// <param name="armorValue">护甲值</param>
        /// <returns>最终伤害（最低1点）</returns>
        public static int CalculateDamage(int baseDamage, AttackType attackType, ArmorType armorType, int armorValue)
        {
            float multiplier = GetMultiplier(attackType, armorType);
            int modifiedDamage = Mathf.RoundToInt(baseDamage * multiplier);
            int finalDamage = Mathf.Max(1, modifiedDamage - armorValue);
            return finalDamage;
        }
        
        #endregion

        #region 辅助方法
        
        private static AttackType ParseAttackType(string str)
        {
            return str switch
            {
                "Normal" => AttackType.Normal,
                "Piercing" => AttackType.Pierce,
                "Pierce" => AttackType.Pierce,
                "Magic" => AttackType.Magic,
                "Siege" => AttackType.Siege,
                "Hero" => AttackType.Hero,
                _ => AttackType.Normal
            };
        }
        
        private static ArmorType ParseArmorType(string str)
        {
            return str switch
            {
                "None" => ArmorType.None,
                "Light" => ArmorType.Light,
                "Medium" => ArmorType.Medium,
                "Heavy" => ArmorType.Heavy,
                "Fortified" => ArmorType.Fortified,
                "Divine" => ArmorType.Divine,
                _ => ArmorType.None
            };
        }
        
        #endregion

        #region 调试
        
        /// <summary>
        /// 打印完整的克制表（调试用）
        /// </summary>
        public static void DebugPrintTable()
        {
            if (!_isInitialized || _multiplierTable == null)
            {
                Debug.Log("[DamageCalculator] 克制表未初始化");
                return;
            }
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[DamageCalculator] 伤害克制表:");
            
            foreach (var attackKvp in _multiplierTable)
            {
                sb.Append($"  {attackKvp.Key}: ");
                foreach (var armorKvp in attackKvp.Value)
                {
                    sb.Append($"{armorKvp.Key}={armorKvp.Value:F2} ");
                }
                sb.AppendLine();
            }
            
            Debug.Log(sb.ToString());
        }
        
        #endregion
    }
}
