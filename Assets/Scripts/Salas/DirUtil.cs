using System.Collections;
using System.Collections.Generic;
using UltimaLinha.Cenario1;
using UnityEngine;

namespace UltimaLinha.Salas
{
    /// <summary>Atalhos usados pelos diretores dos Cenários 3, 6 e 7.</summary>
    public static class DirUtil
    {
        public static IEnumerator SayAndWait(List<DialogueLine> lines)
        {
            if (!DialogueBox.Instance || lines == null || lines.Count == 0) yield break;
            bool done = false;
            DialogueBox.Instance.Play(lines, () => done = true);
            while (!done) yield return null;
        }

        public static void Say(List<DialogueLine> lines)
        {
            if (DialogueBox.Instance && lines != null && lines.Count > 0) DialogueBox.Instance.Play(lines);
        }

        public static IEnumerator FadeAudio(AudioSource src, float to, float duration)
        {
            if (!src) yield break;
            float from = src.volume, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            src.volume = to;
        }

        public static IEnumerator FadeGroup(CanvasGroup g, float to, float duration)
        {
            if (!g) yield break;
            float from = g.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            g.alpha = to;
        }

        public static IEnumerator WalkTo(NpcTalk npc, float x, float speed)
        {
            if (!npc) yield break;
            var rig = npc.rig;
            while (Mathf.Abs(npc.transform.position.x - x) > 0.05f)
            {
                float dir = Mathf.Sign(x - npc.transform.position.x);
                if (rig) { rig.facing = dir; rig.externalSpeed = speed; }
                var p = npc.transform.position;
                p.x = Mathf.MoveTowards(p.x, x, speed * Time.deltaTime);
                npc.transform.position = p;
                yield return null;
            }
            if (rig) rig.externalSpeed = 0f;
        }

        public static IEnumerator ReadAll(string[] titles, string[] bodies, string[] footers)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (!DocumentView.Instance) yield break;
                bool closed = false;
                DocumentView.Instance.Show(titles[i], bodies[i], footers[i], () => closed = true);
                while (!closed) yield return null;
            }
        }
    }
}
