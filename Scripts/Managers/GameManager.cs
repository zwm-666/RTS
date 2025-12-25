// ============================================================
// GameManager.cs
// 游戏管理器 - 管理游戏流程、玩家、胜负判定
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RTS.Core;
using RTS.Buildings;

namespace RTS.Managers
{
    /// <summary>
    /// 游戏状态
    /// </summary>
    public enum GameState
    {
        NotStarted,     // 未开始
        Playing,        // 游戏中
        Paused,         // 暂停
        GameOver        // 游戏结束
    }

    /// <summary>
    /// 游戏管理器（单例）
    /// 挂载位置：场景中的 GameManager 空对象
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region 单例
        
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GameManager>();
                }
                return _instance;
            }
        }
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 游戏开始事件
        /// </summary>
        public event Action OnGameStart;
        
        /// <summary>
        /// 游戏暂停/继续事件
        /// </summary>
        public event Action<bool> OnGamePaused;
        
        /// <summary>
        /// 玩家失败事件（参数：失败的玩家ID）
        /// </summary>
        public event Action<int> OnPlayerDefeated;
        
        /// <summary>
        /// 玩家胜利事件（参数：胜利的玩家ID）
        /// </summary>
        public event Action<int> OnPlayerVictory;
        
        /// <summary>
        /// 游戏结束事件（参数：胜利者ID，-1表示平局）
        /// </summary>
        public event Action<int> OnGameOver;
        
        #endregion

        #region 配置
        
        [Header("玩家配置")]
        [SerializeField] private List<PlayerData> _players = new List<PlayerData>();
        [SerializeField] private int _localPlayerId = 0;
        
        [Header("游戏设置")]
        [SerializeField] private bool _autoStartGame = true;
        [SerializeField] private float _gameOverDelay = 2f;
        
        [Header("UI 引用")]
        [SerializeField] private GameOverUI _gameOverUI;
        
        #endregion

        #region 私有字段
        
        private GameState _currentState = GameState.NotStarted;
        private int _winnerId = -1;
        private float _gameTime = 0f;
        
        #endregion

        #region 属性
        
        public GameState CurrentState => _currentState;
        public List<PlayerData> Players => _players;
        public int LocalPlayerId => _localPlayerId;
        public int WinnerId => _winnerId;
        public float GameTime => _gameTime;
        public bool IsPlaying => _currentState == GameState.Playing;
        public bool IsPaused => _currentState == GameState.Paused;
        
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
            
            // 初始化游戏结束UI
            if (_gameOverUI == null)
            {
                _gameOverUI = FindObjectOfType<GameOverUI>(true);
            }
        }
        
        private void Start()
        {
            // 注册所有主基地的死亡事件
            RegisterMainBasesDeathEvents();
            
            if (_autoStartGame)
            {
                StartGame();
            }
        }
        
        private void Update()
        {
            if (_currentState == GameState.Playing)
            {
                _gameTime += Time.deltaTime;
            }
            
            // ESC 暂停
            if (Input.GetKeyDown(KeyCode.Escape) && _currentState == GameState.Playing)
            {
                TogglePause();
            }
        }
        
        private void OnDestroy()
        {
            UnregisterMainBasesDeathEvents();
        }
        
        #endregion

        #region 游戏流程控制
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            if (_currentState != GameState.NotStarted && _currentState != GameState.GameOver)
            {
                Debug.LogWarning("[GameManager] 游戏已经开始！");
                return;
            }
            
            _currentState = GameState.Playing;
            _gameTime = 0f;
            _winnerId = -1;
            
            // 重置所有玩家状态
            foreach (var player in _players)
            {
                player.state = PlayerState.Playing;
            }
            
            Time.timeScale = 1f;
            
            // 隐藏游戏结束UI
            if (_gameOverUI != null)
            {
                _gameOverUI.Hide();
            }
            
            OnGameStart?.Invoke();
            Debug.Log("[GameManager] 游戏开始！");
        }
        
        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            if (_currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (_currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (_currentState != GameState.Playing) return;
            
            _currentState = GameState.Paused;
            Time.timeScale = 0f;
            
            OnGamePaused?.Invoke(true);
            Debug.Log("[GameManager] 游戏暂停");
        }
        
        /// <summary>
        /// 继续游戏
        /// </summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused) return;
            
            _currentState = GameState.Playing;
            Time.timeScale = 1f;
            
            OnGamePaused?.Invoke(false);
            Debug.Log("[GameManager] 游戏继续");
        }
        
        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[GameManager] 重新开始游戏...");
            
            Time.timeScale = 1f;
            
            // 重新加载当前场景
            string currentScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentScene);
        }
        
        /// <summary>
        /// 退出到主菜单
        /// </summary>
        public void ExitToMainMenu()
        {
            Time.timeScale = 1f;
            
            // 优先使用SceneController
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadMainMenu();
            }
            else
            {
                SceneManager.LoadScene("MainMenuScene");
            }
        }
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] 退出游戏");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        /// <summary>
        /// 玩家认输
        /// </summary>
        /// <param name="playerId">认输的玩家ID，默认为本地玩家</param>
        public void Surrender(int playerId = -1)
        {
            // 默认本地玩家认输
            if (playerId < 0)
            {
                playerId = _localPlayerId;
            }
            
            PlayerData player = GetPlayer(playerId);
            if (player == null || player.state != PlayerState.Playing)
            {
                Debug.LogWarning($"[GameManager] 玩家 {playerId} 无法认输（不存在或已不在游戏中）");
                return;
            }
            
            // 如果游戏已暂停，先恢复
            if (_currentState == GameState.Paused)
            {
                ResumeGame();
            }
            
            // 标记玩家失败
            player.state = PlayerState.Defeated;
            
            Debug.Log($"[GameManager] 玩家 {playerId} ({player.playerName}) 选择认输！");
            
            OnPlayerDefeated?.Invoke(playerId);
            
            // 检查游戏是否结束
            CheckGameOver();
        }
        
        /// <summary>
        /// 本地玩家认输（快捷方法）
        /// </summary>
        public void LocalPlayerSurrender()
        {
            Surrender(_localPlayerId);
        }
        
        #endregion

        #region 胜负判定
        
        /// <summary>
        /// 注册主基地死亡事件
        /// </summary>
        private void RegisterMainBasesDeathEvents()
        {
            foreach (var player in _players)
            {
                if (player.mainBase != null)
                {
                    player.mainBase.OnDestroyed += HandleBuildingDestroyed;
                    Debug.Log($"[GameManager] 注册玩家 {player.playerId} 的主基地死亡事件");
                }
            }
        }
        
        /// <summary>
        /// 取消注册主基地死亡事件
        /// </summary>
        private void UnregisterMainBasesDeathEvents()
        {
            foreach (var player in _players)
            {
                if (player.mainBase != null)
                {
                    player.mainBase.OnDestroyed -= HandleBuildingDestroyed;
                }
            }
        }
        
        /// <summary>
        /// 处理建筑被摧毁事件
        /// </summary>
        private void HandleBuildingDestroyed(Building building)
        {
            // 检查是否是某个玩家的主基地
            foreach (var player in _players)
            {
                if (player.mainBase == building)
                {
                    OnMainBaseDestroyed(player.playerId);
                    return;
                }
            }
        }
        
        /// <summary>
        /// 主基地被摧毁
        /// </summary>
        private void OnMainBaseDestroyed(int playerId)
        {
            PlayerData defeatedPlayer = GetPlayer(playerId);
            if (defeatedPlayer == null || defeatedPlayer.state != PlayerState.Playing)
            {
                return;
            }
            
            // 标记玩家失败
            defeatedPlayer.state = PlayerState.Defeated;
            
            Debug.Log($"[GameManager] 玩家 {playerId} ({defeatedPlayer.playerName}) 的主基地被摧毁，玩家失败！");
            
            OnPlayerDefeated?.Invoke(playerId);
            
            // 检查游戏是否结束
            CheckGameOver();
        }
        
        /// <summary>
        /// 检查游戏是否结束
        /// </summary>
        private void CheckGameOver()
        {
            // 统计还在游戏中的玩家
            List<PlayerData> playingPlayers = new List<PlayerData>();
            foreach (var player in _players)
            {
                if (player.state == PlayerState.Playing)
                {
                    playingPlayers.Add(player);
                }
            }
            
            // 只剩一个玩家，游戏结束
            if (playingPlayers.Count <= 1)
            {
                if (playingPlayers.Count == 1)
                {
                    // 有一个胜利者
                    PlayerData winner = playingPlayers[0];
                    winner.state = PlayerState.Victory;
                    _winnerId = winner.playerId;
                    
                    Debug.Log($"[GameManager] 玩家 {winner.playerId} ({winner.playerName}) 获得胜利！");
                    
                    OnPlayerVictory?.Invoke(winner.playerId);
                }
                else
                {
                    // 平局（所有人都失败了）
                    _winnerId = -1;
                    Debug.Log("[GameManager] 游戏平局！");
                }
                
                // 延迟显示游戏结束
                Invoke(nameof(TriggerGameOver), _gameOverDelay);
            }
        }
        
        /// <summary>
        /// 触发游戏结束
        /// </summary>
        private void TriggerGameOver()
        {
            _currentState = GameState.GameOver;
            
            OnGameOver?.Invoke(_winnerId);
            
            // 显示游戏结束UI
            if (_gameOverUI != null)
            {
                bool isLocalPlayerWinner = (_winnerId == _localPlayerId);
                _gameOverUI.Show(isLocalPlayerWinner, GetPlayer(_winnerId)?.playerName ?? "未知");
            }
            
            Debug.Log("[GameManager] 游戏结束！");
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 获取玩家数据
        /// </summary>
        public PlayerData GetPlayer(int playerId)
        {
            foreach (var player in _players)
            {
                if (player.playerId == playerId)
                {
                    return player;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 获取本地玩家
        /// </summary>
        public PlayerData GetLocalPlayer()
        {
            return GetPlayer(_localPlayerId);
        }
        
        /// <summary>
        /// 设置玩家主基地
        /// </summary>
        public void SetPlayerMainBase(int playerId, Building mainBase)
        {
            PlayerData player = GetPlayer(playerId);
            if (player != null)
            {
                // 取消旧的事件注册
                if (player.mainBase != null)
                {
                    player.mainBase.OnDestroyed -= HandleBuildingDestroyed;
                }
                
                player.mainBase = mainBase;
                
                // 注册新的事件
                if (mainBase != null)
                {
                    mainBase.OnDestroyed += HandleBuildingDestroyed;
                }
            }
        }
        
        /// <summary>
        /// 添加玩家
        /// </summary>
        public void AddPlayer(PlayerData player)
        {
            if (GetPlayer(player.playerId) == null)
            {
                _players.Add(player);
                
                if (player.mainBase != null)
                {
                    player.mainBase.OnDestroyed += HandleBuildingDestroyed;
                }
            }
        }
        
        /// <summary>
        /// 格式化游戏时间
        /// </summary>
        public string GetFormattedGameTime()
        {
            int minutes = Mathf.FloorToInt(_gameTime / 60);
            int seconds = Mathf.FloorToInt(_gameTime % 60);
            return $"{minutes:00}:{seconds:00}";
        }
        
        #endregion
    }
}
