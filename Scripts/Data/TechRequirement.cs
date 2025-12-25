// ============================================================
// TechRequirement.cs
// 科技/建筑前置条件和资源消耗数据结构
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Core;

namespace RTS.Data
{
    /// <summary>
    /// 资源消耗数据（用于 UI 显示和兼容）
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
}
