using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.SceneManagement;

[BepInPlugin("com.adwamogus.skdoubleenemiesmod", "Silksong Double Enemies Mod", "0.2.0")]
public class DoubleEnemiesMod : BaseUnityPlugin
{
    private static readonly string[] Blacklist = new string[]
    {
        "Coral Warrior",
        "Coral Flyer",
    };
    private static ManualLogSource logger;
    private void Awake()
    {
        logger = Logger;
        Log("Double Enemies Mod loaded");

        Harmony.CreateAndPatchAll(typeof(DoubleEnemiesMod));

    }
    public static void Log(string msg)
    {
        if (logger != null)
            logger.LogInfo(msg);
        else
            BepInEx.Logging.Logger.CreateLogSource("DoubleEnemiesMod").LogInfo(msg);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(HealthManager), "Start")]
    private static void OnHealthManagerEnabled(HealthManager __instance)
    {
        if (__instance == null) return;
        TryDuplicateInstance(__instance);
    }
    private static void TryDuplicateInstance(HealthManager healthManager)
    {
        try
        {
            GameObject gameObject = healthManager.gameObject;
            // Ignore if already a clone
            if (gameObject.GetComponent<CloneMarker>() != null || gameObject.GetComponentInParent<CloneMarker>() != null) return;

            // Blacklist check
            foreach (var blocked in Blacklist)
            {
                if (gameObject.name.Contains(blocked) || gameObject.name.Contains(blocked))
                {
                    Log($"[Blacklist] Skipped: {gameObject.name} ({gameObject.name}) in scene {gameObject.scene.name}");
                    return;
                }
            }

            var current = gameObject.transform.parent;
            while (current != null)
            {
                string parentName = current.gameObject.name;
                foreach (var keyword in StringLists.BossParentKeywords) {
                    if (parentName.Contains(keyword))
                    {
                        Log("OMG WUNTERKINT");
                        CloneObject(current.gameObject);
                        return;
                    }
                }
                Log($"[{gameObject.name}] Parent name: {parentName}");

                current = current.parent;
            }

            CloneObject(gameObject);
        }
        catch (Exception ex)
        {
            Log($"[Error] Error while duplicating {healthManager.gameObject?.name}: {ex}");
        }
    }
    private static void CloneObject(GameObject gameObject)
    {
        // Mark the original object before cloning
        gameObject.AddComponent<CloneMarker>();

        // Create clone
        var clone = GameObject.Instantiate(
            gameObject,
            gameObject.transform.position + Vector3.back * 0.01f,
            gameObject.transform.rotation,
            gameObject.transform.parent
        );
        clone.GetComponent<CloneMarker>().CopyState(gameObject, logger);

        // Log clone
        Log($"[Clone] {gameObject.name} -> {clone.name} in scene {gameObject.gameObject.scene.name}");
    }
}

// component for clone detection
public class CloneMarker : MonoBehaviour
{
    private GameObject original;
    private ManualLogSource logger;

    private bool isSynced = false;

    private string lastLoggedState = "";

    public void CopyState(GameObject original, ManualLogSource logger)
    {
        this.original = original;
        this.logger = logger;
        
    }
    // Sync loop for enemies who have long intro animations
    private void Update()
    {
        if (isSynced || original == null) return;

        //Sync
        var activeStateName = original.GetComponent<PlayMakerFSM>().Fsm.ActiveStateName;
        GetComponent<PlayMakerFSM>().SetState(activeStateName);
        transform.position = original.transform.position;


        if (logger != null && activeStateName != lastLoggedState)
        {
            logger.LogInfo($"[{gameObject.name}] Current state: {activeStateName}");
            lastLoggedState = activeStateName;
        }

        bool found = false;
        foreach(var state in StringLists.SyncStates)
        {
            if (state == activeStateName)
            {
                found = true;
            }
        }
        if (!found)
        {
            isSynced = true;
            logger?.LogInfo($"[{gameObject.name}] Stopped syncing: {activeStateName}");
        }
    }
}

public static class StringLists
{
    public static readonly string[] SyncStates = new string[]
    {
        "Pause", // Lost Lace
        "Dormant",
        "Zoom Down", // Moorwing
        "Spear Spawn Pause", // Coral Tower Spawn
        "Burst Out",
        "Fly In",
        "Jump Away Air",
        "Burst Out?",
        "Spawn Antic",
        "Spawn",
        "BG Dance", // Skarrsinger
        "Challenge Pause",
        "Battle Roar Antic",
        "Battle Roar",
        "Battle Roar End",
        "Battle Dance",
        //"Clover Ready", // Clover Dancer
        //"",
        //"Clover Sub Roar",
        //"Clover Roar",
        //"Idle",
        //"Rest", // Cogwork Dancer
        //"Windup Ready",
        //"Windup",
        //"Emerge",
        //"Do Roar",
        //"Sub roar",
        //"Take Control" // Lace 1
    };
    public static readonly string[] BossParentKeywords = new string[]
    {
        "Dancer Control",
        "Boss Scene",
    };
}
