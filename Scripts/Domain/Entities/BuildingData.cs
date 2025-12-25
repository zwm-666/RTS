// ============================================================
// BuildingData.cs - 建筑领域数据（纯POCO，无Unity依赖）
// ============================================================

using System.Collections.Generic;
using RTS.Domain.Enums;

namespace RTS.Domain.Entities
{
    /// <summary>
    /// 建筑领域数据
    /// 纯 POCO 对象，不依赖 UnityEngine
    /// </summary>
    public class BuildingData
    {
        #region 唯一标识
        
        /// <summary>
        /// 建筑唯一ID
        /// </summary>
        public string BuildingId { get; set; }
        
        #endregion

        #region 基础信息
        
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// 阵营
        /// </summary>
        public string Faction { get; set; }
        
        /// <summary>
        /// 科技等级
        /// </summary>
        public int Tier { get; set; }
        
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        
        #endregion

        #region 属性
        
        /// <summary>
        /// 最大生命值
        /// </summary>
        public int MaxHealth { get; set; }
        
        /// <summary>
        /// 护甲值
        /// </summary>
        public int Armor { get; set; }
        
        /// <summary>
        /// 护甲类型
        /// </summary>
        public ArmorType ArmorType { get; set; }
        
        /// <summary>
        /// 视野范围
        /// </summary>
        public float SightRange { get; set; }
        
        #endregion

        #region 尺寸
        
        /// <summary>
        /// 网格宽度
        /// </summary>
        public int GridWidth { get; set; }
        
        /// <summary>
        /// 网格高度
        /// </summary>
        public int GridHeight { get; set; }
        
        #endregion

        #region 造价
        
        /// <summary>
        /// 木材消耗
        /// </summary>
        public int WoodCost { get; set; }
        
        /// <summary>
        /// 粮食消耗
        /// </summary>
        public int FoodCost { get; set; }
        
        /// <summary>
        /// 金币消耗
        /// </summary>
        public int GoldCost { get; set; }
        
        /// <summary>
        /// 建造时间（秒）
        /// </summary>
        public float BuildTime { get; set; }
        
        #endregion

        #region 生产能力
        
        /// <summary>
        /// 提供的人口上限
        /// </summary>
        public int PopulationProvide { get; set; }
        
        /// <summary>
        /// 是否为资源交付点
        /// </summary>
        public bool IsDropOffPoint { get; set; }
        
        /// <summary>
        /// 可生产的单位ID列表
        /// </summary>
        public List<string> ProducibleUnitIds { get; set; } = new List<string>();
        
        /// <summary>
        /// 生产队列大小
        /// </summary>
        public int QueueSize { get; set; }
        
        #endregion

        #region 战斗能力
        
        /// <summary>
        /// 是否可以攻击
        /// </summary>
        public bool CanAttack { get; set; }
        
        /// <summary>
        /// 攻击力
        /// </summary>
        public int AttackDamage { get; set; }
        
        /// <summary>
        /// 攻击范围
        /// </summary>
        public float AttackRange { get; set; }
        
        /// <summary>
        /// 攻击间隔（秒）
        /// </summary>
        public float AttackSpeed { get; set; }
        
        #endregion

        #region 依赖关系
        
        /// <summary>
        /// 前置建筑ID列表
        /// </summary>
        public List<string> RequirementIds { get; set; } = new List<string>();
        
        #endregion

        #region 业务方法
        
        /// <summary>
        /// 获取占用的网格数量
        /// </summary>
        public int GetGridCellCount()
        {
            return GridWidth * GridHeight;
        }
        
        /// <summary>
        /// 计算总资源消耗
        /// </summary>
        public int CalculateTotalCost()
        {
            return WoodCost + FoodCost + GoldCost;
        }
        
        /// <summary>
        /// 检查是否可以生产指定单位
        /// </summary>
        public bool CanProduceUnit(string unitId)
        {
            return ProducibleUnitIds != null && ProducibleUnitIds.Contains(unitId);
        }
        
        /// <summary>
        /// 检查是否满足前置条件
        /// </summary>
        public bool CheckRequirements(IEnumerable<string> ownedBuildingIds)
        {
            if (RequirementIds == null || RequirementIds.Count == 0)
                return true;
            
            var ownedSet = new HashSet<string>(ownedBuildingIds);
            foreach (var reqId in RequirementIds)
            {
                if (!ownedSet.Contains(reqId))
                    return false;
            }
            return true;
        }
        
        #endregion
    }
}
