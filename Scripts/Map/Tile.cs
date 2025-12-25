// ============================================================
// Tile.cs
// 格子数据结构
// ============================================================

using UnityEngine;

namespace RTS.Map
{
    /// <summary>
    /// 格子数据结构
    /// </summary>
    [System.Serializable]
    public class Tile
    {
        #region 属性
        
        /// <summary>
        /// 格子在网格中的X坐标
        /// </summary>
        public int X { get; private set; }
        
        /// <summary>
        /// 格子在网格中的Y坐标
        /// </summary>
        public int Y { get; private set; }
        
        /// <summary>
        /// 地形类型
        /// </summary>
        public TileType TileType { get; set; }
        
        /// <summary>
        /// 是否可通行（考虑地形和建筑占用）
        /// </summary>
        public bool IsWalkable { get; set; }
        
        /// <summary>
        /// 移动消耗（用于寻路权重）
        /// </summary>
        public float MovementCost => TileType.GetMovementCost();
        
        /// <summary>
        /// 格子上的建筑ID（0表示无建筑）
        /// </summary>
        public int OccupiedByBuildingId { get; set; }
        
        /// <summary>
        /// 是否有单位占用
        /// </summary>
        public bool HasUnit { get; set; }
        
        /// <summary>
        /// 格子的世界坐标位置
        /// </summary>
        public Vector3 WorldPosition { get; private set; }
        
        #endregion

        #region A* 寻路临时数据
        
        /// <summary>
        /// G值：从起点到当前节点的实际代价
        /// </summary>
        public float GCost { get; set; }
        
        /// <summary>
        /// H值：从当前节点到终点的预估代价（启发式）
        /// </summary>
        public float HCost { get; set; }
        
        /// <summary>
        /// F值：G + H
        /// </summary>
        public float FCost => GCost + HCost;
        
        /// <summary>
        /// 父节点（用于回溯路径）
        /// </summary>
        public Tile Parent { get; set; }
        
        #endregion

        #region 构造函数
        
        public Tile(int x, int y, TileType type, Vector3 worldPosition)
        {
            X = x;
            Y = y;
            TileType = type;
            WorldPosition = worldPosition;
            IsWalkable = type.IsDefaultWalkable();
            OccupiedByBuildingId = 0;
            HasUnit = false;
        }
        
        #endregion

        #region 方法
        
        /// <summary>
        /// 重置寻路数据（每次寻路前调用）
        /// </summary>
        public void ResetPathfindingData()
        {
            GCost = 0;
            HCost = 0;
            Parent = null;
        }
        
        /// <summary>
        /// 判断指定单位是否可以通过此格子
        /// </summary>
        public bool CanUnitPass(bool canSwim, bool canFly)
        {
            // 飞行单位可以通过任何地形
            if (canFly) return true;
            
            // 水域需要可游泳能力
            if (TileType == TileType.Water) return canSwim;
            
            // 其他地形按默认可通行性
            return IsWalkable && OccupiedByBuildingId == 0;
        }
        
        public override string ToString()
        {
            return $"Tile({X},{Y}) Type:{TileType} Walkable:{IsWalkable}";
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Tile other)
            {
                return X == other.X && Y == other.Y;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return X * 1000 + Y;
        }
        
        #endregion
    }
}
