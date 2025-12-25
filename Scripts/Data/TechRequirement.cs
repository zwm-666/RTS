// ============================================================
// TechRequirement.cs
// 科技/建筑前置条件数据结构
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Core;

namespace RTS.Data
{
    /// <summary>
    /// 资源消耗数据
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public ResourceType resourceType;
        public int amount;
        
        public ResourceCost(ResourceType type, int cost)
        {
            resourceType = type;
            amount = cost;
        }
    }

    /// <summary>
    /// 前置条件类型
    /// </summary>
    public enum RequirementType
    {
        Building,   // 需要某建筑
        Tech        // 需要某科技（预留）
    }

    /// <summary>
    /// 单个前置条件
    /// </summary>
    [Serializable]
    public class TechRequirement
    {
        public RequirementType requirementType;
        public string requiredId;           // 需要的建筑/科技ID
        public string displayName;          // 显示名称（用于UI提示）
    }

    /// <summary>
    /// 生产数据基类
    /// </summary>
    public abstract class EntityData : ScriptableObject
    {
        [Header("基础信息")]
        public string entityId;             // 唯一标识符
        public string displayName;          // 显示名称
        [TextArea(2, 4)]
        public string description;          // 描述
        public Sprite icon;                 // 图标

        [Header("造价")]
        public List<ResourceCost> costs = new List<ResourceCost>();

        [Header("生产时间")]
        public float buildTime = 5f;        // 建造/生产时间（秒）

        [Header("前置条件")]
        public List<TechRequirement> requirements = new List<TechRequirement>();

        /// <summary>
        /// 将资源消耗列表转换为字典格式（用于ResourceManager）
        /// </summary>
        public Dictionary<ResourceType, int> GetCostDictionary()
        {
            var dict = new Dictionary<ResourceType, int>();
            foreach (var cost in costs)
            {
                if (dict.ContainsKey(cost.resourceType))
                    dict[cost.resourceType] += cost.amount;
                else
                    dict[cost.resourceType] = cost.amount;
            }
            return dict;
        }
    }
}
