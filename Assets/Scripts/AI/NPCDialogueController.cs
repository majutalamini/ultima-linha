using UnityEngine;
using TMPro; // Se o projeto não usar TextMeshPro, troque por UnityEngine.UI.Text.

namespace UltimaLinha.AI
{
    /// <summary>
    /// Exemplo de uso do ClaudeApiClient: um NPC que, ao ser interagido pelo jogador,
    /// pede uma fala gerada dinamicamente pela IA e mostra numa caixa de diálogo simples.
    ///
    /// Requer que exista um GameObject na cena com o componente ClaudeApiClient
    /// (ex.: um objeto "GameManager" ou "Services" que fica em DontDestroyOnLoad).
    /// </summary>
    public class NPCDialogueController : MonoBehaviour
    {
        [Header("Identidade do NPC")]
        [SerializeField] private string npcName = "Guarda da Última Linha";
        [SerializeField, TextArea] private string npcPersonality = "cansado, cínico, mas leal ao posto";

        [Header("UI")]
        [Tooltip("Texto onde a fala gerada será exibida.")]
        [SerializeField] private TMP_Text dialogueText;
        [Tooltip("Objeto raiz da caixa de diálogo (ativado/desativado ao abrir/fechar a conversa).")]
        [SerializeField] private GameObject dialogueBox;

        [Header("Fallback")]
        [Tooltip("Fala usada se a chamada à IA falhar (sem internet, backend fora do ar, etc.).")]
        [SerializeField] private string fallbackLine = "...(o guarda apenas acena com a cabeça)";

        private bool isRequesting;

        /// <summary>
        /// Chame este método a partir do seu sistema de interação
        /// (ex.: quando o jogador aperta E perto do NPC).
        /// </summary>
        public void Interact(string playerMessage = null)
        {
            if (isRequesting)
            {
                return;
            }

            if (ClaudeApiClient.Instance == null)
            {
                Debug.LogWarning("ClaudeApiClient não está presente na cena. Usando fala de fallback.");
                ShowDialogue(fallbackLine);
                return;
            }

            isRequesting = true;
            ShowDialogue("..."); // feedback imediato enquanto espera a resposta

            string context = BuildContext();

            ClaudeApiClient.Instance.RequestNpcDialogue(
                npcName: npcName,
                npcPersonality: npcPersonality,
                context: context,
                playerMessage: playerMessage,
                onSuccess: line =>
                {
                    isRequesting = false;
                    ShowDialogue(string.IsNullOrWhiteSpace(line) ? fallbackLine : line);
                },
                onError: error =>
                {
                    isRequesting = false;
                    Debug.LogWarning($"Falha ao gerar diálogo para {npcName}: {error}");
                    ShowDialogue(fallbackLine);
                });
        }

        /// <summary>
        /// Monta o contexto atual do jogo para dar à IA algo relevante para reagir.
        /// Customize isto para puxar dados reais do seu GameManager
        /// (capítulo atual, reputação do jogador, hora do dia, etc.).
        /// </summary>
        private string BuildContext()
        {
            return "o jogador se aproximou do posto de fronteira da última linha";
        }

        private void ShowDialogue(string line)
        {
            if (dialogueBox != null)
            {
                dialogueBox.SetActive(true);
            }

            if (dialogueText != null)
            {
                dialogueText.text = line;
            }
        }

        public void CloseDialogue()
        {
            if (dialogueBox != null)
            {
                dialogueBox.SetActive(false);
            }
        }
    }
}
