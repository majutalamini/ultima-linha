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
    /// Salvamento simples para o "Continuar" do menu: guarda em que cenário o jogador está.
    /// Continuar recomeça esse cenário (sem repetir a cutscene de abertura).
    /// </summary>
    public static class GameSave
    {
        const string SceneKey = "UltimaLinha.Save.Cena";

        /// <summary>Ligado pelo menu ao clicar em Continuar; o cenário lê e desliga.</summary>
        public static bool ContinueRequested;

        public static string SavedScene => PlayerPrefs.GetString(SceneKey, "");
        public static bool HasSave => !string.IsNullOrEmpty(SavedScene);

        public static void Save(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            PlayerPrefs.SetString(SceneKey, sceneName);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SceneKey);
            PlayerPrefs.Save();
        }
    }
}
