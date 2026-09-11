using System.IO;
using System.Reflection;
using System;
using System.Linq;
using UnityEngine;
using Logging;

namespace TerrainTools.Visualization;

internal static class IconCache
{
    private static Texture2D _remove;
    private static Texture2D _cross;
    private static Texture2D _undo;
    private static Texture2D _redo;
    private static Texture2D _box;
    private static Texture2D _pavedRoadSquare;
    private static Texture2D _pavedRoadPath;
    private static Texture2D _pavedRoadPathSquare;
    private static Texture2D _mudRoadSquare;
    private static Texture2D _mudRoadPathSquare;
    private static Texture2D _replantSquare;
    private static Texture2D _cultivateSquare;
    private static Texture2D _cultivatePath;
    private static Texture2D _cultivatePathSquare;
    private static Texture2D _raiseSquare;
    private static Texture2D _lower;
    private static Texture2D _shovel;

    internal static Sprite LoadEmbeddedTextureAsSprite(string fileName)
    {
        Texture2D texture = LoadTextureFromResources(fileName);
        if (texture == null) { return null; }

        var pivot = new Vector2(0.5f, 0.5f);
        float units = 100f;
        return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), pivot, units);
    }

    internal static Texture2D LoadTextureFromResources(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLower();
        if (extension != ".png" && extension != ".jpg")
        {
            Log.LogWarning("LoadTextureFromResources can only load png or jpg textures");
            return null;
        }
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = assembly
            .GetManifestResourceNames()
            .Where(name => name.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (resourceName == null)
        {
            Log.LogWarning($"Embedded texture not found: {fileName}");
            return null;
        }
        using Stream manifestResourceStream = assembly.GetManifestResourceStream(resourceName);
        if (manifestResourceStream == null)
        {
            Log.LogWarning($"Could not open embedded texture: {resourceName}");
            return null;
        }
        using MemoryStream buffer = new();
        manifestResourceStream.CopyTo(buffer);
        Texture2D texture = new(0, 0);
        MethodInfo loadImage = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")?
            .GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
        if (loadImage?.Invoke(null, new object[] { texture, buffer.ToArray() }) is not bool loaded || !loaded)
        {
            Log.LogWarning($"Could not decode embedded texture: {resourceName}");
            return null;
        }
        return texture;
    }

    internal static Texture2D Shovel
    {
        get
        {
            if (_shovel == null)
            {
                _shovel = LoadTextureFromResources("ShovelIcon.png");
            }
            return _shovel;
        }
    }

    internal static Texture2D Lower
    {
        get
        {
            if (_lower == null)
            {
                _lower = LoadTextureFromResources("lower_v2.png");
            }
            return _lower;
        }
    }

    internal static Texture2D CultivateSquare
    {
        get
        {
            if (_cultivateSquare == null)
            {
                _cultivateSquare = LoadTextureFromResources("cultivate_v2_square.png");
            }
            return _cultivateSquare;
        }
    }

    internal static Texture2D CultivatePathSquare
    {
        get
        {
            if (_cultivatePathSquare == null)
            {
                _cultivatePathSquare = LoadTextureFromResources("cultivate_v2_path_square.png");
            }
            return _cultivatePathSquare;
        }
    }

    internal static Texture2D CultivatePath
    {
        get
        {
            if (_cultivatePath == null)
            {
                _cultivatePath = LoadTextureFromResources("cultivate_v2_path.png");
            }
            return _cultivatePath;
        }
    }

    internal static Texture2D ReplantSquare
    {
        get
        {
            if (_replantSquare == null)
            {
                _replantSquare = LoadTextureFromResources("replant_v2_square.png");
            }
            return _replantSquare;
        }
    }

    internal static Texture2D RaiseSquare
    {
        get
        {
            if (_raiseSquare == null)
            {
                _raiseSquare = LoadTextureFromResources("raise_v2_square.png");
            }
            return _raiseSquare;
        }
    }

    internal static Texture2D MudRoadPathSquare
    {
        get
        {
            if (_mudRoadPathSquare == null)
            {
                _mudRoadPathSquare = LoadTextureFromResources("path_v2_square.png");
            }
            return _mudRoadPathSquare;
        }
    }

    internal static Texture2D MudRoadSquare
    {
        get
        {
            if (_mudRoadSquare == null)
            {
                _mudRoadSquare = LoadTextureFromResources("mud_road_v2_square.png");
            }
            return _mudRoadSquare;
        }
    }

    internal static Texture2D PavedRoadPath
    {
        get
        {
            if (_pavedRoadPath == null)
            {
                _pavedRoadPath = LoadTextureFromResources("paved_road_v2_path.png");
            }
            return _pavedRoadPath;
        }
    }

    internal static Texture2D PavedRoadPathSquare
    {
        get
        {
            if (_pavedRoadPathSquare == null)
            {
                _pavedRoadPathSquare = LoadTextureFromResources("paved_road_v2_path_square.png");
            }
            return _pavedRoadPathSquare;
        }
    }

    internal static Texture2D PavedRoadSquare
    {
        get
        {
            if (_pavedRoadSquare == null)
            {
                _pavedRoadSquare = LoadTextureFromResources("paved_road_v2_square.png");
            }
            return _pavedRoadSquare;
        }
    }

    internal static Texture2D Remove
    {
        get
        {
            if (_remove == null)
            {
                _remove = LoadTextureFromResources("remove.png");
            }
            return _remove;
        }
    }

    internal static Texture2D Cross
    {
        get
        {
            if (_cross == null)
            {
                _cross = LoadTextureFromResources("cross.png");
            }
            return _cross;
        }
    }

    internal static Texture2D Undo
    {
        get
        {
            if (_undo == null)
            {
                _undo = LoadTextureFromResources("undo.png");
            }
            return _undo;
        }
    }

    internal static Texture2D Redo
    {
        get
        {
            if (_redo == null)
            {
                _redo = LoadTextureFromResources("redo.png");
            }
            return _redo;
        }
    }

    internal static Texture2D Box
    {
        get
        {
            if (_box == null)
            {
                _box = LoadTextureFromResources("box.png");
            }
            return _box;
        }
    }
}
