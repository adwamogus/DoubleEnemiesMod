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

    public Action StartBattle;

    public void CopyState(GameObject original, HealthManager healthManager, bool isSharedHPEnabled, EnemyType enemyType)
    {
        this.original = original;
        this.originalHealth = healthManager;

        SongGolemFix();

        BattleSceneFix();

        if (enemyType != EnemyType.Arena)
        {
            cloneHealth = GetComponent<HealthManager>();
            if(cloneHealth == null)
            {
                cloneHealth = GetComponentInChildren<HealthManager>();
            }
            CloneSync sync = cloneHealth.gameObject.AddComponent<CloneSync>();
            sync.Init(this, originalHealth, cloneHealth, isSharedHPEnabled);
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
    private void BattleSceneFix()
    {
        cloneBattleScene = GetComponent<BattleScene>();
        if (cloneBattleScene != null)
        {
            if (gameObject.scene.name.Contains("Memory_Coral_Tower"))
            {
                DoubleEnemiesMod.Log("Coral Tower detected");
                cloneBattleScene.StartBattle();
            }
            else
            {
                CloneMarker marker = original.GetComponent<CloneMarker>();
                if (marker != null)
                {
                    marker.StartBattle += cloneBattleScene.StartBattle;
                    DoubleEnemiesMod.Log("Connected to original BattleScene");
                }
                else
                {
                    DoubleEnemiesMod.Log("original BattleScene hase no marker");
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
