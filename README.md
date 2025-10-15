# 🖐️ Advanced HCI: Haptics Demo

[![Unity](https://img.shields.io/badge/Unity-6+-black?logo=unity)](https://unity.com/)
[![XR Interaction Toolkit](https://img.shields.io/badge/XR%20Interaction%20Toolkit-3.0%2B-blue?logo=unity)](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/)
[![Meta XR SDK](https://img.shields.io/badge/Meta%20XR%20SDK-Required-lightblue?logo=meta)](https://developer.oculus.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A modular and interactive **haptic feedback showcase** for **Unity XR Interaction Toolkit** and **Meta (Oculus) Haptics SDK**.

This demo features multiple "stations" that combine tactile and audio feedback for immersive XR experiences — from drumming to drilling and friction simulation.

---

## 🚀 Features

- ⚙️ **Dual-mode haptics** — Basic (Unity impulses) or Advanced (Meta haptic clips)
- 🔊 Audio–haptic synchronization for tactile realism
- 🧩 Modular station system built on `HapticStationBase`
- ✋ Automatic hand detection from XR interactors
- 🎛️ Real-time haptic mode switching via `UISwitch`
- 🧱 Utility components to arrange and rotate stations dynamically
- 🧠 Debug-friendly logging and editor auto-assignment

---

## 🧩 Station Overview

| Station | Description | Haptic Feedback Type |
|---------|-------------|----------------------|
| **🪘 DrumPadStation** | Interactive pads that trigger sound and vibration when struck | Speed-sensitive impulses or Meta clips |
| **🪨 FrictionStation** | Rub a handheld stone to feel resistance | Continuous + grainy tactile feedback |
| **🔨 HammerStation** | Swing and impact objects with a hammer | Velocity-mapped haptic impulse |
| **🪛 DrillStation** | Handheld drill with looping vibration | Sustained or pulsing feedback |
| **🔫 SpaceGunStation** | Fire projectiles with recoil | Burst synced to projectile |
| **🖱️ UIHapticsStation** | Haptic feedback for UI hover/click | Subtle tactile cues |

---

## 🧠 Core Architecture

### `HapticStationBase`

Shared base class for all stations:
- Holds references to `HapticPlayer` and `HapticModeSwitcher`
- Simplifies haptic and audio triggering

```csharp
PlayOneShot(audioSource, config);
PlayBasic(config, HandFrom(args.interactorObject));
```

### `HapticPlayer`

Handles both basic and advanced haptics:
- **Basic**: Unity's `HapticImpulsePlayer`
- **Advanced**: Meta's `HapticClipPlayer` for preauthored haptic clips

```csharp
hapticPlayer.PulseBasic(config, HandTarget.Right);
firePlayer.Play(HapticPlayer.ToMeta(HandTarget.Left));
```

### `HapticConfig`

Defines tactile and audio properties for each interaction.

```csharp
public class HapticConfig {
    public AudioClip sfx;
    public float sfxVolume;
    public float basicAmplitude;
    public Oculus.Haptics.HapticClip advancedClip;
    public float advancedAmplitude;
}
```

Each station uses one or more of these assets.

### `HapticModeSwitcher` & `UISwitch`

Controls global mode switching (Basic ↔ Advanced)
- UI toggle animates between modes and updates the switcher

```csharp
modeSwitcher.Mode = HapticMode.Advanced;
```

### `HapticHandUtil`

Resolves which hand (Left, Right, Both) is interacting — even through nested XR controller hierarchies.

### Collision Relays

| Script | Purpose |
|--------|---------|
| `HammerCollisionRelay` | Detects and relays hammer impacts to `HammerStation`, including velocity |
| `FrictionCollisionRelay` | Detects contact entry/exit for friction surfaces and forwards to `FrictionStation` |

These keep physics cleanly decoupled from station logic.

---

## 🧭 Scene Management Tools

| Script | Purpose |
|--------|---------|
| `WheelArranger` | Arranges demo stations in a circle (podium style) |
| `WheelRotator` | Rotates between stations using input actions or UI buttons |

---

## 🧾 License

MIT License © 2025 — Lui Albæk Thomsen

---

## 🙌 Acknowledgments

- [Meta XR SDK](https://developer.oculus.com/)
- [Unity XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest)
- [DOTween](http://dotween.demigiant.com/) for smooth transitions
