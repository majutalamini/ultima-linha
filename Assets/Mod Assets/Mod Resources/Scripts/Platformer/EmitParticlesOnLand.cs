using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

[RequireComponent(typeof(ParticleSystem))]
public class EmitParticlesOnLand : MonoBehaviour
{

    public bool emitOnLand = true;
    public bool emitOnEnemyDeath = true;

#if UNITY_TEMPLATE_PLATFORMER

    ParticleSystem p;

    void OnEnable()
    {
        p = GetComponent<ParticleSystem>();

        if (emitOnLand) {
            Platformer.Gameplay.PlayerLanded.OnExecute += PlayerLanded_OnExecute;
        }

        if (emitOnEnemyDeath) {
            Platformer.Gameplay.EnemyDeath.OnExecute += EnemyDeath_OnExecute;
        }

    }

    private void OnDisable()
    {
        if (emitOnLand) {
            Platformer.Gameplay.PlayerLanded.OnExecute -= PlayerLanded_OnExecute;
        }

        if (emitOnEnemyDeath) {
            Platformer.Gameplay.EnemyDeath.OnExecute -= EnemyDeath_OnExecute;
        }
    }
    
    void PlayerLanded_OnExecute(Platformer.Gameplay.PlayerLanded obj) {
        p.Play();
    }
    
    void EnemyDeath_OnExecute(Platformer.Gameplay.EnemyDeath obj) {
        p.Play();
    }

#endif

}
