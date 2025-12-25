// ============================================================
// TechManager.cs
// 科技管理器 - 管理科技研发和属性加成
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Core;
using RTS.Managers;
using RTS.Domain.Entities;

namespace RTS.Tech
{
    /// <summary>
    /// 研发队列项
    /// </summary>
    [Serializable]
    public class ResearchQueueItem
    {
        public string TechId;
        public float RemainingTime;
        public float TotalTime;
        
        public float Progress => TotalTime > 0 ? 1f - (RemainingTime / TotalTime) : 1f;
    }

    /// <summary>
    /// 科技管理器（单例模式）
    /// </summary>
    public class TechManager : MonoBehaviour
    {
        #region 单例
        
        private static TechManager _instance;
        public static TechManager Instance => _instance;
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 科技研发完成事件
        /// </summary>
        public event Action<int, string> OnTechResearched;
        
        /// <summary>
        /// 研发开始事件
        /// </summary>
        public event Action<int, string> OnResearchStarted;
        
        /// <summary>
        /// 研发进度更新事件
        /// </summary>
        public event Action<int, string, float> OnResearchProgress;
        
        #endregion

        #region 配置
        
        [Header("调试")]
        [SerializeField] private bool _logResearch = true;
        
        #endregion

        #region 私有字段
        
        // 科技库（所有可用科技）
        private Dictionary<string, TechData> _techLibrary = new Dictionary<string, TechData>();
        
        // 每个玩家已解锁的科技
        private Dictionary<int, HashSet<string>> _unlockedTechs = new Dictionary<int, HashSet<string>>();
        
        // 每个玩家的研发队列
        private Dictionary<int, ResearchQueueItem> _researchQueue = new Dictionary<int, ResearchQueueItem>();
        
        #endregion

        #region 属性
        
        public Dictionary<string, TechData> TechLibrary => _techLibrary;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // 初始化默认科技库
            InitializeDefaultTechs();
        }
        
        private void Update()
        {
            // 处理研发进度
            ProcessResearchQueue();
        }
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化默认科技库
        /// </summary>
        private void InitializeDefaultTechs()
        {
            // ===== 通用科技 =====
            
            // 攻击升级
            RegisterTech(new TechData("attack_upgrade_1", "攻击升级 I", 30f)
                .SetCost(100, 50, 0)
                .AddEffect(StatType.AttackDamage, 2, TechEffectType.FlatBonus));
            
            RegisterTech(new TechData("attack_upgrade_2", "攻击升级 II", 45f)
                .SetCost(200, 100, 0)
                .AddPrerequisite("attack_upgrade_1")
                .AddEffect(StatType.AttackDamage, 3, TechEffectType.FlatBonus));
            
            RegisterTech(new TechData("attack_upgrade_3", "攻击升级 III", 60f)
                .SetCost(300, 150, 0)
                .AddPrerequisite("attack_upgrade_2")
                .AddEffect(StatType.AttackDamage, 4, TechEffectType.FlatBonus));
            
            // 护甲升级
            RegisterTech(new TechData("armor_upgrade_1", "护甲升级 I", 30f)
                .SetCost(100, 50, 0)
                .AddEffect(StatType.Armor, 1, TechEffectType.FlatBonus));
            
            RegisterTech(new TechData("armor_upgrade_2", "护甲升级 II", 45f)
                .SetCost(200, 100, 0)
                .AddPrerequisite("armor_upgrade_1")
                .AddEffect(StatType.Armor, 2, TechEffectType.FlatBonus));
            
            RegisterTech(new TechData("armor_upgrade_3", "护甲升级 III", 60f)
                .SetCost(300, 150, 0)
                .AddPrerequisite("armor_upgrade_2")
                .AddEffect(StatType.Armor, 2, TechEffectType.FlatBonus));
            
            // 移动速度
            RegisterTech(new TechData("speed_upgrade", "急行军", 40f)
                .SetCost(150, 0, 100)
                .AddEffect(StatType.MoveSpeed, 0.1f, TechEffectType.PercentBonus));
            
            // 生命值升级
            RegisterTech(new TechData("health_upgrade", "强化体魄", 50f)
                .SetCost(100, 0, 200)
                .AddEffect(StatType.MaxHealth, 0.15f, TechEffectType.PercentBonus));
            
            // 视野升级
            RegisterTech(new TechData("sight_upgrade", "鹰眼", 25f)
                .SetCost(50, 0, 50)
                .AddEffect(StatType.SightRange, 2, TechEffectType.FlatBonus));
            
            Log($"科技库初始化完成，共 {_techLibrary.Count} 项科技");
        }
        
        /// <summary>
        /// 注册科技
        /// </summary>
        public void RegisterTech(TechData tech)
        {
            if (tech == null || string.IsNullOrEmpty(tech.TechId)) return;
            
            _techLibrary[tech.TechId] = tech;
        }
        
        #endregion

        #region 研发系统
        
        /// <summary>
        /// 开始研发科技
        /// </summary>
        public bool ResearchTech(int playerId, string techId)
        {
            // 检查科技是否存在
            if (!_techLibrary.TryGetValue(techId, out TechData tech))
            {
                Debug.LogWarning($"[TechManager] 未知科技: {techId}");
                return false;
            }
            
            // 检查是否已解锁
            if (HasTech(playerId, techId))
            {
                Debug.LogWarning($"[TechManager] 玩家{playerId} 已拥有科技: {techId}");
                return false;
            }
            
            // 检查前置科技
            foreach (var prereq in tech.Prerequisites)
            {
                if (!HasTech(playerId, prereq))
                {
                    Debug.LogWarning($"[TechManager] 玩家{playerId} 缺少前置科技: {prereq}");
                    return false;
                }
            }
            
            // 检查是否正在研发
            if (_researchQueue.ContainsKey(playerId))
            {
                Debug.LogWarning($"[TechManager] 玩家{playerId} 正在研发其他科技");
                return false;
            }
            
            // 检查资源
            if (ResourceManager.Instance != null && tech.Cost.Count > 0)
            {
                if (!ResourceManager.Instance.SpendResource(playerId, tech.Cost))
                {
                    Debug.LogWarning($"[TechManager] 玩家{playerId} 资源不足");
                    return false;
                }
            }
            
            // 开始研发
            _researchQueue[playerId] = new ResearchQueueItem
            {
                TechId = techId,
                RemainingTime = tech.ResearchTime,
                TotalTime = tech.ResearchTime
            };
            
            Log($"玩家{playerId} 开始研发: {tech.DisplayName}");
            OnResearchStarted?.Invoke(playerId, techId);
            
            return true;
        }
        
        /// <summary>
        /// 取消研发（不退还资源）
        /// </summary>
        public bool CancelResearch(int playerId)
        {
            if (_researchQueue.TryGetValue(playerId, out var item))
            {
                Log($"玩家{playerId} 取消研发: {item.TechId}");
                _researchQueue.Remove(playerId);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 处理研发队列
        /// </summary>
        private void ProcessResearchQueue()
        {
            List<int> completedPlayers = new List<int>();
            
            foreach (var kvp in _researchQueue)
            {
                int playerId = kvp.Key;
                var item = kvp.Value;
                
                item.RemainingTime -= Time.deltaTime;
                
                // 进度更新
                OnResearchProgress?.Invoke(playerId, item.TechId, item.Progress);
                
                if (item.RemainingTime <= 0)
                {
                    completedPlayers.Add(playerId);
                }
            }
            
            // 完成研发
            foreach (int playerId in completedPlayers)
            {
                var item = _researchQueue[playerId];
                CompleteResearch(playerId, item.TechId);
                _researchQueue.Remove(playerId);
            }
        }
        
        /// <summary>
        /// 完成研发
        /// </summary>
        private void CompleteResearch(int playerId, string techId)
        {
            // 添加到已解锁列表
            if (!_unlockedTechs.ContainsKey(playerId))
            {
                _unlockedTechs[playerId] = new HashSet<string>();
            }
            
            _unlockedTechs[playerId].Add(techId);
            
            if (_techLibrary.TryGetValue(techId, out TechData tech))
            {
                Log($"玩家{playerId} 研发完成: {tech.DisplayName}");
            }
            
            OnTechResearched?.Invoke(playerId, techId);
        }
        
        #endregion

        #region 查询方法
        
        /// <summary>
        /// 检查玩家是否拥有某科技
        /// </summary>
        public bool HasTech(int playerId, string techId)
        {
            if (_unlockedTechs.TryGetValue(playerId, out var techs))
            {
                return techs.Contains(techId);
            }
            return false;
        }
        
        /// <summary>
        /// 获取玩家已解锁的科技列表
        /// </summary>
        public HashSet<string> GetUnlockedTechs(int playerId)
        {
            if (_unlockedTechs.TryGetValue(playerId, out var techs))
            {
                return new HashSet<string>(techs);
            }
            return new HashSet<string>();
        }
        
        /// <summary>
        /// 获取当前研发进度
        /// </summary>
        public ResearchQueueItem GetCurrentResearch(int playerId)
        {
            if (_researchQueue.TryGetValue(playerId, out var item))
            {
                return item;
            }
            return null;
        }
        
        /// <summary>
        /// 检查科技是否可研发
        /// </summary>
        public bool CanResearch(int playerId, string techId)
        {
            if (!_techLibrary.TryGetValue(techId, out TechData tech))
                return false;
            
            if (HasTech(playerId, techId))
                return false;
            
            foreach (var prereq in tech.Prerequisites)
            {
                if (!HasTech(playerId, prereq))
                    return false;
            }
            
            return true;
        }
        
        #endregion

        #region 属性加成计算
        
        /// <summary>
        /// 获取科技加成后的属性值
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="baseValue">基础值</param>
        /// <param name="stat">属性类型</param>
        /// <param name="unitCategory">单位类别（可选）</param>
        /// <param name="unitId">单位ID（可选）</param>
        /// <returns>加成后的最终值</returns>
        public float GetBonusValue(int playerId, float baseValue, StatType stat, 
            string unitCategory = null, string unitId = null)
        {
            if (!_unlockedTechs.TryGetValue(playerId, out var techs))
            {
                return baseValue;
            }
            
            float flatBonus = 0f;
            float percentBonus = 0f;
            
            foreach (var techId in techs)
            {
                if (!_techLibrary.TryGetValue(techId, out TechData tech))
                    continue;
                
                foreach (var effect in tech.Effects)
                {
                    if (effect.AffectedStat != stat)
                        continue;
                    
                    // 检查适用性
                    if (!IsEffectApplicable(effect, unitCategory, unitId))
                        continue;
                    
                    switch (effect.EffectType)
                    {
                        case TechEffectType.FlatBonus:
                            flatBonus += effect.Value;
                            break;
                        case TechEffectType.PercentBonus:
                            percentBonus += effect.Value;
                            break;
                    }
                }
            }
            
            // 计算最终值: (基础值 + 固定加成) * (1 + 百分比加成)
            float finalValue = (baseValue + flatBonus) * (1f + percentBonus);
            return finalValue;
        }
        
        /// <summary>
        /// 获取整数类型的加成值
        /// </summary>
        public int GetBonusValueInt(int playerId, int baseValue, StatType stat, 
            string unitCategory = null, string unitId = null)
        {
            return Mathf.RoundToInt(GetBonusValue(playerId, baseValue, stat, unitCategory, unitId));
        }
        
        /// <summary>
        /// 检查效果是否适用于指定单位
        /// </summary>
        private bool IsEffectApplicable(TechEffect effect, string unitCategory, string unitId)
        {
            // 检查类别
            if (!string.IsNullOrEmpty(effect.ApplicableCategory) && 
                effect.ApplicableCategory != "All")
            {
                if (unitCategory != effect.ApplicableCategory)
                    return false;
            }
            
            // 检查单位ID
            if (effect.ApplicableUnitIds != null && effect.ApplicableUnitIds.Count > 0)
            {
                if (string.IsNullOrEmpty(unitId))
                    return false;
                    
                if (!effect.ApplicableUnitIds.Contains(unitId))
                    return false;
            }
            
            return true;
        }
        
        #endregion

        #region 调试
        
        private void Log(string message)
        {
            if (_logResearch)
            {
                Debug.Log($"[TechManager] {message}");
            }
        }
        
        /// <summary>
        /// 直接解锁科技（调试用）
        /// </summary>
        [ContextMenu("Grant All Techs to Player 0")]
        public void DebugGrantAllTechs()
        {
            foreach (var techId in _techLibrary.Keys)
            {
                if (!_unlockedTechs.ContainsKey(0))
                {
                    _unlockedTechs[0] = new HashSet<string>();
                }
                _unlockedTechs[0].Add(techId);
            }
            Debug.Log("[TechManager] 已解锁玩家0的所有科技");
        }
        
        #endregion
    }
}
