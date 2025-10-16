using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SharedHPManager
{
    public static Dictionary<int, SharedHPInstance> SharedHpInstances = new Dictionary<int, SharedHPInstance>();
    private static int id = 0;

    public static void Connect(HealthManager original, HealthManager clone)
    {
        int originalID = id;
        int cloneID = id + 1;

        id += 2;

        if (!SharedHpInstances.ContainsKey(originalID))
        {
            DoubleEnemiesMod.Log($"[SharedHPManager] Created new SharedHPInstance ({original.gameObject.name})");
            SharedHpInstances.Add(originalID, new SharedHPInstance(original));
        }

        SharedHpInstances.Add(cloneID, SharedHpInstances[originalID]);
        SharedHpInstances[cloneID].AddToList(clone);
    }
    public static void Clear()
    {
        if (!SharedHpInstances.IsNullOrEmpty())
        {
            DoubleEnemiesMod.Log("[SharedHPManager] Cleared all SharedHPInstances");
            SharedHpInstances.Clear();
            id = 0;
        }
    }
}

public class SharedHPInstance
{
    private List<HealthManager> hpComponents;

    private float residualDamage = 0f;

    private string name;

    public SharedHPInstance(HealthManager original)
    {
        name = original.gameObject.name;
        hpComponents = new List<HealthManager>();
        AddToList(original);
    }
    public void AddToList(HealthManager healthManager)
    {
        hpComponents.Add(healthManager);
        healthManager.TookDamage += Update;
    }
    public void Update()
    {
        DoubleEnemiesMod.Log($"[SharedHpInstance/{name}] Triggered update");

        if (hpComponents.Count != DoubleEnemiesMod.Multiplier.Value)
        {
            DoubleEnemiesMod.Log($"[SharedHpInstance/{name}] The incorrect amount of enemies are registered ({hpComponents.Count})");
            return;
        }

        DistributeHP();

    }
    private void DistributeHP()
    {
        float sum = residualDamage;

        foreach (HealthManager hp in hpComponents)
        {
            sum += hp.hp;
        }

        float average = sum / DoubleEnemiesMod.Multiplier.Value;

        float targetHP = Mathf.CeilToInt(average);

        residualDamage = (targetHP - average) * DoubleEnemiesMod.Multiplier.Value;

        foreach (HealthManager hp in hpComponents)
        {
            // setting hp doesn't always work for some reason
            hp.ApplyExtraDamage(hp.hp - (int)targetHP);
        }

        DoubleEnemiesMod.Log($"[SharedHpInstance/{name}] HP distributed: targetHP/{targetHP}, residualDamage/{residualDamage}");
    }
}