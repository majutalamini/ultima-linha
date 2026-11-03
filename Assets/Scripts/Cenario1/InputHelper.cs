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

        /// <summary>Espaço, Enter ou clique; A no controle. O E fica só para interagir: quem abre a conversa não pula a fala.</summary>
        public static bool AdvancePressed()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            var g = Gamepad.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame
                                  || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                   || (m != null && m.leftButton.wasPressedThisFrame)
                   || (g != null && g.buttonSouth.wasPressedThisFrame);
        }

        /// <summary>Shift no teclado, gatilho ou ombro direito no controle (segurar).</summary>
        public static bool RunHeld()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed))
                   || (g != null && (g.rightTrigger.isPressed || g.rightShoulder.isPressed));
        }

        /// <summary>Tab no teclado, Select (View) no controle: abre e fecha o diário.</summary>
        public static bool DiaryPressed()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            return (k != null && k.tabKey.wasPressedThisFrame) || (g != null && g.selectButton.wasPressedThisFrame);
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
