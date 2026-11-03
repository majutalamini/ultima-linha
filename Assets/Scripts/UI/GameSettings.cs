using System;
using UnityEngine;

namespace UltimaLinha
{
    /// <summary>
    /// Configurações do jogador (volume geral, volume da música, tela cheia), salvas no PlayerPrefs.
    /// São aplicadas sozinhas quando o jogo abre.
    /// </summary>
    public static class GameSettings
    {
        const string MasterKey = "UltimaLinha.Config.VolumeGeral";
        const string MusicKey = "UltimaLinha.Config.VolumeMusica";
        const string FullscreenKey = "UltimaLinha.Config.TelaCheia";

        /// <summary>Avisa quando alguma configuração muda (o menu ajusta a música na hora).</summary>
        public static event Action Changed;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set
            {
                value = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MasterKey, value);
                AudioListener.volume = value;
                Changed?.Invoke();
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
            set
            {
                PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
                Changed?.Invoke();
            }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                Screen.fullScreen = value;
                Changed?.Invoke();
            }
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnStart()
        {
            AudioListener.volume = MasterVolume;
            if (PlayerPrefs.HasKey(FullscreenKey) && !Application.isEditor)
                Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
        }
    }

    /// <summary>
    /// Salvamento do jogo (PlayerPrefs). Guarda:
    /// - o cenário em que o jogador está (salvo assim que o cenário abre);
    /// - o progresso dentro desse cenário (marcos como "energia ligada", "chave achada");
    /// - a posição do Kai (salva sozinha a cada poucos segundos e ao fechar o jogo, pelo AutoSave).
    /// "Continuar" no menu abre o cenário salvo e o diretor da cena restaura o que estiver marcado.
    /// </summary>
    public static class GameSave
    {
        const string SceneKey = "UltimaLinha.Save.Cena";
        const string ProgressKey = "UltimaLinha.Save.Progresso";
        const string PositionKey = "UltimaLinha.Save.Posicao";
        const string WhenKey = "UltimaLinha.Save.Quando";

        /// <summary>Ligado pelo menu ao clicar em Continuar; o cenário lê (BeginScene) e desliga.</summary>
        public static bool ContinueRequested;

        /// <summary>Verdadeiro enquanto o cenário atual foi aberto pelo "Continuar".</summary>
        public static bool Continuing { get; private set; }

        public static string SavedScene => PlayerPrefs.GetString(SceneKey, "");
        public static bool HasSave => !string.IsNullOrEmpty(SavedScene);
        public static string SavedWhen => PlayerPrefs.GetString(WhenKey, "");

        /// <summary>
        /// Chamado pelo diretor de cada cenário logo no começo. Salva o cenário na hora (mesmo que o jogador saia
        /// durante a abertura) e diz se a cena veio do "Continuar" (aí o diretor restaura o progresso).
        /// Sem "Continuar", o progresso antigo deste cenário é apagado: começa do zero.
        /// </summary>
        public static bool BeginScene(string sceneName)
        {
            bool continuing = ContinueRequested && sceneName == SavedScene;
            ContinueRequested = false;
            Continuing = continuing;
            if (!continuing)
            {
                PlayerPrefs.DeleteKey(ProgressKey);
                PlayerPrefs.DeleteKey(PositionKey);
            }
            Save(sceneName);
            return continuing;
        }

        public static void Save(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            if (PlayerPrefs.GetString(SceneKey, "") != sceneName)
            {
                PlayerPrefs.DeleteKey(ProgressKey);
                PlayerPrefs.DeleteKey(PositionKey);
            }
            PlayerPrefs.SetString(SceneKey, sceneName);
            PlayerPrefs.SetString(WhenKey, DateTime.Now.ToString("dd/MM HH:mm"));
            PlayerPrefs.Save();
        }

        // ---------- progresso dentro do cenário ----------

        static string[] Marks()
        {
            var raw = PlayerPrefs.GetString(ProgressKey, "");
            return string.IsNullOrEmpty(raw) ? new string[0] : raw.Split(';');
        }

        /// <summary>Marca um passo do cenário atual como feito (salvo na hora).</summary>
        public static void Mark(string id)
        {
            if (string.IsNullOrEmpty(id) || Has(id)) return;
            var raw = PlayerPrefs.GetString(ProgressKey, "");
            PlayerPrefs.SetString(ProgressKey, string.IsNullOrEmpty(raw) ? id : raw + ";" + id);
            PlayerPrefs.SetString(WhenKey, DateTime.Now.ToString("dd/MM HH:mm"));
            PlayerPrefs.Save();
        }

        public static bool Has(string id) => Array.IndexOf(Marks(), id) >= 0;

        // ---------- posição do Kai ----------

        public static void SavePosition(Vector2 p)
        {
            PlayerPrefs.SetString(PositionKey, p.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + ";" +
                                               p.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }

        public static bool TryGetPosition(out Vector2 p)
        {
            p = Vector2.zero;
            var parts = PlayerPrefs.GetString(PositionKey, "").Split(';');
            if (parts.Length != 2) return false;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (!float.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out var x)) return false;
            if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out var y)) return false;
            p = new Vector2(x, y);
            return true;
        }

        /// <summary>Grava no disco o que estiver pendente (posição).</summary>
        public static void Flush() => PlayerPrefs.Save();

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SceneKey);
            PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.DeleteKey(PositionKey);
            PlayerPrefs.DeleteKey(WhenKey);
            Continuing = false;
            UltimaLinha.Salas.GameFlags.ClearAll();
            PlayerPrefs.Save();
        }
    }
}
