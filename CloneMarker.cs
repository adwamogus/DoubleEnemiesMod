using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System;
using System.Linq;
using UnityEngine;

public class CloneMarker : MonoBehaviour
{
    private GameObject original;
    private HealthManager originalHealth;

    private HealthManager cloneHealth;

    private BattleScene cloneBattleScene;

    public Action StartBattle;

    public void CopyState(GameObject original, HealthManager healthManager, bool isSharedHPEnabled, EnemyType enemyType)
    {
        this.original = original;
        this.originalHealth = healthManager;

        SongGolemFix();

        DeleteMossBerry();

        if (enemyType != EnemyType.Arena)
        {
            cloneHealth = GetComponent<HealthManager>();

            if(cloneHealth == null)
            {
                var cloneHealths = GetComponentsInChildren<HealthManager>();
                cloneHealths = cloneHealths.OrderBy(c => c.name)
                                       .ToArray();

                var originalHealths = original.GetComponentsInChildren<HealthManager>();
                originalHealths = originalHealths.OrderBy(c => c.name)
                                       .ToArray();

                if (originalHealths.Length != cloneHealths.Length)
                {
                    DoubleEnemiesMod.Log($"[{gameObject.name}] Non equal amount of Healthcomponents ({originalHealths.Length},{cloneHealths.Length})");
                    return;
                }

                for (int i = 0; i < originalHealths.Length; i++)
                {
                    SyncPair(originalHealths[i], cloneHealths[i], isSharedHPEnabled);
                }
            }
            else
            {
                SyncPair(originalHealth, cloneHealth, isSharedHPEnabled);
            }
        }
        else
        {
            BattleSceneFix();
        }
    }
    private void SyncPair(HealthManager _originalHealth, HealthManager _cloneHealth, bool isSharedHPEnabled)
    {
        if (!_cloneHealth.gameObject.name.Contains(_originalHealth.gameObject.name))
        {
            DoubleEnemiesMod.Log($"[{_originalHealth.gameObject.name}] {_originalHealth.gameObject.name} and {_cloneHealth.gameObject.name} do not share a name");
            return;
        }

        foreach (var blocked in StringLists.SyncBlacklist)
        {
            if (_originalHealth.gameObject.name.Contains(blocked))
            {
                DoubleEnemiesMod.Log($"[{_originalHealth.gameObject.name}] is on the sync blacklist");
                return;
            }
        }
        
        DoubleEnemiesMod.Log($"[{gameObject.name}] Pairing {_originalHealth.gameObject.name} with {_cloneHealth.gameObject.name}");
        CloneSync sync = _cloneHealth.gameObject.AddComponent<CloneSync>();
        sync.Init(this, _originalHealth, _cloneHealth, isSharedHPEnabled);
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
    public void LastJudgeFix()
    {
        if (originalHealth.name.Contains("Last Judge"))
        {
            string[] gateNames = { "Great Gate", "Gate L", "Gate Quest Ender", "Gate Open Trigger" };
            foreach (string gateName in gateNames)
            {
                Transform t = gameObject.transform.Find(gateName);
                if (t != null)
                {
                    DoubleEnemiesMod.Log($"LastJudgeFix: Found '{gateName}', destroying...");
                    Destroy(t.gameObject);
                }
                else
                {
                    DoubleEnemiesMod.Log($"LastJudgeFix: Could not find '{gateName}' on {gameObject.name}");
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
    private void DeleteMossBerry()
    {
        if (gameObject.name.Contains("Aspid Collector"))
        {
            string[] berry_names = { "Mossberry Pickup", "Berry Sprite" };
            foreach (string berry_name in berry_names)
            {
                Transform t = gameObject.transform.Find(berry_name);
                if (t != null)
                {
                    DoubleEnemiesMod.Log($"DeleteMossberry: Found '{berry_name}', destroying...");
                    Destroy(t.gameObject);
                }
                else
                {
                    DoubleEnemiesMod.Log($"DeleteMossberry: Could not find '{berry_name}' on {gameObject.name}");
                }
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
