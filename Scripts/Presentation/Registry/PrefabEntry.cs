// ============================================================
// PrefabEntry.cs - 预制体映射条目
// ============================================================

using System;
using UnityEngine;

namespace RTS.Presentation
{
    /// <summary>
    /// 预制体映射条目
    /// Id → GameObject 的映射关系
    /// </summary>
    [Serializable]
    public class PrefabEntry
    {
        [Tooltip("实体ID，与配置中的 unitId/buildingId 对应")]
        public string id;
        
        [Tooltip("对应的预制体")]
        public GameObject prefab;
        
        /// <summary>
        /// 检查条目是否有效
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(id) && prefab != null;
    }
}
