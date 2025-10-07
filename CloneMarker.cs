using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System;
using UnityEngine;

public class CloneMarker : MonoBehaviour
{
    private GameObject original;

    private HealthManager originalHealth;
    private HealthManager cloneHealth;

    private BattleScene originalBattleScene;
    private BattleScene cloneBattleScene;

    private bool isSynced = false;
    private bool isClone = false;

    public bool isSharedHPEnabled;

    private string lastLoggedState = "";

    public void CopyState(GameObject original, HealthManager healthManager)
    {
        this.original = original;
        this.originalHealth = healthManager;
        isClone = true;

        SongGolemFix();

        CoralTowerFix();

        if (isSharedHPEnabled)
        {
            cloneHealth = GetComponent<HealthManager>();
            if(cloneHealth == null)
            {
                cloneHealth = GetComponentInChildren<HealthManager>();
            }

            originalHealth.TookDamage += CloneSharedHPUpdate;
            cloneHealth.TookDamage += CloneSharedHPUpdate;
        }
    }
    private void LateUpdate()
    {
        if (isSynced || !isClone) return;

        //Sync
        var activeStateName = original.GetComponent<PlayMakerFSM>()?.Fsm.ActiveStateName;
        GetComponent<PlayMakerFSM>()?.SetState(activeStateName);
        transform.position = original.transform.position;

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
    private void SongGolemFix()
    {
        if (original.name.Contains("song_golem"))
        {
            foreach (var heroDamager in Resources.FindObjectsOfTypeAll<DamageHero>())
            {
                if (heroDamager.gameObject.name == "Lava Rock Damager")
                {
                    UnityEngine.Object.DestroyImmediate(heroDamager);
                    DoubleEnemiesMod.Log("Destroyed Rock Damager");
                }
            }
        }
    }
    private void CoralTowerFix()
    {
        // Coral Tower Arena fix
        // yeah this code is messy but Coral tower is annoying
        originalBattleScene = this.original.GetComponent<BattleScene>();
        if (originalBattleScene == null)
        {
            originalBattleScene = this.original.GetComponentInChildren<BattleScene>();
        }
        if (originalBattleScene != null)
        {
            DoubleEnemiesMod.Log($"BattleScene component found in {originalBattleScene.gameObject.name}");
            cloneBattleScene = GetComponent<BattleScene>();
            if (cloneBattleScene == null)
            {
                cloneBattleScene = GetComponentInChildren<BattleScene>();
            }

            if (gameObject.scene.name.Contains("Memory_Coral_Tower"))
            {
                DoubleEnemiesMod.Log("Coral Tower detected");
                cloneBattleScene.StartBattle();
            }
        }
    }
    private void CloneSharedHPUpdate()
    {
        // Check if Clone has taken more damage
        if (originalHealth.hp > cloneHealth.hp)
        {
            int delta = originalHealth.hp - cloneHealth.hp;
            DoubleEnemiesMod.Log($"SHP[{gameObject.name}/{cloneHealth.hp}] Apply {delta} to SHP[{original.gameObject.name}/{originalHealth.hp}]");
            originalHealth.ApplyExtraDamage(delta);
        }
        // Check if original has taken more damage
        else if (cloneHealth.hp > originalHealth.hp)
        {
            int delta = cloneHealth.hp - originalHealth.hp;
            DoubleEnemiesMod.Log($"SHP[{gameObject.name}/{cloneHealth.hp}] Take {delta} from SHP[{original.gameObject.name}/{originalHealth.hp}]");
            cloneHealth.ApplyExtraDamage(delta);
        }
    }

    private void LogAllComponents()
    {
        DoubleEnemiesMod.Log($"--- Components on {gameObject.name} ---");

        Component[] components = gameObject.GetComponents<Component>();

        foreach (Component comp in components)
        {
            if (comp != null)
                DoubleEnemiesMod.Log(comp.GetType().Name);
            else
                DoubleEnemiesMod.Log("Missing (null) Component found!");
        }

        foreach (Transform child in transform)
        {
            DoubleEnemiesMod.Log($"[{gameObject.name}] has child: {child.gameObject.name}");
            components = child.GetComponents<Component>();

            DoubleEnemiesMod.Log($"--- Components on {child.name} ---");

            foreach (Component comp in components)
            {
                if (comp != null)
                    DoubleEnemiesMod.Log(comp.GetType().Name);
                else
                    DoubleEnemiesMod.Log("Missing (null) Component found!");
            }
        }
    }
    private static string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}
