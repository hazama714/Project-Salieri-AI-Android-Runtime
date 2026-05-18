# PSAI.unitypackage
# Project Salieri AI Lite Unity Package
# Project Salieri AI Lite Unity Package（日本語 / English）

This Unity package contains the Lite Runtime structure of Project Salieri AI.

この UnityPackage には Project Salieri AI の Lite Runtime 構造が含まれています。

---

# Included
# 含まれるもの

- Runtime architecture
- State system
- Limbo permission layer
- Autonomous runtime flow
- Dummy face perception
- Android TTS fallback flow
- Action execution structure
- Debug runtime components

---

# Intended Purpose
# 想定用途

This package is intended as:

このパッケージは以下を目的としています：

- Runtime architecture reference
- AI runtime research
- Educational reference
- Experimental AI agent framework

---

# External Dependencies
# 外部依存

The following packages are NOT included:

以下は同梱されていません：

- UniVRM
- OpenCVForUnity
- VOICEVOX Runtime
- GGUF models
- Native runtime libraries

---

# Required Dependency
# 必須依存

## UniVRM

Please import UniVRM manually.

UniVRM を別途導入してください。

Official:
https://github.com/vrm-c/UniVRM

Recommended:
VRM0 compatible version.

推奨：
VRM0対応版

---

# Lite Runtime Structure
# Lite Runtime 構造

The Lite Runtime uses:

Lite版では以下を使用します：

- DummyFacePerception
- Android standard TTS
- Debug servo execution
- Runtime state loop

This allows operation without physical robot hardware.

実機ロボット無しでも動作可能です。

---

# Notes
# 注意事項

This repository is NOT a complete production environment.

これは完全な製品版環境ではありません。

The full internal development environment is intentionally not included.

完全な内部開発環境は含まれていません。

---

# Runtime Philosophy
# Runtime 設計思想

Project Salieri AI separates:

Project Salieri AI は以下を分離しています：

- Intention
- Reflex
- Safety
- Hardware execution

The Android runtime remains the central AI layer.

Android Runtime が AI の中核層です。

---

# License
# ライセンス

Apache License 2.0
