---
title: UdonSharp 同步方法指南
description: VRCJson 是 VRChat SDK 提供的 JSON 序列化工具，支持在 UdonSharp 中进行数据序列化和反序列化。
slug: vr-wodi-design
date: 2025-05-07T00:00:00+0000
image: cover.jpg
categories:
  - Udonsharp
tags:
  - VRChat
weight: 3
---

# UdonSharp 同步方法指南

## 同步变量

### 1. 同步模式设置
在类定义上方添加同步模式属性：
```
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]  // 手动同步
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]  // 连续同步
```

### 2. 可同步变量标记
使用 `[UdonSynced]` 属性标记需要同步的变量：
```
[UdonSynced] private int myValue;
[UdonSynced] private string myText;
[UdonSynced] private bool myState;
```

### 3. 复杂数据类型同步
对于复杂数据类型，需要序列化成字符串：
```
[UdonSynced] private string serializedData;  // 存储序列化后的JSON
private DataList myDataList;  // 实际使用的数据对象
```

## 同步方法

### 1. 请求同步
修改变量后，调用请求同步方法将数据发送给所有玩家：
```
RequestSerialization();
```

### 2. 监听反序列化
当接收到同步数据时，通过此方法处理：
```
public override void OnDeserialization()
{
    // 处理同步后的数据
}
```

### 3. 网络事件
向特定目标或所有玩家发送事件：
```
// 发送给所有玩家
SendCustomNetworkEvent(NetworkEventTarget.All, "方法名");

// 发送给除自己外的所有玩家
SendCustomNetworkEvent(NetworkEventTarget.Others, "方法名");
```

### 4. 延迟网络事件
设置延迟执行的事件：
```
SendCustomEventDelayedSeconds("方法名", 延迟秒数);
```

## 所有权控制

### 1. 检查所有权
验证当前玩家是否拥有对象所有权：
```
if (Networking.IsOwner(gameObject)) {
    // 只有拥有所有权的玩家才执行
}
```

### 2. 获取/设置所有权
```
// 获取当前所有者
VRCPlayerApi owner = Networking.GetOwner(gameObject);

// 设置所有权（仅当前玩家能设置自己为所有者）
Networking.SetOwner(Networking.LocalPlayer, gameObject);
```

### 3. 所有权变更监听
监听所有权转移事件：
```
public override void OnOwnershipTransferred(VRCPlayerApi player)
{
    // 处理所有权变更逻辑
}
```

## 数据同步最佳实践

1. **所有权检查**：修改同步变量前验证所有权
2. **批量更新**：一次性修改多个变量后再调用一次 RequestSerialization()
3. **数据验证**：OnDeserialization 中验证数据有效性
4. **同步优化**：使用 Manual 模式减少不必要的网络流量
5. **错误处理**：添加异常情况处理和数据恢复机制

通过这些方法，可以实现 UdonSharp 中的高效数据同步，保证多玩家环境下的一致性体验。



## 我的问题

类型转换问题：序列化后 Int 可能变成 Double
没有原生整数类型：JSON 中所有数字都是浮点数