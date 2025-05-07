---
title: UdonSharp 序列化方案简要指南
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

## 核心知识点

### 1. VRCJson 基础
VRCJson 是 VRChat SDK 提供的 JSON 序列化工具，支持在 UdonSharp 中进行数据序列化和反序列化。

### 2. 基本类型注意事项
- **类型转换问题**：序列化后 Int 可能变成 Double
- **没有原生整数类型**：JSON 中所有数字都是浮点数

### 3. 数据类型
主要使用的数据类型：
- DataList：类似数组
- DataDictionary：类似字典
- DataToken：通用数据容器

## 实用序列化方案

### 方案一：灵活类型读取
在数据反序列化时不指定特定类型，而是根据实际类型进行转换：

```
// 读取数据
if (dataDict.TryGetValue("key", out DataToken token)) {
    // 根据实际类型处理
    if (token.TokenType == TokenType.Int) { ... }
    else if (token.TokenType == TokenType.Double) { ... }
    else if (token.TokenType == TokenType.String) { ... }
}
```

### 方案二：使用字符串作为中间类型
所有数值类型都转换为字符串再序列化，读取时再转回：

```
// 写入时
dict.SetValue("id", playerId.ToString());

// 读取时
int value = int.Parse(token.String);
```

### 方案三：类型统一策略
全部使用 Double 类型存储数值：

```
// 写入时
dict.SetValue("id", (double)playerId);

// 读取时
int id = (int)token.Double;
```

## 最佳实践

1. **验证序列化结果**：序列化后立即反序列化测试
2. **避免固定类型检查**：使用 `TryGetValue(key, out token)` 而非 `TryGetValue(key, TokenType.Int, out token)`
3. **统一类型策略**：项目中对同类数据采用一致的类型处理方式
4. **添加类型检查日志**：在关键处理点记录实际类型
5. **实现数据清理机制**：关键状态变更点执行完整数据重置

通过采用以上方案，可以有效避免 UdonSharp 序列化中的类型转换问题，保证数据一致性。