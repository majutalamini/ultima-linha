using System;
using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using TMPro;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Alavanca do painel de energia. Começa abaixada; E levanta e ela fica levantada.
    /// </summary>
    public class PowerLever : Interactable
    {
        public Transform handle;
        public float upAngle = 38f;
        public float downAngle = -38f;
        public SpriteRenderer lamp;
        public SpriteRenderer lampGlow;
        public AudioSource sfx;
        public AudioClip pullClip;
        public Color offColor = new Color(0.55f, 0.08f, 0.06f, 1f);
        public Color onColor = new Color(0.45f, 1f, 0.55f, 1f);

        public bool IsUp { get; private set; }
        [NonSerialized] public Action<PowerLever> Raised;

        bool moving;

        public override bool CanInteract => base.CanInteract && !IsUp && !moving && !DialogueBox.IsBusy && !Modal.IsOpen;

        void Start()
        {
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, downAngle);
            SetLamp(offColor, 0.35f);
        }

        void Update()
        {
            if (!IsUp && lamp) SetLamp(offColor, 0.2f + 0.15f * Mathf.Sin(Time.time * 2.4f + transform.position.x));
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            StartCoroutine(Pull());
        }

        IEnumerator Pull()
        {
            moving = true;
            if (sfx && pullClip) sfx.PlayOneShot(pullClip);
            float t = 0f, d = 0.45f;
            while (t < d)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / d);
                if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(downAngle, upAngle, k));
                yield return null;
            }
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, upAngle);
            IsUp = true;
            moving = false;
            SetLamp(onColor, 0.6f);
            Raised?.Invoke(this);
        }

        void SetLamp(Color c, float glow)
        {
            c.a = 1f;
            if (lamp) lamp.color = c;
            if (lampGlow)
            {
                c.a = glow;
                lampGlow.color = c;
            }
        }
    }
}
