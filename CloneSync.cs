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
            SharedHPManager.Connect(originalHealth, cloneHealth);

            // We are doing this here so we can ensure that the clone has spawned first
            if(gameObject.name.Contains("Last Judge"))
            {
                cloneHealth.TookDamage += OnLastJudgeDamaged;
            }

            HealthManagerEvents.OnDie += OnDeathOriginal;
            HealthManagerEvents.OnDie += OnDeathClone;
        }
    }
    private void OnDisable()
    {
        if (isSharedHPEnabled)
        {
            HealthManagerEvents.OnDie -= OnDeathOriginal;
            HealthManagerEvents.OnDie -= OnDeathClone;
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
    private void OnDeathOriginal(HealthManager sourceHealthManager)
    {
        if (originalHealth.GetInstanceID() == sourceHealthManager.GetInstanceID())
        {
            if (!HealthManagerEvents.IsDead(cloneHealth.hp))
            {
                DoubleEnemiesMod.Log($"[SharedHP Kill] SHP[{gameObject.name}/{cloneHealth.hp}]");
                cloneHealth.ApplyExtraDamage(cloneHealth.hp);
            }
        }
    }
    private void OnDeathClone(HealthManager sourceHealthManager)
    {
        if (cloneHealth.GetInstanceID() == sourceHealthManager.GetInstanceID())
        {
            if (!HealthManagerEvents.IsDead(originalHealth.hp))
            {
                DoubleEnemiesMod.Log($"[SharedHP Kill] SHP[{originalHealth.gameObject.name}/{originalHealth.hp}]");
                originalHealth.ApplyExtraDamage(originalHealth.hp);
            }
        }
    }
    private void OnLastJudgeDamaged()
    {
        marker.LastJudgeFix();
        cloneHealth.TookDamage -= OnLastJudgeDamaged;
    }
}

public static class HealthManagerEvents
{
    public static Action<HealthManager> OnDie;

    public static void RaiseOnDie(HealthManager hm)
    {
        OnDie?.Invoke(hm);
    }
    public static bool IsDead(int currentHP)
    {
        if (currentHP <= 0 || currentHP == 99999)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}