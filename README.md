# 卡牌裂纹阈值修改器

---

## 简介

在 **Vampire Crawlers** 中，同一张卡牌在一回合内重复使用过多时，卡牌会逐渐出现「裂纹」效果（卡牌受损）。

本模组基于 [BepInEx](https://github.com/BepInEx/BepInEx) 框架开发，让你可以**自由设定**触发裂纹的次数阈值。默认设为 **999**（几乎不触发），你可以按需调低，让游戏更具挑战性或符合个人偏好。

## 功能

- **自定义裂纹触发阈值** — 在 BepInEx 配置文件中修改 `TimesPlayedToStartCracking` 的值，即可控制同一张卡牌在一回合内使用多少次后开始出现裂纹。
  - 默认值：**999**（相当于几乎不会触发）
  - 最小值：**1**（每使用一次就立刻触发）

## 安装

### 前置要求

- 已安装 [BepInEx](https://docs.bepinex.dev/)（IL2CPP 版本）
- 游戏本体：**Vampire Crawlers**

### 步骤

1. 将编译好的 `CardCrackThresholdMod.dll` 放入游戏的 `BepInEx/plugins/` 目录下。
2. 启动游戏，模组将自动加载。

## 配置

模组加载后会自动生成配置文件，路径为：

```
BepInEx/config/com.imoonday.cardcrackthresholdmod.cfg
```

可修改以下选项：

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `TimesPlayedToStartCracking` | 同一张卡牌在一回合内使用多少次后开始出现裂纹。数值越大越难触发，设为 1 则每用一次就裂纹 | `999` |

## 构建

```bash
dotnet build
```

构建完成后 DLL 会自动复制到游戏目录下的 `BepInEx/plugins/`（需确保 `GameDir` 路径正确）。

## 技术实现

- 通过 **HarmonyLib** 对游戏内 `GlobalConfig.OnEnable` 方法注入补丁（Postfix），在全局配置初始化时自动写入自定义阈值
- 基于 **.NET 6.0** 构建，兼容游戏的 **IL2CPP** 运行时

## 许可证

本项目仅供学习与交流使用。

---

# Card Crack Threshold Mod

## Introduction

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Vampire Crawlers** that allows you to customize how many times a card must be played in the same turn before it starts cracking.

## Features

- **Configurable card crack threshold** — Adjust the `TimesPlayedToStartCracking` parameter via BepInEx config to control when cards begin to crack after repeated same-turn plays.
  - Default: **999**
  - Minimum: **1**

## Installation

### Prerequisites

- [BepInEx](https://docs.bepinex.dev/) installed (IL2CPP version)
- Base game: **Vampire Crawlers**

### Steps

1. Place the compiled `CardCrackThresholdMod.dll` into the game's `BepInEx/plugins/` directory.
2. Launch the game — the mod will load automatically.

## Configuration

A config file is automatically generated after the mod loads at:

```
BepInEx/config/com.imoonday.cardcrackthresholdmod.cfg
```

Available options:

| Config Key | Description | Default |
|------------|-------------|---------|
| `TimesPlayedToStartCracking` | The number of times the same card must be played in one turn before it starts cracking | `999` |

## Build

```bash
dotnet build
```

After building, the DLL is automatically copied to `BepInEx/plugins/` (ensure `GameDir` is set correctly).

## Technical Details

- Uses **HarmonyLib** to apply a Postfix patch on `GlobalConfig.OnEnable`, ensuring the custom threshold is applied when the game's global config initializes.
- Target framework: **.NET 6.0**
- Compatible with **IL2CPP** backend

## License

This project is for educational and personal use only.
