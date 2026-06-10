---
title: "events"
slug: "projects"
layout: "projects"
comments: false
menu:
  main:
    weight: 6
    params:
      icon: briefcase
intro:
  eyebrow: "EXPERIENCE"
  description: ""
projects:
  - title: "2026年杭州市人工智能训练师（数据标注员）职业技能竞赛"
    type: "一等奖"
    period: "2026.05"
    icon: "analytics"
    summary: "参加2026年杭州市人工智能训练师（数据标注员）职业技能竞赛并获得一等奖，获杭州市总工会认定为“杭州市职工经济技术创新能手”。"
    media:
      image: "/images/experience/hangzhou-ai-trainer-competition-2026.jpeg"
      alt: "2026年杭州市人工智能训练师（数据标注员）职业技能竞赛参赛人员合影"
      caption: "媒体引用：潮新闻《2026年杭州市人工智能训练师职业技能竞赛圆满落幕》"
      source: "https://tidenews.com.cn/news.html?id=3457724"
    stack:
      - "人工智能训练"
      - "数据采集与清洗"
      - "数据标注"
      - "数据库"
      - "机器学习"
      - "图像与视频处理"
      - "模型部署推理"
      - "数据分析"
  - title: "工业温控设备蓝牙控制App"
    type: "软件开发"
    period: "2025.10 - 至今"
    icon: "mobile"
    summary: "面向工业温控设备的移动端运维 App，支持 BLE 蓝牙连接、实时数据读取、参数配置、固件升级与历史文件导出。项目基于 uni-app、Vue2 与 uView 开发，通过 BLE 与设备进行 Modbus RTU 协议通信，满足无网络环境下的设备调试与维护需求。"
    responsibility:
      title: "个人职责"
      headline: "核心页面开发、蓝牙通信封装、Modbus 数据读写、多型号配置适配与 OTA 升级"
      items:
        - "负责离线设备详情模块开发，实现实时数据、ECO 设置、参数设置、固件升级、历史文件导出等核心页面与交互流程"
        - "封装蓝牙扫描、连接、断线重连、消息监听与异常提示等通用能力，提升弱连接场景下的稳定性"
        - "基于 Modbus RTU 协议实现寄存器数据读取、解析与参数下发，支持数据格式转换、枚举翻译、读写权限与范围校验"
        - "设计多型号温控器 Profile 配置方案，对参数组、变量配置、单位与国际化文案进行结构化管理，降低新型号扩展成本"
        - "实现参数配置导入/导出能力，支持批量读取设备参数、生成配置文件、导入后批量下发，并提供进度展示、失败统计与异常反馈"
        - "参与固件升级流程开发，结合文件下载、二进制读取与 XModem 分包传输，实现移动端 OTA 升级"
        - "接入 vue-i18n 多语言体系，支持中文、英文、西班牙文等语言切换，适配海外设备运维场景"
    outcome:
      title: "项目亮点"
      headline: "离线通信稳定、多型号配置灵活、批量参数操作可靠"
      items:
        - "模块化封装 BLE + Modbus 通信链路，将蓝牙收发、CRC 校验、功能码解析、寄存器读写等逻辑沉淀为可复用能力"
        - "通过设备型号 Profile 配置机制，将参数组、变量类型、读写权限、枚举翻译与单位配置化，减少硬编码"
        - "针对批量参数导入增加预校验、依赖排序、失败记录与进度反馈，降低异常参数直接下发导致的配置风险"
        - "完善蓝牙权限、连接超时、设备未发现、断连重连等异常处理，提升现场调试与维护体验"
    stack:
      - "uni-app"
      - "Vue2"
      - "Vuex"
      - "uView"
      - "BLE"
      - "Modbus"
      - "SQLite"
      - "vue-i18n"
closing: ""
---
