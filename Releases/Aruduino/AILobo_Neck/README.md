# PSAI Arduino Runtime
# PSAI Arduino Runtime（日本語 / English）

Arduino runtime for Project Salieri AI.

Project Salieri AI 用 Arduino Runtime です。

---

# Purpose
# 目的

This Arduino runtime is responsible only for:

この Arduino Runtime は以下のみを担当します：

- Receiving commands
- Parsing servo instructions
- Executing servo movement

---

# Not Included
# 含まれないもの

The Arduino side does NOT contain:

Arduino 側には以下を含みません：

- AI logic
- LLM processing
- Autonomous reasoning
- State management
- Decision making

These responsibilities remain on the Android Runtime side.

これらの責任は Android Runtime 側にあります。

---

# Communication Structure
# 通信構造

Android Runtime
→ Bluetooth
→ Arduino
→ Servo

---

# Supported Hardware Examples
# 対応ハードウェア例

- Arduino Uno
- HC-05 / HC-06
- PCA9685
- SG90 / MG90S Servo

---

# Philosophy
# 設計思想

Project Salieri AI separates:

Project Salieri AI は以下を分離しています：

- Intention
- Safety
- Execution

The Arduino layer is intentionally lightweight.

Arduino 層は意図的に軽量化されています。

The Android Runtime remains the core AI runtime layer.

Android Runtime が AI Runtime の中核です。

---

# Notes
# 注意事項

This is an experimental reference implementation.

これは実験的リファレンス実装です。

Hardware configuration may vary depending on your robot setup.

ロボット構成に応じてハードウェア構成は変更してください。

---

# License
# ライセンス

Apache License 2.0
