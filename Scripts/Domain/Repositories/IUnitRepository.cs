// ============================================================
// IUnitRepository.cs - 单位仓储接口
// ============================================================

using System.Collections.Generic;
using RTS.Domain.Entities;

namespace RTS.Domain.Repositories
{
    /// <summary>
    /// 单位仓储接口
    /// 提供单位数据的查询服务
    /// </summary>
    public interface IUnitRepository
    {
        /// <summary>
        /// 通过ID获取单位数据
        /// </summary>
        UnitData GetById(string unitId);
        
        /// <summary>
        /// 获取所有单位数据
        /// </summary>
        IReadOnlyList<UnitData> GetAll();
        
        /// <summary>
        /// 获取指定阵营的所有单位
        /// </summary>
        IReadOnlyList<UnitData> GetByFaction(string faction);
        
        /// <summary>
        /// 获取指定类别的所有单位
        /// </summary>
        IReadOnlyList<UnitData> GetByCategory(string category);
        
        /// <summary>
        /// 检查单位是否存在
        /// </summary>
        bool Exists(string unitId);
        
        /// <summary>
        /// 获取单位总数
        /// </summary>
        int Count { get; }
    }
}
