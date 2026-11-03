using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha
{
    /// <summary>
    /// Salva sozinho, sem o jogador precisar fazer nada:
    /// a posição do Kai a cada poucos segundos (quando ele está parado no chão e no controle),
    /// e tudo o que estiver pendente quando o jogo é fechado, minimizado ou perde o foco.
    /// Criado automaticamente quando o jogo abre (não precisa estar em nenhuma cena).
    /// </summary>
    public class AutoSave : MonoBehaviour
    {
        const float Interval = 2f;

        static AutoSave instance;

        PlayerController player;
        string playerScene;
        float next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (instance) return;
            var go = new GameObject("AutoSave");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AutoSave>();
        }

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Interval;
            Snapshot(false);
        }

        void OnApplicationQuit() => Snapshot(true);

        void OnApplicationPause(bool paused)
        {
            if (paused) Snapshot(true);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) Snapshot(true);
        }

        /// <summary>Guarda a posição do Kai, se ele estiver num cenário salvo, no chão e no controle.</summary>
        void Snapshot(bool flush)
        {
            var scene = SceneManager.GetActiveScene().name;
            if (scene == GameSave.SavedScene && scene.StartsWith("Cenario"))
            {
                if (playerScene != scene || !player)
                {
                    player = FindFirstObjectByType<PlayerController>();
                    playerScene = scene;
                }
                if (player && player.controlEnabled && player.IsGrounded && !DialogueBox.IsBusy && !Salas.Modal.IsOpen)
                    GameSave.SavePosition(player.transform.position);
            }
            if (flush) GameSave.Flush();
        }

        /// <summary>
        /// Usado pelos diretores ao continuar: põe o Kai onde ele estava quando o jogo foi salvo.
        /// Devolve falso se não havia posição salva.
        /// </summary>
        public static bool RestorePlayer(PlayerController player, CameraFollow2D follow)
        {
            if (!player || !GameSave.TryGetPosition(out var p)) return false;
            var pos = new Vector3(p.x, p.y + 0.05f, player.transform.position.z);
            player.transform.position = pos;
            player.Teleport(pos);
            if (follow) follow.Snap();
            return true;
        }
    }
}
