using UnityEditor;
using UnityEngine;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Importação de tudo em Assets/Art/Personagens/PixelArt: sprite, Point, sem compressão, sem mipmap e pivô nos pés.
    /// Padrão: grade nativa da arte, 58 PPU. Kai 222 px = 3,83 unidades;
    /// Souta 232 px e Ren 215 px mantêm a diferença de altura entre eles.
    /// Câmera com 540 px de arte na altura: escala inteira 2 em 1080p.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        const string Pasta = "Assets/Art/Personagens/PixelArt/";
        public const int Ppu = 58;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Pasta)) return;
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = Ppu;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.isReadable = false;

            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            s.spriteMeshType = SpriteMeshType.FullRect; // malha justa cortaria pixels semitransparentes de animações futuras
            s.spriteExtrude = 0;
            imp.SetTextureSettings(s);
        }
    }
}
