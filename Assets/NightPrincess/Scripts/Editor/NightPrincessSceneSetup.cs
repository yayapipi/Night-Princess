#if UNITY_EDITOR
using System.IO;
using NightPrincess.Akarion;
using NightPrincess.Core;
using NightPrincess.Enemy;
using NightPrincess.Player;
using NightPrincess.Princess;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NightPrincess.EditorTools
{
    public static class NightPrincessSceneSetup
    {
        private const string ConfigPath = "Assets/NightPrincess/Resources/AkarionConfig.asset";
        private const string MenuRoot = "Night Princess/";

        [MenuItem(MenuRoot + "1. Create Akarion Config")]
        public static void CreateConfig()
        {
            EnsureConfigAsset();
            Debug.Log("[NightPrincess] AkarionConfig ready at " + ConfigPath);
        }

        [MenuItem(MenuRoot + "2. Wire Up Scene Components")]
        public static void WireSceneComponents()
        {
            var config = EnsureConfigAsset();

            // --- Akarion bootstrap root (AkarionAPI GameObject) ---
            var apiGo = FindByName("AkarionAPI") ?? new GameObject("AkarionAPI");
            var playerMgr = EnsureComponent<AkarionPlayerManager>(apiGo);
            var evtLogger = EnsureComponent<AkarionEventLogger>(apiGo);
            var llm = EnsureComponent<AkarionLLMClient>(apiGo);
            var imgClient = EnsureComponent<AkarionImageClient>(apiGo);
            SetField(playerMgr, "config", config);
            SetField(evtLogger, "config", config);
            SetField(llm, "config", config);
            SetField(imgClient, "config", config);
            EnsureComponent<GameManager>(apiGo);

            // --- Main Camera: shake ---
            var cam = Camera.main;
            if (cam != null)
            {
                EnsureComponent<CameraShake>(cam.gameObject);
                var existingFlash = cam.GetComponent<FlashEffect>();
                if (existingFlash != null) Object.DestroyImmediate(existingFlash);
            }

            // --- Player (Samurai) ---
            var samurai = FindByName("Samurai");
            if (samurai != null)
            {
                samurai.tag = "Player";
                EnsureRigidbody2D(samurai);
                EnsureBoxCollider2D(samurai);
                EnsureComponent<PlayerController>(samurai);
                var dashFx = EnsureComponent<PlayerDashEffect>(samurai);
                EnsureLineRenderer(dashFx.gameObject);
            }
            else
            {
                Debug.LogWarning("[NightPrincess] 'Samurai' GameObject not found");
            }

            // --- Enemy (Gladiators and any child enemies) ---
            WireEnemy(FindByName("Gladiators"));
            foreach (var child in GetAllSceneObjects())
            {
                if (child == null) continue;
                if (child.name.StartsWith("Gladiator") && child != FindByName("Gladiators"))
                    WireEnemy(child);
            }

            // --- Princess ---
            var princess = FindByName("Princess");
            DialogPanelUI dialogPanel = null;
            CreateItemPanelUI createItemPanel = null;

            var dialogGo = FindByName("DialogPanel");
            if (dialogGo != null)
            {
                dialogPanel = EnsureComponent<DialogPanelUI>(dialogGo);
                var typer = EnsureComponent<TypewriterEffect>(dialogGo);

                var dialogText = FindTMPTextByName(dialogGo.transform, "DialogText") ??
                                 dialogGo.GetComponentInChildren<TMP_Text>(true);
                SetField(typer, "target", dialogText);

                var sendBtn = FindButtonByName(dialogGo.transform, "SendBtn");
                var exitBtn = FindButtonByName(dialogGo.transform, "ExitBtn");
                var buildItemBtn = FindButtonByName(dialogGo.transform, "BuildItemBtn");
                var inputField = FindTMPInputByName(dialogGo.transform, "PlayerInputField");

                SetField(dialogPanel, "panelRoot", dialogGo);
                SetField(dialogPanel, "dialogText", dialogText);
                SetField(dialogPanel, "playerInput", inputField);
                SetField(dialogPanel, "sendBtn", sendBtn);
                SetField(dialogPanel, "buildItemBtn", buildItemBtn);
                SetField(dialogPanel, "exitBtn", exitBtn);
                SetField(dialogPanel, "llm", llm);
                SetField(dialogPanel, "typewriter", typer);
                SetField(dialogPanel, "config", config);
            }

            var createItemGo = FindByName("CreateItemPanel");
            if (createItemGo != null)
            {
                createItemPanel = EnsureComponent<CreateItemPanelUI>(createItemGo);

                var drawTarget = FindByNameRecursive(createItemGo.transform, "DrawTargetImg");
                DrawableCanvas drawable = null;
                if (drawTarget != null)
                {
                    if (drawTarget.GetComponent<RawImage>() == null) drawTarget.AddComponent<RawImage>();
                    drawable = EnsureComponent<DrawableCanvas>(drawTarget);
                }

                var buildBtn = FindButtonByName(createItemGo.transform, "BuildBtn");
                var giveBtn = FindButtonByName(createItemGo.transform, "GiveBtn");
                var closeBtn = FindButtonByName(createItemGo.transform, "CloseBtn");
                var nameInput = FindTMPInputByName(createItemGo.transform, "ItemNameInputField");
                var loadingObj = FindByNameRecursive(createItemGo.transform, "LoadingObj");

                SetField(createItemPanel, "panelRoot", createItemGo);
                SetField(createItemPanel, "drawable", drawable);
                SetField(createItemPanel, "itemNameInput", nameInput);
                SetField(createItemPanel, "buildBtn", buildBtn);
                SetField(createItemPanel, "giveBtn", giveBtn);
                SetField(createItemPanel, "closeBtn", closeBtn);
                SetField(createItemPanel, "loadingObj", loadingObj);
                SetField(createItemPanel, "imageClient", imgClient);
                SetField(createItemPanel, "config", config);
            }

            if (dialogPanel != null && createItemPanel != null)
                SetField(dialogPanel, "createItemPanel", createItemPanel);

            if (princess != null)
            {
                var interactable = EnsureComponent<PrincessInteractable>(princess);
                SetField(interactable, "dialogPanel", dialogPanel);
            }

            // Make sure "Enemy" tag exists
            EnsureTag("Enemy");
            EnsureTag("Player");

            // Save scene
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[NightPrincess] Scene wiring complete.");
        }

        [MenuItem(MenuRoot + "3. Do Everything")]
        public static void DoEverything()
        {
            CreateConfig();
            WireSceneComponents();
        }

        // --- helpers ---

        private static AkarionConfig EnsureConfigAsset()
        {
            var dir = Path.GetDirectoryName(ConfigPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }
            var cfg = AssetDatabase.LoadAssetAtPath<AkarionConfig>(ConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<AkarionConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
                AssetDatabase.SaveAssets();
            }
            return cfg;
        }

        private static void WireEnemy(GameObject enemy)
        {
            if (enemy == null) return;
            enemy.tag = "Enemy";
            EnsureRigidbody2D(enemy);
            EnsureBoxCollider2D(enemy);
            EnsureComponent<EnemyController>(enemy);

            // Add arc detector as a child
            var detectorGo = FindByNameRecursive(enemy.transform, "ArcDetector");
            if (detectorGo == null)
            {
                detectorGo = new GameObject("ArcDetector");
                detectorGo.transform.SetParent(enemy.transform, false);
            }
            var poly = detectorGo.GetComponent<PolygonCollider2D>();
            if (poly == null) poly = detectorGo.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            EnsureComponent<EnemyArcDetector>(detectorGo);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        private static void EnsureRigidbody2D(GameObject go)
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private static void EnsureBoxCollider2D(GameObject go)
        {
            if (go.GetComponent<Collider2D>() == null) go.AddComponent<BoxCollider2D>();
        }

        private static void EnsureLineRenderer(GameObject go)
        {
            var lr = go.GetComponent<LineRenderer>();
            if (lr == null) lr = go.AddComponent<LineRenderer>();
            if (lr.sharedMaterial == null)
                lr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            lr.widthMultiplier = 0.35f;
        }

        private static GameObject FindByName(string name)
        {
            foreach (var go in GetAllSceneObjects())
                if (go != null && go.name == name) return go;
            return null;
        }

        private static GameObject FindByNameRecursive(Transform root, string name)
        {
            if (root.name == name) return root.gameObject;
            foreach (Transform t in root)
            {
                var found = FindByNameRecursive(t, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Button FindButtonByName(Transform root, string name)
        {
            var go = FindByNameRecursive(root, name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        private static TMP_Text FindTMPTextByName(Transform root, string name)
        {
            var go = FindByNameRecursive(root, name);
            return go != null ? go.GetComponent<TMP_Text>() : null;
        }

        private static TMP_InputField FindTMPInputByName(Transform root, string name)
        {
            var go = FindByNameRecursive(root, name);
            return go != null ? go.GetComponent<TMP_InputField>() : null;
        }

        private static System.Collections.Generic.IEnumerable<GameObject> GetAllSceneObjects()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
            {
                yield return r;
                foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
                    if (t != null && t.gameObject != r) yield return t.gameObject;
            }
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"[NightPrincess] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureTag(string tagName)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0) return;
            var so = new SerializedObject(asset[0]);
            var tags = so.FindProperty("tags");
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName) return;
            }
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
