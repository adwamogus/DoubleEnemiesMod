using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CloneSync : MonoBehaviour
{
    private CloneMarker marker;

    private HealthManager originalHealth;
    private HealthManager cloneHealth;

    private PlayMakerFSM originalFSM;
    private PlayMakerFSM cloneFSM;

    private bool isSharedHPEnabled;

    private bool isSynced = false;

    private string lastLoggedState = "";
    public void Init(CloneMarker marker, HealthManager originalHealth, HealthManager cloneHealth, bool isSharedHPEnabled)
    {
        this.marker = marker;
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

            originalHealth.OnDeath += OnDeathOriginal;
            cloneHealth.OnDeath += OnDeathClone;
        }
    }
    private void OnDisable()
    {
        if (isSharedHPEnabled)
        {
            originalHealth.TookDamage -= CloneSharedHPUpdate;
            cloneHealth.TookDamage -= CloneSharedHPUpdate;

            originalHealth.OnDeath -= OnDeathOriginal;
            cloneHealth.OnDeath -= OnDeathClone;
        }
    }
    private void LateUpdate()
    {
        if (isSynced) return;

        //Sync
        var activeStateName = originalFSM.Fsm.ActiveStateName;
        cloneFSM.SetState(activeStateName);
        transform.position = originalHealth.transform.position;

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
        if (originalHealth.isDead|| cloneHealth.isDead)
        {
            return;
        }
        DoubleEnemiesMod.Log($"[SharedHP Event Before] SHP[{gameObject.name}/{cloneHealth.hp}] SHP[{originalHealth.gameObject.name}/{originalHealth.hp}]");
        // Check if Clone has taken more damage
        if (originalHealth.hp > cloneHealth.hp)
        {
            int delta = Mathf.FloorToInt((originalHealth.hp - cloneHealth.hp) / (float)DoubleEnemiesMod.Multiplier.Value);
            originalHealth.ApplyExtraDamage(delta);
            cloneHealth.ApplyExtraDamage(-delta);
        }
        // Check if original has taken more damage
        else if (cloneHealth.hp > originalHealth.hp)
        {
            int delta = Mathf.FloorToInt((cloneHealth.hp - originalHealth.hp) / (float)DoubleEnemiesMod.Multiplier.Value);
            cloneHealth.ApplyExtraDamage(delta);
            originalHealth.ApplyExtraDamage(-delta);
        }
        DoubleEnemiesMod.Log($"[SharedHP Event After] SHP[{gameObject.name}/{cloneHealth.hp}] SHP[{originalHealth.gameObject.name}/{originalHealth.hp}]");
    }
    private void OnDeathOriginal()
    {
        if (!cloneHealth.isDead)
        {
            DoubleEnemiesMod.Log($"[SharedHP Kill] SHP[{gameObject.name}/{cloneHealth.hp}]");
            cloneHealth.ApplyExtraDamage(cloneHealth.hp);
        }
    }
    private void OnDeathClone()
    {
        if (!originalHealth.isDead)
        {
            DoubleEnemiesMod.Log($"[SharedHP Kill] SHP[{originalHealth.gameObject.name}/{originalHealth.hp}]");
            originalHealth.ApplyExtraDamage(originalHealth.hp);
        }
    }
}