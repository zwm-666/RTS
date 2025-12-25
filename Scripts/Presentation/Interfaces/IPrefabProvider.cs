// ============================================================
// IPrefabProvider.cs - 预制体提供者接口
// ============================================================

using UnityEngine;

namespace RTS.Presentation
{
    /// <summary>
    /// 预制体提供者接口
    /// 游戏逻辑层只依赖此接口，方便替换为 Addressables 实现
    /// </summary>
    public interface IPrefabProvider
    {
        /// <summary>
        /// 获取单位预制体
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>预制体，不存在则返回null</returns>
        GameObject GetUnitPrefab(string unitId);
        
        /// <summary>
        /// 获取建筑预制体
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>预制体，不存在则返回null</returns>
        GameObject GetBuildingPrefab(string buildingId);
        
        /// <summary>
        /// 检查单位预制体是否已注册
        /// </summary>
        bool HasUnitPrefab(string unitId);
        
        /// <summary>
        /// 检查建筑预制体是否已注册
        /// </summary>
        bool HasBuildingPrefab(string buildingId);
        
        /// <summary>
        /// 获取已注册的单位预制体数量
        /// </summary>
        int UnitPrefabCount { get; }
        
        /// <summary>
        /// 获取已注册的建筑预制体数量
        /// </summary>
        int BuildingPrefabCount { get; }
    }
}
