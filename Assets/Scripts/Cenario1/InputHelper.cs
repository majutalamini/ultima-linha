using UnityEngine;
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

        /// <summary>E, Espaço, Enter ou clique; A no controle. Quem usa (diálogo, papéis) espera um instante depois de
        /// abrir, então o E que abriu a conversa ou o papel não pula a primeira fala nem fecha o papel na hora.</summary>
        public static bool AdvancePressed()
        {
            var k = Keyboard.current;
            var m = Mouse.current;
            var g = Gamepad.current;
            return (k != null && (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame
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

        /// <summary>Esc no teclado, Start no controle. Falso no quadro em que alguém já usou o Esc (ex.: fechar o diário),
        /// para o mesmo Esc não voltar ao menu.</summary>
        public static bool BackPressed()
        {
            if (Time.frameCount == backConsumedFrame) return false;
            var k = Keyboard.current;
            var g = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.startButton.wasPressedThisFrame);
        }

        static int backConsumedFrame = -1;

        /// <summary>O Esc deste quadro já foi usado: ninguém mais reage a ele.</summary>
        public static void ConsumeBack() => backConsumedFrame = Time.frameCount;

        /// <summary>Troca nas dicas da tela o "ESPAÇO" de avançar/fechar por "E / ESPAÇO" (as cenas já montadas mostram só ESPAÇO).</summary>
        public static void FixHint(TMPro.TMP_Text hint)
        {
            if (!hint || hint.text == null || !hint.text.StartsWith("ESPAÇO")) return;
            hint.text = "E / " + hint.text;
            hint.textWrappingMode = TMPro.TextWrappingModes.NoWrap; // cresce para o lado do alinhamento, sem quebrar a linha
        }
    }
}
