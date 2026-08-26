using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Game;
using UnityEngine;

public class ScenesManager : SaveSerialize {
    public bool ScenesNotLoadedOnTime { get; private set; }

    public float PaddingWidthExtension { get; set; }

    public RuntimeSceneMetaData CurrentScene {
        get {
            for (var i = 0; i < ActiveScenes.Count; i++) {
                var sceneManagerScene = ActiveScenes[i];
                if (!sceneManagerScene.MetaData.DependantScene) {
                    if (sceneManagerScene.IsVisible && UI.Cameras.Current && sceneManagerScene.MetaData.IsInsideSceneBounds(CurrentCameraTargetPosition)) {
                        return ActiveScenes[i].MetaData;
                    }
                }
            }

            return null;
        }
    }

    public SceneManagerScene CurrentSceneManagerScene {
        get {
            for (var i = 0; i < ActiveScenes.Count; i++) {
                if (!ActiveScenes[i].MetaData.DependantScene) {
                    if (UI.Cameras.Current && ActiveScenes[i].MetaData.IsInsideSceneBounds(CurrentCameraTargetPosition)) {
                        return ActiveScenes[i];
                    }
                }
            }

            return null;
        }
    }

    public Vector2 CurrentCameraTargetPosition { get; private set; }

    public Vector2 CurrentCameraTargetPositionExtrapolated { get; private set; }

    public bool SceneVisibleAtPosition(Vector3 position) {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            if (!ActiveScenes[i].MetaData.DependantScene) {
                if (ActiveScenes[i].IsVisible && ActiveScenes[i].MetaData.IsInsideSceneBounds(position)) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool SceneIsEnabled(SceneMetaData sceneMetaData) {
        return SceneIsEnabled(sceneMetaData.SceneMoonGuid);
    }

    public bool SceneIsEnabled(MoonGuid sceneMoonGuid) {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (sceneManagerScene.MetaData.SceneMoonGuid == sceneMoonGuid && sceneManagerScene.CurrentState == SceneManagerScene.State.Loaded) {
                return true;
            }
        }

        return false;
    }

    public void SetTargetPositions(Vector3 target) {
        CurrentCameraTargetPosition = target;
        CurrentCameraTargetPositionExtrapolated = target;
        m_cameraPositions.Clear();
    }

    public bool IsLoadingScenes {
        get {
            for (var i = 0; i < ActiveScenes.Count; i++) {
                var sceneManagerScene = ActiveScenes[i];
                if (sceneManagerScene.CurrentState == SceneManagerScene.State.Loading) {
                    return true;
                }
            }

            return false;
        }
    }

    public Rect GetClampedRect(Vector3 position) {
        var rect = default(Rect);
        var rect2 = rect;
        rect2.width = 48f;
        rect2.height = 48f;
        rect2.center = position;
        rect = rect2;
        Rect rect3;
        if (GetSceneBoundaryAtPosition(rect.center, out rect3)) {
            rect.xMin = Mathf.Max(rect.xMin, rect3.xMin + 0.1f);
            rect.yMin = Mathf.Max(rect.yMin, rect3.yMin + 0.1f);
            rect.xMax = Mathf.Min(rect.xMax, rect3.xMax - 0.1f);
            rect.yMax = Mathf.Min(rect.yMax, rect3.yMax - 0.1f);
        }

        return rect;
    }

    public bool IsLoadingScene(Vector3 position) {
        var clampedRect = GetClampedRect(position);
        Rect rect;
        GetSceneBoundaryAtPosition(position, out rect);
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (!sceneManagerScene.MetaData.DependantScene) {
                if (sceneManagerScene.MetaData.IsInsideSceneBounds(clampedRect) || sceneManagerScene.MetaData.IsInsideScenePaddingBounds(clampedRect, rect)) {
                    if (!sceneManagerScene.IsLoadingComplete) {
                        m_scenes.Clear();
                        return true;
                    }

                    foreach (var moonGuid in sceneManagerScene.MetaData.IncludedScenes) {
                        m_scenes.Add(moonGuid);
                    }
                }
            }
        }

        for (var j = 0; j < ActiveScenes.Count; j++) {
            var sceneManagerScene2 = ActiveScenes[j];
            if (sceneManagerScene2.MetaData.DependantScene && m_scenes.Contains(sceneManagerScene2.MetaData.SceneMoonGuid) && !sceneManagerScene2.IsLoadingComplete) {
                m_scenes.Clear();
                return true;
            }
        }

        m_scenes.Clear();
        return false;
    }

    public bool PositionInsideSceneStillLoading(Vector3 position) {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (!sceneManagerScene.MetaData.DependantScene) {
                if (sceneManagerScene.CurrentState == SceneManagerScene.State.Loading && sceneManagerScene.MetaData.IsInsideSceneBounds(position)) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool ResourcesNeedUnloading {
        get {
            return m_resourcesNeedUnloading != 0;
        }
    }

    public void DrawScenesManagerDebugData() {
        GUILayout.BeginArea(new Rect(8f, 16f, 550f, 500f));
        foreach (var sceneManagerScene in ActiveScenes) {
            GUILayout.BeginHorizontal();
            switch (sceneManagerScene.CurrentState) {
                case SceneManagerScene.State.Disabling:
                    GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                    break;
                case SceneManagerScene.State.Disabled:
                    GUI.color = new Color(0.2f, 0.2f, 0.5f, 1f);
                    break;
                case SceneManagerScene.State.Loading:
                    GUI.color = Color.yellow;
                    break;
                case SceneManagerScene.State.LoadingCancelled:
                    GUI.color = Color.red;
                    break;
                case SceneManagerScene.State.Loaded:
                    GUI.color = Color.white;
                    break;
            }

            GUILayout.Label(sceneManagerScene.MetaData.Scene);
            GUILayout.Label("Loading Time: " + sceneManagerScene.LoadingTime);
            if (sceneManagerScene.KeepLoadedForCheckpoint) {
                GUILayout.Label("(checkpoint)");
            }

            if (sceneManagerScene.PreventUnloading) {
                GUILayout.Label("(preloaded)");
            }

            GUILayout.EndHorizontal();
        }

        GUI.color = Color.white;
        GUILayout.EndArea();
    }

    public RuntimeSceneMetaData GetSceneInformation(string sceneName) {
        for (var i = 0; i < AllScenes.Count; i++) {
            var runtimeSceneMetaData = AllScenes[i];
            if (runtimeSceneMetaData.Scene == sceneName) {
                return runtimeSceneMetaData;
            }
        }

        return null;
    }

    public SceneManagerScene GetSceneManagerScene(string sceneName) {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            if (ActiveScenes[i].MetaData.Scene == sceneName) {
                return ActiveScenes[i];
            }
        }

        return null;
    }

    public override void Awake() {
        base.Awake();
        Scenes.Manager = this;
        GenerateGuidToRuntimeSceneMetaDataDictionary();
        GameController.Instance.GameScheduler.OnPassThroughScrollLock.Add(OnPassThroughScrollLock);
        Game.Checkpoint.Events.OnPostCreate.Add(OnCreateCheckpoint);
        Events.Scheduler.OnGameReset.Add(OnGameReset);
        AspectRatioManager.OnAspectChanged.Add(OnAspectRatioChanged);
    }

    public void OnGameReset() {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            sceneManagerScene.KeepLoadedForCheckpoint = false;
            sceneManagerScene.PreventUnloading = false;
        }
    }

    private void GenerateGuidToRuntimeSceneMetaDataDictionary() {
        for (var i = 0; i < AllScenes.Count; i++) {
            var runtimeSceneMetaData = AllScenes[i];
            m_guidToRuntimeSceneMetaDatas[runtimeSceneMetaData.SceneMoonGuid] = runtimeSceneMetaData;
        }
    }

    public override void OnDestroy() {
        GameController.Instance.GameScheduler.OnPassThroughScrollLock.Remove(OnPassThroughScrollLock);
        Game.Checkpoint.Events.OnPostCreate.Remove(OnCreateCheckpoint);
        Events.Scheduler.OnGameReset.Remove(OnGameReset);
        AspectRatioManager.OnAspectChanged.Remove(OnAspectRatioChanged);
    }

    public void OnAspectRatioChanged() {
        UpdatePaddingWidthExtension();
    }

    public override void Serialize(Archive ar) {
        CurrentCameraTargetPosition = ar.Serialize(CurrentCameraTargetPosition);
        if (ar.Reading) {
            CurrentCameraTargetPositionExtrapolated = CurrentCameraTargetPosition;
        }
    }

    public void MarkLoadingScenesAsCancel() {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (!sceneManagerScene.MetaData.DependantScene && sceneManagerScene.CurrentState == SceneManagerScene.State.Loading) {
                sceneManagerScene.ChangeState(SceneManagerScene.State.LoadingCancelled);
                if (CancelScene(sceneManagerScene)) {
                    i--;
                }
            }
        }
    }

    public void OnCreateCheckpoint() {
        MarkActiveScenesAsKeepLoaded();
    }

    public void MarkActiveScenesAsKeepLoaded() {
        var rect = default(Rect);
        var rect2 = rect;
        rect2.width = 48f;
        rect2.height = 48f;
        rect2.center = CurrentCameraTargetPosition;
        rect = rect2;
        Rect rect3;
        if (GetSceneBoundaryAtPosition(rect.center, out rect3)) {
            rect.xMin = Mathf.Max(rect.xMin, rect3.xMin + 0.1f);
            rect.yMin = Mathf.Max(rect.yMin, rect3.yMin + 0.1f);
            rect.xMax = Mathf.Max(rect.xMax, rect3.xMax - 0.1f);
            rect.yMax = Mathf.Max(rect.yMin, rect3.yMax - 0.1f);
        } else {
            rect.width = 0f;
            rect.height = 0f;
        }

        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (sceneManagerScene.MetaData.IsInsideSceneBounds(rect)) {
                sceneManagerScene.KeepLoadedForCheckpoint = true;
            } else if (sceneManagerScene.MetaData.IsInsideSceneLoadingZone(rect)) {
                sceneManagerScene.KeepLoadedForCheckpoint = true;
            } else if (sceneManagerScene.MetaData.IsInsideScenePaddingBounds(rect)) {
                sceneManagerScene.KeepLoadedForCheckpoint = true;
            } else {
                sceneManagerScene.KeepLoadedForCheckpoint = false;
            }
        }
    }

    public void ClearKeepLoadedForCheckpoint() {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            sceneManagerScene.KeepLoadedForCheckpoint = false;
        }
    }

    public bool HasReportedScenesLoading { get; set; }

    public void ReportScenesThatAreStillLoading() {
        HasReportedScenesLoading = true;
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (sceneManagerScene.CurrentState == SceneManagerScene.State.Loading) {
            }
        }
    }

    private void DetectScenesNotLoadedInTime() {
        if (ScenesNotLoadedOnTime) {
            if (!AnyMissingScenesAtCurrentPosition()) {
                ScenesNotLoadedOnTime = false;
            }
        } else if (AnyMissingScenesAtCurrentPosition()) {
            ScenesNotLoadedOnTime = true;
        }
    }

    private string SceneToLoad {
        get {
            if (m_scenesToLoad.Count > 0) {
                return m_scenesToLoad[0];
            }

            if (m_backgroundsToLoad.Count > 0) {
                return m_backgroundsToLoad[0];
            }

            return string.Empty;
        }
    }

    private string PopSceneToLoad() {
        if (m_scenesToLoad.Count > 0) {
            var text = m_scenesToLoad[0];
            m_scenesToLoad.Remove(text);
            return text;
        }

        if (m_backgroundsToLoad.Count > 0) {
            var text2 = m_backgroundsToLoad[0];
            m_backgroundsToLoad.Remove(text2);
            return text2;
        }

        return string.Empty;
    }

    private void UpdateLoadingScenes() {
        if (m_currentLoad != null && m_currentLoad.isDone) {
            m_currentLoad = null;
        }

        if (m_currentLoad == null && SceneToLoad != string.Empty && CanLoadScenes) {
            m_currentLoad = Application.LoadLevelAdditiveAsync(PopSceneToLoad());
        }
    }

    public void TestForFallOutOfWorld() {
        if (m_testDelayTime <= 0f) {
            m_testDelayTime = 1f;
            if (!IsInsideASceneBoundary(CurrentCameraTargetPosition)) {
                GameController.Instance.RestoreCheckpoint();
            }
        }

        m_testDelayTime -= Time.deltaTime;
    }

    private IEnumerator ShowFellOutOfWorldMessage() {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        var message = UI.MessageController.ShowHintMessage(Scenes.Manager.FellOutOfWorldMessage, OnScreenPositions.TopCenter);
        yield break;
    }

    public void ForceTestForOutOfWorld() {
        m_testDelayTime = 0f;
        TestForFallOutOfWorld();
    }

    public void FixedUpdate() {
        if (UI.Cameras.Current.ScrollLockIsFadingOut) {
            return;
        }

        if (AutoLoadingUnloading) {
            DetectScenesNotLoadedInTime();
            UpdateScenes();
            UpdateExtrapolatedPosition();
            EnableDisabledScenesAtPosition(true);
            TestForFallOutOfWorld();
        }
    }

    private void UpdatePaddingWidthExtension() {
        var current = UI.Cameras.Current;
        if (current) {
            var cameraWidthWorldUnits = UI.Cameras.Current.CameraWidthWorldUnits;
            var num = cameraWidthWorldUnits - cameraWidthWorldUnits * 1.7777778f / AspectRatioManager.AspectRatio;
            PaddingWidthExtension = num * 0.5f;
        }
    }

    public void UpdatePosition() {
        m_cameraPositions.Clear();
        for (var i = 0; i < UI.Cameras.Manager.Cameras.Count; i++) {
            var cameraController = UI.Cameras.Manager.Cameras[i];
            if (cameraController.PuppetController.Tween > 0.5f) {
                m_cameraPositions.Add(cameraController.Position);
            }
        }

        if (UI.Cameras.Current.Target) {
            if (!Scenes.Manager.ScenesNotLoadedOnTime) {
                UI.Cameras.Current.CameraTarget.UpdateTargetPosition();
            }

            CurrentCameraTargetPosition = UI.Cameras.Current.CameraTarget.TargetPosition;
            CurrentCameraTargetPositionExtrapolated = CurrentCameraTargetPosition;
            UpdateExtrapolatedPosition();
        }
    }

    public void ClearCameraPuppetPositions() {
        m_cameraPositions.Clear();
    }

    public void UpdateExtrapolatedPosition() {
        Rect rect;
        if (Characters.Sein && GetSceneBoundaryAtPosition(CurrentCameraTargetPosition, out rect)) {
            var vector = CurrentCameraTargetPosition + Vector2.ClampMagnitude(Characters.Sein.PhysicsSpeed * 2f, 24f);
            CurrentCameraTargetPositionExtrapolated = new Vector2(Mathf.Clamp(vector.x, rect.xMin + 0.1f, rect.xMax - 0.1f), Mathf.Clamp(vector.y, rect.yMin + 0.1f, rect.yMax - 0.1f));
            Vector3 vector2 = CurrentCameraTargetPosition;
            Vector3 vector3 = CurrentCameraTargetPositionExtrapolated;
            var flag = Mathf.Abs(vector3.x - vector2.x) > Mathf.Abs(vector3.y - vector2.y);
            for (var i = 0; i < 6; i++) {
                Debug.DrawLine(vector2, vector3, Color.gray);
                RaycastHit raycastHit;
                if (!Physics.Linecast(vector2, vector3, out raycastHit, RaycastMask)) {
                    CurrentCameraTargetPositionExtrapolated = vector3;
                    break;
                }

                vector2 = raycastHit.point - (vector3 - vector2).normalized * 0.02f;
                if (i == 5) {
                    CurrentCameraTargetPositionExtrapolated = vector2;
                    break;
                }

                var vector4 = vector2;
                vector4 += 4f * (!flag ? raycastHit.normal.x <= 0f ? Vector3.left : Vector3.right : raycastHit.normal.y <= 0f ? Vector3.down : Vector3.up);
                Debug.DrawLine(vector2, vector4, Color.gray);
                vector2 = !Physics.Linecast(vector2, vector4, out raycastHit, RaycastMask) ? vector4 : raycastHit.point - (vector4 - vector2).normalized * 0.02f;
            }
        }
    }

    public bool GetSceneBoundaryAtPosition(Vector3 position, out Rect bound) {
        for (var i = 0; i < AllScenes.Count; i++) {
            var runtimeSceneMetaData = AllScenes[i];
            if (!runtimeSceneMetaData.DependantScene) {
                if (runtimeSceneMetaData.IsInTotal(position)) {
                    if (runtimeSceneMetaData.CanBeLoaded) {
                        for (var j = 0; j < runtimeSceneMetaData.SceneBoundaries.Count; j++) {
                            var rect = runtimeSceneMetaData.SceneBoundaries[j];
                            if (rect.Contains(position)) {
                                bound = rect;
                                return true;
                            }
                        }
                    }
                }
            }
        }

        bound = new Rect(0f, 0f, 0f, 0f);
        return false;
    }

    public bool IsInsideASceneBoundary(Vector3 position) {
        var allScenes = AllScenes;
        for (var i = 0; i < allScenes.Count; i++) {
            var runtimeSceneMetaData = allScenes[i];
            if (!runtimeSceneMetaData.DependantScene) {
                if (runtimeSceneMetaData.IsInTotal(position) && runtimeSceneMetaData.IsInsideSceneBounds(position)) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsInsideActiveSceneBoundary(Vector3 position) {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (!sceneManagerScene.MetaData.DependantScene) {
                if ((sceneManagerScene.CurrentState == SceneManagerScene.State.Loaded || sceneManagerScene.CurrentState == SceneManagerScene.State.Disabling) && sceneManagerScene.MetaData.IsInsideSceneBounds(position)) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsInsideAScenePaddingBoundary(Vector3 position) {
        Rect rect;
        GetSceneBoundaryAtPosition(position, out rect);
        var allScenes = AllScenes;
        for (var i = 0; i < allScenes.Count; i++) {
            if (allScenes[i].IsInsideScenePaddingBounds(position, rect)) {
                return true;
            }
        }

        return false;
    }

    public void Update() {
        if (m_resourcesNeedUnloading == 1) {
            SaveSceneManager.Master.ReleaseNullReferences();
            SuspensionManager.CleanupSuspendables();
        }

        if (m_resourcesNeedUnloading > 0) {
            m_resourcesNeedUnloading--;
        }

        DestroyManager.Update();
    }

    public SceneRoot FindLoadedSceneRootFromPosition(Vector3 position) {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (sceneManagerScene.CurrentState == SceneManagerScene.State.Loaded || sceneManagerScene.CurrentState == SceneManagerScene.State.Disabled || sceneManagerScene.CurrentState == SceneManagerScene.State.Disabling) {
                if (sceneManagerScene.SceneRoot && sceneManagerScene.SceneRoot.MetaData) {
                    if (!sceneManagerScene.SceneRoot.MetaData.DependantScene) {
                        if (sceneManagerScene.MetaData.IsInsideSceneBounds(position)) {
                            if (!sceneManagerScene.MetaData.LoadingCondition || sceneManagerScene.MetaData.LoadingCondition.Validate(null)) {
                                return sceneManagerScene.SceneRoot;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    public SceneManagerScene GetFromCurrentScenes(RuntimeSceneMetaData sceneMetaData) {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (sceneManagerScene.MetaData == sceneMetaData) {
                return sceneManagerScene;
            }
        }

        return null;
    }

    public RuntimeSceneMetaData FindRuntimeSceneMetaData(MoonGuid sceneGuid) {
        RuntimeSceneMetaData runtimeSceneMetaData;
        if (m_guidToRuntimeSceneMetaDatas.TryGetValue(sceneGuid, out runtimeSceneMetaData)) {
            return runtimeSceneMetaData;
        }

        return null;
    }

    public void PreloadScene(RuntimeSceneMetaData sceneMetaData) {
        AdditivelyLoadScenesAtPosition(sceneMetaData.PlaceholderPosition, true, false, true);
    }

    public void PreloadScene(SceneMetaData sceneMetaData) {
        AdditivelyLoadScenesAtPosition(sceneMetaData.SeinPlaceholderPosition, true, false, true);
    }

    private void RemoveScene(SceneManagerScene scene) {
        ActiveScenes.Remove(scene);
    }

    private bool CanLevelBeLoaded(string sceneName) {
        bool flag;
        if (m_canBeStreamed.TryGetValue(sceneName, out flag)) {
            return flag;
        }

        flag = Application.CanStreamedLevelBeLoaded(sceneName);
        m_canBeStreamed[sceneName] = flag;
        return flag;
    }

    public void AdditivelyLoadScenesAtPosition(Vector3 position, bool async, bool loadingZones = true, bool keepPreloaded = false) {
        if (Time.timeScale > 2f) {
            async = false;
        }

        var allScenes = AllScenes;
        var count = allScenes.Count;
        Rect rect;
        GetSceneBoundaryAtPosition(position, out rect);
        for (var i = 0; i < count; i++) {
            var runtimeSceneMetaData = allScenes[i];
            if (!runtimeSceneMetaData.DependantScene) {
                if (runtimeSceneMetaData.IsInTotal(position)) {
                    if (runtimeSceneMetaData.IsInsideSceneBounds(position)) {
                        if (runtimeSceneMetaData.CanBeLoaded) {
                            AdditivelyLoadScene(runtimeSceneMetaData, async, keepPreloaded);
                        }
                    } else if (runtimeSceneMetaData.IsInsideScenePaddingBounds(position, rect)) {
                        if (runtimeSceneMetaData.CanBeLoaded) {
                            AdditivelyLoadScene(runtimeSceneMetaData, async, keepPreloaded);
                        }
                    } else if (runtimeSceneMetaData.IsInsideSceneLoadingZone(position) && runtimeSceneMetaData.CanBeLoaded && loadingZones) {
                        AdditivelyLoadScene(runtimeSceneMetaData, true, keepPreloaded);
                    }
                }
            }
        }
    }

    public void AdditivelyLoadScenesInsideRect(Rect rect, bool async, bool loadingZones = true, bool keepPreloaded = false) {
        if (Time.timeScale > 2f) {
            async = false;
        }

        var allScenes = AllScenes;
        var count = allScenes.Count;
        Rect rect2;
        GetSceneBoundaryAtPosition(rect.center, out rect2);
        for (var i = 0; i < count; i++) {
            var runtimeSceneMetaData = allScenes[i];
            if (!runtimeSceneMetaData.DependantScene) {
                if (runtimeSceneMetaData.IsInTotal(rect)) {
                    if (runtimeSceneMetaData.IsInsideSceneBounds(rect)) {
                        if (runtimeSceneMetaData.CanBeLoaded) {
                            AdditivelyLoadScene(runtimeSceneMetaData, async, keepPreloaded);
                        }
                    } else if (runtimeSceneMetaData.IsInsideScenePaddingBounds(rect, rect2)) {
                        if (runtimeSceneMetaData.CanBeLoaded) {
                            AdditivelyLoadScene(runtimeSceneMetaData, async, keepPreloaded);
                        }
                    } else if (runtimeSceneMetaData.IsInsideSceneLoadingZone(rect) && runtimeSceneMetaData.CanBeLoaded && loadingZones) {
                        AdditivelyLoadScene(runtimeSceneMetaData, true, keepPreloaded);
                    }
                }
            }
        }
    }

    private void AdditivelyLoadScene(RuntimeSceneMetaData sceneMetaData, bool async, bool keepPreloaded = false) {
        var fromCurrentScenes = GetFromCurrentScenes(sceneMetaData);
        if (fromCurrentScenes != null) {
            if (fromCurrentScenes.CurrentState == SceneManagerScene.State.LoadingCancelled) {
                fromCurrentScenes.ChangeState(SceneManagerScene.State.Loading);
                LoadDependantScenes(fromCurrentScenes.MetaData, true);
                if (keepPreloaded) {
                    fromCurrentScenes.PreventUnloading = true;
                }
            }
        } else if (CanLevelBeLoaded(sceneMetaData.Scene)) {
            if (CanLoadScenes) {
                if (async) {
                    var asyncOperation = Application.LoadLevelAdditiveAsync(sceneMetaData.Scene);
                    asyncOperation.priority = 2;
                } else {
                    Application.LoadLevelAdditive(sceneMetaData.Scene);
                }
            }

            var sceneManagerScene = new SceneManagerScene(sceneMetaData);
            ActiveScenes.Add(sceneManagerScene);
            sceneManagerScene.PreventUnloading = keepPreloaded;
            LoadDependantScenes(sceneMetaData, async);
        }
    }

    private void LoadDependantScenes(RuntimeSceneMetaData sceneMetaData, bool async) {
        for (var i = 0; i < sceneMetaData.IncludedScenes.Count; i++) {
            var runtimeSceneMetaData = FindRuntimeSceneMetaData(sceneMetaData.IncludedScenes[i]);
            if (runtimeSceneMetaData != null && runtimeSceneMetaData.CanBeLoaded) {
                AdditivelyLoadScene(runtimeSceneMetaData, async);
            }
        }
    }

    public void UnloadScenesAtPosition(bool instant) {
        var clampedRect = GetClampedRect(CurrentCameraTargetPosition);
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            var metaData = sceneManagerScene.MetaData;
            if (metaData != null) {
                if (!metaData.DependantScene) {
                    var flag = metaData.IsInsideSceneBounds(CurrentCameraTargetPosition) || metaData.IsInsideScenePaddingBounds(CurrentCameraTargetPosition);
                    for (var j = 0; j < m_cameraPositions.Count; j++) {
                        var vector = m_cameraPositions[j];
                        if (metaData.IsInsideSceneBounds(vector) || metaData.IsInsideScenePaddingBounds(vector)) {
                            flag = true;
                        }
                    }

                    if (!flag || !metaData.CanBeLoaded) {
                        var flag2 = (metaData.CanBeLoaded && (metaData.IsInsideSceneLoadingZone(clampedRect) || metaData.IsInsideSceneBounds(clampedRect) || metaData.IsInsideScenePaddingBoundsExpanded(clampedRect))) || sceneManagerScene.PreventUnloading || sceneManagerScene.KeepLoadedForCheckpoint || sceneManagerScene.IsTitleScreen;
                        if (UnloadScene(sceneManagerScene, flag2, instant || !metaData.CanBeLoaded)) {
                            i--;
                        }
                    }
                }
            }
        }

        UnloadDependantScenes();
    }

    public void OnPassThroughScrollLock() {
        UpdateScenes();
    }

    public void OnDisableSceneRoot(SceneRoot sceneRoot) {
        try {
            Events.Scheduler.OnSceneRootDisabled.Call(sceneRoot);
        } catch (Exception ex) {
        }
    }

    public bool UnloadScene(SceneManagerScene scene, bool keepInMemory, bool instant) {
        if (!AllowDestroying) {
            keepInMemory = true;
        }

        if (keepInMemory) {
            switch (scene.CurrentState) {
                case SceneManagerScene.State.Disabling:
                    if (Time.time > scene.UnloadTime || instant) {
                        OnDisableSceneRoot(scene.SceneRoot);
                        scene.ChangeState(SceneManagerScene.State.Disabled);
                        scene.SceneRoot.Save();
                        scene.SceneRoot.DisableScene();
                    }

                    return false;
                case SceneManagerScene.State.LoadingCancelled:
                    scene.ChangeState(SceneManagerScene.State.Loading);
                    return false;
                case SceneManagerScene.State.Loaded:
                    if (instant) {
                        scene.ChangeState(SceneManagerScene.State.Disabled);
                        scene.SceneRoot.Save();
                        OnDisableSceneRoot(scene.SceneRoot);
                        scene.SceneRoot.DisableScene();
                    } else {
                        scene.ChangeState(SceneManagerScene.State.Disabling);
                        scene.UnloadTime = Time.time + UnloadDelay;
                    }

                    return false;
            }
        } else {
            switch (scene.CurrentState) {
                case SceneManagerScene.State.Disabling:
                    if (Time.time > scene.UnloadTime) {
                        OnDisableSceneRoot(scene.SceneRoot);
                        scene.SceneRoot.SaveAndUnload();
                        RemoveScene(scene);
                        return true;
                    }

                    return false;
                case SceneManagerScene.State.Disabled:
                    scene.SceneRoot.Unload();
                    RemoveScene(scene);
                    return true;
                case SceneManagerScene.State.Loading:
                    scene.ChangeState(SceneManagerScene.State.LoadingCancelled);
                    return CancelScene(scene);
                case SceneManagerScene.State.Loaded:
                    if (instant) {
                        OnDisableSceneRoot(scene.SceneRoot);
                        scene.SceneRoot.SaveAndUnload();
                        RemoveScene(scene);
                        return true;
                    }

                    scene.ChangeState(SceneManagerScene.State.Disabling);
                    scene.UnloadTime = Time.time + UnloadDelay;
                    return false;
            }
        }

        return false;
    }

    public void ReleaseUnusedResources() {
        m_resourcesNeedUnloading = 3;
    }

    public void UnloadDependantScenes() {
        Vector3 vector = CurrentCameraTargetPosition;
        m_scenesToDisable.Clear();
        m_scenesToInclude.Clear();
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (!sceneManagerScene.MetaData.DependantScene) {
                if (sceneManagerScene.CurrentState == SceneManagerScene.State.Disabled || sceneManagerScene.CurrentState == SceneManagerScene.State.Loading) {
                    for (var j = 0; j < sceneManagerScene.MetaData.IncludedScenes.Count; j++) {
                        var runtimeSceneMetaData = FindRuntimeSceneMetaData(sceneManagerScene.MetaData.IncludedScenes[j]);
                        if (runtimeSceneMetaData != null) {
                            m_scenesToDisable.Add(runtimeSceneMetaData);
                        }
                    }
                }

                if (sceneManagerScene.CurrentState == SceneManagerScene.State.Loaded || sceneManagerScene.CurrentState == SceneManagerScene.State.Disabling) {
                    if (sceneManagerScene.MetaData.IsInsideSceneBounds(vector) || sceneManagerScene.MetaData.IsInsideScenePaddingBounds(vector)) {
                        for (var k = 0; k < sceneManagerScene.MetaData.IncludedScenes.Count; k++) {
                            var runtimeSceneMetaData2 = FindRuntimeSceneMetaData(sceneManagerScene.MetaData.IncludedScenes[k]);
                            if (runtimeSceneMetaData2 != null) {
                                m_scenesToInclude.Add(runtimeSceneMetaData2);
                            }
                        }
                    } else {
                        for (var l = 0; l < sceneManagerScene.MetaData.IncludedScenes.Count; l++) {
                            var runtimeSceneMetaData3 = FindRuntimeSceneMetaData(sceneManagerScene.MetaData.IncludedScenes[l]);
                            if (runtimeSceneMetaData3 != null) {
                                m_scenesToDisable.Add(runtimeSceneMetaData3);
                            }
                        }
                    }
                }
            }
        }

        for (var m = 0; m < ActiveScenes.Count; m++) {
            var sceneManagerScene2 = ActiveScenes[m];
            var metaData = sceneManagerScene2.MetaData;
            if (metaData.DependantScene && !m_scenesToInclude.Contains(metaData) && UnloadScene(sceneManagerScene2, m_scenesToDisable.Contains(metaData), true)) {
                m--;
            }
        }

        m_scenesToDisable.Clear();
        m_scenesToInclude.Clear();
    }

    public void UpdateScenes() {
        UpdatePosition();
        if (IsInsideASceneBoundary(CurrentCameraTargetPosition) && !ScenesNotLoadedOnTime) {
            UnloadScenesAtPosition(false);
        }

        AdditivelyLoadScenesAtPosition(CurrentCameraTargetPositionExtrapolated, true);
    }

    public void OnSceneStartCompleted(SceneRoot sceneRoot) {
        var runtimeSceneMetaData = FindRuntimeSceneMetaData(sceneRoot.MetaData.SceneMoonGuid);
        var fromCurrentScenes = GetFromCurrentScenes(runtimeSceneMetaData);
        if (fromCurrentScenes != null) {
            fromCurrentScenes.HasStartBeenCalled = true;
        }
    }

    public void Register(SceneRoot sceneRoot) {
        if (sceneRoot.name == "worldMapScene") {
            WorldMapUI.OnFinishedLoading(sceneRoot);
            return;
        }

        var runtimeSceneMetaData = FindRuntimeSceneMetaData(sceneRoot.MetaData.SceneMoonGuid);
        var sceneManagerScene = GetFromCurrentScenes(runtimeSceneMetaData);
        var metaData = sceneRoot.MetaData;
        if (sceneManagerScene == null) {
            sceneManagerScene = new SceneManagerScene(sceneRoot, runtimeSceneMetaData);
            UpdatePosition();
            if (sceneRoot.MetaData.IsInsideSceneBounds(CurrentCameraTargetPosition) || sceneRoot.MetaData.IsInsideScenePaddingBounds(CurrentCameraTargetPosition)) {
                ActiveScenes.Add(sceneManagerScene);
                EnableDisabledScene(sceneManagerScene);
            } else {
                sceneManagerScene.CurrentState = SceneManagerScene.State.Disabled;
                ActiveScenes.Add(sceneManagerScene);
                sceneRoot.DisableScene();
            }
        } else {
            if (sceneManagerScene.SceneRoot == sceneRoot) {
                return;
            }

            if (sceneManagerScene.CurrentState == SceneManagerScene.State.Loading) {
                sceneManagerScene.ChangeState(SceneManagerScene.State.Disabled);
                if (sceneRoot.MetaData.RootPosition != sceneRoot.transform.position) {
                    sceneRoot.transform.position = sceneRoot.MetaData.RootPosition;
                }

                sceneManagerScene.SceneRoot = sceneRoot;
                sceneRoot.DisableScene();
            } else if (sceneManagerScene.CurrentState == SceneManagerScene.State.LoadingCancelled) {
                sceneManagerScene.SceneRoot = sceneRoot;
                sceneRoot.Unload();
                RemoveScene(sceneManagerScene);
            } else {
                sceneRoot.Unload();
            }
        }

        sceneManagerScene.LoadingTime = Time.realtimeSinceStartup - sceneManagerScene.TimeOfLoad;
        SceneFrameworkPerformanceMonitor.AddSceneLoadItem(sceneManagerScene);
    }

    public bool AnyMissingScenesAtCurrentPosition() {
        Vector3 vector = CurrentCameraTargetPosition;
        var cameraBoundingBox = UI.Cameras.Current.CameraBoundingBox;
        cameraBoundingBox.Expand(2f);
        cameraBoundingBox.center = vector;
        var rect = Utility.RectFromBounds(cameraBoundingBox);
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            var metaData = sceneManagerScene.MetaData;
            if (!metaData.DependantScene) {
                var flag = metaData.IsInsideSceneBounds(rect) && (metaData.IsInsideSceneBounds(vector) || metaData.IsInsideScenePaddingBounds(vector));
                if (flag && sceneManagerScene.UnityIsLoading) {
                    return true;
                }
            }
        }

        return false;
    }

    public void EnableDisabledScenesAtPosition(bool limitOnce = false) {
        Vector3 vector = CurrentCameraTargetPosition;
        m_scenesToEnable.Clear();
        Rect rect;
        GetSceneBoundaryAtPosition(vector, out rect);
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            if (!sceneManagerScene.UnityIsLoading) {
                if (sceneManagerScene.MetaData != null) {
                    var metaData = sceneManagerScene.MetaData;
                    if (!metaData.DependantScene) {
                        if (sceneManagerScene.CurrentState == SceneManagerScene.State.Disabled || sceneManagerScene.CurrentState == SceneManagerScene.State.Disabling) {
                            var flag = metaData.IsInsideSceneBounds(vector) || metaData.IsInsideScenePaddingBounds(vector, rect);
                            for (var j = 0; j < m_cameraPositions.Count; j++) {
                                var vector2 = m_cameraPositions[j];
                                if (metaData.IsInsideSceneBounds(vector2) || metaData.IsInsideScenePaddingBounds(vector2, rect)) {
                                    flag = true;
                                }
                            }

                            if (flag && metaData.CanBeLoaded) {
                                if (sceneManagerScene.CurrentState == SceneManagerScene.State.Disabled) {
                                    EnableDisabledScene(sceneManagerScene);
                                    if (limitOnce) {
                                        m_scenesToEnable.Clear();
                                        return;
                                    }
                                } else {
                                    sceneManagerScene.CurrentState = SceneManagerScene.State.Loaded;
                                }
                            }
                        }

                        if ((sceneManagerScene.CurrentState == SceneManagerScene.State.Loaded || sceneManagerScene.CurrentState == SceneManagerScene.State.Disabling) && (sceneManagerScene.MetaData.IsInsideSceneBounds(vector) || metaData.IsInsideScenePaddingBounds(vector))) {
                            for (var k = 0; k < sceneManagerScene.MetaData.IncludedScenes.Count; k++) {
                                var moonGuid = sceneManagerScene.MetaData.IncludedScenes[k];
                                var runtimeSceneMetaData = FindRuntimeSceneMetaData(moonGuid);
                                if (runtimeSceneMetaData != null) {
                                    m_scenesToEnable.Add(runtimeSceneMetaData);
                                }
                            }
                        }
                    }
                }
            }
        }

        for (var l = 0; l < ActiveScenes.Count; l++) {
            var sceneManagerScene2 = ActiveScenes[l];
            if (sceneManagerScene2.CurrentState == SceneManagerScene.State.Disabled) {
                var metaData2 = sceneManagerScene2.MetaData;
                if (metaData2.DependantScene && m_scenesToEnable.Contains(sceneManagerScene2.MetaData)) {
                    EnableDisabledScene(sceneManagerScene2);
                    if (limitOnce) {
                        m_scenesToEnable.Clear();
                        return;
                    }
                }
            }
        }

        m_scenesToEnable.Clear();
    }

    private void EnableDisabledScene(SceneManagerScene scene) {
        scene.ChangeState(SceneManagerScene.State.Loaded);
        Events.Scheduler.OnSceneRootPreEnabled.Call(scene.SceneRoot);
        scene.PreventUnloading = false;
        scene.SceneRoot.EnableScene();
        if (!scene.HasStartBeenCalled) {
            scene.SceneRoot.EarlyStart();
        }

        LateStartHook.AddLateStartMethod(scene.SceneRoot.RegisterSceneRootEnabledAfterSerialize);
    }

    public void CheckForScenesFinishedLoading() {
        InstantLoadScenesController.Instance.OnScenesManagerFixedUpdate();
        GoToSceneController.Instance.OnScenesManagerFixedUpdate();
    }

    public void UnloadAllScenes() {
        foreach (var sceneManagerScene in ActiveScenes.ToArray()) {
            if (!sceneManagerScene.IsTitleScreen) {
                switch (sceneManagerScene.CurrentState) {
                    case SceneManagerScene.State.Disabling:
                        sceneManagerScene.SceneRoot.SaveAndUnload();
                        RemoveScene(sceneManagerScene);
                        break;
                    case SceneManagerScene.State.Disabled:
                        sceneManagerScene.SceneRoot.Unload();
                        RemoveScene(sceneManagerScene);
                        break;
                    case SceneManagerScene.State.Loading:
                        sceneManagerScene.ChangeState(SceneManagerScene.State.LoadingCancelled);
                        CancelScene(sceneManagerScene);
                        break;
                    case SceneManagerScene.State.Loaded:
                        sceneManagerScene.SceneRoot.SaveAndUnload();
                        RemoveScene(sceneManagerScene);
                        break;
                }
            }
        }
    }

    private bool CancelScene(SceneManagerScene scene) {
        return false;
    }

    public void AllowUnloadingOnAllScenes() {
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            sceneManagerScene.PreventUnloading = false;
            sceneManagerScene.KeepLoadedForCheckpoint = false;
        }
    }

    public void AllowUnloadingOnScenes(Vector3 position) {
        var clampedRect = GetClampedRect(position);
        for (var i = 0; i < ActiveScenes.Count; i++) {
            var sceneManagerScene = ActiveScenes[i];
            var metaData = sceneManagerScene.MetaData;
            if (!metaData.DependantScene) {
                if (metaData.CanBeLoaded && (metaData.IsInsideSceneLoadingZone(clampedRect) || metaData.IsInsideSceneBounds(clampedRect) || metaData.IsInsideScenePaddingBounds(clampedRect))) {
                    sceneManagerScene.PreventUnloading = false;
                }
            }
        }
    }

    public bool SceneIsLoaded(MoonGuid sceneGuid) {
        foreach (var sceneManagerScene in ActiveScenes) {
            if (sceneManagerScene.MetaData.SceneMoonGuid == sceneGuid) {
                if (sceneManagerScene.CurrentState == SceneManagerScene.State.Loading || sceneManagerScene.CurrentState == SceneManagerScene.State.LoadingCancelled) {
                    return false;
                }

                return true;
            }
        }

        return false;
    }

    public void OnFinishedStreamingInstall() {
        m_canBeStreamed.Clear();
    }

    public string GetSceneNameAtPosition(Vector3 position) {
        for (var i = 0; i < AllScenes.Count; i++) {
            var runtimeSceneMetaData = AllScenes[i];
            if (!runtimeSceneMetaData.DependantScene && runtimeSceneMetaData.IsInTotal(position) && runtimeSceneMetaData.IsInsideSceneBounds(position)) {
                return runtimeSceneMetaData.Scene;
            }
        }

        return null;
    }

    public float UnloadDelay = 1f;

    public List<SceneManagerScene> ActiveScenes = new List<SceneManagerScene>();

    public DestroyManager DestroyManager = new DestroyManager();

    public bool AutoLoadingUnloading = true;

    public MessageProvider FellOutOfWorldMessage;

    public bool AllowDestroying;

    public List<RuntimeSceneMetaData> AllScenes = new List<RuntimeSceneMetaData>();

    private readonly List<Vector3> m_cameraPositions = new List<Vector3>();

    private int m_resourcesNeedUnloading;

    private readonly HashSet<RuntimeSceneMetaData> m_scenesToDisable = new HashSet<RuntimeSceneMetaData>();

    private readonly HashSet<RuntimeSceneMetaData> m_scenesToInclude = new HashSet<RuntimeSceneMetaData>();

    private readonly HashSet<RuntimeSceneMetaData> m_scenesToEnable = new HashSet<RuntimeSceneMetaData>();

    private readonly Dictionary<string, bool> m_canBeStreamed = new Dictionary<string, bool>();

    private readonly Dictionary<MoonGuid, RuntimeSceneMetaData> m_guidToRuntimeSceneMetaDatas = new Dictionary<MoonGuid, RuntimeSceneMetaData>();

    public bool CanLoadScenes = true;

    private AsyncOperation m_currentLoad;

    private List<string> m_scenesToLoad = new List<string>();

    private List<string> m_backgroundsToLoad = new List<string>();

    private HashSet<MoonGuid> m_scenes = new HashSet<MoonGuid>();

    private float m_testDelayTime;

    public LayerMask RaycastMask;
}
