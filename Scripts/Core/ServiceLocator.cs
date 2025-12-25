// ============================================================
// ServiceLocator.cs - 简易服务定位器
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Core
{
    /// <summary>
    /// 简易服务定位器
    /// 用于在各层之间共享服务实例
    /// 可替换为 Zenject / VContainer 等 DI 框架
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        
        /// <summary>
        /// 注册服务
        /// </summary>
        public static void Register<T>(T service)
        {
            var type = typeof(T);
            
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] 服务已存在，将被覆盖: {type.Name}");
            }
            
            _services[type] = service;
        }
        
        /// <summary>
        /// 获取服务
        /// </summary>
        public static T Get<T>()
        {
            var type = typeof(T);
            
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            Debug.LogError($"[ServiceLocator] 服务未注册: {type.Name}");
            return default;
        }
        
        /// <summary>
        /// 尝试获取服务
        /// </summary>
        public static bool TryGet<T>(out T service)
        {
            var type = typeof(T);
            
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }
            
            service = default;
            return false;
        }
        
        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public static bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// 注销服务
        /// </summary>
        public static void Unregister<T>()
        {
            _services.Remove(typeof(T));
        }
        
        /// <summary>
        /// 清空所有服务（用于场景切换时）
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
