using UnityEditor;
using UnityEngine;

namespace UltimaLinha.EditorTools
{
    /// <summary>Tudo em Assets/Resources/Cursor/ é importado como cursor (legível, sem compressão, sem mipmap).</summary>
    public class CursorImporter : AssetPostprocessor
    {
        const string Pasta = "Assets/Resources/Cursor/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Pasta)) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Cursor;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
