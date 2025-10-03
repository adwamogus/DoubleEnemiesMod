using BepInEx;
using BepInEx.Configuration;
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

[BepInPlugin("com.adwamogus.skdoubleenemiesmod", "Silksong Double Enemies Mod", "0.3.0")]
public class DoubleEnemiesMod : BaseUnityPlugin
{
    public static ConfigEntry<int> Multiplier;

    public static ConfigEntry<bool> debugLog;

    private static ManualLogSource logger;
    private void Awake()
    {
        logger = Logger;

        Multiplier = Config.Bind(
            "General",
            "Multiplier",
            2,
            "Enemy * Multiplier = the amount of enemies spawned"
            );

        debugLog = Config.Bind(
            "Debug",
            "DebugLog",
            false,
            "For development"
            );

        Log("Double Enemies Mod loaded");
        Harmony.CreateAndPatchAll(typeof(DoubleEnemiesMod));
    }
    public static void Log(string msg)
    {
        if (debugLog.Value)
        {
            if (logger != null)
                logger.LogInfo(msg);
            else
                BepInEx.Logging.Logger.CreateLogSource("DoubleEnemiesMod").LogInfo(msg);
        }
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
            foreach (var blocked in StringLists.Blacklist)
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
                Log($"[{gameObject.name}] Parent name: {parentName}");
                foreach (var keyword in StringLists.BossParentKeywords) {
                    // Special Treatment for Lace 2 because she bricks the game if we clone the boss scene
                    // Same for Grandmother silk because she is scuffed af without it
                    if (parentName.Contains(keyword) && !gameObject.name.Contains("Lace Boss2 New") && !gameObject.name.Contains("Silk Boss"))
                    {
                        if (current.GetComponent<CloneMarker>() == null)
                        {
                            CloneObject(current.gameObject);
                        }
                        else
                        {
                            Log($"[{gameObject.name}] Parent was already cloned");
                        }
                        return;
                    }
                }
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

        for(int i = 0; i < Multiplier.Value - 1; i++)
        {
            // Create clone
            var clone = GameObject.Instantiate(
                gameObject,
                gameObject.transform.position + Vector3.back * 0.01f,
                gameObject.transform.rotation,
                gameObject.transform.parent
            );
            clone.name += "DECLONE";
            clone.GetComponent<CloneMarker>().CopyState(gameObject);

            // Log clone
            Log($"[Clone] {gameObject.name} -> {clone.name} in scene {gameObject.gameObject.scene.name}");
        }
    }
}

// component for clone detection
public class CloneMarker : MonoBehaviour
{
    private GameObject original;

    private bool isSynced = false;

    private string lastLoggedState = "";

    public void CopyState(GameObject original)
    {
        this.original = original;
    }
    // Sync loop for enemies who have long intro animations
    private void LateUpdate()
    {
        if (isSynced || original == null) return;

        //Sync
        var activeStateName = original.GetComponent<PlayMakerFSM>()?.Fsm.ActiveStateName;
        GetComponent<PlayMakerFSM>()?.SetState(activeStateName);
        transform.position = original.transform.position;

        if(activeStateName == null)
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
            EnsureCollider();
            DoubleEnemiesMod.Log($"[{gameObject.name}] Stopped syncing: {activeStateName}");
        }
    }
    public void EnsureCollider()
    {
        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = GetComponentInChildren<Collider2D>();
        }
        if (collider == null)
        {
            var circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = false;
            circle.radius = 1f;

            DoubleEnemiesMod.Log($"[{gameObject.name}] No collider found -> CircleCollider2D added");
        }
    }
}
public static class StringLists
{
    public static readonly string[] Blacklist = new string[]
    {
        "Coral Warrior",
        "Coral Flyer",
    };
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
        "Take Control", // Lace 1
        "Init", // Grandmother
        "Ready",
    };
    public static readonly string[] BossParentKeywords = new string[]
    {
        "Dancer Control",
        "Boss Scene",
        "Battle Scene",
    };
}
