using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimaLinha.Salas
{
    /// <summary>Uma casa do painel de desvios: o mouse em cima seleciona, o clique gira (botão direito gira ao contrário).</summary>
    public class SwitchTileCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        public SwitchPanelUI panel;
        public int index;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (panel) panel.Click(index, eventData.button == PointerEventData.InputButton.Right ? -1 : 1);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (panel) panel.Hover(index);
        }
    }
}
