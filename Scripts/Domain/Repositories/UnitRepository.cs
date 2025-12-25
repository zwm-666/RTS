// ============================================================
// UnitRepository.cs - 单位仓储实现
// ============================================================

using System.Collections.Generic;
using System.Linq;
using RTS.Domain.Entities;

namespace RTS.Domain.Repositories
{
    /// <summary>
    /// 单位仓储实现
    /// 使用字典缓存，提供快速查询
    /// </summary>
    public class UnitRepository : IUnitRepository
    {
        private readonly Dictionary<string, UnitData> _cache = new Dictionary<string, UnitData>();
        
        #region 注册方法
        
        /// <summary>
        /// 注册单个单位
        /// </summary>
        public void Register(UnitData unit)
        {
            if (unit != null && !string.IsNullOrEmpty(unit.UnitId))
            {
                _cache[unit.UnitId] = unit;
            }
        }
        
        /// <summary>
        /// 批量注册单位
        /// </summary>
        public void RegisterRange(IEnumerable<UnitData> units)
        {
            foreach (var unit in units)
            {
                Register(unit);
            }
        }
        
        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
        
        #endregion

        #region IUnitRepository 实现
        
        public UnitData GetById(string unitId)
        {
            return _cache.TryGetValue(unitId, out var data) ? data : null;
        }
        
        public IReadOnlyList<UnitData> GetAll()
        {
            return _cache.Values.ToList();
        }
        
        public IReadOnlyList<UnitData> GetByFaction(string faction)
        {
            return _cache.Values
                .Where(u => u.Faction == faction)
                .ToList();
        }
        
        public IReadOnlyList<UnitData> GetByCategory(string category)
        {
            return _cache.Values
                .Where(u => u.Category == category)
                .ToList();
        }
        
        public bool Exists(string unitId)
        {
            return _cache.ContainsKey(unitId);
        }
        
        public int Count => _cache.Count;
        
        #endregion
    }
}
