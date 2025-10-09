using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CloneSync : MonoBehaviour
{
    private HealthManager originalHealth;
    private HealthManager cloneHealth;

    private PlayMakerFSM originalFSM;
    private PlayMakerFSM cloneFSM;

    private bool isSynced = false;

    private bool alwaysSync = false;

    private string lastLoggedState = "";
    public void Init(HealthManager originalHealth, HealthManager cloneHealth, bool isSharedHPEnabled)
    {
        this.originalHealth = originalHealth;
        this.cloneHealth = cloneHealth;

        originalFSM = originalHealth.GetComponent<PlayMakerFSM>();
        cloneFSM = cloneHealth.GetComponent<PlayMakerFSM>();

        if (originalFSM == null || cloneFSM == null)
        {
            isSynced = true;
        }

        if (isSharedHPEnabled)
        {
            originalHealth.TookDamage += CloneSharedHPUpdate;
            cloneHealth.TookDamage += CloneSharedHPUpdate;
        }

        if (gameObject.name.Contains("Zap Core Enemy"))
        {
            alwaysSync = true;
            DoubleEnemiesMod.Log($"[{gameObject.name}] AlwaysSync activated");
        }
    }
    private void LateUpdate()
    {
        if (isSynced) return;

        //Sync
        var activeStateName = originalFSM.Fsm.ActiveStateName;
        cloneFSM.SetState(activeStateName);
        transform.position = originalHealth.transform.position;

        if (alwaysSync)
        {
            return;
        }

        if (activeStateName == null)
        {
            return;
        }

        if (activeStateName != lastLoggedState)
        {
            DoubleEnemiesMod.Log($"[{gameObject.name}] Current state: {activeStateName}");
            lastLoggedState = activeStateName;
        }

        bool found = false;
        foreach (var state in StringLists.SyncStates)
        {
            if (state == activeStateName)
            {
                found = true;
            }
        }
        if (!found)
        {
            isSynced = true;
            DoubleEnemiesMod.Log($"[{gameObject.name}] Stopped syncing: {activeStateName}");
        }
    }
    private void CloneSharedHPUpdate()
    {
        // Check if Clone has taken more damage
        if (originalHealth.hp > cloneHealth.hp)
        {
            int delta = Mathf.FloorToInt((originalHealth.hp - cloneHealth.hp) / (float)DoubleEnemiesMod.Multiplier.Value);
            DoubleEnemiesMod.Log($"SHP[{gameObject.name}/{cloneHealth.hp}] Apply {delta} to SHP[{originalHealth.gameObject.name}/{originalHealth.hp}]");
            originalHealth.ApplyExtraDamage(delta);
            cloneHealth.ApplyExtraDamage(-delta);
        }
        // Check if original has taken more damage
        else if (cloneHealth.hp > originalHealth.hp)
        {
            int delta = Mathf.FloorToInt((cloneHealth.hp - originalHealth.hp) / (float)DoubleEnemiesMod.Multiplier.Value);
            DoubleEnemiesMod.Log($"SHP[{gameObject.name}/{cloneHealth.hp}] Take {delta} from SHP[{originalHealth.gameObject.name}/{originalHealth.hp}]");
            cloneHealth.ApplyExtraDamage(delta);
            originalHealth.ApplyExtraDamage(-delta);
        }
    }
}