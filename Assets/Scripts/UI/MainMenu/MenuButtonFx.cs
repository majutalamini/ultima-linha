using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Efeito dos botões do menu: quando selecionado (mouse por cima, teclado ou controle),
    /// o texto acende em âmbar, desliza para a direita e um traço aparece à esquerda.
    /// Passar o mouse por cima seleciona o botão, então mouse e teclado ficam sempre em sincronia.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class MenuButtonFx : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Graphic marker;
        [SerializeField] Color normalColor = new Color(0.62f, 0.65f, 0.70f, 1f);
        [SerializeField] Color highlightColor = new Color(0.95f, 0.72f, 0.33f, 1f);
        [SerializeField] float slideDistance = 44f;
        [SerializeField] float speed = 14f;

        RectTransform labelRect;
        Vector2 labelBasePos;
        float current, target;

        void Awake()
        {
            if (label)
            {
                labelRect = label.rectTransform;
                labelBasePos = labelRect.anchoredPosition;
            }
            Apply(0f);
        }

        void OnEnable()
        {
            var es = EventSystem.current;
            target = current = (es && es.currentSelectedGameObject == gameObject) ? 1f : 0f;
            Apply(current);
        }

        public void OnSelect(BaseEventData _) => target = 1f;
        public void OnDeselect(BaseEventData _) => target = 0f;

        public void OnPointerEnter(PointerEventData _)
        {
            var es = EventSystem.current;
            var sel = GetComponent<Selectable>();
            if (es && sel.IsInteractable() && es.currentSelectedGameObject != gameObject)
                es.SetSelectedGameObject(gameObject);
        }

        void Update()
        {
            if (Mathf.Approximately(current, target)) return;
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(current - target) < 0.002f) current = target;
            Apply(current);
        }

        void Apply(float k)
        {
            if (label)
            {
                label.color = Color.Lerp(normalColor, highlightColor, k);
                if (labelRect) labelRect.anchoredPosition = labelBasePos + Vector2.right * (slideDistance * k);
            }
            if (marker)
            {
                marker.rectTransform.localScale = new Vector3(k, 1f, 1f);
                var c = marker.color;
                c.a = k;
                marker.color = c;
            }
        }
    }
}
