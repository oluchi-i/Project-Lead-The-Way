using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LeadTheWayIconTools
{
    private const string FurnitureIconFolder = "Assets/Art/Furniture/Icons";
    private const string GeneratedIconFolder = "Assets/Art/UI/ObjectIcons";
    private const int IconSize = 256;

    [MenuItem("Tools/Lead The Way/UI/Icons/Configure Furniture Icons As Sprites")]
    public static void ConfigureFurnitureIconsAsSprites()
    {
        ConfigureTextureFolderAsSprites(FurnitureIconFolder);
    }

    [MenuItem("Tools/Lead The Way/UI/Icons/Configure Selected Textures As Sprites")]
    public static void ConfigureSelectedTexturesAsSprites()
    {
        var texturePaths = Selection.objects
            .Select(AssetDatabase.GetAssetPath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .SelectMany(GetTexturePaths)
            .Distinct()
            .ToList();

        if (texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Configure Selected Textures", "Select one or more PNG/JPG/TGA textures or folders first.", "OK");
            return;
        }

        ConfigureTexturesAsSprites(texturePaths);
    }

    [MenuItem("Tools/Lead The Way/UI/Icons/Generate Icons From Selected Objects")]
    public static void GenerateIconsFromSelectedObjects()
    {
        var selectedObjects = Selection.objects.OfType<GameObject>().ToList();
        if (selectedObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("Generate Object Icons", "Select one or more scene objects or prefab assets first.", "OK");
            return;
        }

        EnsureGeneratedIconFolder();

        var generatedCount = 0;
        foreach (var selectedObject in selectedObjects)
        {
            if (selectedObject == null)
                continue;

            var iconPath = $"{GeneratedIconFolder}/{CreateSafeFileName(selectedObject.name)}.png";
            var sprite = GenerateIconAssetForObject(selectedObject, iconPath);
            if (sprite == null)
                continue;

            AssignIconToSelectable(selectedObject, sprite);
            generatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Generate Object Icons",
            generatedCount == 1
                ? $"Generated 1 icon in {GeneratedIconFolder}."
                : $"Generated {generatedCount} icons in {GeneratedIconFolder}.",
            "OK");
    }

    private static void ConfigureTextureFolderAsSprites(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Configure Icons", $"Folder does not exist:\n{folderPath}", "OK");
            return;
        }

        var texturePaths = GetTexturePaths(folderPath).ToList();
        ConfigureTexturesAsSprites(texturePaths);
    }

    private static IEnumerable<string> GetTexturePaths(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return AssetDatabase
                .FindAssets("t:Texture2D", new[] { path })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsSupportedTexturePath);
        }

        return IsSupportedTexturePath(path) ? new[] { path } : Enumerable.Empty<string>();
    }

    private static bool IsSupportedTexturePath(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".tga";
    }

    private static void ConfigureTexturesAsSprites(IReadOnlyCollection<string> texturePaths)
    {
        if (texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog("Configure Icons", "No supported texture files were found.", "OK");
            return;
        }

        var changedCount = 0;
        foreach (var texturePath in texturePaths)
        {
            if (ConfigureTextureAsSprite(texturePath))
                changedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Configure Icons",
            $"Configured {texturePaths.Count} texture(s) as UI sprites. Updated import settings on {changedCount}.",
            "OK");
    }

    private static bool ConfigureTextureAsSprite(string texturePath)
    {
        if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
            return false;

        var changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            changed = true;
        }

        if (importer.maxTextureSize != 512)
        {
            importer.maxTextureSize = 512;
            changed = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Compressed)
        {
            importer.textureCompression = TextureImporterCompression.Compressed;
            changed = true;
        }

        if (!changed)
            return false;

        importer.SaveAndReimport();
        return true;
    }

    public static Sprite GenerateIconAssetForObject(GameObject sourceObject, string iconPath)
    {
        if (sourceObject == null || string.IsNullOrWhiteSpace(iconPath))
            return null;

        var folderPath = Path.GetDirectoryName(iconPath)?.Replace("\\", "/");
        if (!string.IsNullOrWhiteSpace(folderPath))
            EnsureFolder(folderPath);

        var iconTexture = RenderObjectIcon(sourceObject);
        if (iconTexture == null)
            return null;

        File.WriteAllBytes(iconPath, iconTexture.EncodeToPNG());
        Object.DestroyImmediate(iconTexture);

        AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        ConfigureTextureAsSprite(iconPath);
        return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
    }

    private static Texture2D RenderObjectIcon(GameObject sourceObject)
    {
        var preview = new PreviewRenderUtility();
        GameObject instance = null;
        var previousActive = RenderTexture.active;

        try
        {
            instance = Object.Instantiate(sourceObject);
            instance.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(instance, 0);
            preview.AddSingleGO(instance);

            if (!TryCalculateBounds(instance, out var bounds))
                return null;

            instance.transform.position -= bounds.center;
            bounds.center = Vector3.zero;

            var maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z, 0.5f);
            var cameraDirection = GetIconCameraDirection(bounds);
            var cameraDistance = maxExtent * 5.5f;

            preview.camera.orthographic = true;
            preview.camera.orthographicSize = maxExtent * 1.02f;
            preview.camera.nearClipPlane = 0.01f;
            preview.camera.farClipPlane = cameraDistance + maxExtent * 4f;
            preview.camera.clearFlags = CameraClearFlags.Color;
            preview.camera.backgroundColor = Color.clear;
            preview.camera.transform.position = cameraDirection * cameraDistance;
            preview.camera.transform.LookAt(Vector3.zero);

            preview.ambientColor = new Color(0.62f, 0.58f, 0.5f, 1f);
            preview.lights[0].intensity = 2.4f;
            preview.lights[0].transform.rotation = Quaternion.LookRotation(-cameraDirection, Vector3.up);
            preview.lights[1].intensity = 1.1f;
            preview.lights[1].transform.rotation = Quaternion.Euler(70f, -35f, 0f);

            preview.BeginPreview(new Rect(0f, 0f, IconSize, IconSize), GUIStyle.none);
            preview.Render(true);
            var previewTexture = preview.EndPreview();

            var icon = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            if (previewTexture is RenderTexture previewRenderTexture)
            {
                RenderTexture.active = previewRenderTexture;
                icon.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
                icon.Apply();
            }
            else
            {
                Graphics.CopyTexture(previewTexture, icon);
            }

            var framedIcon = FrameIconContent(icon);
            if (framedIcon != icon)
                Object.DestroyImmediate(icon);

            return framedIcon;
        }
        finally
        {
            RenderTexture.active = previousActive;

            if (instance != null)
                Object.DestroyImmediate(instance);

            preview.Cleanup();
        }
    }

    private static bool TryCalculateBounds(GameObject root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled)
            .ToList();

        if (renderers.Count == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Count; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    private static Vector3 GetIconCameraDirection(Bounds bounds)
    {
        var xExtent = Mathf.Max(0.001f, bounds.extents.x);
        var zExtent = Mathf.Max(0.001f, bounds.extents.z);
        var thinnerHorizontalExtent = Mathf.Min(xExtent, zExtent);
        var widerHorizontalExtent = Mathf.Max(xExtent, zExtent);
        var isTallThinObject = bounds.extents.y > widerHorizontalExtent * 0.9f
            && thinnerHorizontalExtent / widerHorizontalExtent < 0.45f;

        if (isTallThinObject)
        {
            if (xExtent < zExtent)
                return new Vector3(1.85f, 0.32f, 0f).normalized;

            return new Vector3(0f, 0.32f, -1.85f).normalized;
        }

        return new Vector3(0.72f, 0.64f, -1.55f).normalized;
    }

    private static Texture2D FrameIconContent(Texture2D source)
    {
        var pixels = source.GetPixels32();
        var minX = IconSize;
        var minY = IconSize;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < IconSize; y++)
        {
            for (var x = 0; x < IconSize; x++)
            {
                if (pixels[y * IconSize + x].a <= 8)
                    continue;

                minX = Mathf.Min(minX, x);
                minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x);
                maxY = Mathf.Max(maxY, y);
            }
        }

        if (maxX < minX || maxY < minY)
            return source;

        var contentWidth = maxX - minX + 1;
        var contentHeight = maxY - minY + 1;
        var padding = Mathf.RoundToInt(IconSize * 0.06f);
        var targetSize = IconSize - padding * 2;
        var scale = targetSize / (float)Mathf.Max(contentWidth, contentHeight);
        var destinationWidth = Mathf.Max(1, Mathf.RoundToInt(contentWidth * scale));
        var destinationHeight = Mathf.Max(1, Mathf.RoundToInt(contentHeight * scale));
        var destinationX = (IconSize - destinationWidth) / 2;
        var destinationY = (IconSize - destinationHeight) / 2;

        var framed = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        var transparent = new Color32[IconSize * IconSize];
        for (var i = 0; i < transparent.Length; i++)
            transparent[i] = new Color32(0, 0, 0, 0);

        framed.SetPixels32(transparent);

        for (var y = 0; y < destinationHeight; y++)
        {
            var sourceY = Mathf.Lerp(minY, maxY, destinationHeight == 1 ? 0.5f : y / (float)(destinationHeight - 1));
            for (var x = 0; x < destinationWidth; x++)
            {
                var sourceX = Mathf.Lerp(minX, maxX, destinationWidth == 1 ? 0.5f : x / (float)(destinationWidth - 1));
                var color = source.GetPixelBilinear(sourceX / (IconSize - 1), sourceY / (IconSize - 1));
                framed.SetPixel(destinationX + x, destinationY + y, color);
            }
        }

        framed.Apply();
        return framed;
    }

    private static void AssignIconToSelectable(GameObject selectedObject, Sprite sprite)
    {
        if (sprite == null)
            return;

        var selectable = selectedObject.GetComponentInChildren<SelectableControlObject>(true);
        if (selectable == null)
            return;

        var serializedObject = new SerializedObject(selectable);
        var iconProperty = serializedObject.FindProperty("icon");
        if (iconProperty == null)
            return;

        iconProperty.objectReferenceValue = sprite;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectable);

        if (!EditorUtility.IsPersistent(selectable))
            EditorSceneManager.MarkSceneDirty(selectable.gameObject.scene);
    }

    private static void EnsureGeneratedIconFolder()
    {
        EnsureFolder("Assets/Art/UI");
        EnsureFolder(GeneratedIconFolder);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        var name = Path.GetFileName(folderPath);
        if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            return;

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static string CreateSafeFileName(string value)
    {
        var safeName = string.IsNullOrWhiteSpace(value) ? "ObjectIcon" : value.Trim();
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(invalidCharacter, '_');

        return safeName.Replace(' ', '_');
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
