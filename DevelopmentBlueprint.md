# 《裂星纪元》开发实现蓝图

## 一、核心循环伪代码 (Core Loop)

### 1.1 游戏主循环 (Game Loop)
```csharp
void Update() {
    // 1. 处理输入
    InputManager.HandleTouchEvents();
    
    // 2. 核心逻辑更新 (每帧)
    ResourceManager.UpdateRegeneration(Time.deltaTime); // 资源再生
    UnitManager.UpdateAllUnits(Time.deltaTime);         // 单位行为(移动/攻击)
    ProjectileManager.UpdateProjectiles(Time.deltaTime);// 投射物飞行
    
    // 3. AI 决策 (低频, 例如每0.2秒一次)
    if (Time.frameCount % 12 == 0) {
        AIManager.UpdateDecisions();
    }
    
    // 4. 定时器检查
    GameTimer.CheckWinCondition(); // 检查主基地是否存活
}
```

### 1.2 资源采集逻辑
```csharp
class WorkerUnit : BaseUnit {
    void UpdateState() {
        switch(currentState) {
            case State.MovingToResource:
                if (ArrivedAt(targetResource)) StartGathering();
                break;
            case State.Gathering:
                gatherTimer += Time.deltaTime;
                if (gatherTimer >= gatherTime) {
                    currentLoad += gatherAmount;
                    targetResource.Deplete(gatherAmount);
                    ReturnToBase();
                }
                break;
            case State.Returning:
                if (ArrivedAt(Base)) {
                    GlobalResources.Add(currentLoad);
                    currentLoad = 0;
                    MoveToResource(lastResource);
                }
                break;
        }
    }
}
```

---

## 二、关键算法实现

### 2.1 A* 寻路系统 (Pathfinding)
- **网格系统**：使用 Unity NavMesh 或自定义 Grid 系统。
- **动态避障**：采用 RVO (Reciprocal Velocity Obstacles) 算法，防止大量单位互相推挤。
- **分层寻路**：
  - **L1 全局路径**：A* 搜索大区域节点。
  - **L2 局部避障**：Steering Behaviors (分离/队列/跟随)。

### 2.2 单位 AI 状态机 (FSM)
每个单位拥有独立的状态机：
- **Idle (闲置)**：寻找索敌范围内的敌人 -> 切换至 Chase。
- **Chase (追击)**：移动向目标 -> 目标进入射程 -> 切换至 Attack。
- **Attack (攻击)**：
  - 播放动画 -> 生成伤害/投射物 -> 等待冷却 (Backswing)。
  - 若目标死亡/丢失 -> 切换至 Idle。
  - 若自身血量 < 20% (AI参数) -> 切换至 Flee (撤退)。

### 2.3 战斗平衡公式
- **实际伤害** = `(攻击力 * 攻防克制系数) * (1 - 伤害减免)`
- **伤害减免** = `(护甲值 * 0.05) / (1 + 护甲值 * 0.05)`  
  *(注：经典的魔兽3公式变体，护甲收益递减)*
- **命中判定**：
  - 低地打高地：30% 丢失率 (Miss)。
  - 有[闪避]技能：额外计算独立判定。

---

## 三、美术风格指引 (Art Direction)

### 3.1 总体风格：Stylized PBR (风格化PBR)
- 结合《火炬之光》的夸张轮廓与《星际争霸2》的材质质感。
- **模型面数**：
  - 英雄：3000-5000 面
  - 基础单位：800-1500 面
  - 建筑：1000-2000 面 (LOD技术)

### 3.2 种族视觉特征
- **🔥 星火族**：
  - **配色**：橙红、黑曜石色、流动岩浆光效。
  - **形状**：尖锐、不规则、破碎感。
- **🌑 幽影族**：
  - **配色**：深紫、幽绿、半透明材质。
  - **形状**：流线型、生物有机感、烟雾特效。
- **⚙ 钢铁联军**：
  - **配色**：铁灰、黄铜、工业警示黄。
  - **形状**：块状、厚重机械、齿轮与蒸汽细节。

### 3.3 界面 (UI)
- **扁平化 + 科技感**：半透明磨砂玻璃背景，高亮霓虹边框。
- **操作反馈**：点击地面产生动态波纹，拖拽框选要有明确的高亮边缘。

---

## 四、开发优先级清单 (MVP Roadmap)

### Phase 1: 核心框架 (2周)
| 优先级 | 功能模块 | 描述 |
|--------|----------|------|
| **P0** | 基础移动与控制 | 框选单位，右键移动，摄像机平移/缩放。 |
| **P0** | 资源循环 | 工人采集木/粮，资源数字UI更新。 |
| **P0** | 建造系统 | 选择工人建造[兵营]，兵营生产[基础单位]。 |

### Phase 2: 战斗验证 (2周)
| 优先级 | 功能模块 | 描述 |
|--------|----------|------|
| **P0** | 攻击与伤害 | 单位索敌，播放攻击动作，扣血与死亡。 |
| **P1** | 属性与克制 | 护甲类型判定，伤害公式实装。 |
| **P1** | 战争迷雾 | 简单的迷雾系统，单位视野剔除。 |

### Phase 3: 种族与内容 (4周)
| 优先级 | 功能模块 | 描述 |
|--------|----------|------|
| **P1** | 三大种族差异化 | 实现所有T1/T2单位数据与模型占位。 |
| **P2** | 技能系统 | 英雄技能框架 (AoE/单体/被动)。 |
| **P2** | 简单AI | 电脑敌人会自动暴兵并攻击玩家基地。 |

### Phase 4: 移动端适配与打磨 (2周)
| 优先级 | 功能模块 | 描述 |
|--------|----------|------|
| **P1** | 触控操作 | 虚拟摇杆(可选)或双指手势。 |
| **P2** | 性能优化 | 对象池，合批，降帧处理。 |
| **P2** | UI美化 | 替换所有开发者UI素材。 |
