// ============================================================
// BuildingRepository.cs - 建筑仓储实现
// ============================================================

using System.Collections.Generic;
using System.Linq;
using RTS.Domain.Entities;

namespace RTS.Domain.Repositories
{
    /// <summary>
    /// 建筑仓储实现
    /// 使用字典缓存，提供快速查询
    /// </summary>
    public class BuildingRepository : IBuildingRepository
    {
        private readonly Dictionary<string, BuildingData> _cache = new Dictionary<string, BuildingData>();
        
        #region 注册方法
        
        /// <summary>
        /// 注册单个建筑
        /// </summary>
        public void Register(BuildingData building)
        {
            if (building != null && !string.IsNullOrEmpty(building.BuildingId))
            {
                _cache[building.BuildingId] = building;
            }
        }
        
        /// <summary>
        /// 批量注册建筑
        /// </summary>
        public void RegisterRange(IEnumerable<BuildingData> buildings)
        {
            foreach (var building in buildings)
            {
                Register(building);
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

        #region IBuildingRepository 实现
        
        public BuildingData GetById(string buildingId)
        {
            return _cache.TryGetValue(buildingId, out var data) ? data : null;
        }
        
        public IReadOnlyList<BuildingData> GetAll()
        {
            return _cache.Values.ToList();
        }
        
        public IReadOnlyList<BuildingData> GetByFaction(string faction)
        {
            return _cache.Values
                .Where(b => b.Faction == faction)
                .ToList();
        }
        
        public IReadOnlyList<BuildingData> GetByTier(int tier)
        {
            return _cache.Values
                .Where(b => b.Tier == tier)
                .ToList();
        }
        
        public bool Exists(string buildingId)
        {
            return _cache.ContainsKey(buildingId);
        }
        
        public int Count => _cache.Count;
        
        #endregion
    }
}
