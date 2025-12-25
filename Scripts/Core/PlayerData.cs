// ============================================================
// PlayerData.cs
// 玩家数据结构
// ============================================================

using UnityEngine;
using RTS.Buildings;

namespace RTS.Core
{
    /// <summary>
    /// 玩家状态
    /// </summary>
    public enum PlayerState
    {
        Playing,    // 游戏中
        Victory,    // 胜利
        Defeated    // 失败
    }

    /// <summary>
    /// 玩家数据
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        [Header("基础信息")]
        public int playerId;
        public string playerName;
        public Color playerColor = Color.blue;
        public bool isLocalPlayer = false;
        public bool isAI = false;
        
        [Header("主基地")]
        public Building mainBase;
        
        [Header("状态")]
        public PlayerState state = PlayerState.Playing;
        
        /// <summary>
        /// 玩家是否还在游戏中
        /// </summary>
        public bool IsPlaying => state == PlayerState.Playing;
        
        /// <summary>
        /// 主基地是否存活
        /// </summary>
        public bool IsMainBaseAlive => mainBase != null && mainBase.IsAlive;
    }
}
