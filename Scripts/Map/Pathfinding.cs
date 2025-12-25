// ============================================================
// Pathfinding.cs
// A* 寻路算法实现
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace RTS.Map
{
    /// <summary>
    /// A* 寻路系统
    /// </summary>
    public class Pathfinding : MonoBehaviour
    {
        #region 单例
        
        private static Pathfinding _instance;
        public static Pathfinding Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<Pathfinding>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("Pathfinding");
                        _instance = go.AddComponent<Pathfinding>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region 配置
        
        [Header("寻路配置")]
        [SerializeField] private bool _allowDiagonalMovement = true;
        [SerializeField] private int _maxIterations = 10000;  // 防止死循环
        
        [Header("调试")]
        [SerializeField] private bool _debugPath = false;
        
        #endregion

        #region 寻路常量
        
        private const float STRAIGHT_COST = 1f;
        private const float DIAGONAL_COST = 1.414f;  // √2
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 寻找从起点到终点的路径
        /// </summary>
        /// <param name="startPos">起点世界坐标</param>
        /// <param name="endPos">终点世界坐标</param>
        /// <param name="canSwim">单位是否可游泳</param>
        /// <param name="canFly">单位是否可飞行</param>
        /// <returns>路径点列表（世界坐标），如果找不到路径返回null</returns>
        public List<Vector3> FindPath(Vector3 startPos, Vector3 endPos, bool canSwim = false, bool canFly = false)
        {
            GridManager grid = GridManager.Instance;
            if (grid == null)
            {
                Debug.LogError("[Pathfinding] GridManager 未初始化！");
                return null;
            }
            
            Tile startTile = grid.GetTileAtWorldPosition(startPos);
            Tile endTile = grid.GetTileAtWorldPosition(endPos);
            
            if (startTile == null || endTile == null)
            {
                Debug.LogWarning("[Pathfinding] 起点或终点无效！");
                return null;
            }
            
            // 检查终点是否可达
            if (!endTile.CanUnitPass(canSwim, canFly))
            {
                // 尝试找一个最近的可达点
                endTile = FindNearestWalkableTile(endTile, canSwim, canFly);
                if (endTile == null)
                {
                    Debug.LogWarning("[Pathfinding] 无法到达目标位置！");
                    return null;
                }
            }
            
            return FindPathInternal(startTile, endTile, canSwim, canFly);
        }
        
        /// <summary>
        /// 寻找从起点格子到终点格子的路径
        /// </summary>
        public List<Vector3> FindPath(Tile startTile, Tile endTile, bool canSwim = false, bool canFly = false)
        {
            return FindPathInternal(startTile, endTile, canSwim, canFly);
        }
        
        #endregion

        #region A* 算法核心
        
        /// <summary>
        /// A* 寻路核心实现
        /// </summary>
        private List<Vector3> FindPathInternal(Tile startTile, Tile endTile, bool canSwim, bool canFly)
        {
            GridManager grid = GridManager.Instance;
            
            // 重置所有格子的寻路数据
            ResetPathfindingData(grid);
            
            // 开放列表和关闭列表
            List<Tile> openList = new List<Tile>();
            HashSet<Tile> closedSet = new HashSet<Tile>();
            
            // 初始化起点
            startTile.GCost = 0;
            startTile.HCost = CalculateHeuristic(startTile, endTile);
            openList.Add(startTile);
            
            int iterations = 0;
            
            while (openList.Count > 0 && iterations < _maxIterations)
            {
                iterations++;
                
                // 找到 F 值最小的节点
                Tile currentTile = GetLowestFCostTile(openList);
                
                // 到达终点
                if (currentTile.Equals(endTile))
                {
                    List<Vector3> path = RetracePath(startTile, endTile);
                    
                    if (_debugPath)
                    {
                        Debug.Log($"[Pathfinding] 路径找到！长度: {path.Count}, 迭代次数: {iterations}");
                        DrawDebugPath(path);
                    }
                    
                    return path;
                }
                
                // 从开放列表移到关闭列表
                openList.Remove(currentTile);
                closedSet.Add(currentTile);
                
                // 遍历邻居
                foreach (Tile neighbor in grid.GetNeighbors(currentTile, _allowDiagonalMovement))
                {
                    // 跳过已在关闭列表或不可通行的节点
                    if (closedSet.Contains(neighbor) || !neighbor.CanUnitPass(canSwim, canFly))
                    {
                        continue;
                    }
                    
                    // 计算从当前节点到邻居的代价
                    float movementCost = GetMovementCost(currentTile, neighbor);
                    float tentativeGCost = currentTile.GCost + movementCost * neighbor.MovementCost;
                    
                    // 如果邻居不在开放列表，或找到更短的路径
                    if (!openList.Contains(neighbor))
                    {
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = CalculateHeuristic(neighbor, endTile);
                        neighbor.Parent = currentTile;
                        openList.Add(neighbor);
                    }
                    else if (tentativeGCost < neighbor.GCost)
                    {
                        neighbor.GCost = tentativeGCost;
                        neighbor.Parent = currentTile;
                    }
                }
            }
            
            // 没有找到路径
            Debug.LogWarning($"[Pathfinding] 未找到路径！迭代次数: {iterations}");
            return null;
        }
        
        /// <summary>
        /// 计算启发式值（使用曼哈顿距离 + 对角线优化）
        /// </summary>
        private float CalculateHeuristic(Tile a, Tile b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            
            // 对角线距离公式
            return STRAIGHT_COST * (dx + dy) + (DIAGONAL_COST - 2 * STRAIGHT_COST) * Mathf.Min(dx, dy);
        }
        
        /// <summary>
        /// 获取两个相邻格子之间的移动代价
        /// </summary>
        private float GetMovementCost(Tile from, Tile to)
        {
            // 对角线移动
            if (from.X != to.X && from.Y != to.Y)
            {
                return DIAGONAL_COST;
            }
            return STRAIGHT_COST;
        }
        
        /// <summary>
        /// 获取开放列表中 F 值最小的节点
        /// </summary>
        private Tile GetLowestFCostTile(List<Tile> openList)
        {
            Tile lowestTile = openList[0];
            
            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].FCost < lowestTile.FCost ||
                    (openList[i].FCost == lowestTile.FCost && openList[i].HCost < lowestTile.HCost))
                {
                    lowestTile = openList[i];
                }
            }
            
            return lowestTile;
        }
        
        /// <summary>
        /// 回溯路径
        /// </summary>
        private List<Vector3> RetracePath(Tile startTile, Tile endTile)
        {
            List<Vector3> path = new List<Vector3>();
            Tile currentTile = endTile;
            
            while (currentTile != null && !currentTile.Equals(startTile))
            {
                path.Add(currentTile.WorldPosition);
                currentTile = currentTile.Parent;
            }
            
            path.Reverse();
            return path;
        }
        
        /// <summary>
        /// 重置所有格子的寻路数据
        /// </summary>
        private void ResetPathfindingData(GridManager grid)
        {
            for (int x = 0; x < grid.GridWidth; x++)
            {
                for (int y = 0; y < grid.GridHeight; y++)
                {
                    grid.Tiles[x, y]?.ResetPathfindingData();
                }
            }
        }
        
        /// <summary>
        /// 查找最近的可通行格子
        /// </summary>
        private Tile FindNearestWalkableTile(Tile center, bool canSwim, bool canFly)
        {
            GridManager grid = GridManager.Instance;
            int searchRadius = 10;
            
            for (int radius = 1; radius <= searchRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        
                        Tile tile = grid.GetTile(center.X + dx, center.Y + dy);
                        if (tile != null && tile.CanUnitPass(canSwim, canFly))
                        {
                            return tile;
                        }
                    }
                }
            }
            
            return null;
        }
        
        #endregion

        #region 调试
        
        /// <summary>
        /// 绘制调试路径
        /// </summary>
        private void DrawDebugPath(List<Vector3> path)
        {
            if (path == null || path.Count < 2) return;
            
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(
                    path[i] + Vector3.up * 0.5f,
                    path[i + 1] + Vector3.up * 0.5f,
                    Color.green,
                    5f
                );
            }
        }
        
        #endregion
    }
}
