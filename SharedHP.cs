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
using static DamageTag;

public static class SharedHPManager
{
    public static Dictionary<int, SharedHPInstance> SharedHpInstances = new Dictionary<int, SharedHPInstance>();
    private static int id = 0;

    public static void Connect(HealthManager original, HealthManager clone)
    {
        SharedHPID sharedHPID = original.GetComponent<SharedHPID>();

        if (sharedHPID == null)
        {
            int instanceID = id;
            id++;

            SharedHpInstances.Add(instanceID, new SharedHPInstance(original));
            sharedHPID = AddSharedHPID(original, instanceID);

            DoubleEnemiesMod.Log($"[SharedHPManager] Created new SharedHPInstance ({original.gameObject.name})");
        }

        SharedHpInstances[sharedHPID.ID].AddToList(clone);
        AddSharedHPID(clone, sharedHPID.ID);
    }
    public static void Clear()
    {
        if (!SharedHpInstances.IsNullOrEmpty())
        {
            DoubleEnemiesMod.Log("[SharedHPManager] Cleared all SharedHPInstances");
            SharedHpInstances.Clear();
        }
    }
    public static SharedHPID AddSharedHPID(HealthManager healthManager, int id)
    {
        SharedHPID idComponent = healthManager.gameObject.AddComponent<SharedHPID>();
        idComponent.ID = id;
        return idComponent;
    }
    public static void ReportDeath(HealthManager healthManager, int id)
    {
        if (IsDead(healthManager.hp))
        {
            SharedHpInstances[id].DeathSync();
        }
        
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
        float sum = -residualDamage;

        foreach (HealthManager hp in hpComponents)
        {
            sum += hp.hp;
        }

        float average = sum / DoubleEnemiesMod.Multiplier.Value;

        int targetHP = Mathf.CeilToInt(average);

        residualDamage = (targetHP - average) * DoubleEnemiesMod.Multiplier.Value;

        foreach (HealthManager hp in hpComponents)
        {
            hp.ApplyExtraDamage(hp.hp - targetHP);
        }

        DoubleEnemiesMod.Log($"[SharedHpInstance/{name}] HP distributed: targetHP/{targetHP}, residualDamage/{residualDamage}");
    }
    public void DeathSync()
    {
        foreach (HealthManager hp in hpComponents)
        {
            if (!SharedHPManager.IsDead(hp.hp))
            {
                DamageTagInstance damageTag = new DamageTagInstance();
                damageTag.isHeroDamage = true;
                damageTag.amount = hp.hp;
                damageTag.specialDamageType = SpecialDamageTypes.None;
                damageTag.nailElements = NailElements.None;

                hp.ApplyTagDamage(damageTag);
            }
        }
    }
}

public class SharedHPID : MonoBehaviour
{
    public int ID;
}