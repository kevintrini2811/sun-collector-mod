using BloomEngine.ModMenu;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SunCollector;

internal static class MenuPresentation
{
    private static Sprite? icon;

    internal static void Register(MelonMod mod)
    {
        using var stream = typeof(MenuPresentation).Assembly.GetManifestResourceStream("SunCollector.Assets.menu-icon.png")
            ?? throw new InvalidDataException("Missing Sun Collector menu icon.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
                throw new InvalidDataException("Invalid Sun Collector menu icon.");
            texture.hideFlags = HideFlags.HideAndDontSave;
            icon = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            icon.hideFlags = HideFlags.HideAndDontSave;
            ModMenuService.CreateEntry(mod).AddIcon(icon)
                .AddDescription("Automatically collects existing and newly spawned sun for player 1. " +
                    "Press Space or the right stick (R3) during gameplay to toggle it. " +
                    "The setting is saved; a rotating HUD sun indicates that the collector is active. " +
                    "Coins and rewards are left untouched.")
                .Register();
        }
        catch
        {
            if (icon != null) { Object.Destroy(icon); icon = null; }
            Object.Destroy(texture);
            throw;
        }
    }

    internal static void Dispose()
    {
        if (icon == null) return;
        Object.Destroy(icon.texture);
        Object.Destroy(icon);
        icon = null;
    }
}
