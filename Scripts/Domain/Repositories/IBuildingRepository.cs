// ============================================================
// IBuildingRepository.cs - 建筑仓储接口
// ============================================================

using System.Collections.Generic;
using RTS.Domain.Entities;

namespace RTS.Domain.Repositories
{
    /// <summary>
    /// 建筑仓储接口
    /// 提供建筑数据的查询服务
    /// </summary>
    public interface IBuildingRepository
    {
        /// <summary>
        /// 通过ID获取建筑数据
        /// </summary>
        BuildingData GetById(string buildingId);
        
        /// <summary>
        /// 获取所有建筑数据
        /// </summary>
        IReadOnlyList<BuildingData> GetAll();
        
        /// <summary>
        /// 获取指定阵营的所有建筑
        /// </summary>
        IReadOnlyList<BuildingData> GetByFaction(string faction);
        
        /// <summary>
        /// 获取指定科技等级的所有建筑
        /// </summary>
        IReadOnlyList<BuildingData> GetByTier(int tier);
        
        /// <summary>
        /// 检查建筑是否存在
        /// </summary>
        bool Exists(string buildingId);
        
        /// <summary>
        /// 获取建筑总数
        /// </summary>
        int Count { get; }
    }
}
