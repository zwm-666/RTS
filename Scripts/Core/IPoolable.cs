// ============================================================
// IPoolable.cs
// 可池化对象接口
// ============================================================

namespace RTS.Core
{
    /// <summary>
    /// 可池化对象接口
    /// 实现此接口的对象可以被 ObjectPoolManager 管理
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从对象池中取出时调用
        /// 用于重置对象状态
        /// </summary>
        void OnSpawn();
        
        /// <summary>
        /// 放回对象池时调用
        /// 用于清理状态和停止协程
        /// </summary>
        void OnDespawn();
    }
}
