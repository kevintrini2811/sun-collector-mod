using HarmonyLib;
using Il2CppReloaded.Gameplay;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(SunCollector.SunCollectorMod), "Sun Collector", "1.2.2", "Kevin / Codex")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]
[assembly: MelonOptionalDependencies("DebugMod")]
[assembly: MelonAdditionalDependencies("BloomEngine")]

namespace SunCollector;

public sealed class SunCollectorMod : MelonMod
{
    internal static SunCollectorMod Instance = null!;
    private MelonPreferences_Category preferences = null!;
    private MelonPreferences_Entry<bool> enabled = null!;
    private int lastInputFrame = -1;
    private int lastToggleFrame = -1;
    private bool collectionFailed;
    private readonly SunIndicator indicator = new();
    internal readonly ControllerInput Controller = new();

    public override void OnLateUpdate() => indicator.Update(enabled.Value && !collectionFailed, Controller.DeviceType);

    public override void OnDeinitializeMelon()
    {
        indicator.Dispose();
        MenuPresentation.Dispose();
    }

    public override void OnInitializeMelon()
    {
        Instance = this;
        preferences = MelonPreferences.CreateCategory("SunCollector");
        preferences.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "SunCollector.cfg"));
        enabled = preferences.CreateEntry("Enabled", false,
            description: "Automatically collect existing and future suns. Toggle with Space or right stick press (R3) during gameplay.");
        MenuPresentation.Register(this);
        LoggerInstance.Msg($"Sun Collector ready: {(enabled.Value ? "ON" : "OFF")}. Press Space or right stick (R3) during gameplay to toggle.");
    }

    internal static bool IsPlaying(Board board) =>
        Application.isFocused && !board.mPaused && board.mApp != null &&
        board.mApp.GameScene == GameScenes.Playing;

    internal void ReadInput(Board board)
    {
        // Board.Update can run several times per rendered frame at higher game speeds.
        if (lastInputFrame == Time.frameCount) return;
        lastInputFrame = Time.frameCount;
        if (!IsPlaying(board) || !Input.GetKeyDown(KeyCode.Space)) return;
        Toggle(board);
    }

    internal void Toggle(Board board)
    {
        // Keyboard and controller can report a press in the same rendered frame.
        if (!IsPlaying(board) || lastToggleFrame == Time.frameCount) return;
        lastToggleFrame = Time.frameCount;
        enabled.Value = !enabled.Value;
        collectionFailed = false;
        try { preferences.SaveToFile(false); }
        catch (Exception error)
        {
            LoggerInstance.Error($"Could not save Sun Collector setting; restart persistence is unavailable: {error}");
        }
        LoggerInstance.Msg($"Sun Collector: {(enabled.Value ? "ON" : "OFF")}");
    }

    internal void CollectSuns(Board board)
    {
        if (!enabled.Value || collectionFailed || !IsPlaying(board)) return;
        try
        {
            var coins = board.m_coins;
            if (coins == null) return;
            // Native Collect owns scoring, animation and collection flags. Never
            // modify sun money directly or collect the same object twice.
            for (int index = coins.Count - 1; index >= 0; index--)
            {
                var coin = coins[index];
                // IsSun also includes Brain in this game; restrict this mod to actual suns.
                if (coin != null && !coin.mDead && !coin.mIsBeingCollected &&
                    coin.mType is CoinType.Sun or CoinType.SmallSun or CoinType.LargeSun or CoinType.DoubleSun)
                    coin.Collect(0);
            }
        }
        catch (Exception error)
        {
            collectionFailed = true;
            LoggerInstance.Error($"Sun collection stopped after an error. Press Space or right stick (R3) to toggle/reset: {error}");
        }
    }
}

[HarmonyPatch(typeof(Board), nameof(Board.Update))]
internal static class BoardUpdatePatch
{
    private static void Prefix(Board __instance) => SunCollectorMod.Instance.ReadInput(__instance);
    // After native updates, include suns that were created during this tick.
    private static void Postfix(Board __instance) => SunCollectorMod.Instance.CollectSuns(__instance);
}
