using HarmonyLib;
using Il2CppReloaded.Input;
using Il2CppSource.Controllers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SunCollector;

// Use the primary player's paired device, never Gamepad.current (which may
// belong to player 2). Right stick press has no native gameplay binding.
internal sealed class ControllerInput
{
    private GamepadCursorController? controller;
    private int lastInputFrame = -1;

    private static bool CanUse(GamepadCursorController? value) =>
        value != null && value && value.gameObject.activeInHierarchy && value.PlayerIndex == 0 &&
        value.Board != null && SunCollectorMod.IsPlaying(value.Board) &&
        value.m_cursor != null && value.m_cursor.Enabled &&
        value.m_playerInput != null && value.m_playerInput.Paired &&
        value.m_playerInput.CurrentControlType == ControlType.Controller;

    private static Gamepad? GetGamepad(GamepadCursorController value)
    {
        var player = value.m_playerInput.Player;
        if (player == null) return null;
        foreach (var device in player.devices)
        {
            var gamepad = device.TryCast<Gamepad>();
            if (gamepad != null && gamepad.added && gamepad.enabled) return gamepad;
        }
        return null;
    }

    internal ControlDeviceType DeviceType => CanUse(controller) && GetGamepad(controller!) != null
        ? controller!.m_playerInput.CurrentDeviceType : ControlDeviceType.None;

    internal void Update(GamepadCursorController value)
    {
        if (value.PlayerIndex != 0) return;
        controller = value;
        if (!CanUse(value) || lastInputFrame == Time.frameCount) return;
        lastInputFrame = Time.frameCount;
        if (GetGamepad(value)?.rightStickButton.wasPressedThisFrame == true)
            SunCollectorMod.Instance.Toggle(value.Board);
    }
}

[HarmonyPatch(typeof(GamepadCursorController), nameof(GamepadCursorController.Update))]
internal static class ControllerUpdatePatch
{
    private static void Postfix(GamepadCursorController __instance) =>
        SunCollectorMod.Instance.Controller.Update(__instance);
}
