// ============================================================
// PrefabRegistry.cs - 预制体注册表 ScriptableObject
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace RTS.Presentation
{
    /// <summary>
    /// 预制体注册表（ScriptableObject）
    /// 在编辑器中配置 Id → Prefab 映射
    /// 创建方式：右键 -> Create -> RTS -> Prefab Registry
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabRegistry", menuName = "RTS/Prefab Registry", order = 100)]
    public class PrefabRegistry : ScriptableObject
    {
        [Header("单位预制体映射")]
        [Tooltip("配置单位ID与预制体的对应关系")]
        [SerializeField] private List<PrefabEntry> unitPrefabs = new List<PrefabEntry>();
        
        [Header("建筑预制体映射")]
        [Tooltip("配置建筑ID与预制体的对应关系")]
        [SerializeField] private List<PrefabEntry> buildingPrefabs = new List<PrefabEntry>();
        
        #region 只读访问
        
        /// <summary>
        /// 单位预制体列表（只读）
        /// </summary>
        public IReadOnlyList<PrefabEntry> UnitPrefabs => unitPrefabs;
        
        /// <summary>
        /// 建筑预制体列表（只读）
        /// </summary>
        public IReadOnlyList<PrefabEntry> BuildingPrefabs => buildingPrefabs;
        
        #endregion

        #region 编辑器辅助
        
        /// <summary>
        /// 获取所有单位ID（用于编辑器校验）
        /// </summary>
        public IEnumerable<string> GetAllUnitIds()
        {
            foreach (var entry in unitPrefabs)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    yield return entry.id;
            }
        }
        
        /// <summary>
        /// 获取所有建筑ID（用于编辑器校验）
        /// </summary>
        public IEnumerable<string> GetAllBuildingIds()
        {
            foreach (var entry in buildingPrefabs)
            {
                if (!string.IsNullOrEmpty(entry.id))
                    yield return entry.id;
            }
        }
        
        #endregion

        #region 校验
        
        /// <summary>
        /// 校验注册表完整性
        /// </summary>
        public void Validate()
        {
            // 检查单位
            foreach (var entry in unitPrefabs)
            {
                if (string.IsNullOrEmpty(entry.id))
                {
                    Debug.LogWarning($"[PrefabRegistry] 单位条目缺少ID", this);
                }
                else if (entry.prefab == null)
                {
                    Debug.LogWarning($"[PrefabRegistry] 单位 {entry.id} 缺少预制体", this);
                }
            }
            
            // 检查建筑
            foreach (var entry in buildingPrefabs)
            {
                if (string.IsNullOrEmpty(entry.id))
                {
                    Debug.LogWarning($"[PrefabRegistry] 建筑条目缺少ID", this);
                }
                else if (entry.prefab == null)
                {
                    Debug.LogWarning($"[PrefabRegistry] 建筑 {entry.id} 缺少预制体", this);
                }
            }
        }
        
        #endregion
    }
}
