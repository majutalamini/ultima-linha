using UnityEngine.InputSystem;

namespace UltimaLinha.Cenario1
{
    /// <summary>Teclas usadas fora do controle de movimento (interagir, avançar diálogo, voltar ao menu).</summary>
    public static class InputHelper
    {
        /// <summary>E no teclado, X/Y (Xbox) no controle.</summary>
        public static bool InteractPressed()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            return (k != null && k.eKey.wasPressedThisFrame)
                   || (g != null && (g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame));
        }

        /// <summary>E, Espaço, Enter ou clique; A/X no controle.</summary>
        public static bool AdvancePressed()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            var g = Gamepad.current;
            return (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame
                                  || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                   || (m != null && m.leftButton.wasPressedThisFrame)
                   || (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame));
        }

        /// <summary>Esc no teclado, Start no controle.</summary>
        public static bool BackPressed()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
        }
    }
}
