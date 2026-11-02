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
    /// <summary>Conta quantas janelas (documento, cadeado) estão abertas: enquanto houver, o Kai não interage com nada.</summary>
    public static class Modal
    {
        public static int Count { get; private set; }
        public static bool IsOpen => Count > 0;

        public static void Push() => Count++;
        public static void Pop() => Count = Mathf.Max(0, Count - 1);
        public static void Reset() => Count = 0;

        /// <summary>Liga ou desliga o controle do Kai (só se houver jogador).</summary>
        public static void SetControl(bool enabled)
        {
            var model = Simulation.GetModel<PlatformerModel>();
            if (model != null && model.player) model.player.controlEnabled = enabled;
        }
    }

    /// <summary>
    /// Itens e fatos que passam de um cenário para o outro (guardados no PlayerPrefs):
    /// a alavanca que falta (achada no Cenário 5) e a chave de cada sala.
    /// </summary>
    public static class GameFlags
    {
        const string Prefix = "UltimaLinha.Flag.";

        public const string MissingLever = "alavanca_faltando";
        public const string Cenario4Key = "chave_cenario4";
        public const string Cenario5Key = "chave_cenario5";
        public const string NewsRead = "jornal_lido";

        public static bool Get(string flag) => PlayerPrefs.GetInt(Prefix + flag, 0) == 1;

        public static void Set(string flag, bool value = true)
        {
            PlayerPrefs.SetInt(Prefix + flag, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            foreach (var f in new[] { MissingLever, Cenario4Key, Cenario5Key, NewsRead })
                PlayerPrefs.DeleteKey(Prefix + f);
            PlayerPrefs.Save();
        }
    }
}
