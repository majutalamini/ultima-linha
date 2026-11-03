using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Painel de desvios da estação (Cenário 3). Uma grade de trilhos: cada peça gira de 90 em 90 graus.
    /// A corrente sai da PLATAFORMA (à esquerda) e acende cada trilho ligado a ela; quando chega até a ADMINISTRAÇÃO
    /// (à direita), a porta abre. Algumas peças foram arrancadas do painel: o Kai as acha pela estação e elas
    /// entram no lugar quando ele abre o painel.
    /// Teclado: setas/WASD movem, E/Espaço/Enter giram, Q gira para o outro lado, Esc fecha. Mouse: clique gira (direito: ao contrário).
    /// </summary>
    public class SwitchPanelUI : MonoBehaviour
    {
        public const int N = 1, E = 2, S = 4, W = 8;

        public static SwitchPanelUI Instance { get; private set; }

        [Header("Grade (linha por linha, de cima para baixo)")]
        public int columns = 5;
        public int rows = 3;
        [Tooltip("Trilhos de cada peça na posição certa: N=1, L=2, S=4, O=8 (soma).")]
        public int[] masks = new int[0];
        [Tooltip("Quantas vezes cada peça começa girada (sentido horário).")]
        public int[] startTurns = new int[0];
        [Tooltip("Qual peça achada pela estação ocupa a casa (-1: já está no painel).")]
        public int[] pieceOf = new int[0];
        [Tooltip("Linha onde a corrente entra (lado esquerdo).")]
        public int entryRow = 1;
        [Tooltip("Linha onde ela tem que sair (lado direito).")]
        public int exitRow = 2;

        [Header("Interface")]
        public CanvasGroup group;
        public RectTransform grid;
        public RectTransform entryMarker;
        public RectTransform exitMarker;
        public Image exitLamp;
        public TMP_Text status;
        public TMP_FontAsset font;
        public float cellSize = 132f;
        public float gap = 10f;

        [Header("Cores")]
        public Color plate = new Color(0.075f, 0.07f, 0.07f, 1f);
        public Color emptyPlate = new Color(0.03f, 0.028f, 0.028f, 1f);
        public Color rail = new Color(0.34f, 0.32f, 0.29f, 1f);
        public Color live = new Color(1f, 0.68f, 0.28f, 1f);
        public Color done = new Color(0.5f, 1f, 0.6f, 1f);
        public Color cursorColor = new Color(1f, 0.62f, 0.25f, 0.95f);
        public Color idleFrame = new Color(0.3f, 0.28f, 0.25f, 0.8f);

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip turnClip;
        public AudioClip placeClip;
        public AudioClip solvedClip;

        /// <summary>A linha ficou completa (o painel fecha sozinho logo depois).</summary>
        public event Action Solved;

        public bool IsOpen { get; private set; }
        public bool IsSolved { get; private set; }

        class Cell
        {
            public RectTransform root, spin;
            public Image frame, back;
            public readonly List<Image> rails = new List<Image>();
            public Image hub;
            public TMP_Text missing;
            public int turns;
            public float angle;
            public bool present;
            public SwitchTileCell input;
        }

        Cell[] cells;
        bool[] pieces = new bool[0];
        int cursor;
        float openedAt;
        bool busy;
        HashSet<int> powered = new HashSet<int>();

        void Awake()
        {
            Instance = this;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            BuildCells();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsOpen) Modal.Pop();
        }

        // ------------------------------------------------------------------ montagem

        void BuildCells()
        {
            int n = columns * rows;
            if (!grid) { cells = new Cell[0]; return; }
            cells = new Cell[n];
            for (int i = 0; i < n; i++)
            {
                int r = i / columns, c = i % columns;
                var cell = new Cell();
                cell.root = NewRect("Casa" + (i + 1), grid);
                cell.root.anchorMin = cell.root.anchorMax = new Vector2(0f, 1f);
                cell.root.pivot = new Vector2(0.5f, 0.5f);
                cell.root.sizeDelta = new Vector2(cellSize, cellSize);
                cell.root.anchoredPosition = new Vector2(c * (cellSize + gap) + cellSize / 2f, -(r * (cellSize + gap) + cellSize / 2f));

                cell.frame = AddImage(cell.root, idleFrame);
                var backRt = NewRect("Fundo", cell.root);
                Stretch(backRt, 3f);
                cell.back = AddImage(backRt, plate);

                cell.spin = NewRect("Gira", cell.root);
                Stretch(cell.spin, 0f);
                int m = Mask(i, 0);
                float half = cellSize / 2f, thick = cellSize * 0.2f;
                foreach (var dir in new[] { N, E, S, W })
                {
                    if ((m & dir) == 0) continue;
                    var arm = NewRect("Trilho", cell.spin);
                    arm.anchorMin = arm.anchorMax = new Vector2(0.5f, 0.5f);
                    // o trilho vai do centro (um pouco antes, para emendar) até a borda da casa
                    bool vertical = dir == N || dir == S;
                    float len = half + thick / 2f;
                    arm.sizeDelta = vertical ? new Vector2(thick, len) : new Vector2(len, thick);
                    float off = (half - thick / 2f) / 2f;
                    arm.anchoredPosition = dir == N ? new Vector2(0f, off) : dir == S ? new Vector2(0f, -off)
                        : dir == E ? new Vector2(off, 0f) : new Vector2(-off, 0f);
                    cell.rails.Add(AddImage(arm, rail));
                    // dormentes: dois tracinhos escuros atravessando o trilho
                    float sign = dir == N || dir == E ? 1f : -1f;
                    for (int k = 0; k < 2; k++)
                    {
                        var tie = NewRect("Dormente", arm);
                        tie.anchorMin = tie.anchorMax = new Vector2(0.5f, 0.5f);
                        float along = sign * (-len / 2f + (k == 0 ? 0.5f : 0.82f) * len);
                        tie.sizeDelta = vertical ? new Vector2(thick * 1.6f, 6f) : new Vector2(6f, thick * 1.6f);
                        tie.anchoredPosition = vertical ? new Vector2(0f, along) : new Vector2(along, 0f);
                        AddImage(tie, new Color(0.02f, 0.02f, 0.02f, 0.6f));
                    }
                }
                var hubRt = NewRect("Centro", cell.spin);
                hubRt.anchorMin = hubRt.anchorMax = new Vector2(0.5f, 0.5f);
                hubRt.sizeDelta = new Vector2(thick * 1.25f, thick * 1.25f);
                cell.hub = AddImage(hubRt, rail);

                var missingRt = NewRect("Falta", cell.root);
                Stretch(missingRt, 0f);
                cell.missing = missingRt.gameObject.AddComponent<TextMeshProUGUI>();
                cell.missing.font = font;
                cell.missing.fontSize = 22;
                cell.missing.characterSpacing = 8;
                cell.missing.alignment = TextAlignmentOptions.Center;
                cell.missing.color = new Color(0.55f, 0.5f, 0.45f, 0.8f);
                cell.missing.text = "FALTA\nA PEÇA";
                cell.missing.raycastTarget = false;

                cell.input = cell.root.gameObject.AddComponent<SwitchTileCell>();
                cell.input.panel = this;
                cell.input.index = i;
                cell.frame.raycastTarget = true;

                cell.turns = i < startTurns.Length ? startTurns[i] : 0;
                cell.angle = -90f * cell.turns;
                cell.spin.localRotation = Quaternion.Euler(0f, 0f, cell.angle);
                cells[i] = cell;
            }
            PrepareFixed();
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        static Image AddImage(RectTransform rt, Color c)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        // ------------------------------------------------------------------ abrir / fechar

        /// <summary>Abre o painel. "found" diz quais peças o Kai já achou; as novas entram no lugar com um estalo.</summary>
        public void Open(bool[] found)
        {
            if (IsOpen || IsSolved) return;
            pieces = found ?? new bool[0];
            IsOpen = true;
            busy = false;
            openedAt = Time.unscaledTime;
            Modal.Push();
            Modal.SetControl(false);
            if (cells != null && (cursor < 0 || cursor >= cells.Length)) cursor = 0;
            StopAllCoroutines();
            StartCoroutine(Fade(1f, 0.25f));
            if (group)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
            }
            StartCoroutine(PlacePieces());
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (group)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            yield return Fade(0f, 0.25f);
            yield return null;
            Modal.Pop();
            if (!Modal.IsOpen && !DialogueBox.IsBusy) Modal.SetControl(true);
        }

        /// <summary>As peças achadas desde a última vez caem no lugar, uma de cada vez.</summary>
        IEnumerator PlacePieces()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                if (cell.present) continue;
                int p = i < pieceOf.Length ? pieceOf[i] : -1;
                bool has = p < 0 || (p < pieces.Length && pieces[p]);
                if (!has) continue;
                cell.present = true;
                if (p >= 0)
                {
                    if (sfx && placeClip) sfx.PlayOneShot(placeClip, 0.8f);
                    float t = 0f;
                    while (t < 0.22f)
                    {
                        t += Time.unscaledDeltaTime;
                        cell.root.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, t / 0.22f);
                        Refresh();
                        yield return null;
                    }
                    cell.root.localScale = Vector3.one;
                    yield return new WaitForSecondsRealtime(0.12f);
                }
                Refresh();
            }
            Refresh();
            CheckSolved();
        }

        /// <summary>Marca as peças fixas como presentes sem abrir (usado ao montar a grade e ao continuar).</summary>
        public void PrepareFixed()
        {
            if (cells == null) return;
            for (int i = 0; i < cells.Length; i++)
                if (i >= pieceOf.Length || pieceOf[i] < 0) cells[i].present = true;
        }

        // ------------------------------------------------------------------ entrada

        void Update()
        {
            if (cells == null) return;
            // a peça gira suave até o ângulo novo
            foreach (var cell in cells)
            {
                float target = -90f * cell.turns;
                if (!Mathf.Approximately(cell.angle, target))
                {
                    cell.angle = Mathf.MoveTowards(cell.angle, target, 900f * Time.unscaledDeltaTime);
                    cell.spin.localRotation = Quaternion.Euler(0f, 0f, cell.angle);
                }
            }
            if (exitLamp)
            {
                var c = IsSolved ? done : new Color(0.75f, 0.12f, 0.08f, 1f);
                c.a = IsSolved ? 1f : 0.55f + 0.35f * Mathf.Sin(Time.unscaledTime * 3f);
                exitLamp.color = c;
            }

            if (!IsOpen || busy || Time.unscaledTime - openedAt < 0.25f) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            if (k != null)
            {
                if (k.escapeKey.wasPressedThisFrame) { Close(); return; }
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Move(-1, 0);
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Move(1, 0);
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Move(0, -1);
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Move(0, 1);
                if (k.eKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)
                    Turn(cursor, 1);
                if (k.qKey.wasPressedThisFrame) Turn(cursor, -1);
            }
            if (g != null)
            {
                if (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame) { Close(); return; }
                if (g.dpad.left.wasPressedThisFrame) Move(-1, 0);
                if (g.dpad.right.wasPressedThisFrame) Move(1, 0);
                if (g.dpad.up.wasPressedThisFrame) Move(0, -1);
                if (g.dpad.down.wasPressedThisFrame) Move(0, 1);
                if (g.buttonSouth.wasPressedThisFrame) Turn(cursor, 1);
                if (g.buttonWest.wasPressedThisFrame) Turn(cursor, -1);
            }
        }

        void Move(int dx, int dy)
        {
            int r = cursor / columns, c = cursor % columns;
            c = Mathf.Clamp(c + dx, 0, columns - 1);
            r = Mathf.Clamp(r + dy, 0, rows - 1);
            cursor = r * columns + c;
            Refresh();
        }

        /// <summary>Chamado pelo mouse (SwitchTileCell).</summary>
        public void Hover(int index)
        {
            if (!IsOpen || busy) return;
            cursor = index;
            Refresh();
        }

        public void Click(int index, int dir)
        {
            if (!IsOpen || busy || Time.unscaledTime - openedAt < 0.25f) return;
            cursor = index;
            Turn(index, dir);
        }

        void Turn(int index, int dir)
        {
            if (IsSolved || index < 0 || index >= cells.Length) return;
            var cell = cells[index];
            if (!cell.present)
            {
                if (status) status.text = "Falta uma peça aqui. Ela deve estar em algum lugar da estação.";
                return;
            }
            cell.turns = ((cell.turns + dir) % 4 + 4) % 4;
            // o ângulo mostrado continua de onde está (gira sempre pelo caminho curto)
            float target = -90f * cell.turns;
            while (cell.angle - target > 180f) cell.angle -= 360f;
            while (target - cell.angle > 180f) cell.angle += 360f;
            if (sfx && turnClip) sfx.PlayOneShot(turnClip, 0.7f);
            Refresh();
            CheckSolved();
        }

        // ------------------------------------------------------------------ corrente

        int Mask(int i, int turns)
        {
            int m = i < masks.Length ? masks[i] : 0;
            for (int k = 0; k < ((turns % 4) + 4) % 4; k++) m = ((m << 1) | (m >> 3)) & 15;
            return m;
        }

        int Current(int i) => cells[i].present ? Mask(i, cells[i].turns) : 0;

        void Flow()
        {
            powered.Clear();
            int start = entryRow * columns;
            if (start >= cells.Length || (Current(start) & W) == 0) return;
            var stack = new Stack<int>();
            powered.Add(start);
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int r = i / columns, c = i % columns, m = Current(i);
                TryLink(m, N, r - 1, c, S, stack);
                TryLink(m, S, r + 1, c, N, stack);
                TryLink(m, E, r, c + 1, W, stack);
                TryLink(m, W, r, c - 1, E, stack);
            }
        }

        void TryLink(int m, int dir, int r, int c, int back, Stack<int> stack)
        {
            if ((m & dir) == 0 || r < 0 || r >= rows || c < 0 || c >= columns) return;
            int j = r * columns + c;
            if (powered.Contains(j) || (Current(j) & back) == 0) return;
            powered.Add(j);
            stack.Push(j);
        }

        bool AllPresent()
        {
            foreach (var cell in cells) if (!cell.present) return false;
            return true;
        }

        void CheckSolved()
        {
            Flow();
            int exit = exitRow * columns + columns - 1;
            bool reached = powered.Contains(exit) && (Current(exit) & E) != 0;
            if (reached && AllPresent() && !IsSolved) StartCoroutine(SolvedRoutine());
            else UpdateStatus();
        }

        void UpdateStatus()
        {
            if (!status || IsSolved) return;
            int missing = 0;
            foreach (var cell in cells) if (!cell.present) missing++;
            status.text = missing > 0
                ? (missing == 1 ? "Falta 1 peça no painel." : $"Faltam {missing} peças no painel.") + "  Gire os trilhos até a luz chegar à administração."
                : "Gire os trilhos até a luz chegar à administração.";
        }

        IEnumerator SolvedRoutine()
        {
            IsSolved = true;
            busy = true;
            if (status) status.text = "LINHA COMPLETA";
            if (sfx && solvedClip) sfx.PlayOneShot(solvedClip);
            for (int k = 0; k < 3; k++)
            {
                Refresh(done);
                yield return new WaitForSecondsRealtime(0.18f);
                Refresh(live);
                yield return new WaitForSecondsRealtime(0.12f);
            }
            Refresh(done);
            yield return new WaitForSecondsRealtime(0.8f);
            busy = false;
            // fecha sem parar esta rotina (Close() pararia tudo antes de avisar o diretor)
            IsOpen = false;
            if (group)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            yield return CloseRoutine();
            Solved?.Invoke();
        }

        /// <summary>Já resolvido (jogo continuado): só deixa tudo aceso.</summary>
        public void MarkSolved()
        {
            IsSolved = true;
            if (cells == null) return;
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].present = true;
                cells[i].turns = 0;
                cells[i].angle = 0f;
                cells[i].spin.localRotation = Quaternion.identity;
            }
            Flow();
            Refresh(done);
        }

        void Refresh() => Refresh(live);

        void Refresh(Color on)
        {
            if (cells == null) return;
            Flow();
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                bool lit = powered.Contains(i);
                var col = lit ? on : rail;
                cell.spin.gameObject.SetActive(cell.present);
                if (cell.missing) cell.missing.gameObject.SetActive(!cell.present);
                cell.back.color = cell.present ? plate : emptyPlate;
                foreach (var img in cell.rails) img.color = col;
                if (cell.hub) cell.hub.color = col;
                cell.frame.color = IsOpen && i == cursor ? cursorColor : idleFrame;
            }
            if (entryMarker)
                foreach (var img in entryMarker.GetComponentsInChildren<Image>()) img.color = on;
            if (exitMarker)
            {
                int exit = exitRow * columns + columns - 1;
                bool reached = powered.Contains(exit) && (Current(exit) & E) != 0;
                foreach (var img in exitMarker.GetComponentsInChildren<Image>())
                    if (img != exitLamp) img.color = reached ? on : rail;
            }
            if (IsOpen) UpdateStatus();
        }

        IEnumerator Fade(float to, float duration)
        {
            if (!group) yield break;
            float from = group.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
