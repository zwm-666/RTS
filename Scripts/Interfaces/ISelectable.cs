// ============================================================
// ISelectable.cs
// 可选择接口 - 所有可被选中的对象需要实现此接口
// ============================================================

using UnityEngine;

namespace RTS.Interfaces
{
    /// <summary>
    /// 可选择接口
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// 所属玩家ID
        /// </summary>
        int PlayerId { get; }
        
        /// <summary>
        /// 是否存活/有效
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// 对象的 Transform
        /// </summary>
        Transform Transform { get; }
        
        /// <summary>
        /// 被选中时调用
        /// </summary>
        void OnSelected();
        
        /// <summary>
        /// 取消选中时调用
        /// </summary>
        void OnDeselected();
    }
    
    /// <summary>
    /// 可命令接口 - 可以接收移动/攻击命令的对象
    /// </summary>
    public interface ICommandable : ISelectable
    {
        /// <summary>
        /// 移动到目标位置
        /// </summary>
        /// <param name="destination">目标世界坐标</param>
        /// <returns>是否成功开始移动</returns>
        bool MoveTo(Vector3 destination);
        
        /// <summary>
        /// 停止当前动作
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 攻击目标
        /// </summary>
        /// <param name="target">攻击目标</param>
        void AttackTarget(ISelectable target);
    }
}
