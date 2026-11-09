using Il2CppReloaded.Data;
using Il2CppReloaded.Input;
using UnityEngine;
using UnityEngine.UI;

namespace SunCollector;

// The HUD sun is baked into the bank background. Overlay only its circular
// glass face, copied from the native sprite; never rotate the bank or label.
internal sealed class SunIndicator : IDisposable
{
    private readonly List<Image> faces = new();
    private readonly List<Image> prompts = new();
    private readonly Dictionary<string, Sprite> sprites = new();
    private ControlDeviceType promptDevice;
    private Sprite? promptSprite;
    private float nextSearch;
    private float angle;
    private bool failed;

    internal void Update(bool enabled, ControlDeviceType deviceType)
    {
        if (failed) return;
        try
        {
            faces.RemoveAll(face => face == null || !face);
            prompts.RemoveAll(prompt => prompt == null || !prompt);
            if (deviceType != promptDevice)
            {
                promptDevice = deviceType;
                promptSprite = null;
                nextSearch = 0;
            }
            // Search while disabled too: the controller hint must explain how
            // to turn the collector on before the first activation.
            if (Time.unscaledTime >= nextSearch)
            {
                nextSearch = Time.unscaledTime + 1f;
                if (deviceType != ControlDeviceType.None && !promptSprite)
                    promptSprite = FindPrompt(deviceType);
                foreach (var image in Resources.FindObjectsOfTypeAll<Image>())
                {
                    if (!image.gameObject.activeInHierarchy || image.sprite == null) continue;
                    if (image.gameObject.name is not ("SunBankNormal" or "SunBankIsolated")) continue;
                    if (image.transform.parent == null || image.transform.parent.name != "SunAmount_Background") continue;
                    if (faces.Any(face => face.transform.parent.Pointer == image.transform.Pointer)) continue;
                    AddFace(image);
                    AddPrompt(image);
                }
            }
            angle = enabled ? Mathf.Repeat(angle - 30f * Time.unscaledDeltaTime, 360f) : 0f;
            foreach (var face in faces)
            {
                face.gameObject.SetActive(enabled);
                face.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }
            foreach (var prompt in prompts)
            {
                prompt.sprite = promptSprite;
                prompt.gameObject.SetActive(deviceType != ControlDeviceType.None && promptSprite);
            }
        }
        catch (Exception error)
        {
            failed = true;
            Dispose();
            MelonLoader.MelonLogger.Error($"Sun indicator disabled after an error: {error}");
        }
    }

    private static Sprite? FindPrompt(ControlDeviceType deviceType)
    {
        foreach (var data in Resources.FindObjectsOfTypeAll<ControllerData>())
        {
            if (data.DeviceType != deviceType || !data.PromptsAsset) continue;
            var glyphs = data.PromptsAsset.spriteGlyphTable;
            // Native prompt sheets share this index: right stick PRESS, not tilt.
            if (glyphs != null && glyphs.Count > 20) return glyphs[20].sprite;
        }
        return null;
    }

    private void AddPrompt(Image bank)
    {
        var go = new GameObject("SunCollectorControllerPrompt");
        var prompt = go.AddComponent<Image>();
        prompts.Add(prompt);
        var rect = prompt.rectTransform;
        rect.SetParent(bank.transform, false);
        var size = bank.sprite.rect;
        // Upper left of the glass sun, above the separate sun-count label.
        rect.anchorMin = new Vector2(26f / size.width, 222f / size.height);
        rect.anchorMax = new Vector2(90f / size.width, 286f / size.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        prompt.preserveAspect = true;
        prompt.raycastTarget = false;
        go.SetActive(false);
    }

    private void AddFace(Image bank)
    {
        var source = bank.sprite;
        if (!sprites.TryGetValue(source.name, out var sprite))
        {
            sprite = CopyFace(source);
            sprites.Add(source.name, sprite);
        }
        var go = new GameObject("SunCollectorRotatingFace");
        var face = go.AddComponent<Image>();
        faces.Add(face);
        var rect = face.rectTransform;
        rect.SetParent(bank.transform, false);
        // Coordinates measured in the native 275/285 x 314 HUD sprites.
        rect.anchorMin = new Vector2(77f / source.rect.width, 128f / source.rect.height);
        rect.anchorMax = new Vector2(207f / source.rect.width, 258f / source.rect.height);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // Relative anchors keep the crop aligned when the HUD layout resizes.
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        face.sprite = sprite;
        face.raycastTarget = false;
        face.color = bank.color;
    }

    private static Sprite CopyFace(Sprite source)
    {
        const int diameter = 130;
        var texture = source.texture;
        var buffer = RenderTexture.GetTemporary(texture.width, texture.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previous = RenderTexture.active;
        Texture2D? copy = null;
        try
        {
            // Atlas textures need not be CPU-readable. Copy through a GPU buffer.
            Graphics.Blit(texture, buffer);
            RenderTexture.active = buffer;
            copy = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            // The managed cache is invisible to Unity's unused-asset scan.
            // Keep both native assets alive across disabled HUDs/scene changes;
            // Dispose explicitly releases them when the mod unloads.
            copy.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var packed = source.textureRect;
            var offset = source.textureRectOffset;
            copy.ReadPixels(new Rect(packed.x + 142 - offset.x - 65,
                packed.y + 193 - offset.y - 65, diameter, diameter), 0, 0);
            var pixels = copy.GetPixels();
            for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - 65, dy = y + 0.5f - 65;
                    var color = pixels[y * diameter + x];
                    color.a *= Mathf.Clamp01(65f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * diameter + x] = color;
                }
            copy.SetPixels(pixels);
            copy.wrapMode = TextureWrapMode.Clamp;
            copy.Apply(false, true);
            var sprite = Sprite.Create(copy, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprite.name = "SunCollectorFace";
            return sprite;
        }
        catch
        {
            if (copy != null) UnityEngine.Object.Destroy(copy);
            throw;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(buffer);
        }
    }

    public void Dispose()
    {
        foreach (var face in faces)
            if (face != null && face) UnityEngine.Object.Destroy(face.gameObject);
        faces.Clear();
        foreach (var prompt in prompts)
            if (prompt != null && prompt) UnityEngine.Object.Destroy(prompt.gameObject);
        prompts.Clear();
        promptSprite = null;
        foreach (var sprite in sprites.Values)
        {
            // Unity may already have destroyed native objects during shutdown.
            if (sprite == null || !sprite) continue;
            var texture = sprite.texture;
            if (texture != null && texture) UnityEngine.Object.Destroy(texture);
            UnityEngine.Object.Destroy(sprite);
        }
        sprites.Clear();
    }
}
