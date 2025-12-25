// ============================================================
// ConfigToDomainFactory.cs - DTO → 领域对象的转换工厂
// ============================================================

using RTS.Domain.Entities;
using RTS.Domain.Enums;

namespace RTS.Config
{
    /// <summary>
    /// 配置到领域对象的转换工厂
    /// DTO → 领域对象的唯一转换点
    /// </summary>
    public class ConfigToDomainFactory
    {
        /// <summary>
        /// 将 UnitDto 转换为 UnitData（领域对象）
        /// </summary>
        public UnitData CreateUnitData(UnitDto dto)
        {
            if (dto == null) return null;
            
            return new UnitData
            {
                // 标识
                UnitId = dto.unitId,
                
                // 基础信息
                DisplayName = dto.displayName,
                Faction = dto.faction,
                Category = dto.category,
                Description = dto.description,
                
                // 战斗属性
                MaxHealth = dto.stats?.maxHealth ?? 100,
                HealthRegen = dto.stats?.healthRegen ?? 0,
                AttackDamage = dto.stats?.attackDamage ?? 0,
                AttackType = ParseAttackType(dto.stats?.attackType),
                AttackSpeed = dto.stats?.attackSpeed ?? 1f,
                AttackRange = dto.stats?.attackRange ?? 1f,
                Armor = dto.stats?.armor ?? 0,
                ArmorType = ParseArmorType(dto.stats?.armorType),
                MoveSpeed = dto.stats?.moveSpeed ?? 5f,
                SightRange = dto.stats?.sightRange ?? 10f,
                
                // 造价
                WoodCost = dto.cost?.wood ?? 0,
                FoodCost = dto.cost?.food ?? 0,
                GoldCost = dto.cost?.gold ?? 0,
                BuildTime = dto.buildTime,
                PopulationCost = dto.populationCost,
                
                // 能力
                CanGather = dto.abilities?.canGather ?? false,
                CanBuild = dto.abilities?.canBuild ?? false,
                CanHeal = dto.abilities?.canHeal ?? false
            };
        }
        
        /// <summary>
        /// 将 BuildingDto 转换为 BuildingData（领域对象）
        /// </summary>
        public BuildingData CreateBuildingData(BuildingDto dto)
        {
            if (dto == null) return null;
            
            return new BuildingData
            {
                // 标识
                BuildingId = dto.buildingId,
                
                // 基础信息
                DisplayName = dto.displayName,
                Faction = dto.faction,
                Tier = dto.tier,
                Description = dto.description,
                
                // 属性
                MaxHealth = dto.stats?.maxHealth ?? 500,
                Armor = dto.stats?.armor ?? 0,
                ArmorType = ParseArmorType(dto.stats?.armorType),
                SightRange = dto.stats?.sightRange ?? 10f,
                
                // 尺寸
                GridWidth = dto.size?.gridWidth ?? 2,
                GridHeight = dto.size?.gridHeight ?? 2,
                
                // 造价
                WoodCost = dto.cost?.wood ?? 0,
                FoodCost = dto.cost?.food ?? 0,
                GoldCost = dto.cost?.gold ?? 0,
                BuildTime = dto.buildTime,
                
                // 生产
                PopulationProvide = dto.production?.populationProvide ?? 0,
                IsDropOffPoint = dto.production?.isDropOffPoint ?? false,
                ProducibleUnitIds = dto.production?.producibleUnits ?? new System.Collections.Generic.List<string>(),
                QueueSize = dto.production?.queueSize ?? 5,
                
                // 战斗
                CanAttack = dto.combat?.canAttack ?? false,
                AttackDamage = dto.combat?.attackDamage ?? 0,
                AttackRange = dto.combat?.attackRange ?? 0,
                AttackSpeed = dto.combat?.attackSpeed ?? 0,
                
                // 依赖
                RequirementIds = dto.requirements ?? new System.Collections.Generic.List<string>()
            };
        }
        
        #region 辅助方法
        
        private AttackType ParseAttackType(string str)
        {
            return str switch
            {
                "Normal" => AttackType.Normal,
                "Piercing" => AttackType.Pierce,
                "Magic" => AttackType.Magic,
                "Siege" => AttackType.Siege,
                "Hero" => AttackType.Hero,
                _ => AttackType.Normal
            };
        }
        
        private ArmorType ParseArmorType(string str)
        {
            return str switch
            {
                "None" => ArmorType.None,
                "Light" => ArmorType.Light,
                "Medium" => ArmorType.Medium,
                "Heavy" => ArmorType.Heavy,
                "Fortified" => ArmorType.Fortified,
                "Divine" => ArmorType.Divine,
                _ => ArmorType.None
            };
        }
        
        #endregion
    }
}
