// ============================================================
// StatsManager.cs
// 游戏统计管理器 - 记录本局数据
// ============================================================

using System;
using UnityEngine;

namespace RTS.Managers
{
    /// <summary>
    /// 游戏统计数据
    /// </summary>
    [Serializable]
    public class GameStats
    {
        public int UnitsCreated;
        public int UnitsLost;
        public int EnemiesKilled;
        public int BuildingsConstructed;
        public int BuildingsDestroyed;
        public int ResourcesGathered;
        public int GoldGathered;
        public int WoodGathered;
        public int FoodGathered;
        public float MatchDuration;
        public int TechsResearched;
        
        public void Reset()
        {
            UnitsCreated = 0;
            UnitsLost = 0;
            EnemiesKilled = 0;
            BuildingsConstructed = 0;
            BuildingsDestroyed = 0;
            ResourcesGathered = 0;
            GoldGathered = 0;
            WoodGathered = 0;
            FoodGathered = 0;
            MatchDuration = 0f;
            TechsResearched = 0;
        }
    }

    /// <summary>
    /// 游戏统计管理器（单例模式）
    /// </summary>
    public class StatsManager : MonoBehaviour
    {
        #region 单例
        
        private static StatsManager _instance;
        public static StatsManager Instance => _instance;
        
        #endregion

        #region 事件
        
        public event Action<GameStats> OnStatsUpdated;
        
        #endregion

        #region 私有字段
        
        private GameStats _currentStats = new GameStats();
        private float _matchStartTime;
        private bool _isMatchActive = false;
        
        #endregion

        #region 属性
        
        public GameStats CurrentStats => _currentStats;
        public bool IsMatchActive => _isMatchActive;
        
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
        
        private void Update()
        {
            if (_isMatchActive)
            {
                _currentStats.MatchDuration = Time.time - _matchStartTime;
            }
        }
        
        #endregion

        #region 比赛控制
        
        /// <summary>
        /// 开始新比赛
        /// </summary>
        public void StartMatch()
        {
            _currentStats.Reset();
            _matchStartTime = Time.time;
            _isMatchActive = true;
            
            Debug.Log("[StatsManager] 比赛开始");
        }
        
        /// <summary>
        /// 结束比赛
        /// </summary>
        public void EndMatch()
        {
            _isMatchActive = false;
            _currentStats.MatchDuration = Time.time - _matchStartTime;
            
            Debug.Log($"[StatsManager] 比赛结束 - 时长: {FormatDuration(_currentStats.MatchDuration)}");
        }
        
        #endregion

        #region 统计记录
        
        public void RecordUnitCreated()
        {
            _currentStats.UnitsCreated++;
            OnStatsUpdated?.Invoke(_currentStats);
        }
        
        public void RecordUnitLost()
        {
            _currentStats.UnitsLost++;
            OnStatsUpdated?.Invoke(_currentStats);
        }
        
        public void RecordEnemyKilled()
        {
            _currentStats.EnemiesKilled++;
            OnStatsUpdated?.Invoke(_currentStats);
        }
        
        public void RecordBuildingConstructed()
        {
            _currentStats.BuildingsConstructed++;
            OnStatsUpdated?.Invoke(_currentStats);
        }
        
        public void RecordBuildingDestroyed()
        {
            _currentStats.BuildingsDestroyed++;
            OnStatsUpdated?.Invoke(_currentStats);
        }
        
        public void RecordResourceGathered(RTS.Core.ResourceType type, int amount)
        {
            _currentStats.ResourcesGathered += amount;
            
            switch (type)
            {
                case RTS.Core.ResourceType.Gold:
                    _currentStats.GoldGathered += amount;
                    break;
                case RTS.Core.ResourceType.Wood:
                    _currentStats.WoodGathered += amount;
                    break;
                case RTS.Core.ResourceType.Food:
                    _currentStats.FoodGathered += amount;
                    break;
            }
            
            OnStatsUpdated?.Invoke(_currentStats);
        }
        
        public void RecordTechResearched()
        {
            _currentStats.TechsResearched++;
            OnStatsUpdated?.Invoke(_currentStats);
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 格式化时长显示
        /// </summary>
        public static string FormatDuration(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins:00}:{secs:00}";
        }
        
        /// <summary>
        /// 获取统计摘要
        /// </summary>
        public string GetStatsSummary()
        {
            return $"时长: {FormatDuration(_currentStats.MatchDuration)}\n" +
                   $"单位创建: {_currentStats.UnitsCreated}\n" +
                   $"击杀敌人: {_currentStats.EnemiesKilled}\n" +
                   $"资源采集: {_currentStats.ResourcesGathered}";
        }
        
        #endregion
    }
}
