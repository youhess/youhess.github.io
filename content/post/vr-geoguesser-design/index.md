
---
title: vrchat geoguesser china
description: 一款基于 VRChat 平台开发的“地理猜图”游戏，完整复现了多人同步、地图定位、得分计算等系统功能，记录其设计与实现过程。
slug: vr-geoguesser-design
date: 2025-05-27T00:00:00+0000
image: GeoGuesser.jpg
categories:
  - Game Design
tags:
  - VRChat
  - UdonSharp
  - 地理游戏
  - 多人同步
weight: 3
---

### 前言

这是我开发的一款“VRChat 地理猜图”游戏，玩家需要根据全景图像猜测其地理位置，并将自己的 Pin 拖拽到地图上。项目灵感来源于 Geoguessr，但加入了中文地图、得分系统等自定义设计。我的朋友Oikki对我的游戏开发提供了极为重要的帮助，在这里再次感谢！

### 游戏机制简述

玩家操作：拖动属于自己的 Pin 到地图上进行定位

核心流程：展示全景图 → 玩家放置位置 → 答案揭晓 → 计算得分

胜利规则：在多轮游戏后总得分最高者胜出

### 技术实现

#### 坐标映射系统

使用 LatLongMapper 将地图 UI 坐标与实际经纬度进行双向映射

```jsx
Vector2 LatLongToUICoords(Vector2 latLong); // 经纬度 → UI坐标  
Vector2 UICoordsToLatLong(Vector2 position); // UI坐标 → 经纬度
```

#### 玩家 Pin 控制

每位玩家使用 Cyan.PlayerObjectPool 分配一个 Pin（GameObject）

使用 Networking.GetOwner() 限定拾取权限

仅本地玩家可见自己的 Pin，非 owner 设置为透明状态

放置检测通过与 MapTable 碰撞触发逻辑判断是否在地图范围内PinController

#### 数据管理系统

PinDataManager 使用 UdonSynced + VRCJson 实现跨客户端同步玩家的经纬度数据

每轮玩家猜测被存储为 DataList，每个回合自动序列化/反序列化数据PinDataManager

最终得分通过与正确坐标的欧几里得距离计算出，采用非线性函数平滑处理分数

#### 地图与答案

正确答案存储在 LocationRoundData.cs 中，预先导入 json 文件

每轮显示的全景图与答案位置通过 imageUrls[] 和 locationDataList[] 对应LocationRoundData

#### 得分逻辑

```jsx
float distance = Vector2.Distance(correct, guess);
float score = distance < 1f ? 95~100 : Mathf.Max(0, 95 * (1 - pow(normalized, 2)));
```

放置不合法（未命中地图）得分为 0

支持对 每轮成绩 和 最终总分 进行排序展示

#### 多人同步与网络优化

房主负责开始游戏并推进阶段（准备 / 猜测 / 揭晓）

OnDeserialization 中 UI 状态恢复 + 图片同步处理

RequestSerialization + SendCustomNetworkEvent 用于状态广播与视图更新

#### 音效交互

不同阶段播放不同 AudioClip（按钮音、倒计时音、揭晓音）
