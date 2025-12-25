// ============================================================
// ObjectPoolManager.cs
// 通用对象池管理器
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace RTS.Core
{
    /// <summary>
    /// 对象池管理器（单例模式）
    /// 使用 Prefab 作为 Key 管理对象池
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        #region 单例
        
        private static ObjectPoolManager _instance;
        public static ObjectPoolManager Instance => _instance;
        
        #endregion

        #region 配置
        
        [Header("配置")]
        [SerializeField] private int _defaultPreloadCount = 5;
        [SerializeField] private bool _logPoolActivity = false;
        
        #endregion

        #region 私有字段
        
        // Prefab -> 可用对象队列
        private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
        
        // 实例 -> Prefab 映射（用于回收时查找）
        private Dictionary<GameObject, GameObject> _instanceToPrefab = new Dictionary<GameObject, GameObject>();
        
        // 每个 Prefab 的容器对象
        private Dictionary<GameObject, Transform> _poolContainers = new Dictionary<GameObject, Transform>();
        
        // 根容器
        private Transform _poolRoot;
        
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
            
            // 创建根容器
            _poolRoot = new GameObject("--- OBJECT POOLS ---").transform;
            _poolRoot.SetParent(transform);
        }
        
        #endregion

        #region 核心方法
        
        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ObjectPool] Spawn: prefab 为空");
                return null;
            }
            
            // 确保该 Prefab 有对应的池
            EnsurePoolExists(prefab);
            
            GameObject instance;
            Queue<GameObject> pool = _pools[prefab];
            
            if (pool.Count > 0)
            {
                // 从池中取出
                instance = pool.Dequeue();
                
                // 确保对象未被销毁
                if (instance == null)
                {
                    // 对象已被销毁，创建新的
                    instance = CreateNewInstance(prefab);
                }
                else
                {
                    instance.SetActive(true);
                }
            }
            else
            {
                // 池为空，创建新实例
                instance = CreateNewInstance(prefab);
            }
            
            // 设置变换
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            
            if (parent != null)
            {
                instance.transform.SetParent(parent);
            }
            
            // 调用 IPoolable 回调
            IPoolable poolable = instance.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawn();
            }
            
            if (_logPoolActivity)
            {
                Debug.Log($"[ObjectPool] Spawn: {prefab.name} (池剩余: {pool.Count})");
            }
            
            return instance;
        }
        
        /// <summary>
        /// 从对象池获取对象（简化版）
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position)
        {
            return Spawn(prefab, position, Quaternion.identity, null);
        }
        
        /// <summary>
        /// 从对象池获取对象（带泛型组件）
        /// </summary>
        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject instance = Spawn(prefab, position, rotation, null);
            return instance != null ? instance.GetComponent<T>() : null;
        }
        
        /// <summary>
        /// 回收对象到池中
        /// </summary>
        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            
            // 查找对应的 Prefab
            if (!_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
            {
                // 未找到对应的 Prefab，直接销毁
                if (_logPoolActivity)
                {
                    Debug.LogWarning($"[ObjectPool] Despawn: {instance.name} 不是从对象池创建的，直接销毁");
                }
                Destroy(instance);
                return;
            }
            
            // 调用 IPoolable 回调
            IPoolable poolable = instance.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnDespawn();
            }
            
            // 禁用并放回池中
            instance.SetActive(false);
            instance.transform.SetParent(GetPoolContainer(prefab));
            
            _pools[prefab].Enqueue(instance);
            
            if (_logPoolActivity)
            {
                Debug.Log($"[ObjectPool] Despawn: {prefab.name} (池大小: {_pools[prefab].Count})");
            }
        }
        
        /// <summary>
        /// 延迟回收
        /// </summary>
        public void DespawnDelayed(GameObject instance, float delay)
        {
            if (instance == null) return;
            StartCoroutine(DespawnDelayedCoroutine(instance, delay));
        }
        
        private System.Collections.IEnumerator DespawnDelayedCoroutine(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn(instance);
        }
        
        #endregion

        #region 预加载
        
        /// <summary>
        /// 预加载对象到池中
        /// </summary>
        public void Preload(GameObject prefab, int count)
        {
            if (prefab == null) return;
            
            EnsurePoolExists(prefab);
            
            Queue<GameObject> pool = _pools[prefab];
            Transform container = GetPoolContainer(prefab);
            
            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateNewInstance(prefab);
                instance.SetActive(false);
                instance.transform.SetParent(container);
                pool.Enqueue(instance);
            }
            
            Debug.Log($"[ObjectPool] 预加载 {count} 个 {prefab.name} (池大小: {pool.Count})");
        }
        
        /// <summary>
        /// 预热池（如果池为空或小于指定数量）
        /// </summary>
        public void Warmup(GameObject prefab, int minCount)
        {
            if (prefab == null) return;
            
            EnsurePoolExists(prefab);
            
            int currentCount = _pools[prefab].Count;
            if (currentCount < minCount)
            {
                Preload(prefab, minCount - currentCount);
            }
        }
        
        #endregion

        #region 池管理
        
        /// <summary>
        /// 清空指定 Prefab 的池
        /// </summary>
        public void ClearPool(GameObject prefab)
        {
            if (prefab == null || !_pools.ContainsKey(prefab)) return;
            
            Queue<GameObject> pool = _pools[prefab];
            while (pool.Count > 0)
            {
                GameObject instance = pool.Dequeue();
                if (instance != null)
                {
                    _instanceToPrefab.Remove(instance);
                    Destroy(instance);
                }
            }
            
            Debug.Log($"[ObjectPool] 清空池: {prefab.name}");
        }
        
        /// <summary>
        /// 清空所有池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var prefab in new List<GameObject>(_pools.Keys))
            {
                ClearPool(prefab);
            }
            
            _pools.Clear();
            _instanceToPrefab.Clear();
            
            Debug.Log("[ObjectPool] 已清空所有池");
        }
        
        /// <summary>
        /// 获取池的当前大小
        /// </summary>
        public int GetPoolSize(GameObject prefab)
        {
            if (prefab == null || !_pools.ContainsKey(prefab)) return 0;
            return _pools[prefab].Count;
        }
        
        #endregion

        #region 辅助方法
        
        private void EnsurePoolExists(GameObject prefab)
        {
            if (!_pools.ContainsKey(prefab))
            {
                _pools[prefab] = new Queue<GameObject>();
            }
        }
        
        private GameObject CreateNewInstance(GameObject prefab)
        {
            GameObject instance = Instantiate(prefab);
            instance.name = prefab.name; // 移除 "(Clone)" 后缀
            _instanceToPrefab[instance] = prefab;
            return instance;
        }
        
        private Transform GetPoolContainer(GameObject prefab)
        {
            if (!_poolContainers.TryGetValue(prefab, out Transform container))
            {
                container = new GameObject($"Pool_{prefab.name}").transform;
                container.SetParent(_poolRoot);
                _poolContainers[prefab] = container;
            }
            return container;
        }
        
        #endregion

        #region 调试
        
        /// <summary>
        /// 获取所有池的统计信息
        /// </summary>
        public Dictionary<string, int> GetPoolStats()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();
            foreach (var kvp in _pools)
            {
                stats[kvp.Key.name] = kvp.Value.Count;
            }
            return stats;
        }
        
        #endregion
    }
}
