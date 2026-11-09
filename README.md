# Sun Collector 1.2.1

## Dependencies

- Game/runtime: PvZ Replanted (Windows/IL2CPP), MelonLoader 0.7.3, and .NET 6.
- Required mods: `BloomEngine.dll`.
- Optional integrations: `DebugMod`.
- Building: .NET SDK with `net6.0` support and the game/MelonLoader reference assemblies declared in `SunCollector.csproj`.

MelonLoader mod for existing installation of PvZ Replanted.

## Controls

- Initial state: **off**.
- Spacebar during active game: turn it on and collect all available suns. Newly created suns are also automatically collected.
- Space bar again: switch off. Collection animations that have already started will end.
- Controller for Player 1: **press right stick (R3)** to turn the Collector on or off. Only the gamepad assigned to this player by the game is used.
- When controller control is active, a native button icon to the left above the HUD sun above the sun meter indicates the right stick press. The icon remains visible when the collector is turned off and follows the controller type recognized by the game. It is hidden when using mouse/keyboard control.
- Holding does not switch repeatedly.
- When the collector is active, the static sun in the HUD slowly rotates clockwise (one revolution in twelve seconds). When turned off, the original display is restored. The animation uses real time and continues to run even when paused as long as the display is visible.
- Each change is immediately saved in `UserData/SunCollector.cfg` in the game folder. The status also applies after changing levels and restarting the game.
- There is no switching or automatic collection during pauses, menus and when there is no window focus. The setting is retained.

Small, normal, large and double suns are collected via `Coin.Collect(0)` for player 1. Coins, diamonds, brains and rewards remain behind. In multiplayer mode, player 1 is also used.

## Source and Build

```sh
cd /home/kevin/modding/pvzreplanted
.tools/dotnet/dotnet build sun-collector-mod/SunCollector.csproj -c Release
```

Requires the existing MelonLoader and IL2CPP interop DLLs as well as BloomEngine for the mod menu entry. Another game path can be specified with `-p:GameDir="/pfad/zum/spiel"`. No additional runtime dependency on ReplantAPI.

Version 1.2.1 adds a German description in the BloomEngine mod menu and an embedded icon: the native collective sun from `Sun1` (round core) and `Sun2` (rays) on the daylight lawn `Area1_Lawn`. The procedure corresponds to Plant Library: export native graphics offline, compose a solid 512 × 512 pixel icon and register it via `AddIcon`. The menu takes over the position and frame. The mod does not require Plant Library at runtime.

The icon can be re-exported from the `pvzreplanted` directory with `.tools/fusion-workflow/bin/python sun-collector-mod/tools/export_menu_assets.py`. The sources and checksums are in `assets/sources.json`.

Installed: `/mnt/FastGaming1/SteamLibrary/steamapps/common/PVZ Replanted/Mods/SunCollector.dll`.

To remove, delete this DLL while the game is closed. Optionally also delete `UserData/SunCollector.cfg` to reset the switch.

## Audit and manual playtest

Check manually for 1.2.1: Version 1.2.1, the German description and a sun on daylight lawn must appear in the mod menu. Then spacebar/R3, check automatic collection and HUD display as before. The game will not start for the change; the playtest remains with the user.

Release build against the installed assemblies successfully, with no warnings or errors. The game didn't start; no gameplay tests conducted. The playtest remains with the user.

Version 1.2.0 adds R3 and the controller note. The native input assignments were read from the installed assets: Left embroidery pressure is already assigned for `ChangeSpeed`; Right stick pressure has no native gameplay assignment. Shovel and Glove continue to use their existing tool action. The mod does not change any native input assignments.

For 1.2.0 check manually: Use controller, check R3 symbol on the left above the sun with the collector switched off. Press R3, observe existing and new suns; press again and leave new suns. Holding down may only switch once. Check switching between controller and mouse/keyboard, pause, loss of focus, controller disconnection and level change. R3 may not trigger game speed or shovel/glove. If necessary, check other controller types supported by the game. Player 2 is not allowed to switch the collector. The position, size and visibility of the symbol have not yet been checked in the current game.

Version 1.1.1 protects the generated sun sprite and its texture with `HideFlags.DontUnloadUnusedAsset` from Unity's resource cleanup. Managed cache alone will not keep these native objects alive if the HUD display is hidden or removed when changing levels. This allowed the rotating sun to appear as a white square. When unloading, the mod explicitly releases the resources and skips objects already destroyed by Unity.

For this fix, check especially when the Collector is active, pause/menus, change levels and switch off/on several times: The rotating sun should remain permanently visible and should not show a white square.

Recommended test: Let the sun lie, press the space bar, observe existing and new suns; press again and leave new suns. Restart with the power on and off and check persistence. Hold, pause and check level change.

## Technical basis

The gameplay scene uses `SunBankNormal` and `SunBankIsolated` under `SunAmount_Background`. Their native sprites contain the sun, glass area and frame together; the number is separate. The mod therefore copies the circular interior area (130 pixels diameter, center 142/193 in sprite coordinates) from the native atlas into its own UI image and rotates it. The glass reflection also rotates within the circle. The frame and number remain unchanged. Relative anchors follow the HUD size, the image does not intercept any input. When turned off it is hidden, when discharged it is removed. The scene hierarchy and sprite dimensions were read directly from the installed asset bundles.

`Board.Update` reads the space bar at most once per Unity frame before the native update. Collectable suns are then collected in the native `m_coins` container. Dead objects and objects that have already been collected are skipped. The native function handles value, sound and animation; Scores are not edited directly.

A postfix to `GamepadCursorController.Update` reads `rightStickButton.wasPressedThisFrame` of the assigned player 1 gamepad from Unity's input system. Switching only occurs when the gameplay cursor and controller control are active. Both input paths use the same switch with an additional lock against double switching in the same frame. Focus, pause, and gameplay checks apply to both input paths.

The hint directly uses the existing sprite from `ControllerData.PromptsAsset.spriteGlyphTable[20]` (right embroidery print). The native controller prompt assets have the same glyph index. No controller graphics are created or exported graphics are included. A separate UI image under `SunBankNormal` or `SunBankIsolated` follows the HUD via relative anchors and does not intercept any input. When changing devices, the appropriate native sprite is redetermined; when unloading, only its own UI object is removed. The native sprites still belong to the game.

The native `Coin.IsSun` implementation (RVA `0x6986E0` of this installation) also recognizes brains as a solar resource. That's why this mod explicitly uses the four actual sun types. Native `Board.KeyDown` has been tested for concurrent collection functionality; it does not contain such a collective call.
