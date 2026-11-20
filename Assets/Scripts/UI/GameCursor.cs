using UnityEngine;

namespace UltimaLinha
{
    /// <summary>
    /// Cursor do mouse do jogo inteiro (menu e cenários): a ponta facetada de RPG em
    /// Assets/Resources/Cursor/cursor_rpg.png (desenhada por Ferramentas/Interface/cursor_rpg.py).
    /// O clique é a ponta, no canto de cima à esquerda. Ligado sozinho quando o jogo abre.
    /// </summary>
    public static class GameCursor
    {
        const string Caminho = "Cursor/cursor_rpg";
        static readonly Vector2 Ponta = new Vector2(2f, 2f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Liga()
        {
            var tex = Resources.Load<Texture2D>(Caminho);
            if (!tex)
            {
                Debug.LogWarning("[Última Linha] Cursor não encontrado em Resources/" + Caminho);
                return;
            }
            Cursor.SetCursor(tex, Ponta, CursorMode.Auto);
        }
    }
}
