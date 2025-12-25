// ============================================================
// FogOfWarManager.cs
// 战争迷雾管理器 - 基于网格的迷雾系统
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Units;
using RTS.Buildings;

namespace RTS.Map
{
    /// <summary>
    /// 战争迷雾管理器（单例模式）
    /// 使用 Texture2D 方案实现，适合移动端优化
    /// </summary>
    public class FogOfWarManager : MonoBehaviour
    {
        #region 单例
        
        private static FogOfWarManager _instance;
        public static FogOfWarManager Instance => _instance;
        
        #endregion

        #region 配置
        
        [Header("玩家配置")]
        [SerializeField] private int _localPlayerId = 0;
        
        [Header("迷雾配置")]
        [Tooltip("迷雾更新频率（每秒多少次）")]
        [SerializeField] private float _updateRate = 10f;
        
        [Tooltip("视野边缘软化程度")]
        [SerializeField] [Range(0f, 1f)] private float _edgeSoftness = 0.3f;
        
        [Header("迷雾颜色")]
        [SerializeField] private Color _unexploredColor = Color.black;
        [SerializeField] private Color _exploredColor = new Color(0, 0, 0, 0.5f);
        [SerializeField] private Color _visibleColor = new Color(0, 0, 0, 0f);
        
        [Header("迷雾渲染")]
        [Tooltip("迷雾平面（自动创建或手动指定）")]
        [SerializeField] private GameObject _fogPlane;
        [SerializeField] private Material _fogMaterial;
        [SerializeField] private float _fogHeight = 0.5f;
        
        [Header("性能优化")]
        [Tooltip("纹理分辨率相对于网格的缩放（1=1:1，0.5=一半分辨率）")]
        [SerializeField] [Range(0.25f, 1f)] private float _textureScale = 0.5f;
        
        #endregion

        #region 私有字段
        
        // 迷雾状态数组
        private bool[,] _isExplored;
        private bool[,] _isVisible;
        private int _gridWidth;
        private int _gridHeight;
        
        // 迷雾纹理
        private Texture2D _fogTexture;
        private Color[] _fogColors;
        private int _texWidth;
        private int _texHeight;
        
        // 更新计时
        private float _updateTimer;
        private float _updateInterval;
        
        // 缓存
        private List<Unit> _allUnits = new List<Unit>();
        private List<Building> _allBuildings = new List<Building>();
        
        #endregion

        #region 属性
        
        public int LocalPlayerId => _localPlayerId;
        
        /// <summary>
        /// 检查网格位置是否可见
        /// </summary>
        public bool IsVisible(int x, int y)
        {
            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                return false;
            return _isVisible[x, y];
        }
        
        /// <summary>
        /// 检查网格位置是否已探索
        /// </summary>
        public bool IsExplored(int x, int y)
        {
            if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                return false;
            return _isExplored[x, y];
        }
        
        /// <summary>
        /// 检查世界位置是否可见
        /// </summary>
        public bool IsVisibleAtWorld(Vector3 worldPos)
        {
            if (GridManager.Instance == null) return true;
            
            GridManager.Instance.GetGridPosition(worldPos, out int x, out int y);
            return IsVisible(x, y);
        }
        
        /// <summary>
        /// 检查世界位置是否已探索
        /// </summary>
        public bool IsExploredAtWorld(Vector3 worldPos)
        {
            if (GridManager.Instance == null) return true;
            
            GridManager.Instance.GetGridPosition(worldPos, out int x, out int y);
            return IsExplored(x, y);
        }
        
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
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0;
                UpdateFogOfWar();
            }
        }
        
        private void OnDestroy()
        {
            if (_fogTexture != null)
            {
                Destroy(_fogTexture);
            }
        }
        
        #endregion

        #region 初始化
        
        private void Initialize()
        {
            if (GridManager.Instance == null)
            {
                Debug.LogError("[FogOfWar] GridManager 未找到！");
                return;
            }
            
            _gridWidth = GridManager.Instance.GridWidth;
            _gridHeight = GridManager.Instance.GridHeight;
            _updateInterval = 1f / _updateRate;
            
            // 初始化状态数组
            _isExplored = new bool[_gridWidth, _gridHeight];
            _isVisible = new bool[_gridWidth, _gridHeight];
            
            // 计算纹理尺寸
            _texWidth = Mathf.CeilToInt(_gridWidth * _textureScale);
            _texHeight = Mathf.CeilToInt(_gridHeight * _textureScale);
            
            // 创建迷雾纹理
            CreateFogTexture();
            
            // 创建迷雾平面
            CreateFogPlane();
            
            Debug.Log($"[FogOfWar] 初始化完成 - 网格:{_gridWidth}x{_gridHeight}, 纹理:{_texWidth}x{_texHeight}");
        }
        
        private void CreateFogTexture()
        {
            _fogTexture = new Texture2D(_texWidth, _texHeight, TextureFormat.RGBA32, false);
            _fogTexture.filterMode = FilterMode.Bilinear;
            _fogTexture.wrapMode = TextureWrapMode.Clamp;
            
            _fogColors = new Color[_texWidth * _texHeight];
            
            // 初始化为完全黑色（未探索）
            for (int i = 0; i < _fogColors.Length; i++)
            {
                _fogColors[i] = _unexploredColor;
            }
            
            _fogTexture.SetPixels(_fogColors);
            _fogTexture.Apply();
        }
        
        private void CreateFogPlane()
        {
            if (_fogPlane == null)
            {
                // 自动创建迷雾平面
                _fogPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _fogPlane.name = "FogOfWarPlane";
                _fogPlane.transform.SetParent(transform);
                
                // 移除碰撞体
                Collider col = _fogPlane.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }
            
            // 设置位置和大小
            float worldWidth = _gridWidth * GridManager.Instance.CellSize;
            float worldHeight = _gridHeight * GridManager.Instance.CellSize;
            
            _fogPlane.transform.position = GridManager.Instance.transform.position + 
                new Vector3(worldWidth / 2f, _fogHeight, worldHeight / 2f);
            _fogPlane.transform.rotation = Quaternion.Euler(90, 0, 0);
            _fogPlane.transform.localScale = new Vector3(worldWidth, worldHeight, 1);
            
            // 应用材质
            Renderer renderer = _fogPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (_fogMaterial == null)
                {
                    // 创建默认迷雾材质
                    _fogMaterial = new Material(Shader.Find("RTS/FogOfWar"));
                    if (_fogMaterial.shader == null || !_fogMaterial.shader.isSupported)
                    {
                        // 备用：使用透明材质
                        _fogMaterial = new Material(Shader.Find("Unlit/Transparent"));
                    }
                }
                
                renderer.material = _fogMaterial;
                renderer.material.mainTexture = _fogTexture;
            }
        }
        
        #endregion

        #region 迷雾更新
        
        private void UpdateFogOfWar()
        {
            // 重置当前可见状态
            ResetVisibility();
            
            // 刷新单位和建筑列表
            RefreshEntities();
            
            // 计算本地玩家的视野
            CalculateVisibility();
            
            // 更新迷雾纹理
            UpdateFogTexture();
        }
        
        private void ResetVisibility()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _isVisible[x, y] = false;
                }
            }
        }
        
        private void RefreshEntities()
        {
            // 刷新单位列表
            _allUnits.Clear();
            _allUnits.AddRange(FindObjectsOfType<Unit>());
            
            // 刷新建筑列表
            _allBuildings.Clear();
            _allBuildings.AddRange(FindObjectsOfType<Building>());
        }
        
        private void CalculateVisibility()
        {
            // 处理单位视野
            foreach (var unit in _allUnits)
            {
                if (unit == null || !unit.IsAlive) continue;
                if (unit.PlayerId != _localPlayerId) continue;
                
                float sightRange = unit.DomainData?.SightRange ?? 10f;
                RevealArea(unit.transform.position, sightRange);
            }
            
            // 处理建筑视野
            foreach (var building in _allBuildings)
            {
                if (building == null || !building.IsAlive) continue;
                if (building.PlayerId != _localPlayerId) continue;
                
                float sightRange = building.DomainData?.SightRange ?? 10f;
                RevealArea(building.transform.position, sightRange);
            }
        }
        
        /// <summary>
        /// 揭示指定位置周围的区域
        /// </summary>
        private void RevealArea(Vector3 worldPos, float radius)
        {
            if (GridManager.Instance == null) return;
            
            GridManager.Instance.GetGridPosition(worldPos, out int centerX, out int centerY);
            
            int gridRadius = Mathf.CeilToInt(radius / GridManager.Instance.CellSize);
            
            for (int dx = -gridRadius; dx <= gridRadius; dx++)
            {
                for (int dy = -gridRadius; dy <= gridRadius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    
                    if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                        continue;
                    
                    // 圆形检测
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) * GridManager.Instance.CellSize;
                    if (distance <= radius)
                    {
                        _isVisible[x, y] = true;
                        _isExplored[x, y] = true;
                    }
                }
            }
        }
        
        private void UpdateFogTexture()
        {
            float scaleX = (float)_gridWidth / _texWidth;
            float scaleY = (float)_gridHeight / _texHeight;
            
            for (int tx = 0; tx < _texWidth; tx++)
            {
                for (int ty = 0; ty < _texHeight; ty++)
                {
                    // 映射纹理坐标到网格坐标
                    int gx = Mathf.FloorToInt(tx * scaleX);
                    int gy = Mathf.FloorToInt(ty * scaleY);
                    
                    gx = Mathf.Clamp(gx, 0, _gridWidth - 1);
                    gy = Mathf.Clamp(gy, 0, _gridHeight - 1);
                    
                    Color color;
                    
                    if (_isVisible[gx, gy])
                    {
                        color = _visibleColor;
                    }
                    else if (_isExplored[gx, gy])
                    {
                        color = _exploredColor;
                    }
                    else
                    {
                        color = _unexploredColor;
                    }
                    
                    int index = ty * _texWidth + tx;
                    
                    // 平滑过渡
                    _fogColors[index] = Color.Lerp(_fogColors[index], color, _edgeSoftness);
                }
            }
            
            _fogTexture.SetPixels(_fogColors);
            _fogTexture.Apply();
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 手动揭示区域（用于技能等）
        /// </summary>
        public void RevealAreaManual(Vector3 worldPos, float radius, bool permanent = false)
        {
            RevealArea(worldPos, radius);
            
            if (permanent)
            {
                // 永久揭示（标记为已探索）
                // 已在 RevealArea 中自动设置
            }
            
            UpdateFogTexture();
        }
        
        /// <summary>
        /// 完全揭示地图（作弊/调试用）
        /// </summary>
        [ContextMenu("Reveal All")]
        public void RevealAll()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _isVisible[x, y] = true;
                    _isExplored[x, y] = true;
                }
            }
            
            UpdateFogTexture();
            Debug.Log("[FogOfWar] 地图已完全揭示");
        }
        
        /// <summary>
        /// 重置迷雾（隐藏所有区域）
        /// </summary>
        [ContextMenu("Reset Fog")]
        public void ResetFog()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _isVisible[x, y] = false;
                    _isExplored[x, y] = false;
                }
            }
            
            UpdateFogTexture();
            Debug.Log("[FogOfWar] 迷雾已重置");
        }
        
        #endregion
    }
}
