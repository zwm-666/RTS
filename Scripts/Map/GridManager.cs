// ============================================================
// GridManager.cs
// 网格管理器 - 管理128x128格子地图
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace RTS.Map
{
    /// <summary>
    /// 网格管理器（单例模式）
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        #region 单例
        
        private static GridManager _instance;
        public static GridManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GridManager>();
                }
                return _instance;
            }
        }
        
        #endregion

        #region 配置
        
        [Header("网格配置")]
        [SerializeField] private int _gridWidth = 128;
        [SerializeField] private int _gridHeight = 128;
        [SerializeField] private float _cellSize = 1f;
        
        [Header("可视化")]
        [SerializeField] private bool _showGrid = true;
        [SerializeField] private bool _showTileTypes = false;
        
        [Header("地形材质（可选）")]
        [SerializeField] private Material _plainMaterial;
        [SerializeField] private Material _highlandMaterial;
        [SerializeField] private Material _lavaMaterial;
        [SerializeField] private Material _waterMaterial;
        
        #endregion

        #region 私有字段
        
        private Tile[,] _tiles;
        private Vector3 _originPosition;
        
        #endregion

        #region 属性
        
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float CellSize => _cellSize;
        public Tile[,] Tiles => _tiles;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            _originPosition = transform.position;
            InitializeGrid();
        }
        
        #endregion

        #region 初始化
        
        /// <summary>
        /// 初始化网格
        /// </summary>
        public void InitializeGrid()
        {
            _tiles = new Tile[_gridWidth, _gridHeight];
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    Vector3 worldPos = GetWorldPosition(x, y);
                    TileType type = GenerateTerrainType(x, y);
                    _tiles[x, y] = new Tile(x, y, type, worldPos);
                }
            }
            
            Debug.Log($"[GridManager] 网格初始化完成：{_gridWidth}x{_gridHeight}，格子大小：{_cellSize}");
        }
        
        /// <summary>
        /// 生成地形类型（可根据噪声或地图数据自定义）
        /// </summary>
        private TileType GenerateTerrainType(int x, int y)
        {
            // 使用 Perlin 噪声生成基础地形
            float noiseValue = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
            
            // 边缘区域生成水域
            int borderSize = 5;
            if (x < borderSize || x >= _gridWidth - borderSize || 
                y < borderSize || y >= _gridHeight - borderSize)
            {
                return TileType.Water;
            }
            
            // 根据噪声值分配地形
            if (noiseValue < 0.2f)
                return TileType.Water;
            else if (noiseValue < 0.35f)
                return TileType.Forest;
            else if (noiseValue < 0.7f)
                return TileType.Plain;
            else if (noiseValue < 0.85f)
                return TileType.Highland;
            else
                return TileType.Mountain;
        }
        
        #endregion

        #region 坐标转换
        
        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        public Vector3 GetWorldPosition(int x, int y)
        {
            return _originPosition + new Vector3(
                x * _cellSize + _cellSize / 2f,
                0,
                y * _cellSize + _cellSize / 2f
            );
        }
        
        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        public void GetGridPosition(Vector3 worldPosition, out int x, out int y)
        {
            Vector3 localPos = worldPosition - _originPosition;
            x = Mathf.FloorToInt(localPos.x / _cellSize);
            y = Mathf.FloorToInt(localPos.z / _cellSize);
            
            // 边界检查
            x = Mathf.Clamp(x, 0, _gridWidth - 1);
            y = Mathf.Clamp(y, 0, _gridHeight - 1);
        }
        
        /// <summary>
        /// 获取指定网格坐标的格子
        /// </summary>
        public Tile GetTile(int x, int y)
        {
            if (IsValidPosition(x, y))
            {
                return _tiles[x, y];
            }
            return null;
        }
        
        /// <summary>
        /// 获取世界坐标对应的格子
        /// </summary>
        public Tile GetTileAtWorldPosition(Vector3 worldPosition)
        {
            GetGridPosition(worldPosition, out int x, out int y);
            return GetTile(x, y);
        }
        
        /// <summary>
        /// 检查网格坐标是否有效
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight;
        }
        
        #endregion

        #region 格子操作
        
        /// <summary>
        /// 设置格子的地形类型
        /// </summary>
        public void SetTileType(int x, int y, TileType type)
        {
            Tile tile = GetTile(x, y);
            if (tile != null)
            {
                tile.TileType = type;
                tile.IsWalkable = type.IsDefaultWalkable();
            }
        }
        
        /// <summary>
        /// 设置格子是否可通行（用于建筑占用）
        /// </summary>
        public void SetTileWalkable(int x, int y, bool walkable, int buildingId = 0)
        {
            Tile tile = GetTile(x, y);
            if (tile != null)
            {
                tile.IsWalkable = walkable;
                tile.OccupiedByBuildingId = buildingId;
            }
        }
        
        /// <summary>
        /// 获取格子的邻居（用于寻路）
        /// </summary>
        public List<Tile> GetNeighbors(Tile tile, bool includeDiagonals = true)
        {
            List<Tile> neighbors = new List<Tile>();
            
            // 四方向
            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };
            
            for (int i = 0; i < 4; i++)
            {
                int nx = tile.X + dx[i];
                int ny = tile.Y + dy[i];
                Tile neighbor = GetTile(nx, ny);
                if (neighbor != null)
                {
                    neighbors.Add(neighbor);
                }
            }
            
            // 对角线方向
            if (includeDiagonals)
            {
                int[] ddx = { 1, 1, -1, -1 };
                int[] ddy = { 1, -1, 1, -1 };
                
                for (int i = 0; i < 4; i++)
                {
                    int nx = tile.X + ddx[i];
                    int ny = tile.Y + ddy[i];
                    Tile neighbor = GetTile(nx, ny);
                    if (neighbor != null)
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }
            
            return neighbors;
        }
        
        #endregion

        #region 调试可视化
        
        private void OnDrawGizmos()
        {
            if (!_showGrid || _tiles == null) return;
            
            // 限制绘制范围以提高性能
            int maxDraw = Mathf.Min(64, _gridWidth);
            
            for (int x = 0; x < maxDraw; x++)
            {
                for (int y = 0; y < maxDraw; y++)
                {
                    Tile tile = _tiles[x, y];
                    if (tile == null) continue;
                    
                    // 根据地形类型设置颜色
                    if (_showTileTypes)
                    {
                        switch (tile.TileType)
                        {
                            case TileType.Plain:    Gizmos.color = new Color(0.5f, 0.8f, 0.5f, 0.3f); break;
                            case TileType.Highland: Gizmos.color = new Color(0.6f, 0.5f, 0.3f, 0.3f); break;
                            case TileType.Lava:     Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f); break;
                            case TileType.Water:    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f); break;
                            case TileType.Forest:   Gizmos.color = new Color(0f, 0.5f, 0f, 0.4f); break;
                            case TileType.Mountain: Gizmos.color = new Color(0.4f, 0.4f, 0.4f, 0.5f); break;
                        }
                        Gizmos.DrawCube(tile.WorldPosition, new Vector3(_cellSize * 0.9f, 0.1f, _cellSize * 0.9f));
                    }
                    
                    // 绘制网格线
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(tile.WorldPosition, new Vector3(_cellSize, 0.1f, _cellSize));
                }
            }
        }
        
        #endregion
    }
}
