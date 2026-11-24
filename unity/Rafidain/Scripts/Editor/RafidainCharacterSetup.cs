using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Menu: Rafidain > Setup Furat Character (Mixamo)
    /// 1. Put the character FBX in   Assets/Rafidain/Characters/Furat/Model/
    /// 2. Put animation FBX files in Assets/Rafidain/Characters/Furat/Animations/
    ///    (file names tell the tool what they are: idle, walk, run, sprint, roll, slash/attack,
    ///     heavy/great/power, hit/impact, death/dying, drink/heal)
    /// 3. Open the region scene and run the menu.
    /// The tool rigs everything as Humanoid, builds the Animator Controller, replaces the capsule
    /// with the real model, puts the sword in its right hand and connects the animations to gameplay.
    /// </summary>
    public static class RafidainCharacterSetup
    {
        const string Root = "Assets/Rafidain/Characters/Furat";
        const string ModelFolder = Root + "/Model";
        const string AnimFolder = Root + "/Animations";
        const string ControllerPath = Root + "/FuratAnimator.controller";

        static readonly string[] Looping = { "idle", "walk", "run", "sprint" };

        [MenuItem("Rafidain/Setup Furat Character (Mixamo)")]
        public static void Setup()
        {
            RafidainSceneBuilder.EnsureFolder(ModelFolder);
            RafidainSceneBuilder.EnsureFolder(AnimFolder);
            AssetDatabase.Refresh();

            string modelPath = FindFbx(ModelFolder);
            if (modelPath == null)
            {
                Dialog("No character found.\n\nPut Furat's FBX file in:\n" + ModelFolder);
                return;
            }

            var furat = Object.FindAnyObjectByType<PlayerStats>();
            if (furat == null)
            {
                Dialog("Open the game scene first (Zakho_Region or Zakho_Test), then run this again.");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Rafidain", "Rigging Furat as Humanoid…", 0.1f);
                var avatar = PrepareModel(modelPath, Root);
                if (avatar == null || !avatar.isHuman)
                {
                    Dialog("The character could not be rigged as Humanoid.\nDownload it from Mixamo as 'FBX for Unity' with skin.");
                    return;
                }

                EditorUtility.DisplayProgressBar("Rafidain", "Importing animations…", 0.4f);
                var clips = PrepareAnimations(avatar, AnimFolder);
                if (!clips.ContainsKey("idle"))
                {
                    Dialog("At least an Idle animation is needed in:\n" + AnimFolder);
                    return;
                }

                EditorUtility.DisplayProgressBar("Rafidain", "Building the Animator…", 0.7f);
                var controller = BuildController(clips, ControllerPath);

                EditorUtility.DisplayProgressBar("Rafidain", "Putting Furat in the world…", 0.9f);
                PlaceModel(furat, modelPath, avatar, controller);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Dialog("Furat is ready.\n\nPress Play.\n\nIf the sword sits wrong in his hand, select Furat > FuratModel > … > RightHand > WeaponPivot and rotate it in the Inspector.");
        }

        // ------------------------------------------------------------------

        static Avatar PrepareModel(string modelPath, string Root)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.SaveAndReimport();
            }

            string texFolder = Root + "/Textures";
            if (!AssetDatabase.IsValidFolder(texFolder))
            {
                RafidainSceneBuilder.EnsureFolder(texFolder);
                try { mi.ExtractTextures(texFolder); } catch { /* some FBX have no embedded textures */ }
                AssetDatabase.Refresh();
            }

            return AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
        }

        static Dictionary<string, AnimationClip> PrepareAnimations(Avatar avatar, string animFolder)
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var raw in Directory.GetFiles(animFolder, "*.fbx", SearchOption.AllDirectories))
            {
                string path = raw.Replace('\\', '/');
                string key = Classify(Path.GetFileNameWithoutExtension(path));
                if (key == null || clips.ContainsKey(key)) continue;

                var ai = (ModelImporter)AssetImporter.GetAtPath(path);
                ai.animationType = ModelImporterAnimationType.Human;
                ai.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                ai.sourceAvatar = avatar;

                var defs = ai.defaultClipAnimations;
                if (defs.Length > 0)
                {
                    var c = defs[0];
                    c.name = key;
                    bool loop = Looping.Contains(key);
                    c.loopTime = loop;
                    c.loopPose = loop;
                    // Bake root motion into the pose: the game moves Furat, not the animation
                    c.lockRootRotation = true;
                    c.keepOriginalOrientation = true;
                    c.lockRootHeightY = true;
                    c.keepOriginalPositionY = true;
                    c.lockRootPositionXZ = true;
                    ai.clipAnimations = new[] { c };
                }
                ai.SaveAndReimport();

                var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .FirstOrDefault(x => !x.name.StartsWith("__preview__"));
                if (clip != null) clips[key] = clip;
            }
            return clips;
        }

        static string Classify(string fileName)
        {
            string n = fileName.ToLowerInvariant();
            if (n.Contains("idle")) return "idle";
            if (n.Contains("walk")) return "walk";
            if (n.Contains("sprint")) return "sprint";
            if (n.Contains("run") || n.Contains("jog")) return "run";
            if (n.Contains("roll") || n.Contains("dodge")) return "roll";
            if (n.Contains("death") || n.Contains("dying") || n.Contains("die")) return "death";
            if (n.Contains("impact") || n.Contains("hit") || n.Contains("react")) return "hit";
            if (n.Contains("drink") || n.Contains("heal") || n.Contains("potion")) return "heal";
            if (n.Contains("heavy") || n.Contains("great") || n.Contains("power") || n.Contains("jump attack")) return "heavy";
            if (n.Contains("attack") || n.Contains("slash") || n.Contains("swing")) return "attack";
            return null;
        }

        static AnimatorController BuildController(Dictionary<string, AnimationClip> clips, string controllerPath, float attackSeconds = 0.62f, float heavySeconds = 1.3f)
        {
            AssetDatabase.DeleteAsset(controllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            foreach (var t in new[] { "Attack", "Heavy", "Dodge", "Hit", "Heal" })
                ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            var sm = ctrl.layers[0].stateMachine;

            // Locomotion blend tree driven by real movement speed (m/s)
            var loco = ctrl.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(clips["idle"], 0f);
            if (clips.TryGetValue("walk", out var walk)) tree.AddChild(walk, 2.2f);
            if (clips.TryGetValue("run", out var run)) tree.AddChild(run, 4.8f);
            if (clips.TryGetValue("sprint", out var sprint)) tree.AddChild(sprint, 7.8f);
            else if (run != null) tree.AddChild(run, 7.8f);
            sm.defaultState = loco;

            // One-shot actions, each fitted to the gameplay timing
            AddAction(sm, loco, clips, "attack", "Attack", attackSeconds);
            AddAction(sm, loco, clips, "heavy", "Heavy", heavySeconds);
            AddAction(sm, loco, clips, "roll", "Dodge", 0.55f);
            AddAction(sm, loco, clips, "hit", "Hit", 0.4f);
            AddAction(sm, loco, clips, "heal", "Heal", 1.0f);

            if (clips.TryGetValue("death", out var death))
            {
                var dead = sm.AddState("Death");
                dead.motion = death;
                var toDead = sm.AddAnyStateTransition(dead);
                toDead.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
                toDead.canTransitionToSelf = false;
                toDead.hasExitTime = false;
                toDead.duration = 0.15f;
                var revive = dead.AddTransition(loco);
                revive.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
                revive.hasExitTime = false;
                revive.duration = 0.1f;
            }

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        static void AddAction(AnimatorStateMachine sm, AnimatorState loco, Dictionary<string, AnimationClip> clips,
            string key, string trigger, float gameplaySeconds)
        {
            if (!clips.TryGetValue(key, out var clip)) return;
            var st = sm.AddState(key);
            st.motion = clip;
            st.speed = Mathf.Clamp(clip.length / gameplaySeconds, 0.5f, 3f);

            var enter = sm.AddAnyStateTransition(st);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            enter.canTransitionToSelf = false;
            enter.hasExitTime = false;
            enter.duration = 0.06f;

            var back = st.AddTransition(loco);
            back.hasExitTime = true;
            back.exitTime = 0.88f;
            back.duration = 0.12f;
        }

        static void PlaceModel(PlayerStats furat, string modelPath, Avatar avatar, AnimatorController controller)
        {
            var root = furat.transform;
            var old = root.Find("FuratModel");
            var combat = furat.GetComponent<PlayerCombat>();

            // keep the weapon safe before removing an old model
            if (combat != null && combat.weaponPivot != null && old != null && combat.weaponPivot.IsChildOf(old))
                combat.weaponPivot.SetParent(root, true);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            model.name = "FuratModel";
            model.transform.localPosition = new Vector3(0f, -1f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // scale to ~1.85 m tall
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                if (b.size.y > 0.01f) model.transform.localScale = Vector3.one * (1.85f / b.size.y);
            }

            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // hide the capsule and its visor
            var capsule = root.GetComponent<MeshRenderer>();
            if (capsule != null) capsule.enabled = false;
            var visor = root.Find("Visor");
            if (visor != null) visor.gameObject.SetActive(false);

            // sword into the right hand
            if (combat != null && combat.weaponPivot != null)
            {
                var hand = model.GetComponentsInChildren<Transform>()
                    .FirstOrDefault(t => t.name.EndsWith("RightHand"));
                if (hand != null)
                {
                    combat.weaponPivot.SetParent(hand, true);
                    combat.weaponPivot.position = hand.position;
                    combat.weaponPivot.rotation = hand.rotation;
                    combat.animatedWeapon = true;
                    EditorUtility.SetDirty(combat);
                }
            }

            var pa = furat.GetComponent<PlayerAnimator>();
            if (pa == null) pa = furat.gameObject.AddComponent<PlayerAnimator>();
            pa.animator = anim;

            EditorSceneManager.MarkSceneDirty(furat.gameObject.scene);
            EditorSceneManager.SaveScene(furat.gameObject.scene);
        }

        // ==================================================================
        // Enemies
        // ==================================================================

        const string EnemyRoot = "Assets/Rafidain/Characters/Enemies";
        static readonly string[] EnemyTypes = { "Hollow", "Brute", "Guardian" };

        [MenuItem("Rafidain/Setup Enemy Characters (Mixamo)")]
        public static void SetupEnemies()
        {
            foreach (var t in EnemyTypes)
            {
                RafidainSceneBuilder.EnsureFolder($"{EnemyRoot}/{t}/Model");
                RafidainSceneBuilder.EnsureFolder($"{EnemyRoot}/{t}/Animations");
            }
            AssetDatabase.Refresh();

            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include);
            if (enemies.Length == 0) { Dialog("Open the game scene first."); return; }

            var ready = new Dictionary<string, (string model, Avatar avatar, AnimatorController ctrl)>();
            try
            {
                for (int i = 0; i < EnemyTypes.Length; i++)
                {
                    string type = EnemyTypes[i];
                    string root = $"{EnemyRoot}/{type}";
                    EditorUtility.DisplayProgressBar("Rafidain", $"Preparing {type}…", (i + 0.5f) / EnemyTypes.Length);
                    string model = FindFbx(root + "/Model");
                    if (model == null) continue;
                    var avatar = PrepareModel(model, root);
                    if (avatar == null || !avatar.isHuman) continue;
                    var clips = PrepareAnimations(avatar, root + "/Animations");
                    if (!clips.ContainsKey("idle")) continue;
                    float attack = type == "Guardian" ? 1.05f : type == "Brute" ? 1.15f : 0.85f;
                    var ctrl = BuildController(clips, $"{root}/{type}Animator.controller", attack, attack * 1.5f);
                    ready[type] = (model, avatar, ctrl);
                }

                int count = 0;
                foreach (var e in enemies)
                {
                    string type = e.isBoss ? "Guardian" : e.displayName.ToLowerInvariant().Contains("brute") ? "Brute" : "Hollow";
                    if (!ready.ContainsKey(type)) type = ready.ContainsKey("Hollow") ? "Hollow" : null;
                    if (type == null) continue;
                    var r = ready[type];
                    PlaceEnemyModel(e, r.model, r.avatar, r.ctrl);
                    count++;
                }

                if (count > 0)
                {
                    var scene = enemies[0].gameObject.scene;
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                Dialog(count > 0
                    ? $"{count} enemies now have real models.\n\nTypes ready: {string.Join(", ", ready.Keys)}"
                    : "No enemy models found.\n\nPut FBX files in:\n" + EnemyRoot + "/Hollow/Model (and /Animations)\nBrute and Guardian are optional.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void PlaceEnemyModel(EnemyAI enemy, string modelPath, Avatar avatar, AnimatorController ctrl)
        {
            var root = enemy.transform;
            var old = root.Find("EnemyModel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            model.name = "EnemyModel";
            model.transform.localPosition = new Vector3(0f, -1f, 0f);
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // fit to the capsule (local height 2 = 1.85 m body), the enemy's own scale does the rest
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var rr in renderers) b.Encapsulate(rr.bounds);
                float worldTarget = 1.85f * root.lossyScale.y;
                if (b.size.y > 0.01f) model.transform.localScale = Vector3.one * (worldTarget / b.size.y);
            }

            var anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var capsule = root.GetComponent<MeshRenderer>();
            if (capsule != null) capsule.enabled = false;
            var visor = root.Find("Visor");
            if (visor != null) visor.gameObject.SetActive(false);

            // the placeholder blade goes into the right hand
            var pivot = root.Find("WeaponPivot");
            var hand = model.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.EndsWith("RightHand"));
            if (pivot != null && hand != null)
            {
                pivot.SetParent(hand, true);
                pivot.position = hand.position;
                pivot.rotation = hand.rotation;
            }

            var ea = enemy.GetComponent<EnemyAnimator>();
            if (ea == null) ea = enemy.gameObject.AddComponent<EnemyAnimator>();
            ea.animator = anim;
        }

        static string FindFbx(string folder)
        {
            if (!Directory.Exists(folder)) return null;
            var f = Directory.GetFiles(folder, "*.fbx", SearchOption.TopDirectoryOnly).FirstOrDefault();
            return f?.Replace('\\', '/');
        }

        static void Dialog(string msg) => EditorUtility.DisplayDialog("Rafidain", msg, "OK");
    }
}
