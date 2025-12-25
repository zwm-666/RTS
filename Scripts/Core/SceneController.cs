// ============================================================
// SceneController.cs
// 全局场景控制器 - 管理场景加载和跨场景服务
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RTS.Core
{
    /// <summary>
    /// 全局场景控制器（单例模式）
    /// 负责场景加载和跨场景服务管理
    /// </summary>
    public class SceneController : MonoBehaviour
    {
        #region 单例
        
        private static SceneController _instance;
        public static SceneController Instance => _instance;
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 场景加载开始
        /// </summary>
        public event Action<string> OnSceneLoadStart;
        
        /// <summary>
        /// 场景加载进度更新
        /// </summary>
        public event Action<float> OnSceneLoadProgress;
        
        /// <summary>
        /// 场景加载完成
        /// </summary>
        public event Action<string> OnSceneLoadComplete;
        
        #endregion

        #region 配置
        
        [Header("场景名称")]
        [SerializeField] private string _mainMenuScene = "MainMenuScene";
        [SerializeField] private string _gameScene = "GameScene";
        
        [Header("加载UI")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private Slider _loadingSlider;
        [SerializeField] private Text _loadingText;
        [SerializeField] private Text _tipText;
        
        [Header("配置")]
        [SerializeField] private float _minLoadingTime = 0.5f;
        [SerializeField] private bool _autoLoadMainMenu = true;
        
        [Header("加载提示")]
        [SerializeField] private string[] _loadingTips = new string[]
        {
            "正在加载资源...",
            "准备战斗...",
            "初始化系统..."
        };
        
        #endregion

        #region 私有字段
        
        private bool _isLoading = false;
        private string _currentScene = "";
        
        #endregion

        #region 属性
        
        public bool IsLoading => _isLoading;
        public string CurrentScene => _currentScene;
        public string MainMenuScene => _mainMenuScene;
        public string GameScene => _gameScene;
        
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
            DontDestroyOnLoad(gameObject);
            
            _currentScene = SceneManager.GetActiveScene().name;
            
            // 初始化加载UI
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(false);
            }
            
            // 创建加载UI（如果没有）
            if (_loadingPanel == null)
            {
                CreateLoadingUI();
            }
        }
        
        private void Start()
        {
            // 自动加载主菜单
            if (_autoLoadMainMenu && _currentScene != _mainMenuScene)
            {
                LoadScene(_mainMenuScene);
            }
        }
        
        #endregion

        #region 场景加载
        
        /// <summary>
        /// 加载场景
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneController] 正在加载中，请稍候");
                return;
            }
            
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        
        /// <summary>
        /// 加载游戏场景
        /// </summary>
        public void LoadGameScene()
        {
            LoadScene(_gameScene);
        }
        
        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene(_mainMenuScene);
        }
        
        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(_currentScene);
        }
        
        /// <summary>
        /// 异步加载场景
        /// </summary>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            _isLoading = true;
            
            Debug.Log($"[SceneController] 开始加载场景: {sceneName}");
            OnSceneLoadStart?.Invoke(sceneName);
            
            // 显示加载UI
            ShowLoadingUI();
            UpdateLoadingProgress(0f);
            
            // 显示随机提示
            ShowRandomTip();
            
            float startTime = Time.time;
            
            // 开始异步加载
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;
            
            // 等待加载完成
            while (!asyncLoad.isDone)
            {
                // 进度 0-0.9 表示加载中，0.9 表示加载完成等待激活
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateLoadingProgress(progress);
                OnSceneLoadProgress?.Invoke(progress);
                
                // 加载完成（progress >= 0.9）
                if (asyncLoad.progress >= 0.9f)
                {
                    // 确保最小加载时间
                    float elapsed = Time.time - startTime;
                    if (elapsed < _minLoadingTime)
                    {
                        yield return new WaitForSeconds(_minLoadingTime - elapsed);
                    }
                    
                    UpdateLoadingProgress(1f);
                    yield return new WaitForSeconds(0.1f);
                    
                    // 激活场景
                    asyncLoad.allowSceneActivation = true;
                }
                
                yield return null;
            }
            
            // 隐藏加载UI
            HideLoadingUI();
            
            _currentScene = sceneName;
            _isLoading = false;
            
            Debug.Log($"[SceneController] 场景加载完成: {sceneName}");
            OnSceneLoadComplete?.Invoke(sceneName);
        }
        
        #endregion

        #region 加载UI
        
        /// <summary>
        /// 创建默认加载UI
        /// </summary>
        private void CreateLoadingUI()
        {
            // 创建 Canvas
            GameObject canvasObj = new GameObject("LoadingCanvas");
            canvasObj.transform.SetParent(transform);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 最顶层
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 创建背景面板
            _loadingPanel = new GameObject("LoadingPanel");
            _loadingPanel.transform.SetParent(canvasObj.transform, false);
            
            RectTransform panelRect = _loadingPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            Image panelImage = _loadingPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            
            // 创建进度条背景
            GameObject sliderBg = new GameObject("SliderBackground");
            sliderBg.transform.SetParent(_loadingPanel.transform, false);
            
            RectTransform sliderBgRect = sliderBg.AddComponent<RectTransform>();
            sliderBgRect.anchorMin = new Vector2(0.2f, 0.4f);
            sliderBgRect.anchorMax = new Vector2(0.8f, 0.45f);
            sliderBgRect.offsetMin = Vector2.zero;
            sliderBgRect.offsetMax = Vector2.zero;
            
            Image sliderBgImage = sliderBg.AddComponent<Image>();
            sliderBgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            
            // 创建进度条
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(_loadingPanel.transform, false);
            
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.2f, 0.4f);
            sliderRect.anchorMax = new Vector2(0.8f, 0.45f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            
            _loadingSlider = sliderObj.AddComponent<Slider>();
            _loadingSlider.minValue = 0f;
            _loadingSlider.maxValue = 1f;
            _loadingSlider.interactable = false;
            
            // 创建填充区域
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f, 1f);
            
            _loadingSlider.fillRect = fillRect;
            
            // 创建百分比文本
            GameObject textObj = new GameObject("LoadingText");
            textObj.transform.SetParent(_loadingPanel.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.2f, 0.46f);
            textRect.anchorMax = new Vector2(0.8f, 0.55f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            _loadingText = textObj.AddComponent<Text>();
            _loadingText.text = "加载中... 0%";
            _loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _loadingText.fontSize = 24;
            _loadingText.alignment = TextAnchor.MiddleCenter;
            _loadingText.color = Color.white;
            
            // 创建提示文本
            GameObject tipObj = new GameObject("TipText");
            tipObj.transform.SetParent(_loadingPanel.transform, false);
            
            RectTransform tipRect = tipObj.AddComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.2f, 0.3f);
            tipRect.anchorMax = new Vector2(0.8f, 0.38f);
            tipRect.offsetMin = Vector2.zero;
            tipRect.offsetMax = Vector2.zero;
            
            _tipText = tipObj.AddComponent<Text>();
            _tipText.text = "";
            _tipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tipText.fontSize = 18;
            _tipText.alignment = TextAnchor.MiddleCenter;
            _tipText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            _loadingPanel.SetActive(false);
            
            DontDestroyOnLoad(canvasObj);
        }
        
        private void ShowLoadingUI()
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(true);
            }
        }
        
        private void HideLoadingUI()
        {
            if (_loadingPanel != null)
            {
                _loadingPanel.SetActive(false);
            }
        }
        
        private void UpdateLoadingProgress(float progress)
        {
            if (_loadingSlider != null)
            {
                _loadingSlider.value = progress;
            }
            
            if (_loadingText != null)
            {
                _loadingText.text = $"加载中... {Mathf.RoundToInt(progress * 100)}%";
            }
        }
        
        private void ShowRandomTip()
        {
            if (_tipText != null && _loadingTips.Length > 0)
            {
                int index = UnityEngine.Random.Range(0, _loadingTips.Length);
                _tipText.text = _loadingTips[index];
            }
        }
        
        #endregion

        #region 应用控制
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[SceneController] 退出游戏");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        #endregion
    }
}
