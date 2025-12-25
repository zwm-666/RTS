// ============================================================
// MarketManager.cs
// 市场管理器 - 资源兑换交易系统
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RTS.Core;
using RTS.Managers;

namespace RTS.Resources
{
    /// <summary>
    /// 交易记录
    /// </summary>
    [Serializable]
    public class TradeRecord
    {
        public int playerId;
        public ResourceType fromType;
        public ResourceType toType;
        public int fromAmount;
        public int toAmount;
        public float timestamp;
    }

    /// <summary>
    /// 市场管理器（单例模式）
    /// 处理资源兑换逻辑
    /// </summary>
    public class MarketManager : MonoBehaviour
    {
        #region 单例
        
        private static MarketManager _instance;
        public static MarketManager Instance => _instance;
        
        #endregion

        #region 事件
        
        /// <summary>
        /// 交易完成事件
        /// </summary>
        public event Action<TradeRecord> OnTradeCompleted;
        
        /// <summary>
        /// 交易失败事件
        /// </summary>
        public event Action<string> OnTradeFailed;
        
        #endregion

        #region 汇率配置
        
        [Header("基础汇率配置")]
        [Tooltip("木材/粮食兑换金币的汇率（100木/粮 = X金）")]
        [SerializeField] private float _resourceToGoldRate = 55f;
        
        [Tooltip("金币兑换木材/粮食的汇率（100金 = X木/粮）")]
        [SerializeField] private float _goldToResourceRate = 125f;
        
        [Tooltip("木材和粮食互换汇率（1:1，无损耗）")]
        [SerializeField] private float _resourceToResourceRate = 100f;
        
        [Header("交易限制")]
        [Tooltip("单次交易最小量")]
        [SerializeField] private int _minTradeAmount = 10;
        
        [Tooltip("单次交易最大量")]
        [SerializeField] private int _maxTradeAmount = 1000;
        
        [Tooltip("交易冷却时间（秒）")]
        [SerializeField] private float _tradeCooldown = 1f;
        
        [Header("调试")]
        [SerializeField] private bool _logTrades = true;
        
        #endregion

        #region 私有字段
        
        private Dictionary<int, float> _lastTradeTime = new Dictionary<int, float>();
        private List<TradeRecord> _tradeHistory = new List<TradeRecord>();
        
        #endregion

        #region 属性
        
        public float ResourceToGoldRate => _resourceToGoldRate;
        public float GoldToResourceRate => _goldToResourceRate;
        public int MinTradeAmount => _minTradeAmount;
        public int MaxTradeAmount => _maxTradeAmount;
        
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
        
        #endregion

        #region 汇率查询
        
        /// <summary>
        /// 获取兑换汇率
        /// </summary>
        /// <param name="fromType">源资源类型</param>
        /// <param name="toType">目标资源类型</param>
        /// <returns>100单位源资源可兑换的目标资源数量</returns>
        public float GetExchangeRate(ResourceType fromType, ResourceType toType)
        {
            if (fromType == toType)
            {
                return 100f; // 同类型不需要兑换
            }
            
            if (fromType == ResourceType.Gold)
            {
                // 金币换木材/粮食
                return _goldToResourceRate;
            }
            else if (toType == ResourceType.Gold)
            {
                // 木材/粮食换金币
                return _resourceToGoldRate;
            }
            else
            {
                // 木材和粮食互换
                return _resourceToResourceRate;
            }
        }
        
        /// <summary>
        /// 预览兑换结果
        /// </summary>
        /// <param name="fromType">源资源类型</param>
        /// <param name="toType">目标资源类型</param>
        /// <param name="amount">源资源数量</param>
        /// <returns>可获得的目标资源数量</returns>
        public int PreviewExchange(ResourceType fromType, ResourceType toType, int amount)
        {
            if (fromType == toType)
            {
                return amount;
            }
            
            float rate = GetExchangeRate(fromType, toType);
            return Mathf.FloorToInt(amount * rate / 100f);
        }
        
        #endregion

        #region 交易执行
        
        /// <summary>
        /// 执行资源兑换
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="fromType">源资源类型</param>
        /// <param name="toType">目标资源类型</param>
        /// <param name="amount">源资源数量</param>
        /// <returns>是否成功</returns>
        public bool ExchangeResource(int playerId, ResourceType fromType, ResourceType toType, int amount)
        {
            // 验证参数
            if (fromType == toType)
            {
                OnTradeFailed?.Invoke("不能兑换相同类型的资源");
                return false;
            }
            
            if (amount < _minTradeAmount)
            {
                OnTradeFailed?.Invoke($"交易数量不能少于 {_minTradeAmount}");
                return false;
            }
            
            if (amount > _maxTradeAmount)
            {
                OnTradeFailed?.Invoke($"交易数量不能超过 {_maxTradeAmount}");
                return false;
            }
            
            // 检查冷却
            if (!CheckCooldown(playerId))
            {
                OnTradeFailed?.Invoke("交易冷却中，请稍后再试");
                return false;
            }
            
            // 检查资源是否足够
            if (ResourceManager.Instance == null)
            {
                OnTradeFailed?.Invoke("资源管理器不可用");
                return false;
            }
            
            int currentAmount = ResourceManager.Instance.GetResource(playerId, fromType);
            if (currentAmount < amount)
            {
                OnTradeFailed?.Invoke($"{GetResourceName(fromType)} 不足");
                return false;
            }
            
            // 计算兑换结果
            int resultAmount = PreviewExchange(fromType, toType, amount);
            
            if (resultAmount <= 0)
            {
                OnTradeFailed?.Invoke("兑换数量太少");
                return false;
            }
            
            // 执行交易
            var cost = new Dictionary<ResourceType, int> { { fromType, amount } };
            if (!ResourceManager.Instance.SpendResource(playerId, cost))
            {
                OnTradeFailed?.Invoke("扣除资源失败");
                return false;
            }
            
            ResourceManager.Instance.AddResource(playerId, toType, resultAmount);
            
            // 记录交易
            var record = new TradeRecord
            {
                playerId = playerId,
                fromType = fromType,
                toType = toType,
                fromAmount = amount,
                toAmount = resultAmount,
                timestamp = Time.time
            };
            
            _tradeHistory.Add(record);
            _lastTradeTime[playerId] = Time.time;
            
            if (_logTrades)
            {
                Debug.Log($"[Market] 玩家{playerId} 交易: {amount} {GetResourceName(fromType)} -> {resultAmount} {GetResourceName(toType)}");
            }
            
            OnTradeCompleted?.Invoke(record);
            
            return true;
        }
        
        /// <summary>
        /// 检查交易冷却
        /// </summary>
        private bool CheckCooldown(int playerId)
        {
            if (_lastTradeTime.TryGetValue(playerId, out float lastTime))
            {
                return (Time.time - lastTime) >= _tradeCooldown;
            }
            return true;
        }
        
        /// <summary>
        /// 获取剩余冷却时间
        /// </summary>
        public float GetRemainingCooldown(int playerId)
        {
            if (_lastTradeTime.TryGetValue(playerId, out float lastTime))
            {
                float remaining = _tradeCooldown - (Time.time - lastTime);
                return Mathf.Max(0, remaining);
            }
            return 0f;
        }
        
        #endregion

        #region 辅助方法
        
        /// <summary>
        /// 获取资源名称
        /// </summary>
        private string GetResourceName(ResourceType type)
        {
            return type switch
            {
                ResourceType.Gold => "金币",
                ResourceType.Wood => "木材",
                ResourceType.Food => "粮食",
                _ => type.ToString()
            };
        }
        
        /// <summary>
        /// 获取交易历史
        /// </summary>
        public List<TradeRecord> GetTradeHistory(int playerId = -1)
        {
            if (playerId < 0)
            {
                return new List<TradeRecord>(_tradeHistory);
            }
            
            return _tradeHistory.FindAll(r => r.playerId == playerId);
        }
        
        /// <summary>
        /// 清空交易历史
        /// </summary>
        public void ClearTradeHistory()
        {
            _tradeHistory.Clear();
        }
        
        #endregion
    }
}
