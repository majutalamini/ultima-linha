using UnityEngine;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Trem do fundo do menu: desliza sem parar da direita para a esquerda, em loop contínuo
    /// (os vagões se repetem, então ele nunca "acaba"), com uma leve trepidação.
    /// </summary>
    public class TrainPass : MonoBehaviour
    {
        [Tooltip("Objeto que contém todos os vagões (pivô no canto superior esquerdo).")]
        [SerializeField] RectTransform train;
        [Tooltip("Filho que treme levemente (normalmente o mesmo conjunto de vagões).")]
        [SerializeField] RectTransform body;
        [Tooltip("Distância depois da qual o desenho do trem se repete (largura de 2 vagões + engates).")]
        [SerializeField] float loopLength = 4056f;
        [SerializeField] float speed = 140f;
        [SerializeField] float rumble = 1.2f;

        float startX;
        float offset;

        void Start()
        {
            startX = train.anchoredPosition.x;
        }

        void Update()
        {
            offset -= speed * Time.unscaledDeltaTime;
            if (offset <= -loopLength) offset += loopLength;
            train.anchoredPosition = new Vector2(startX + offset, train.anchoredPosition.y);

            if (body)
            {
                float y = (Mathf.PerlinNoise(Time.unscaledTime * 12f, 0.37f) - 0.5f) * 2f * rumble;
                body.anchoredPosition = new Vector2(0f, y);
            }
        }
    }
}
