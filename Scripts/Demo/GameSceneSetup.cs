// ============================================================
// GameSceneSetup.cs
// 游戏场景自动配置 - 一键创建所有管理器和测试对象
// ============================================================

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RTS.Demo
{
    /// <summary>
    /// 场景自动配置工具
    /// 挂载到空对象上运行一次即可配置整个场景
    /// </summary>
    public class GameSceneSetup : MonoBehaviour
    {
        [Header("配置选项")]
        [SerializeField] private bool _setupOnStart = true;
        [SerializeField] private bool _createTestUnits = true;
        [SerializeField] private int _testUnitCount = 5;
        
        private void Start()
        {
            if (_setupOnStart)
            {
                SetupScene();
            }
        }
        
        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            Debug.Log("========== 开始配置场景 ==========");
            
            // 1. 创建管理器根对象
            GameObject managersRoot = FindOrCreate("--- MANAGERS ---");
            
            // 2. 创建各个管理器
            CreateManager<RTS.Core.GameBootstrap>("GameBootstrap", managersRoot);
            CreateManager<RTS.Managers.GameManager>("GameManager", managersRoot);
            CreateManager<RTS.Core.ObjectPoolManager>("ObjectPoolManager", managersRoot);
            CreateManager<RTS.Map.GridManager>("GridManager", managersRoot);
            CreateManager<RTS.Managers.ResourceManager>("ResourceManager", managersRoot);
            CreateManager<RTS.Managers.StatsManager>("StatsManager", managersRoot);
            CreateManager<RTS.Controllers.SelectionManager>("SelectionManager", managersRoot);
            CreateManager<RTS.Map.FogOfWarManager>("FogOfWarManager", managersRoot);
            CreateManager<RTS.Tech.TechManager>("TechManager", managersRoot);
            CreateManager<RTS.Resources.MarketManager>("MarketManager", managersRoot);
            CreateManager<RTS.Presentation.PrefabBinder>("PrefabBinder", managersRoot);
            CreateManager<RTS.Presentation.EntitySpawner>("EntitySpawner", managersRoot);
            
            // 3. 配置摄像机
            SetupCamera();
            
            // 4. 创建地面
            CreateGround();
            
            // 5. 创建UI
            SetupUI();
            
            // 6. 创建测试单位
            if (_createTestUnits)
            {
                CreateTestUnits();
            }
            
            Debug.Log("========== 场景配置完成 ==========");
        }
        
        private GameObject FindOrCreate(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
            }
            return obj;
        }
        
        private T CreateManager<T>(string name, GameObject parent) where T : Component
        {
            // 检查是否已存在
            T existing = FindObjectOfType<T>();
            if (existing != null)
            {
                Debug.Log($"[SceneSetup] {name} 已存在");
                return existing;
            }
            
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform);
            T component = obj.AddComponent<T>();
            Debug.Log($"[SceneSetup] 创建 {name}");
            return component;
        }
        
        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            
            // 配置为俯视相机
            mainCam.transform.position = new Vector3(64, 30, 45);
            mainCam.transform.rotation = Quaternion.Euler(50, 0, 0);
            
            // 添加移动端摄像机控制
            if (mainCam.GetComponent<RTS.Controllers.MobileCameraController>() == null)
            {
                mainCam.gameObject.AddComponent<RTS.Controllers.MobileCameraController>();
            }
            
            Debug.Log("[SceneSetup] 摄像机配置完成");
        }
        
        private void CreateGround()
        {
            if (GameObject.Find("Ground") != null) return;
            
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(64, 0, 64);
            ground.transform.localScale = new Vector3(12.8f, 1, 12.8f);
            
            // 创建简单材质
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.5f, 0.3f);
                renderer.material = mat;
            }
            
            Debug.Log("[SceneSetup] 地面创建完成");
        }
        
        private void SetupUI()
        {
            // 检查是否已有Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // 检查EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            Debug.Log("[SceneSetup] UI 配置完成");
        }
        
        private void CreateTestUnits()
        {
            GameObject unitsRoot = FindOrCreate("--- TEST UNITS ---");
            
            // 创建玩家单位
            for (int i = 0; i < _testUnitCount; i++)
            {
                CreateTestUnit($"PlayerUnit_{i}", 0, 
                    new Vector3(30 + i * 3, 0.5f, 64), Color.blue, unitsRoot);
            }
            
            // 创建敌方单位
            for (int i = 0; i < _testUnitCount; i++)
            {
                CreateTestUnit($"EnemyUnit_{i}", 1, 
                    new Vector3(98 - i * 3, 0.5f, 64), Color.red, unitsRoot);
            }
            
            Debug.Log($"[SceneSetup] 创建了 {_testUnitCount * 2} 个测试单位");
        }
        
        private void CreateTestUnit(string name, int playerId, Vector3 position, Color color, GameObject parent)
        {
            GameObject unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name = name;
            unit.transform.position = position;
            unit.transform.SetParent(parent.transform);
            
            // 设置颜色
            Renderer renderer = unit.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
            
            // 添加Unit组件
            var unitComponent = unit.AddComponent<RTS.Units.Unit>();
            
            // 由于UnitData需要从配置加载，这里只是创建物理对象
            // 实际数据需要通过EntitySpawner或手动设置
        }
        
        #if UNITY_EDITOR
        [MenuItem("RTS/Setup Test Scene")]
        public static void SetupTestSceneMenu()
        {
            // 查找或创建Setup对象
            GameSceneSetup setup = FindObjectOfType<GameSceneSetup>();
            if (setup == null)
            {
                GameObject obj = new GameObject("_SceneSetup");
                setup = obj.AddComponent<GameSceneSetup>();
            }
            
            setup.SetupScene();
            
            // 删除Setup对象
            DestroyImmediate(setup.gameObject);
        }
        #endif
    }
}
