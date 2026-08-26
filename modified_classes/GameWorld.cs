using System.Collections.Generic;
using Game;
using UnityEngine;

public class GameWorld : SaveSerialize {
    public bool HasCompletedEverything() {
        var flag = false;
        foreach (var runtimeGameWorldArea in RuntimeAreas) {
            foreach (var runtimeWorldMapIcon in runtimeGameWorldArea.Icons) {
                var icon = runtimeWorldMapIcon.Icon;
                switch (icon) {
                    case WorldMapIconType.HealthUpgrade:
                    case WorldMapIconType.EnergyUpgrade:
                    case WorldMapIconType.AbilityPoint:
                    case WorldMapIconType.Experience:
                    case WorldMapIconType.MapstonePickup:
                        break;
                    default:
                        if (icon != WorldMapIconType.Keystone) {
                            continue;
                        }

                        break;
                }

                flag = true;
            }
        }

        return !flag && CompletionPercentage == 100;
    }

    public void RevealIcon(MoonGuid icon) {
        m_revealedIcons.Add(icon);
    }

    public bool IconRevealed(MoonGuid icon) {
        return m_revealedIcons.Contains(icon);
    }

    public float CompletionAmount {
        get {
            var num = 0;
            var num2 = 0f;
            for (var i = 0; i < RuntimeAreas.Count; i++) {
                var runtimeGameWorldArea = RuntimeAreas[i];
                num++;
                num2 += runtimeGameWorldArea.CompletionAmount;
            }

            return num2 / num;
        }
    }

    public int CompletionPercentage {
        get {
            var completionAmount = CompletionAmount;
            if (Mathf.Approximately(completionAmount, 1f)) {
                return 100;
            }

            return Mathf.Clamp(Mathf.RoundToInt(CompletionAmount * 100f), 0, 99);
        }
    }

    public GameWorldArea FindAreaFromPosition(Vector3 position) {
        for (var i = 0; i < Areas.Count; i++) {
            var gameWorldArea = Areas[i];
            if (gameWorldArea.InsideFace(position)) {
                return gameWorldArea;
            }
        }

        return null;
    }

    public RuntimeGameWorldArea FindRuntimeArea(GameWorldArea area) {
        for (var i = 0; i < RuntimeAreas.Count; i++) {
            var runtimeGameWorldArea = RuntimeAreas[i];
            if (runtimeGameWorldArea.Area == area) {
                return runtimeGameWorldArea;
            }
        }

        return null;
    }

    public override void Awake() {
        Instance = this;
        RuntimeAreas.Capacity = Areas.Count;
        for (var i = 0; i < Areas.Count; i++) {
            var gameWorldArea = Areas[i];
            RuntimeAreas.Add(new RuntimeGameWorldArea(gameWorldArea));
        }

        Events.Scheduler.OnGameReset.Add(OnGameReset);
        base.Awake();
    }

    public override void OnDestroy() {
        Events.Scheduler.OnGameReset.Remove(OnGameReset);
        base.OnDestroy();
    }

    public void OnGameReset() {
        for (var i = 0; i < RuntimeAreas.Count; i++) {
            var runtimeGameWorldArea = RuntimeAreas[i];
            runtimeGameWorldArea.Initialize();
        }

        m_revealedIcons.Clear();
        ObjectiveText = null;
    }

    public GameWorldArea AreaFromIndex(int i) {
        if (i < 0 || i >= RuntimeAreas.Count) {
            return null;
        }

        return RuntimeAreas[i].Area;
    }

    public int IndexOfArea(GameWorldArea area) {
        return RuntimeAreas.FindIndex(a => a.Area == area);
    }

    public override void Serialize(Archive ar) {
        if (ar.Reading) {
            var num = 0;
            ar.Serialize(ref num);
            if (Areas.Count != num) {
                return;
            }

            var num2 = 0;
            while (num2 < num && num2 < RuntimeAreas.Count) {
                var runtimeGameWorldArea = RuntimeAreas[num2];
                runtimeGameWorldArea.Serialize(ar);
                num2++;
            }

            m_revealedIcons.Clear();
            var num3 = ar.Serialize(0);
            for (var i = 0; i < num3; i++) {
                var moonGuid = new MoonGuid(0, 0, 0, 0);
                moonGuid.Serialize(ar);
                m_revealedIcons.Add(moonGuid);
            }

            var num4 = ar.Serialize(0);
            if (num4 != -1) {
                ObjectiveText = ObjectiveTextProviders[num4];
            }
        } else {
            ar.Serialize(Areas.Count);
            for (var j = 0; j < RuntimeAreas.Count; j++) {
                var runtimeGameWorldArea2 = RuntimeAreas[j];
                runtimeGameWorldArea2.Serialize(ar);
            }

            ar.Serialize(m_revealedIcons.Count);
            foreach (var moonGuid2 in m_revealedIcons) {
                moonGuid2.Serialize(ar);
            }

            ar.Serialize(ObjectiveTextProviders.IndexOf(ObjectiveText));
        }
    }

    public void VisitMapAreasAtPosition(Vector3 currentPlayerPosition) {
        // When we are random spawning this ignores the default spawn location 
        // until we see something else.
        if (Randomizer.ShouldHideGladesStart) {
            var spawnPosition = new Vector3(189.0f, -219.5f, 0.0f);
            if (Vector3.Distance(currentPlayerPosition, spawnPosition) < 0.1) {
                return;
            }
        }

        for (var i = 0; i < RuntimeAreas.Count; i++) {
            var runtimeGameWorldArea = RuntimeAreas[i];
            runtimeGameWorldArea.VisitMapAreaAtPosition(currentPlayerPosition);
        }

        Randomizer.ShouldHideGladesStart = false;
    }

    public GameWorldArea WorldAreaAtPosition(Vector3 worldPosition) {
        for (var i = 0; i < RuntimeAreas.Count; i++) {
            var runtimeGameWorldArea = RuntimeAreas[i];
            var vector = runtimeGameWorldArea.Area.CageStructureTool.transform.InverseTransformPoint(worldPosition);
            var face = runtimeGameWorldArea.Area.CageStructureTool.FindFaceAtPositionFaster(vector);
            if (face != null) {
                return runtimeGameWorldArea.Area;
            }
        }

        return null;
    }

    public static GameWorld Instance;

    public List<GameWorldArea> Areas = new List<GameWorldArea>();

    public List<RuntimeGameWorldArea> RuntimeAreas = new List<RuntimeGameWorldArea>();

    public RuntimeGameWorldArea CurrentArea;

    private readonly HashSet<MoonGuid> m_revealedIcons = new HashSet<MoonGuid>();

    public List<MessageProvider> ObjectiveTextProviders = new List<MessageProvider>();

    public MessageProvider ObjectiveText;
}
