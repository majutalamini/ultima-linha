using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UltimaLinha.AI
{
    /// <summary>
    /// Cliente genérico para falar com o backend do jogo (NÃO com a Anthropic API
    /// diretamente). O backend é quem guarda a ANTHROPIC_API_KEY e chama a Claude API
    /// server-side. Isto evita expor a chave dentro do build do jogo.
    ///
    /// Uso típico:
    ///   ClaudeApiClient.Instance.RequestNpcDialogue(
    ///       npcName: "Guarda da Última Linha",
    ///       npcPersonality: "cansado, cínico, mas leal",
    ///       context: "o jogador atravessou a fronteira sem permissão",
    ///       playerMessage: null,
    ///       onSuccess: dialogue => Debug.Log(dialogue),
    ///       onError: error => Debug.LogWarning(error));
    /// </summary>
    public class ClaudeApiClient : MonoBehaviour
    {
        public static ClaudeApiClient Instance { get; private set; }

        [Tooltip("URL do SEU backend (não da Anthropic). Ex: http://localhost:3000 em dev, " +
                 "ou a URL do backend hospedado em produção.")]
        [SerializeField]
        private string backendUrl = "http://localhost:3000";

        [Tooltip("Timeout em segundos para cada requisição.")]
        [SerializeField]
        private int timeoutSeconds = 15;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [Serializable]
        private class NpcDialogueRequest
        {
            public string npcName;
            public string npcPersonality;
            public string context;
            public string playerMessage;
        }

        [Serializable]
        private class NpcDialogueResponse
        {
            public string text;
        }

        [Serializable]
        private class ErrorResponse
        {
            public string error;
        }

        /// <summary>
        /// Pede uma fala gerada dinamicamente para um NPC.
        /// </summary>
        public void RequestNpcDialogue(
            string npcName,
            string npcPersonality,
            string context,
            string playerMessage,
            Action<string> onSuccess,
            Action<string> onError)
        {
            var payload = new NpcDialogueRequest
            {
                npcName = npcName,
                npcPersonality = npcPersonality,
                context = context,
                playerMessage = playerMessage,
            };

            StartCoroutine(PostJson(
                "/api/npc-dialogue",
                JsonUtility.ToJson(payload),
                onRawSuccess: raw =>
                {
                    var response = JsonUtility.FromJson<NpcDialogueResponse>(raw);
                    onSuccess?.Invoke(response.text);
                },
                onError: onError));
        }

        private IEnumerator PostJson(string path, string jsonBody, Action<string> onRawSuccess, Action<string> onError)
        {
            string url = backendUrl.TrimEnd('/') + path;
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = timeoutSeconds;

                yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool failed = request.result != UnityWebRequest.Result.Success;
#else
                bool failed = request.isNetworkError || request.isHttpError;
#endif

                if (failed)
                {
                    string errorMessage = request.error;

                    // Tenta extrair a mensagem de erro estruturada do backend, se houver.
                    if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                    {
                        try
                        {
                            var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                            if (!string.IsNullOrEmpty(errorResponse?.error))
                            {
                                errorMessage = errorResponse.error;
                            }
                        }
                        catch
                        {
                            // Resposta não era JSON estruturado; mantém o erro original.
                        }
                    }

                    onError?.Invoke(errorMessage);
                    yield break;
                }

                onRawSuccess?.Invoke(request.downloadHandler.text);
            }
        }
    }
}
