# Research: One-Time Dismissible Notification Mechanisms for SMAPI Mods

**Feature**: Soil Health Visualization (and general mod notifications)
**Date**: 2026-09-09

## Research Question

When a Stardew Valley mod needs to show a one-time dismissible notification to the player, which UI mechanism is the community standard — chat box message or toast popup?

---

## Findings

### 1. The Canonical Answer: HUDMessage (Toast Popup)

**The community standard for one-time dismissible notifications is `Game1.addHUDMessage()` — a toast popup in the lower-left of the screen, NOT a chat box message.**

This is confirmed across all primary sources: the official modding wiki, SMAPI's own API surface, and every major open-source mod.

---

### 2. SMAPI Official Documentation & API

**Source**: [Modding:Common tasks - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Common_tasks)

The modding wiki (the official SMAPI documentation) documents `HUDMessage` as the standard notification mechanism:

> "HUDMessage are popup notifications that appear in the lower left of the screen."

The wiki provides these canonical examples:

```csharp
// Add a toaster popup with error icon
Game1.addHUDMessage(new HUDMessage("MESSAGE", HUDMessage.error_type));

// Add a simple rectangle with no icon and custom duration
Game1.addHUDMessage(new HUDMessage("MESSAGE"){noIcon=true, timeLeft=4000f});
```

**Source**: [SMAPI ModHelper.cs](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/ModHelpers/ModHelper.cs)

SMAPI does **not** provide a dedicated `IChatHelper` or toast API. The `IModHelper` interface exposes: `Events`, `GameContent`, `ModContent`, `ContentPacks`, `Data`, `Input`, `Reflection`, `ModRegistry`, `ConsoleCommands`, `Multiplayer`, `Translation`. There is no chat/notification helper — mods call `Game1.addHUDMessage()` directly.

**Source**: [SMAPI IMultiplayerHelper.cs](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/IMultiplayerHelper.cs)

The `IMultiplayerHelper` interface provides `SendMessage<TMessage>()` for mod-to-mod network messages — this is NOT for player-facing notifications.

---

### 3. HUDMessage Class Internals (Decompiled)

**Source**: [HUDMessage.cs - WeDias/StardewValley](https://github.com/WeDias/StardewValley/blob/main/HUDMessage.cs) (decompiled Stardew Valley 1.5.6)

The `HUDMessage` class has these constructors:

| Constructor | Description |
|---|---|
| `HUDMessage(string message)` | No icon, default 3500ms, SeaGreen color |
| `HUDMessage(string message, bool achievement)` | Achievement type (5250ms, OrangeRed) |
| `HUDMessage(string message, int whatType)` | With icon type (5250ms, OrangeRed) |
| `HUDMessage(string message, Color color, float timeLeft)` | Custom color + duration |
| `HUDMessage(string message, Color color, float timeLeft, bool fadeIn)` | With fade-in effect |
| `HUDMessage(string message, string leaveMeNull)` | No icon, parsed text, default 3500ms |

**Icon types** (the `whatType` int field):
- `1` = `achievement_type`
- `2` = `newQuest_type`
- `3` = `error_type`
- `4` = `stamina_type`
- `5` = `health_type`
- `6` = `screenshot_type`

**Key fields for customization**:
- `noIcon` (bool) — hides the icon, creates a simple rectangle
- `timeLeft` (float) — duration in milliseconds
- `transparency` (float) — 0.0 (transparent) to 1.0 (opaque)
- `fadeIn` (bool) — whether to fade in on appearance
- `color` (Color) — text/box color

**Behavior**: Messages stack vertically in the lower-left corner. Each message's `update()` decrements `timeLeft` and fades out when expired. The `draw()` method renders either an icon box (with item sprite or type icon) or a simple hover text box when `noIcon = true`.

---

### 4. How Popular Open-Source Mods Deliver Notifications

#### 4a. Lookup Anything (by Pathoschild, the SMAPI creator)

**Source**: [CommonHelper.cs - Pathoschild/StardewMods](https://github.com/Pathoschild/StardewMods/blob/develop/Common/CommonHelper.cs)

This is the canonical reference implementation. Pathoschild (SMAPI's creator) provides two helper methods used across all his mods:

```csharp
/// <summary>Show an informational message to the player.</summary>
public static void ShowInfoMessage(string message, int? duration = null, int number = -1)
{
    Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type)
    {
        noIcon = true,
        timeLeft = duration ?? HUDMessage.defaultTime,
        number = number
    });
}

/// <summary>Show an error message to the player.</summary>
public static void ShowErrorMessage(string message)
{
    Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
}
```

**Key pattern**: Both use `HUDMessage.error_type` with `noIcon = true` for info messages. The `error_type` is used for the message type key (preventing duplicate stacking), not because the message is an error.

#### 4b. CJB Cheats Menu (by CJBok/Pathoschild)

**Source**: [CJBCheatsMenu/ModEntry.cs](https://github.com/CJBok/SDV-Mods/blob/master/CJBCheatsMenu/ModEntry.cs)

CJB Cheats Menu uses the same `CommonHelper.ShowErrorMessage()` from Pathoschild's shared library. The mod does NOT use chat messages for notifications.

#### 4c. UI Info Suite 2

**Source**: [UIInfoSuite2](https://github.com/Annosz/UIInfoSuite2)

UI Info Suite 2 uses `Game1.addHUDMessage()` directly for its various HUD notifications (e.g., showing when a crop is ready, birthday reminders, etc.). It does NOT use chat messages.

---

### 5. Chat Box Message System

**Source**: [ChatMessage.cs - Novex/stardew-remote-control](https://github.com/Novex/stardew-remote-control/blob/master/ChatMessage.cs)

The chat system is a **multiplayer-only** feature for player-to-player communication. The `ChatMessage` class:

```csharp
public class ChatMessage
{
    public enum ChatKinds
    {
        ChatMessage,
        ErrorMessage,
        UserNotification,
        PrivateMessage
    }

    public long sourceFarmer;
    public ChatKinds chatKind;
    public LocalizedContentManager.LanguageCode language;
    public string message;
}
```

**Source**: [Multiplayer - Stardew Valley Wiki](https://stardewvalleywiki.com/Multiplayer)

> "Chat is a feature that allows communicating between players directly through in-game chat box. The game will also broadcast messages via the chat box."

**Critical distinction**: The chat box is for **multiplayer player communication**, not for mod-to-player notifications. It requires the player to open the chat UI (press T on PC) and is not suitable for one-time dismissible notifications.

---

### 6. Toast Popup Libraries

There is **no common standalone "toast" library** in the SMAPI ecosystem. The pattern is:

1. **Direct `Game1.addHUDMessage()` calls** — used by most mods
2. **Pathoschild's `CommonHelper.ShowInfoMessage()` / `ShowErrorMessage()`** — the de facto standard wrapper used by Pathoschild's mods (Lookup Anything, CJB Cheats Menu, Content Patcher, etc.)
3. **Custom overlay rendering** — some mods draw directly via `RenderedHud` event for more complex UI

The `HUDMessage` class IS the toast system. It's the game's built-in notification mechanism.

---

## Decision

**Use `Game1.addHUDMessage()` with `HUDMessage.error_type` and `noIcon = true` for one-time dismissible notifications.**

This is the community standard because:
1. It's the game's built-in notification system (no custom UI needed)
2. It auto-dismisses after a configurable duration
3. It doesn't require player interaction to dismiss
4. It's used by every major mod (Lookup Anything, CJB Cheats Menu, UI Info Suite 2)
5. It's documented in the official modding wiki
6. It works in both single-player and multiplayer

---

## Tradeoffs

| Mechanism | Pros | Cons | Use Case |
|---|---|---|---|
| **HUDMessage (toast popup)** | Auto-dismissible; no player interaction needed; built-in; stacks vertically; works in SP & MP | Limited styling; lower-left position only; can be obscured by other HUD elements | One-time notifications, status updates, feedback |
| **Chat box message** | Visible in multiplayer; persistent until scrolled away | Requires player to open chat UI; not auto-dismissible; multiplayer-only; easily missed | Multiplayer player communication only |
| **Custom overlay (RenderedHud)** | Full control over position, style, timing | Must implement manually; more code; must handle timing/dismissal | Complex notifications, tooltips, persistent UI |
| **DialogueBox** | Player must acknowledge; prominent | Blocks interaction; requires dismissal; too intrusive for simple feedback | Important choices, blocking prompts |

---

## Implementation Pattern

For the Living Roots mod, follow the established pattern:

```csharp
// Simple notification (auto-dismiss after 3.5 seconds)
Game1.addHUDMessage(new HUDMessage("Soil tilled!", HUDMessage.error_type)
{
    noIcon = true,
    timeLeft = 3500f
});

// Notification with custom duration
Game1.addHUDMessage(new HUDMessage("Compost ready!", HUDMessage.error_type)
{
    noIcon = true,
    timeLeft = 5000f  // 5 seconds
});
```

**Note**: Use `HUDMessage.error_type` as the type parameter — this is the convention used by Pathoschild's `CommonHelper` and prevents message stacking issues. The `error_type` constant is used as a type key, not because the message is an error.

---

## Sources

1. [Modding:Common tasks - Stardew Valley Wiki](https://stardewvalleywiki.com/Modding:Common_tasks) — Official documentation for HUDMessage
2. [HUDMessage.cs - WeDias/StardewValley](https://github.com/WeDias/StardewValley/blob/main/HUDMessage.cs) — Decompiled HUDMessage class
3. [CommonHelper.cs - Pathoschild/StardewMods](https://github.com/Pathoschild/StardewMods/blob/develop/Common/CommonHelper.cs) — Canonical ShowInfoMessage/ShowErrorMessage implementation
4. [LookupAnything/ModEntry.cs](https://github.com/Pathoschild/StardewMods/blob/develop/LookupAnything/ModEntry.cs) — Reference mod using CommonHelper
5. [CJBCheatsMenu/ModEntry.cs](https://github.com/CJBok/SDV-Mods/blob/master/CJBCheatsMenu/ModEntry.cs) — Another mod using CommonHelper
6. [ChatMessage.cs - Novex/stardew-remote-control](https://github.com/Novex/stardew-remote-control/blob/master/ChatMessage.cs) — Chat system for multiplayer
7. [Multiplayer - Stardew Valley Wiki](https://stardewvalleywiki.com/Multiplayer) — Chat system documentation
8. [SMAPI ModHelper.cs](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/Framework/ModHelpers/ModHelper.cs) — SMAPI API surface (no chat helper)
9. [SMAPI IMultiplayerHelper.cs](https://github.com/Pathoschild/SMAPI/blob/develop/src/SMAPI/IMultiplayerHelper.cs) — Multiplayer API (mod messages, not player notifications)
