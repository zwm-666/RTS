// ============================================================
// ResourceSystemDemo.cs
// 资源系统演示脚本
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using RTS.Core;
using RTS.Managers;

namespace RTS.Demo
{
    /// <summary>
    /// 资源系统演示脚本
    /// 用于测试 ResourceManager 的各项功能
    /// </summary>
    public class ResourceSystemDemo : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private int _testPlayerId = 0;
        
        private void Start()
        {
            // 初始化玩家资源
            ResourceManager.Instance.InitializePlayer(_testPlayerId);
            
            // 订阅资源变化事件
            ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
            
            // 运行测试
            RunTests();
        }
        
        private void OnDestroy()
        {
            // 取消订阅
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
            }
        }
        
        /// <summary>
        /// 运行测试用例
        /// </summary>
        private void RunTests()
        {
            Debug.Log("========== 资源系统测试开始 ==========");
            
            // 测试1：添加资源
            Debug.Log("\n--- 测试1：添加资源 ---");
            ResourceManager.Instance.AddResource(_testPlayerId, ResourceType.Gold, 100);
            ResourceManager.Instance.AddResource(_testPlayerId, ResourceType.Wood, 50);
            
            // 测试2：查询资源
            Debug.Log("\n--- 测试2：查询资源 ---");
            int gold = ResourceManager.Instance.GetResource(_testPlayerId, ResourceType.Gold);
            int wood = ResourceManager.Instance.GetResource(_testPlayerId, ResourceType.Wood);
            int food = ResourceManager.Instance.GetResource(_testPlayerId, ResourceType.Food);
            Debug.Log($"当前资源 - 金币: {gold}, 木材: {wood}, 粮食: {food}");
            
            // 测试3：检查是否能负担
            Debug.Log("\n--- 测试3：检查能否负担 ---");
            var buildingCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Gold, 200 },
                { ResourceType.Wood, 100 }
            };
            
            bool canAfford = ResourceManager.Instance.CanAfford(_testPlayerId, buildingCost);
            Debug.Log($"能否负担建筑（需要200金币+100木材）: {canAfford}");
            
            // 测试4：消耗资源
            Debug.Log("\n--- 测试4：消耗资源 ---");
            var unitCost = new Dictionary<ResourceType, int>
            {
                { ResourceType.Gold, 50 },
                { ResourceType.Food, 20 }
            };
            
            bool success = ResourceManager.Instance.SpendResource(_testPlayerId, unitCost);
            Debug.Log($"消耗资源（50金币+20粮食）: {(success ? "成功" : "失败")}");
            
            // 测试5：再次查询
            Debug.Log("\n--- 测试5：消耗后查询 ---");
            var allResources = ResourceManager.Instance.GetAllResources(_testPlayerId);
            foreach (var kvp in allResources)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
            
            Debug.Log("\n========== 资源系统测试完成 ==========");
        }
        
        /// <summary>
        /// 处理资源变化事件
        /// </summary>
        private void HandleResourceChanged(int playerId, ResourceType type, int delta, int current)
        {
            string action = delta > 0 ? "增加" : "减少";
            Debug.Log($"[事件] 玩家{playerId} {type} {action} {Mathf.Abs(delta)}，当前: {current}");
        }
        
        /// <summary>
        /// 按键测试（运行时按键触发）
        /// </summary>
        private void Update()
        {
            // 按 G 键添加金币
            if (Input.GetKeyDown(KeyCode.G))
            {
                ResourceManager.Instance.AddResource(_testPlayerId, ResourceType.Gold, 50);
            }
            
            // 按 W 键添加木材
            if (Input.GetKeyDown(KeyCode.W))
            {
                ResourceManager.Instance.AddResource(_testPlayerId, ResourceType.Wood, 30);
            }
            
            // 按 F 键添加粮食
            if (Input.GetKeyDown(KeyCode.F))
            {
                ResourceManager.Instance.AddResource(_testPlayerId, ResourceType.Food, 20);
            }
        }
    }
}
