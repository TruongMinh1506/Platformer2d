using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Platformer.Common;
using Platformer.Enemies;
using Platformer.Managers;
using Platformer.Player;
using Platformer.UI;

namespace Platformer.Editor
{
    /// <summary>
    /// Automation tool for Task 6 under Tools/Platformer/6 - Create Animations.
    /// Generates frame-based procedural animation sprites, AnimationClips, and AnimatorControllers
    /// for Player and Enemies with all required parameters and transitions.
    /// Generates landing dust and coin pickup particle system prefabs.
    /// Generates procedural synthesized audio clips for AudioManager.
    /// Updates all level scenes with volume sliders, audio event bindings, and animation controllers.
    /// </summary>
    public static class AnimationAndAudioBuilder
    {
        private const string AnimArtDir = "Assets/Art/Animations";
        private const string AnimClipDir = "Assets/Art/Animations/Clips";
        private const string PlayerAnimDir = "Assets/Art/Animations/Player";
        private const string EnemyAnimDir = "Assets/Art/Animations/Enemies";

        private const string PlayerAnimatorPath = "Assets/Art/Animations/Player_Animator.controller";
        private const string PatrollerAnimatorPath = "Assets/Art/Animations/Patroller_Animator.controller";
        private const string ChaserAnimatorPath = "Assets/Art/Animations/Chaser_Animator.controller";

        private const string DustPrefabPath = "Assets/Prefabs/DustParticle.prefab";
        private const string CoinPickupPrefabPath = "Assets/Prefabs/CoinPickupParticle.prefab";
        private const string HeartPickupPrefabPath = "Assets/Prefabs/HeartPickupParticle.prefab";
        private const string EnemyDefeatPrefabPath = "Assets/Prefabs/EnemyDefeatParticle.prefab";

        [MenuItem("Tools/Platformer/6 - Create Animations", priority = 6)]
        public static void CreateAnimationsAndPolish()
        {
            EnsureDirectories();

            // 1. Generate frame-based sprites for Player animations
            Sprite[] idleFrames = GeneratePlayerIdleFrames();
            Sprite[] runFrames = GeneratePlayerRunFrames();
            Sprite[] jumpFrames = GeneratePlayerJumpFrames();
            Sprite[] fallFrames = GeneratePlayerFallFrames();
            Sprite[] attackFrames = GeneratePlayerAttackFrames();
            Sprite[] hurtFrames = GeneratePlayerHurtFrames();
            Sprite[] dieFrames = GeneratePlayerDieFrames();

            // 2. Generate frame-based sprites for Enemy animations
            Sprite[] patrollerWalk = GeneratePatrollerWalkFrames();
            Sprite[] chaserWalk = GenerateChaserWalkFrames();

            // 3. Create Animation Clips
            AnimationClip clipIdle = CreateClip($"{AnimClipDir}/Player_Idle.anim", idleFrames, 4f, true);
            AnimationClip clipRun = CreateClip($"{AnimClipDir}/Player_Run.anim", runFrames, 8f, true);
            AnimationClip clipJump = CreateClip($"{AnimClipDir}/Player_Jump.anim", jumpFrames, 4f, false);
            AnimationClip clipFall = CreateClip($"{AnimClipDir}/Player_Fall.anim", fallFrames, 4f, false);
            AnimationClip clipAttack = CreateClip($"{AnimClipDir}/Player_Attack.anim", attackFrames, 10f, false);
            AnimationClip clipHurt = CreateClip($"{AnimClipDir}/Player_Hurt.anim", hurtFrames, 6f, false);
            AnimationClip clipDie = CreateClip($"{AnimClipDir}/Player_Die.anim", dieFrames, 4f, false);

            AnimationClip clipPatroller = CreateClip($"{AnimClipDir}/Patroller_Walk.anim", patrollerWalk, 4f, true);
            AnimationClip clipChaser = CreateClip($"{AnimClipDir}/Chaser_Walk.anim", chaserWalk, 6f, true);

            // 4. Build Player Animator Controller with parameters and transitions
            AnimatorController playerCtrl = BuildPlayerAnimator(PlayerAnimatorPath, clipIdle, clipRun, clipJump, clipFall, clipAttack, clipHurt, clipDie);

            // 5. Build Enemy Animator Controllers
            AnimatorController patrollerCtrl = BuildEnemyAnimator(PatrollerAnimatorPath, clipPatroller);
            AnimatorController chaserCtrl = BuildEnemyAnimator(ChaserAnimatorPath, clipChaser);

            // 6. Build Particle System Prefabs
            GameObject dustPrefab = BuildDustParticlePrefab();
            GameObject coinPickupPrefab = BuildCoinPickupParticlePrefab();
            GameObject heartPickupPrefab = BuildHeartPickupParticlePrefab();
            GameObject enemyDefeatPrefab = BuildEnemyDefeatParticlePrefab();

            // 7. Update Player Prefab
            UpdatePlayerPrefab(playerCtrl, dustPrefab);

            // 8. Update Enemy Prefabs
            UpdateEnemyPrefabs(patrollerCtrl, chaserCtrl, enemyDefeatPrefab);

            // 9. Update Collectible Prefabs
            UpdateCoinPrefab(coinPickupPrefab);
            UpdateHeartPrefab(heartPickupPrefab);

            // 10. Re-build / update all scenes with animations, audio, and volume sliders
            RebuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#2ecc71>[Platformer]</color> Animations, particles, audio, and volume sliders successfully created and assigned!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { AnimArtDir, AnimClipDir, PlayerAnimDir, EnemyAnimDir, "Assets/Prefabs", "Assets/Audio" };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        private static void UpdatePlayerPrefab(AnimatorController ctrl, GameObject dustPrefab)
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                TestSceneBuilder.CreateTestScene();
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);

            // Animator
            Animator anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;

            // PlayerAnimation
            PlayerAnimation pAnim = instance.GetComponent<PlayerAnimation>();
            if (pAnim == null) pAnim = instance.AddComponent<PlayerAnimation>();

            SerializedObject so = new SerializedObject(pAnim);
            so.FindProperty("animator").objectReferenceValue = anim;
            so.FindProperty("rb").objectReferenceValue = instance.GetComponent<Rigidbody2D>();
            so.FindProperty("playerController").objectReferenceValue = instance.GetComponent<PlayerController>();
            so.FindProperty("playerCombat").objectReferenceValue = instance.GetComponent<PlayerCombat>();
            so.FindProperty("playerHealth").objectReferenceValue = instance.GetComponent<Health>();
            so.FindProperty("landingDustPrefab").objectReferenceValue = dustPrefab;
            so.FindProperty("jumpDustPrefab").objectReferenceValue = dustPrefab;
            so.ApplyModifiedProperties();

            // PlayerSquashAndStretch
            PlayerSquashAndStretch pSquash = instance.GetComponent<PlayerSquashAndStretch>();
            if (pSquash == null) pSquash = instance.AddComponent<PlayerSquashAndStretch>();

            // PlayerGhostTrail
            PlayerGhostTrail pGhost = instance.GetComponent<PlayerGhostTrail>();
            if (pGhost == null) pGhost = instance.AddComponent<PlayerGhostTrail>();

            PrefabUtility.SaveAsPrefabAsset(instance, "Assets/Prefabs/Player.prefab");
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void UpdateEnemyPrefabs(AnimatorController pCtrl, AnimatorController cCtrl, GameObject defeatParticle)
        {
            // Patroller
            GameObject patrollerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Patroller.prefab");
            if (patrollerPrefab != null)
            {
                GameObject pInst = (GameObject)PrefabUtility.InstantiatePrefab(patrollerPrefab);
                Animator anim = pInst.GetComponent<Animator>();
                if (anim == null) anim = pInst.AddComponent<Animator>();
                anim.runtimeAnimatorController = pCtrl;

                EnemyPatroller ep = pInst.GetComponent<EnemyPatroller>();
                if (ep != null)
                {
                    SerializedObject so = new SerializedObject(ep);
                    so.FindProperty("deathEffectPrefab").objectReferenceValue = defeatParticle;
                    so.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(pInst, "Assets/Prefabs/Enemy_Patroller.prefab");
                UnityEngine.Object.DestroyImmediate(pInst);
            }

            // Chaser
            GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chaser.prefab");
            if (chaserPrefab != null)
            {
                GameObject cInst = (GameObject)PrefabUtility.InstantiatePrefab(chaserPrefab);
                Animator anim = cInst.GetComponent<Animator>();
                if (anim == null) anim = cInst.AddComponent<Animator>();
                anim.runtimeAnimatorController = cCtrl;

                EnemyChaser ec = cInst.GetComponent<EnemyChaser>();
                if (ec != null)
                {
                    SerializedObject so = new SerializedObject(ec);
                    so.FindProperty("deathEffectPrefab").objectReferenceValue = defeatParticle;
                    so.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(cInst, "Assets/Prefabs/Enemy_Chaser.prefab");
                UnityEngine.Object.DestroyImmediate(cInst);
            }
        }

        private static void UpdateCoinPrefab(GameObject pickupParticle)
        {
            GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Collectible_Coin.prefab");
            if (coinPrefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
                Collectible c = inst.GetComponent<Collectible>();
                if (c != null)
                {
                    SerializedObject so = new SerializedObject(c);
                    so.FindProperty("pickupEffectPrefab").objectReferenceValue = pickupParticle;
                    so.ApplyModifiedProperties();
                }
                PrefabUtility.SaveAsPrefabAsset(inst, "Assets/Prefabs/Collectible_Coin.prefab");
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        private static void UpdateHeartPrefab(GameObject pickupParticle)
        {
            GameObject heartPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Collectible_Heart.prefab");
            if (heartPrefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(heartPrefab);
                Collectible c = inst.GetComponent<Collectible>();
                if (c != null)
                {
                    SerializedObject so = new SerializedObject(c);
                    so.FindProperty("pickupEffectPrefab").objectReferenceValue = pickupParticle;
                    so.ApplyModifiedProperties();
                }
                PrefabUtility.SaveAsPrefabAsset(inst, "Assets/Prefabs/Collectible_Heart.prefab");
                UnityEngine.Object.DestroyImmediate(inst);
            }
        }

        private static void RebuildScenes()
        {
            // Re-run scene flow builder to propagate updated prefabs, audio binders, and volume sliders
            LevelSceneFlowBuilder.CreateLevels2And3AndFlow();
        }

        private static AnimatorController BuildPlayerAnimator(string path,
            AnimationClip idle, AnimationClip run, AnimationClip jump, AnimationClip fall,
            AnimationClip attack, AnimationClip hurt, AnimationClip die)
        {
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // Add parameters: Speed, IsGrounded, VerticalVelocity, Attack, Hurt, Dead
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;

            var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
            var sRun = sm.AddState("Run"); sRun.motion = run;
            var sJump = sm.AddState("Jump"); sJump.motion = jump;
            var sFall = sm.AddState("Fall"); sFall.motion = fall;
            var sAttack = sm.AddState("Attack"); sAttack.motion = attack;
            var sHurt = sm.AddState("Hurt"); sHurt.motion = hurt;
            var sDie = sm.AddState("Die"); sDie.motion = die;

            sm.defaultState = sIdle;

            // Transitions: Idle <-> Run
            var t1 = sIdle.AddTransition(sRun);
            t1.hasExitTime = false; t1.duration = 0f;
            t1.AddCondition(AnimatorConditionMode.Greater, 0.15f, "Speed");

            var t2 = sRun.AddTransition(sIdle);
            t2.hasExitTime = false; t2.duration = 0f;
            t2.AddCondition(AnimatorConditionMode.Less, 0.15f, "Speed");

            // AnyState -> Jump
            var tAnyJump = sm.AddAnyStateTransition(sJump);
            tAnyJump.hasExitTime = false; tAnyJump.duration = 0f; tAnyJump.canTransitionToSelf = false;
            tAnyJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            tAnyJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");
            tAnyJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");

            // Jump -> Fall
            var tJumpFall = sJump.AddTransition(sFall);
            tJumpFall.hasExitTime = false; tJumpFall.duration = 0f;
            tJumpFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

            // Fall -> Idle
            var tFallIdle = sFall.AddTransition(sIdle);
            tFallIdle.hasExitTime = false; tFallIdle.duration = 0f;
            tFallIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

            // AnyState -> Attack
            var tAnyAtk = sm.AddAnyStateTransition(sAttack);
            tAnyAtk.hasExitTime = false; tAnyAtk.duration = 0f;
            tAnyAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            tAnyAtk.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");

            var tAtkIdle = sAttack.AddTransition(sIdle);
            tAtkIdle.hasExitTime = true; tAtkIdle.exitTime = 1f; tAtkIdle.duration = 0f;

            // AnyState -> Hurt
            var tAnyHurt = sm.AddAnyStateTransition(sHurt);
            tAnyHurt.hasExitTime = false; tAnyHurt.duration = 0f;
            tAnyHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
            tAnyHurt.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");

            var tHurtIdle = sHurt.AddTransition(sIdle);
            tHurtIdle.hasExitTime = true; tHurtIdle.exitTime = 1f; tHurtIdle.duration = 0f;

            // AnyState -> Die
            var tAnyDie = sm.AddAnyStateTransition(sDie);
            tAnyDie.hasExitTime = false; tAnyDie.duration = 0f;
            tAnyDie.AddCondition(AnimatorConditionMode.If, 0, "Dead");

            return controller;
        }

        private static AnimatorController BuildEnemyAnimator(string path, AnimationClip walkClip)
        {
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = controller.layers[0].stateMachine;
            var sWalk = sm.AddState("Walk");
            sWalk.motion = walkClip;
            sm.defaultState = sWalk;
            return controller;
        }

        private static AnimationClip CreateClip(string path, Sprite[] frames, float fps, bool loop)
        {
            AnimationClip clip = new AnimationClip();
            clip.frameRate = fps;

            EditorCurveBinding binding = new EditorCurveBinding();
            binding.type = typeof(SpriteRenderer);
            binding.path = "";
            binding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = frames[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            if (loop)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static GameObject BuildDustParticlePrefab()
        {
            GameObject go = new GameObject("DustParticle");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = 0.35f;
            main.startSpeed = 1.6f;
            main.startSize = 0.18f;
            main.startColor = new Color(0.85f, 0.88f, 0.92f, 0.75f);
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 6) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f, 0.75f, 0.8f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, DustPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildCoinPickupParticlePrefab()
        {
            GameObject go = new GameObject("CoinPickupParticle");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.45f;
            main.loop = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.4f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.9f, 0.2f, 1f);
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.65f, 0.1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, CoinPickupPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildHeartPickupParticlePrefab()
        {
            GameObject go = new GameObject("HeartPickupParticle");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.45f;
            main.loop = false;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.0f;
            main.startSize = 0.24f;
            main.startColor = new Color(1f, 0.3f, 0.45f, 1f);
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.4f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.85f, 0.9f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, HeartPickupPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildEnemyDefeatParticlePrefab()
        {
            GameObject go = new GameObject("EnemyDefeatParticle");
            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 3.2f;
            main.startSize = 0.28f;
            main.startColor = new Color(1f, 0.85f, 0.4f, 1f);
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.75f, 0.65f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, EnemyDefeatPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        #region Sprite Frame Generators

        private static Sprite[] GeneratePlayerIdleFrames()
        {
            Sprite s0 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Idle_0.png", 0, 0, false, Color.clear);
            Sprite s1 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Idle_1.png", 0, 1, false, Color.clear);
            return new Sprite[] { s0, s1 };
        }

        private static Sprite[] GeneratePlayerRunFrames()
        {
            Sprite s0 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Run_0.png", -1, 0, false, Color.clear);
            Sprite s1 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Run_1.png", 0, 1, false, Color.clear);
            Sprite s2 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Run_2.png", 1, 0, false, Color.clear);
            Sprite s3 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Run_3.png", 0, 1, false, Color.clear);
            return new Sprite[] { s0, s1, s2, s3 };
        }

        private static Sprite[] GeneratePlayerJumpFrames()
        {
            Sprite s0 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Jump.png", 0, 2, false, Color.clear);
            return new Sprite[] { s0 };
        }

        private static Sprite[] GeneratePlayerFallFrames()
        {
            Sprite s0 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Fall.png", 0, -1, false, Color.clear);
            return new Sprite[] { s0 };
        }

        private static Sprite[] GeneratePlayerAttackFrames()
        {
            Sprite s0 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Attack_0.png", -1, 0, false, Color.clear);
            Sprite s1 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Attack_1.png", 2, 0, true, Color.clear);
            return new Sprite[] { s0, s1 };
        }

        private static Sprite[] GeneratePlayerHurtFrames()
        {
            Sprite s0 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Hurt.png", -2, 0, false, new Color(1f, 0.4f, 0.4f, 0.5f));
            return new Sprite[] { s0 };
        }

        private static Sprite[] GeneratePlayerDieFrames()
        {
            Sprite s0 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Die_0.png", -2, -2, false, new Color(1f, 0.2f, 0.2f, 0.7f));
            Sprite s1 = DrawPlayerFrame($"{PlayerAnimDir}/Player_Die_1.png", -4, -6, false, new Color(0.6f, 0.1f, 0.1f, 0.9f));
            return new Sprite[] { s0, s1 };
        }

        private static Sprite[] GeneratePatrollerWalkFrames()
        {
            Sprite s0 = DrawEnemyFrame($"{EnemyAnimDir}/Patroller_Walk_0.png", new Color(0.9f, 0.28f, 0.24f, 1f), 0);
            Sprite s1 = DrawEnemyFrame($"{EnemyAnimDir}/Patroller_Walk_1.png", new Color(0.9f, 0.28f, 0.24f, 1f), 1);
            return new Sprite[] { s0, s1 };
        }

        private static Sprite[] GenerateChaserWalkFrames()
        {
            Sprite s0 = DrawEnemyFrame($"{EnemyAnimDir}/Chaser_Walk_0.png", new Color(0.56f, 0.24f, 0.72f, 1f), 0);
            Sprite s1 = DrawEnemyFrame($"{EnemyAnimDir}/Chaser_Walk_1.png", new Color(0.56f, 0.24f, 0.72f, 1f), 1);
            return new Sprite[] { s0, s1 };
        }

        private static Sprite DrawPlayerFrame(string path, int legOffset, int bobOffset, bool thrustSword, Color tintOverlay)
        {
            int w = 32; int h = 48;
            Color body = new Color(0f, 0.82f, 0.83f, 1f);
            Color border = new Color(0.05f, 0.28f, 0.38f, 1f);
            Color visor = new Color(0.08f, 0.12f, 0.18f, 1f);
            Color eye = new Color(1f, 0.85f, 0.2f, 1f);
            Color boot = new Color(0.12f, 0.18f, 0.24f, 1f);
            Color sword = new Color(1f, 0.95f, 0.4f, 1f);

            return CreateSpriteFile(path, w, h, (x, y) =>
            {
                int py = y - bobOffset;
                if (thrustSword && py >= 18 && py <= 24 && x >= 24 && x <= 30) return sword;

                if (x < 4 || x > 27 || py < 2 || py > 45) return Color.clear;
                if ((x <= 6 || x >= 25) && (py <= 4 || py >= 43)) return Color.clear;

                if (x == 4 || x == 27 || py == 2 || py == 45) return border;

                // Boots with leg offset
                if (py <= 7)
                {
                    int lx = x + legOffset;
                    if (lx >= 5 && lx <= 13) return boot;
                    if (lx >= 18 && lx <= 26) return boot;
                    return Color.clear;
                }

                // Visor & Eyes
                if (py >= 29 && py <= 40 && x >= 7 && x <= 26)
                {
                    if (py >= 32 && py <= 36 && ((x >= 13 && x <= 16) || (x >= 20 && x <= 24)))
                        return eye;
                    return visor;
                }

                if (tintOverlay.a > 0.1f) return Color.Lerp(body, tintOverlay, tintOverlay.a);
                return body;
            });
        }

        private static Sprite DrawEnemyFrame(string path, Color baseColor, int step)
        {
            int w = 32; int h = 32;
            Color border = new Color(0.2f, 0.1f, 0.1f, 1f);
            Color eye = Color.white;
            Color pupil = Color.black;

            return CreateSpriteFile(path, w, h, (x, y) =>
            {
                if (x < 3 || x > 28 || y < 2 || y > 27) return Color.clear;
                if (y <= 5)
                {
                    int footShift = step * 2;
                    if ((x >= 5 + footShift && x <= 10 + footShift) || (x >= 20 - footShift && x <= 25 - footShift))
                        return border;
                    return Color.clear;
                }
                if (x == 3 || x == 28 || y == 6 || y == 27) return border;
                if (x >= 6 && x <= 12 && y >= 12 && y <= 17)
                {
                    if (x <= 8) return pupil;
                    return eye;
                }
                return baseColor;
            });
        }

        private static Sprite CreateSpriteFile(string relativePath, int width, int height, Func<int, int, Color> pixelColor)
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath.Substring("Assets/".Length));
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, pixelColor(x, y));
                }
            }
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(fullPath, bytes);

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        }

        #endregion
    }
}

