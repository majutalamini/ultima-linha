// Gerado por Ferramentas/Cenario2/cenario2.py — não edite à mão.
using UnityEngine;
namespace UltimaLinha.EditorTools
{
    /// <summary>Medidas do fundo do Cenário 2 (px da imagem; y a partir do topo da imagem original).</summary>
    internal static class Cenario2Layout
    {
        public const float LevelWidth = 7955f, LevelHeight = 1101f, ImageHeight = 941f;
        public const float FloorPy = 870f, DoorTopPy = 498f;
        public const float Slab = 90f, Post = 10f;
        public static readonly float[] CarX = { 0f, 2190f, 4205f, 6190f };
        public static readonly float[] CarW = { 1640f, 1325f, 1325f, 1155f };
        // passagens: parede A, parede B (x da borda esquerda de cada parede), vãos e chapas
        public static readonly float[] SlabA = { 1640f, 3515f, 5530f };
        public static readonly float[] SlabB = { 2100f, 4115f, 6100f };
        public static readonly Vector2[][] Holes =
        {
            new[] { new Vector2(1830f, 1980f) },
            new[] { new Vector2(3705f, 3815f), new Vector2(3885f, 3995f) },
            new[] { new Vector2(5720f, 5980f) },
        };
        public static readonly Vector2[][] Plates =
        {
            new[] { new Vector2(1730f, 1830f), new Vector2(1980f, 2100f) },
            new[] { new Vector2(3605f, 3705f), new Vector2(3815f, 3885f), new Vector2(3995f, 4115f) },
            new[] { new Vector2(5620f, 5720f), new Vector2(5980f, 6100f) },
        };
        public const float EndSlabX = 7345f, StationX = 7435f, StationW = 520f;
        // janelas (x0, x1, y0, y1) por onde passa a luz do túnel
        public static readonly Vector4[] Windows =
        {
            new Vector4(318f, 862f, 236f, 480f),
            new Vector4(1110f, 1210f, 262f, 498f),
            new Vector4(1290f, 1390f, 262f, 498f),
            new Vector4(2198f, 2737f, 236f, 480f),
            new Vector4(2985f, 3085f, 262f, 498f),
            new Vector4(3165f, 3265f, 262f, 498f),
            new Vector4(4213f, 4752f, 236f, 480f),
            new Vector4(5000f, 5100f, 262f, 498f),
            new Vector4(5180f, 5280f, 262f, 498f),
            new Vector4(6198f, 6737f, 236f, 480f),
            new Vector4(6985f, 7085f, 262f, 498f),
            new Vector4(7165f, 7265f, 262f, 498f),
        };
        // luminárias do teto (centro x, largura)
        public static readonly Vector2[] Tubes = { new Vector2(558f, 225f), new Vector2(1177f, 197f), new Vector2(2433f, 225f), new Vector2(3052f, 197f), new Vector2(4448f, 225f), new Vector2(5067f, 197f), new Vector2(6433f, 225f), new Vector2(7052f, 197f) };
    }
}
