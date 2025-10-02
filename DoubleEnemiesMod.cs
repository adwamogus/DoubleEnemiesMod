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
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.SceneManagement;

[BepInPlugin("com.adwamogus.skdoubleenemiesmod", "Silksong Double Enemies Mod", "0.1.0")]
public class DoubleEnemiesMod : BaseUnityPlugin
{
    private static readonly string[] Blacklist = new string[]
    {
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
    [HarmonyPatch(typeof(HealthManager), "OnEnable")]
    private static void OnHealthManagerEnabled(HealthManager __instance)
    {
        if (__instance == null) return;
        TryDuplicateInstance(__instance);
    }
    private static void TryDuplicateInstance(HealthManager healthManager)
    {
        try
        {
            // Ignore if already a clone
            if (healthManager.GetComponent<CloneMarker>() != null) return;

            // Blacklist check
            foreach (var blocked in Blacklist)
            {
                if (healthManager.name.Contains(blocked) || healthManager.gameObject.name.Contains(blocked))
                {
                    Log($"[Blacklist] Skipped: {healthManager.name} ({healthManager.gameObject.name}) in scene {healthManager.gameObject.scene.name}");
                    return;
                }
            }

            // Mark the original object before cloning
            healthManager.gameObject.AddComponent<CloneMarker>();

            // Create clone
            var clone = GameObject.Instantiate(
                healthManager.gameObject,
                healthManager.transform.position + Vector3.right * 0.05f + Vector3.back * 0.01f, 
                healthManager.transform.rotation,
                healthManager.transform.parent
            );

            // Log clone
            Log($"[Clone] {healthManager.name} -> {clone.name} in scene {healthManager.gameObject.scene.name}");
        }
        catch (Exception ex)
        {
            Log($"[Error] Error while duplicating {healthManager?.name}: {ex}");
        }
    }
}

// Empty component for clone detection
public class CloneMarker : MonoBehaviour
{

}
