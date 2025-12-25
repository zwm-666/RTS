// ============================================================
// MapDemo.cs
// 地图演示脚本 - 快速搭建测试场景
// ============================================================

using UnityEngine;
using RTS.Map;
using RTS.Units;

namespace RTS.Demo
{
    /// <summary>
    /// 地图演示脚本
    /// 用于快速生成测试场景和演示寻路功能
    /// </summary>
    public class MapDemo : MonoBehaviour
    {
        [Header("地图配置")]
        [SerializeField] private int _mapSize = 32;  // 演示用小地图
        [SerializeField] private float _cellSize = 1f;
        
        [Header("地形可视化")]
        [SerializeField] private bool _generateVisualTiles = true;
        [SerializeField] private Material _plainMaterial;
        [SerializeField] private Material _highlandMaterial;
        [SerializeField] private Material _waterMaterial;
        [SerializeField] private Material _forestMaterial;
        [SerializeField] private Material _mountainMaterial;
        
        [Header("测试单位")]
        [SerializeField] private GameObject _testUnitPrefab;
        
        private GameObject _tilesParent;
        
        private void Start()
        {
            InitializeDemo();
        }
        
        /// <summary>
        /// 初始化演示场景
        /// </summary>
        public void InitializeDemo()
        {
            // 确保有 GridManager
            if (GridManager.Instance == null)
            {
                Debug.LogError("[MapDemo] 请确保场景中有 GridManager！");
                return;
            }
            
            // 确保有 Pathfinding
            if (Pathfinding.Instance == null)
            {
                Debug.LogError("[MapDemo] 请确保场景中有 Pathfinding！");
                return;
            }
            
            // 生成可视化地形
            if (_generateVisualTiles)
            {
                GenerateVisualTiles();
            }
            
            Debug.Log("[MapDemo] 演示场景初始化完成");
            Debug.Log("=== 操作说明 ===");
            Debug.Log("左键点击：选择单位");
            Debug.Log("左键拖拽：框选单位");
            Debug.Log("右键点击地面：移动命令");
            Debug.Log("右键点击敌方单位：攻击命令");
            Debug.Log("S 键：停止移动");
            Debug.Log("ESC 键：取消选择");
        }
        
        /// <summary>
        /// 生成可视化地砖
        /// </summary>
        private void GenerateVisualTiles()
        {
            GridManager grid = GridManager.Instance;
            
            _tilesParent = new GameObject("VisualTiles");
            _tilesParent.transform.SetParent(transform);
            
            // 限制可视化范围以保证性能
            int visualSize = Mathf.Min(_mapSize, grid.GridWidth, grid.GridHeight);
            
            for (int x = 0; x < visualSize; x++)
            {
                for (int y = 0; y < visualSize; y++)
                {
                    Tile tile = grid.GetTile(x, y);
                    if (tile == null) continue;
                    
                    // 创建地砖可视化
                    GameObject tileObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tileObj.name = $"Tile_{x}_{y}";
                    tileObj.transform.SetParent(_tilesParent.transform);
                    tileObj.transform.position = tile.WorldPosition + Vector3.up * 0.01f;
                    tileObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                    tileObj.transform.localScale = new Vector3(grid.CellSize * 0.95f, grid.CellSize * 0.95f, 1);
                    
                    // 移除碰撞器（使用地面碰撞）
                    Destroy(tileObj.GetComponent<Collider>());
                    
                    // 设置材质
                    Renderer renderer = tileObj.GetComponent<Renderer>();
                    renderer.material = GetMaterialForTileType(tile.TileType);
                }
            }
            
            Debug.Log($"[MapDemo] 生成了 {visualSize}x{visualSize} 个可视化地砖");
        }
        
        /// <summary>
        /// 获取地形类型对应的材质
        /// </summary>
        private Material GetMaterialForTileType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Plain:
                    return _plainMaterial ?? CreateDefaultMaterial(new Color(0.5f, 0.8f, 0.4f));
                case TileType.Highland:
                    return _highlandMaterial ?? CreateDefaultMaterial(new Color(0.6f, 0.5f, 0.3f));
                case TileType.Water:
                    return _waterMaterial ?? CreateDefaultMaterial(new Color(0.2f, 0.5f, 1f));
                case TileType.Forest:
                    return _forestMaterial ?? CreateDefaultMaterial(new Color(0.1f, 0.4f, 0.1f));
                case TileType.Mountain:
                    return _mountainMaterial ?? CreateDefaultMaterial(new Color(0.4f, 0.4f, 0.4f));
                case TileType.Lava:
                    return CreateDefaultMaterial(new Color(1f, 0.3f, 0f));
                default:
                    return CreateDefaultMaterial(Color.gray);
            }
        }
        
        /// <summary>
        /// 创建默认材质
        /// </summary>
        private Material CreateDefaultMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            return mat;
        }
        
        /// <summary>
        /// 生成测试单位（按 T 键）
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                SpawnTestUnit();
            }
        }
        
        /// <summary>
        /// 在随机位置生成测试单位
        /// </summary>
        private void SpawnTestUnit()
        {
            if (_testUnitPrefab == null)
            {
                // 创建简单的测试单位
                GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                unitObj.name = "TestUnit";
                
                Vector3 pos = new Vector3(
                    Random.Range(10, 20),
                    1,
                    Random.Range(10, 20)
                );
                unitObj.transform.position = pos;
                
                // 添加 Unit 组件
                Unit unit = unitObj.AddComponent<Unit>();
                
                // 设置颜色
                unitObj.GetComponent<Renderer>().material.color = Color.blue;
                
                Debug.Log($"[MapDemo] 在 {pos} 生成了测试单位");
            }
            else
            {
                Vector3 pos = new Vector3(
                    Random.Range(10, 20),
                    0,
                    Random.Range(10, 20)
                );
                Instantiate(_testUnitPrefab, pos, Quaternion.identity);
                Debug.Log($"[MapDemo] 在 {pos} 生成了测试单位");
            }
        }
    }
}
