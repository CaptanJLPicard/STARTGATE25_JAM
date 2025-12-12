# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity multiplayer game built with **Photon Fusion** networking. Version 0.0.1 using Unity 6000.0.60f1 (2024 LTS).

## Architecture

### Networking (Photon Fusion)

**FusionManager** (`Assets/LobbySystem/Scripts/FusionScripts/Fuison/FusionManager.cs`)
- Singleton managing entire network lifecycle
- Implements `INetworkRunnerCallbacks`
- Session lobby: `SessionLobby.ClientServer` mode
- Room passwords: SHA256 hashed
- Character selection via connection token

**NetworkInputData** (`Assets/PLAYER/Scripts/NetworkInputData.cs`)
- Implements `INetworkInput` for synchronized input
- Contains: direction (Vector3), mouseDelta, jump/sprint/freeze states

**Player** (`Assets/PLAYER/Scripts/Player.cs`)
- Extends `NetworkBehaviour`
- Uses `NetworkCharacterController` for physics
- `[Networked]` properties: Yaw, Pitch, ModelRotation, IsMoving, IsSprinting, Nick, IsReady
- RPC methods for teleportation and state sync

### Key Systems

| System | Location | Purpose |
|--------|----------|---------|
| FusionManager | `LobbySystem/Scripts/FusionScripts/Fuison/` | Network session management |
| Player | `PLAYER/Scripts/` | Character control and networking |
| GameReadyController | `LobbySystem/Scripts/FusionScripts/` | Pre-game waiting state machine |
| LevelManager | `LobbySystem/Scripts/OtherScripts/` | Level progression, escape menu |
| RoomBrowserUI | `LobbySystem/Scripts/UIScripts/` | Room creation/joining UI |
| LanguageManager | `LobbySystem/Scripts/OtherScripts/` | 8 language localization |

### Scene Structure

| Scene | BuildIndex | Purpose |
|-------|-----------|---------|
| MainMenu | 0 | Lobby, room browser, character selection |
| Level1+ | 1+ | Gameplay with networked players |

### Character System

4 characters (Warrior, Mage, Archer, Rogue) with prefabs in `Assets/PLAYER/Prefabs/`. Selection index passed via connection token.

### Data Persistence

- **Progress**: `Assets/LobbySystem/SaveData/player_progress.json` (JSON with `lastLevelIndex`)
- **PlayerPrefs**: `SelectedCharacter`, `Nick`, `DilTercihi` (language)

## Code Conventions

### Fusion Networking Patterns

```csharp
// Networked property declaration
[Networked] private NetworkBool IsMoving { get; set; }
[Networked] public NetworkString<_16> Nick { get; set; }

// Input handling in FixedUpdateNetwork
public override void FixedUpdateNetwork()
{
    if (GetInput(out NetworkInputData input))
    {
        // Process input
    }
}

// RPC declaration
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_SetReady(bool value) => IsReady = value;
```

### Input Authority Pattern

- Only input authority processes player input
- Use `Object.HasInputAuthority` to check ownership
- Camera only enabled for local player

### Physics

- Custom gravity (-20f) via `NetworkCharacterController`
- `rb.useGravity = false` when using custom physics
- `RigidbodyConstraints.FreezeRotation` for character controllers

## Game Flow

1. MainMenu → RoomBrowserUI for room list/creation
2. FusionManager.Create() (Host) or JoinWithPassword() (Client)
3. OnSceneLoadDone() spawns Player objects based on character index
4. GameReadyController waits for all players to be ready
5. Level progression via NextLevelTrigger collision detection
