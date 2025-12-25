// ============================================================
// ResourceManager.cs
// 资源管理器 - 管理所有玩家的资源
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Core;

namespace RTS.Managers
{
    /// <summary>
    /// 资源管理器（单例模式）
    /// 负责管理所有玩家的资源存储、消耗和查询
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        #region 单例模式
        
        private static ResourceManager _instance;
        
        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ResourceManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ResourceManager");
                        _instance = go.AddComponent<ResourceManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 资源变化事件，参数：玩家ID，资源类型，变化量，当前总量
        /// </summary>
        public event Action<int, ResourceType, int, int> OnResourceChanged;
        
        #endregion

        #region 私有字段
        
        // 玩家资源存储：playerId -> (ResourceType -> amount)
        private Dictionary<int, Dictionary<ResourceType, int>> _playerResources 
            = new Dictionary<int, Dictionary<ResourceType, int>>();
        
        // 初始资源配置
        [Header("初始资源配置")]
        [SerializeField] private int _initialGold = 500;
        [SerializeField] private int _initialWood = 200;
        [SerializeField] private int _initialFood = 100;
        
        #endregion

        #region Unity 生命周期
        
        private void Awake()
        {
            // 确保单例唯一性
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        #endregion

        #region 公共方法
        
        /// <summary>
        /// 初始化玩家资源（游戏开始时调用）
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        public void InitializePlayer(int playerId)
        {
            if (_playerResources.ContainsKey(playerId))
            {
                Debug.LogWarning($"玩家 {playerId} 已经初始化过资源！");
                return;
            }
            
            var resources = new Dictionary<ResourceType, int>
            {
                { ResourceType.Gold, _initialGold },
                { ResourceType.Wood, _initialWood },
                { ResourceType.Food, _initialFood }
            };
            
            _playerResources[playerId] = resources;
            Debug.Log($"玩家 {playerId} 资源已初始化：金币={_initialGold}, 木材={_initialWood}, 粮食={_initialFood}");
        }
        
        /// <summary>
        /// 添加资源
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="type">资源类型</param>
        /// <param name="amount">数量（必须为正数）</param>
        /// <returns>是否成功</returns>
        public bool AddResource(int playerId, ResourceType type, int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"添加资源数量必须为正数！当前值: {amount}");
                return false;
            }
            
            if (!EnsurePlayerExists(playerId))
            {
                return false;
            }
            
            _playerResources[playerId][type] += amount;
            int currentAmount = _playerResources[playerId][type];
            
            // 触发资源变化事件
            OnResourceChanged?.Invoke(playerId, type, amount, currentAmount);
            
            Debug.Log($"玩家 {playerId} 获得 {amount} {GetResourceName(type)}，当前: {currentAmount}");
            return true;
        }
        
        /// <summary>
        /// 检查玩家是否能负担指定的资源消耗
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="cost">资源消耗字典</param>
        /// <returns>是否能负担</returns>
        public bool CanAfford(int playerId, Dictionary<ResourceType, int> cost)
        {
            if (cost == null || cost.Count == 0)
            {
                return true;
            }
            
            if (!_playerResources.ContainsKey(playerId))
            {
                Debug.LogWarning($"玩家 {playerId} 不存在！");
                return false;
            }
            
            foreach (var kvp in cost)
            {
                if (GetResource(playerId, kvp.Key) < kvp.Value)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 消耗资源（建造单位/建筑时调用）
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="cost">资源消耗字典</param>
        /// <returns>是否成功消耗</returns>
        public bool SpendResource(int playerId, Dictionary<ResourceType, int> cost)
        {
            if (!CanAfford(playerId, cost))
            {
                Debug.LogWarning($"玩家 {playerId} 资源不足！");
                return false;
            }
            
            // 扣除资源
            foreach (var kvp in cost)
            {
                _playerResources[playerId][kvp.Key] -= kvp.Value;
                int currentAmount = _playerResources[playerId][kvp.Key];
                
                // 触发资源变化事件（负数表示消耗）
                OnResourceChanged?.Invoke(playerId, kvp.Key, -kvp.Value, currentAmount);
                
                Debug.Log($"玩家 {playerId} 消耗 {kvp.Value} {GetResourceName(kvp.Key)}，剩余: {currentAmount}");
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取玩家指定类型的资源数量
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="type">资源类型</param>
        /// <returns>资源数量</returns>
        public int GetResource(int playerId, ResourceType type)
        {
            if (!_playerResources.ContainsKey(playerId))
            {
                Debug.LogWarning($"玩家 {playerId} 不存在！");
                return 0;
            }
            
            return _playerResources[playerId].TryGetValue(type, out int amount) ? amount : 0;
        }
        
        /// <summary>
        /// 获取玩家所有资源
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>资源字典的只读副本</returns>
        public Dictionary<ResourceType, int> GetAllResources(int playerId)
        {
            if (!_playerResources.ContainsKey(playerId))
            {
                Debug.LogWarning($"玩家 {playerId} 不存在！");
                return new Dictionary<ResourceType, int>();
            }
            
            // 返回副本以防止外部修改
            return new Dictionary<ResourceType, int>(_playerResources[playerId]);
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 确保玩家存在，如果不存在则自动初始化
        /// </summary>
        private bool EnsurePlayerExists(int playerId)
        {
            if (!_playerResources.ContainsKey(playerId))
            {
                Debug.LogWarning($"玩家 {playerId} 不存在，自动初始化...");
                InitializePlayer(playerId);
            }
            return true;
        }
        
        /// <summary>
        /// 获取资源的中文名称
        /// </summary>
        private string GetResourceName(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Gold: return "金币";
                case ResourceType.Wood: return "木材";
                case ResourceType.Food: return "粮食";
                default: return type.ToString();
            }
        }
        
        #endregion
    }
}
