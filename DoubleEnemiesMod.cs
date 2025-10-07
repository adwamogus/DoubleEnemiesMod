using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System;
using UnityEngine;

[BepInPlugin("com.adwamogus.skdoubleenemiesmod", "Silksong Double Enemies Mod", "0.4.5")]
public class DoubleEnemiesMod : BaseUnityPlugin
{
    public static ConfigEntry<int> Multiplier;

    public static ConfigEntry<bool> debugLog;

    public static ConfigEntry<bool> EnableEnemies;
    public static ConfigEntry<bool> EnableArenas;
    public static ConfigEntry<bool> EnableBosses;

    public static ConfigEntry<bool> EnableSharedHP;

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

        EnableEnemies = Config.Bind(
            "Control",
            "Enable Enemies",
            true,
            "Enables cloning of all normal enemies."
            );

        EnableArenas = Config.Bind(
            "Control",
            "Enable Arenas",
            true,
            "Enables cloning of all arenas."
            );

        EnableBosses = Config.Bind(
            "Control",
            "Enable Bosses",
            true,
            "Enables cloning of all bosses."
            );

        EnableSharedHP = Config.Bind(
            "Control",
            "Enable Shared HP",
            true,
            "Multiplies boss hp with the Multiplier and shares all damage between bosses. Supported bosses will now die at the same time."
            );

        Logger.LogInfo("Double Enemies Mod loaded");
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
                if (gameObject.name.Contains(blocked))
                {
                    Log($"[Blacklist] Skipped: {gameObject.name} ({gameObject.name}) in scene {gameObject.scene.name}");
                    return;
                }
            }

            var current = gameObject.transform;
            while (current != null)
            {
                string parentName = current.gameObject.name;
                Log($"[{gameObject.name}] Parent name: {parentName}");
                foreach (var keyword in StringLists.ParentKeywords) {
                    // sister splinter arena funny moment
                    if (parentName.Contains(keyword) && !gameObject.scene.name.Contains("Shellwood_18"))
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
        if (!CheckEnemyEnabled(gameObject.name))
        {
            return;
        }

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
    private static bool CheckEnemyEnabled(string gameObjectName)
    {
        EnemyType type = GetEnemyType(gameObjectName);
        switch (type)
        {
            case EnemyType.Enemy:
                if (EnableEnemies.Value)
                {
                    return true;
                }
                else
                {
                    Log($"[{gameObjectName}] was not cloned due to config settings (Enemy)");
                    return false;
                }
            case EnemyType.Arena:
                if (EnableArenas.Value)
                {
                    return true;
                }
                else
                {
                    Log($"[{gameObjectName}] was not cloned due to config settings (Arena)");
                    return false;
                }
            case EnemyType.Boss:
                if (EnableBosses.Value)
                {
                    return true;
                }
                else
                {
                    Log($"[{gameObjectName}] was not cloned due to config settings (Boss)");
                    return false;
                }
        }
        Log("$[{gameObjectName}] Invalid enemy type exception");
        return false;
    }
    private static EnemyType GetEnemyType(string gameObjectName)
    {
        // Arena Check
        foreach (var keyword in StringLists.ArenaFilterKeywords)
        {
            if (gameObjectName.Contains(keyword))
            {
                return EnemyType.Arena;
            }
        }
        // Boss Check
        foreach (var keyword in StringLists.BossFilterKeywords)
        {
            if (gameObjectName.Contains(keyword))
            {
                return EnemyType.Boss;
            }
        }
        // Enemy Check
        return EnemyType.Enemy;
    } 
}