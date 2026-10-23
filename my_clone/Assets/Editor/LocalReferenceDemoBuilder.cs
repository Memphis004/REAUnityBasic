using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyClone.LocalReference;
using MyClone.SpineSystem;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MyClone.EditorTools
{
	/// <summary>
	/// Builds Assets/_LocalReference/Demo.unity from the two extracted Spine sets.
	///
	/// Menu:  Tools/Local Reference/Build Demo Scene
	/// Batch: -executeMethod MyClone.EditorTools.LocalReferenceDemoBuilder.BuildBatch
	///
	/// The builder only touches Assets/_LocalReference (gitignored). It never reads the game
	/// folder; the extracted .skel/.atlas/.png files are the only inputs.
	/// </summary>
	public static class LocalReferenceDemoBuilder
	{
		const string Root = "Assets/_LocalReference";
		const string FullDir = Root + "/FullBody";
		const string ChibiDir = Root + "/Chibi";
		const string ScenePath = Root + "/Demo.unity";

		// Canvas layout in reference pixels (CanvasScaler 1920x1080, match 0.5).
		const float CanvasWidth = 1920f;
		const float CanvasHeight = 1080f;
		const float FullBodyHeight = CanvasHeight * 0.70f;   // 756 px
		const float ChibiHeight = CanvasHeight * 0.25f;      // 270 px
		const float FeetBaseline = -400f;                    // feet 140 px above the bottom edge
		const float ThirdHeight = CanvasHeight * 0.25f;      // 270 px

		// Actor slots as shares of the canvas (used by DemoCanvasLayout and the builder alike).
		const float FullSide = -0.25f;
		const float ChibiSide = 0.20f;
		const float ThirdSide = 0.40f;

		const string FullSkeleton = FullDir + "/1113002_1.skel.bytes";
		const string FullAtlasText = FullDir + "/1113002_1.atlas.txt";
		const string FullTexture = FullDir + "/1113002_1.png";
		const string FullAtlasAsset = FullDir + "/1113002_1_Atlas.asset";
		const string FullMaterial = FullDir + "/1113002_1_Material.mat";
		const string FullGrayMaterial = FullDir + "/1113002_1_Material_Gray.mat";
		const string FullDataAsset = FullDir + "/1113002_1_SkeletonData.asset";

		const string ChibiSkeleton = ChibiDir + "/1123011.skel.bytes";
		const string ChibiAtlasText = ChibiDir + "/1123011.atlas.txt";
		const string ChibiTexture = ChibiDir + "/1123011.png";
		const string ChibiAtlasAsset = ChibiDir + "/1123011_Atlas.asset";
		const string ChibiMaterial = ChibiDir + "/1123011_Material.mat";
		const string ChibiGrayMaterial = ChibiDir + "/1123011_Material_Gray.mat";
		const string ChibiDataAsset = ChibiDir + "/1123011_SkeletonData.asset";

		// Third extracted character (character/spinelib/body/1113101_1), copied from the game bundles
		// like the two above. Shown side by side with the other two.
		const string ThirdDir = Root + "/Third";
		const string ThirdSkeleton = ThirdDir + "/1113101_1.skel.bytes";
		const string ThirdAtlasText = ThirdDir + "/1113101_1.atlas.txt";
		const string ThirdTexture = ThirdDir + "/1113101_1.png";
		const string ThirdAtlasAsset = ThirdDir + "/1113101_1_Atlas.asset";
		const string ThirdMaterial = ThirdDir + "/1113101_1_Material.mat";
		const string ThirdGrayMaterial = ThirdDir + "/1113101_1_Material_Gray.mat";
		const string ThirdDataAsset = ThirdDir + "/1113101_1_SkeletonData.asset";

		// Optional: the shared weapon skeleton (character/spinelib/weapon). Used to mount a
		// second skeleton on the full body, mirroring dbcomponentcfg.followSlot in the game.
		const string WeaponDir = Root + "/Weapon";
		const string WeaponSkeleton = WeaponDir + "/weapon.skel.bytes";
		const string WeaponAtlasText = WeaponDir + "/weapon.atlas.txt";
		const string WeaponTexture = WeaponDir + "/weapon.png";
		const string WeaponAtlasAsset = WeaponDir + "/weapon_Atlas.asset";
		const string WeaponMaterial = WeaponDir + "/weapon_Material.mat";
		const string WeaponDataAsset = WeaponDir + "/weapon_SkeletonData.asset";
		const string MountBone = "wuqi";

		[MenuItem ("Tools/Local Reference/Build Demo Scene")]
		public		static void BuildFromMenu ()
		{
			// The build reopens Demo.unity; a dirty (e.g. Untitled) scene would otherwise be replaced
			// without warning.
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo ())
				return;

			Build ();
			EditorUtility.DisplayDialog ("Local Reference",
				"Built " + ScenePath + "\nVerification errors: " + s_LastVerifyErrors +
				"\nOpen it and press Play.", "OK");
		}


		public static void BuildBatch ()
		{
			try {
				Build ();
				if (s_LastVerifyErrors > 0) {
					Debug.LogError ("[LocalReferenceDemo] verification errors = " + s_LastVerifyErrors);
					EditorApplication.Exit (1);
					return;
				}
				EditorApplication.Exit (0);
			} catch (Exception e) {
				Debug.LogError ("[LocalReferenceDemo] FAILED: " + e);
				EditorApplication.Exit (1);
			}
		}

		static int s_LastVerifyErrors;

		public static void Build ()
		{
			Debug.Log ("[LocalReferenceDemo] build start");

			if (PlayerSettings.colorSpace == ColorSpace.Linear)
				Debug.LogWarning ("[LocalReferenceDemo] Linear color space: Spine PMA textures are not supported. " +
					"Spine's MaterialChecks would report this; the demo will still build.");

			ConfigureTexture (FullTexture);
			ConfigureTexture (ChibiTexture);

			var fullData = EnsureSkeletonData (
				FullSkeleton, FullAtlasText, FullTexture, FullAtlasAsset, FullMaterial, FullDataAsset);
			var chibiData = EnsureSkeletonData (
				ChibiSkeleton, ChibiAtlasText, ChibiTexture, ChibiAtlasAsset, ChibiMaterial, ChibiDataAsset);

			ConfigureTexture (ThirdTexture);
			var thirdData = EnsureSkeletonData (
				ThirdSkeleton, ThirdAtlasText, ThirdTexture, ThirdAtlasAsset, ThirdMaterial, ThirdDataAsset);

			var fullGray = EnsureGrayMaterial (FullGrayMaterial, FullMaterial, "1113002_1");
			var chibiGray = EnsureGrayMaterial (ChibiGrayMaterial, ChibiMaterial, "1123011");
			var thirdGray = EnsureGrayMaterial (ThirdGrayMaterial, ThirdMaterial, "1113101_1");

			SkeletonDataAsset weaponData = null;
			if (File.Exists (WeaponSkeleton)) {
				ConfigureTexture (WeaponTexture);
				weaponData = EnsureSkeletonData (
					WeaponSkeleton, WeaponAtlasText, WeaponTexture, WeaponAtlasAsset, WeaponMaterial, WeaponDataAsset);
			} else {
				Debug.LogWarning ("[LocalReferenceDemo] " + WeaponSkeleton +
					" not found; the mount button will be disabled");
			}

			RemoveUnusedNewSkeletonDataAsset ();

			var fullNormal = AssetDatabase.LoadAssetAtPath<Material> (FullMaterial);
			var chibiNormal = AssetDatabase.LoadAssetAtPath<Material> (ChibiMaterial);
			var thirdNormal = AssetDatabase.LoadAssetAtPath<Material> (ThirdMaterial);

			BuildScene (fullData, chibiData, thirdData, weaponData,
				fullNormal, fullGray, chibiNormal, chibiGray, thirdNormal, thirdGray);
			AssetDatabase.SaveAssets ();

			Debug.Log ("[LocalReferenceDemo] build done -> " + ScenePath);
		}

		// ------------------------------------------------------------------
		// Import
		// ------------------------------------------------------------------

		static void ConfigureTexture (string path)
		{
			var importer = AssetImporter.GetAtPath (path) as TextureImporter;
			if (importer == null)
				throw new Exception ("TextureImporter missing for " + path + " (is the file imported?)");

			// Spine PMA atlas: no alpha dilation, no mips, clamp, bilinear.
			importer.textureType = TextureImporterType.Default;
			importer.alphaIsTransparency = false;
			importer.mipmapEnabled = false;
			importer.sRGBTexture = true;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.filterMode = FilterMode.Bilinear;
			importer.npotScale = TextureImporterNPOTScale.None;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.maxTextureSize = 2048;
			importer.SaveAndReimport ();
		}

		static SkeletonDataAsset EnsureSkeletonData (
			string skeletonPath, string atlasTextPath, string texturePath,
			string atlasAssetPath, string materialPath, string dataAssetPath)
		{
			var skeletonText = AssetDatabase.LoadAssetAtPath<TextAsset> (skeletonPath);
			var atlasText = AssetDatabase.LoadAssetAtPath<TextAsset> (atlasTextPath);
			var texture = AssetDatabase.LoadAssetAtPath<Texture2D> (texturePath);
			if (skeletonText == null) throw new Exception ("Missing " + skeletonPath);
			if (atlasText == null) throw new Exception ("Missing " + atlasTextPath);
			if (texture == null) throw new Exception ("Missing " + texturePath);

			// The material is shared by the atlas asset. Use a SkeletonGraphic shader because
			// the demo renders through SpineUiActor (SkeletonGraphic), not SkeletonRenderer.
			var shader = Shader.Find ("Spine/SkeletonGraphic");
			if (shader == null)
				throw new Exception ("Shader 'Spine/SkeletonGraphic' not found. Is Assets/Spine imported?");

			var material = AssetDatabase.LoadAssetAtPath<Material> (materialPath);
			if (material == null) {
				material = new Material (shader);
				AssetDatabase.CreateAsset (material, materialPath);
			}
			material.shader = shader;
			material.mainTexture = texture;
			material.SetTexture ("_MainTex", texture);

			// Game materials have _StraightAlphaInput = 0 (premultiplied alpha).
			if (material.HasProperty ("_StraightAlphaInput"))
				material.SetFloat ("_StraightAlphaInput", 0f);
			material.DisableKeyword ("_STRAIGHT_ALPHA_INPUT");
			EditorUtility.SetDirty (material);

			var atlasAsset = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset> (atlasAssetPath);
			if (atlasAsset == null) {
				atlasAsset = SpineAtlasAsset.CreateRuntimeInstance (atlasText, new[] { material }, false);
				AssetDatabase.CreateAsset (atlasAsset, atlasAssetPath);
			} else {
				atlasAsset.atlasFile = atlasText;
				atlasAsset.materials = new[] { material };
				atlasAsset.Clear ();
				EditorUtility.SetDirty (atlasAsset);
			}

			var dataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset> (dataAssetPath);
			if (dataAsset == null) {
				dataAsset = SkeletonDataAsset.CreateRuntimeInstance (skeletonText, atlasAsset, false, 0.01f);
				AssetDatabase.CreateAsset (dataAsset, dataAssetPath);
			} else {
				dataAsset.skeletonJSON = skeletonText;
				dataAsset.atlasAssets = new AtlasAssetBase[] { atlasAsset };
				dataAsset.scale = 0.01f;
				dataAsset.Clear ();
			}
			dataAsset.defaultMix = 0.2f;
			EditorUtility.SetDirty (dataAsset);

			var data = dataAsset.GetSkeletonData (true);
			if (data == null)
				throw new Exception ("SkeletonDataAsset could not read " + skeletonPath +
					" (check the 3.8.75 guard patch in Assets/Spine/Runtime/spine-csharp/SkeletonBinary.cs)");

			Debug.LogFormat (
				"[LocalReferenceDemo] {0}: version={1} bones={2} slots={3} skins={4} animations={5}",
				dataAsset.name, data.Version, data.Bones.Count, data.Slots.Count,
				data.Skins.Count, data.Animations.Count);
			return dataAsset;
		}

		/// <summary>
		/// Gray variant of the normal material, with the shader SpineUiActor's ResolveGrayMaterial
		/// looks for (Assets/Scripts/SpineSystem/Shaders/Spine-SkeletonGraphic-Grayscale.shader).
		/// The PMA setup is identical to the normal material: the sprite is already premultiplied, the
		/// shader keeps Blend One OneMinusSrcAlpha and only desaturates after the alpha assembly.
		/// </summary>
		static Material EnsureGrayMaterial (string grayPath, string normalPath, string label)
		{
			var shader = Shader.Find ("Spine/SkeletonGraphic Grayscale");
			if (shader == null)
				throw new Exception ("Shader 'Spine/SkeletonGraphic Grayscale' not found. Expected " +
					"Assets/Scripts/SpineSystem/Shaders/Spine-SkeletonGraphic-Grayscale.shader");

			var normal = AssetDatabase.LoadAssetAtPath<Material> (normalPath);
			if (normal == null)
				throw new Exception ("Missing " + normalPath);

			var gray = AssetDatabase.LoadAssetAtPath<Material> (grayPath);
			if (gray == null) {
				gray = new Material (shader);
				AssetDatabase.CreateAsset (gray, grayPath);
			}

			gray.shader = shader;
			gray.mainTexture = normal.mainTexture;
			gray.SetTexture ("_MainTex", normal.mainTexture);
			gray.SetFloat ("_StraightAlphaInput", 0f);
			gray.SetFloat ("_GrayAmount", 1f);
			gray.DisableKeyword ("_STRAIGHT_ALPHA_INPUT");
			gray.DisableKeyword ("_CANVAS_GROUP_COMPATIBLE");
			EditorUtility.SetDirty (gray);

			Debug.LogFormat ("[LocalReferenceDemo] gray material {0}: shader={1} texture={2} grayAmount=1",
				label, shader.name, gray.mainTexture == null ? "null" : gray.mainTexture.name);
			return gray;
		}

		// ------------------------------------------------------------------
		// Scene
		// ------------------------------------------------------------------

		static void BuildScene (
			SkeletonDataAsset fullData, SkeletonDataAsset chibiData, SkeletonDataAsset thirdData,
			SkeletonDataAsset weaponData,
			Material fullNormal, Material fullGray, Material chibiNormal, Material chibiGray,
			Material thirdNormal, Material thirdGray)
		{
			// Reopen the existing scene so its Main Camera survives a rebuild; only a missing scene
			// falls back to an empty one, which keeps this menu usable on a fresh checkout.
			Scene scene;
			if (File.Exists (ScenePath)) {
				scene = EditorSceneManager.OpenScene (ScenePath, OpenSceneMode.Single);
				Debug.Log ("[LocalReferenceDemo] opened existing " + ScenePath);
			} else {
				scene = EditorSceneManager.NewScene (NewSceneSetup.EmptyScene, NewSceneMode.Single);
				Debug.Log ("[LocalReferenceDemo] created new scene " + ScenePath);
			}

			var camera = KeepMainCamera ();

			// Everything else is rebuilt from scratch (this is what removes the stale objects of an
			// older build, e.g. the single leftover GameObject of a failed manual attempt).
			foreach (var root in scene.GetRootGameObjects ()) {
				if (camera != null && root == camera.gameObject)
					continue;
				Object.DestroyImmediate (root);
			}

			new GameObject ("EventSystem", typeof (EventSystem), typeof (StandaloneInputModule));

			var canvasGo = new GameObject ("Canvas", typeof (Canvas), typeof (CanvasScaler), typeof (GraphicRaycaster));
			var canvas = canvasGo.GetComponent<Canvas> ();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = canvasGo.GetComponent<CanvasScaler> ();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2 (1920f, 1080f);
			scaler.matchWidthOrHeight = 0.5f;

			var title = CreateText (canvas.transform, "Title",
				"Spine local reference - 1113002_1 (70%) / 1123011 (25%) / 1113101_1 (25%)   |   wheel = zoom, right-drag = pan", 32, TextAnchor.UpperCenter);
			var titleRt = title.rectTransform;
			titleRt.anchorMin = new Vector2 (0f, 1f);
			titleRt.anchorMax = new Vector2 (1f, 1f);
			titleRt.pivot = new Vector2 (0.5f, 1f);
			titleRt.anchoredPosition = new Vector2 (0f, -24f);
			titleRt.sizeDelta = new Vector2 (0f, 56f);

			// Actors live under a full-canvas root so zoom/pan can move them without touching the UI.
			var actorRootGo = new GameObject ("ActorRoot", typeof (RectTransform));
			actorRootGo.transform.SetParent (canvas.transform, false);
			var actorRoot = (RectTransform) actorRootGo.transform;
			actorRoot.anchorMin = Vector2.zero;
			actorRoot.anchorMax = Vector2.one;
			actorRoot.offsetMin = Vector2.zero;
			actorRoot.offsetMax = Vector2.zero;

			Vector2 fullMeshSize, fullMeshCenter, chibiMeshSize, chibiMeshCenter, thirdMeshSize, thirdMeshCenter;
			var full = CreateActor (actorRoot, fullData, fullNormal, fullGray,
				"FullBody_1113002_1", new Vector2 (CanvasWidth * FullSide, FeetBaseline + FullBodyHeight * 0.5f), FullBodyHeight,
				out fullMeshSize, out fullMeshCenter);
			var chibi = CreateActor (actorRoot, chibiData, chibiNormal, chibiGray,
				"Chibi_1123011", new Vector2 (CanvasWidth * ChibiSide, FeetBaseline + ChibiHeight * 0.5f), ChibiHeight,
				out chibiMeshSize, out chibiMeshCenter);
			var third = CreateActor (actorRoot, thirdData, thirdNormal, thirdGray,
				"Third_1113101_1", new Vector2 (CanvasWidth * ThirdSide, FeetBaseline + ThirdHeight * 0.5f), ThirdHeight,
				out thirdMeshSize, out thirdMeshCenter);

			var layout = actorRootGo.AddComponent<DemoCanvasLayout> ();
			layout.Configure (
				new DemoCanvasLayout.Slot { actor = full, heightFraction = 0.70f, sideFraction = FullSide, referenceMeshSize = fullMeshSize, referenceMeshCenter = fullMeshCenter },
				new DemoCanvasLayout.Slot { actor = chibi, heightFraction = 0.25f, sideFraction = ChibiSide, referenceMeshSize = chibiMeshSize, referenceMeshCenter = chibiMeshCenter },
				new DemoCanvasLayout.Slot { actor = third, heightFraction = 0.25f, sideFraction = ThirdSide, referenceMeshSize = thirdMeshSize, referenceMeshCenter = thirdMeshCenter });

			var zoom = actorRootGo.AddComponent<DemoZoomPan> ();

			// Dark plate behind the label: the full body's dress reaches this corner when it is
			// scaled to 70% of the canvas, so white text alone would sit on the artwork.
			var infoPanelGo = new GameObject ("InfoPanel", typeof (RectTransform), typeof (Image));
			infoPanelGo.transform.SetParent (canvas.transform, false);
			var infoPanelRt = (RectTransform) infoPanelGo.transform;
			infoPanelRt.anchorMin = new Vector2 (0f, 0f);
			infoPanelRt.anchorMax = new Vector2 (0f, 0f);
			infoPanelRt.pivot = new Vector2 (0f, 0f);
			infoPanelRt.anchoredPosition = new Vector2 (24f, 118f);
			infoPanelRt.sizeDelta = new Vector2 (980f, 156f);
			infoPanelGo.GetComponent<Image> ().color = new Color (0.06f, 0.07f, 0.1f, 0.82f);

			var info = CreateText (infoPanelGo.transform, "Info", "", 20, TextAnchor.LowerLeft);
			var infoRt = info.rectTransform;
			infoRt.anchorMin = Vector2.zero;
			infoRt.anchorMax = Vector2.one;
			infoRt.pivot = new Vector2 (0.5f, 0.5f);
			infoRt.anchoredPosition = Vector2.zero;
			infoRt.sizeDelta = new Vector2 (-24f, -16f);

			var controllerGo = new GameObject ("DemoController", typeof (SpineDemoController));
			var controller = controllerGo.GetComponent<SpineDemoController> ();
			controller.fullBody = full;
			controller.chibi = chibi;
			controller.third = third;
			controller.info = info;
			controller.mountBone = MountBone;
			if (weaponData != null) {
				controller.mountData = weaponData;
				controller.mountSkin = PickWeaponSkin (weaponData.GetSkeletonData (true));
				Debug.Log ("[LocalReferenceDemo] mount skin: " + controller.mountSkin +
					" @ bone " + MountBone + " (" + weaponData.name + ")");
			}

			CreateControls (canvas.transform, controller, zoom);

			s_LastVerifyErrors = Verify (scene, camera, canvas, scaler, actorRoot, layout, zoom, full, chibi, third, info, controller);

			EditorSceneManager.MarkSceneDirty (scene);
			EditorSceneManager.SaveScene (scene, ScenePath);
		}

		static SpineUiActor CreateActor (
			Transform parent, SkeletonDataAsset data, Material normalMaterial, Material grayMaterial,
			string name, Vector2 anchoredPosition, float targetHeightPixels,
			out Vector2 referenceMeshSize, out Vector2 referenceMeshCenter)
		{
			referenceMeshSize = Vector2.one;
			referenceMeshCenter = Vector2.zero;
			var skeletonData = data.GetSkeletonData (true);
			string idle = PickIdle (skeletonData);
			string initialSkin = PickInitialSkin (skeletonData);

			// build:false so the material fields and CanvasGroupCompatible are in place before the
			// mesh settings are baked. false -> pmaVertexColors = true, matching the game's PMA
			// atlas/material.
			var actor = SpineActorFactory.CreateUi (parent, data, initialSkin, idle, true, name, build: false);
			actor.CanvasGroupCompatible = false;
			AssignMaterialFields (actor, normalMaterial, grayMaterial);
			actor.Build ();

			if (!actor.IsBuilt)
				Debug.LogError ("[LocalReferenceDemo] " + name + ": actor failed to build (see SpineUiActor errors above)");

			var rt = (RectTransform) actor.transform;
			rt.anchorMin = new Vector2 (0.5f, 0.5f);
			rt.anchorMax = new Vector2 (0.5f, 0.5f);
			rt.pivot = new Vector2 (0.5f, 0.5f);
			rt.anchoredPosition = anchoredPosition;

			// Editor preview: apply the same combined skin the runtime controller uses, then refresh
			// the mesh, so the saved scene shows a full character (Start does this in play mode).
			ApplyEditorSkin (actor, initialSkin);

			// Not every idle loops in place: the chibi's idle1 rolls ~1000 units sideways, which would
			// shrink the character to fit its travel. Pick a loop that stays put (chibi -> stand).
			string loop = PickStableLoop (actor, skeletonData, idle);
			if (!string.IsNullOrEmpty (loop) && loop != idle) {
				Debug.Log ("[LocalReferenceDemo] " + name + ": idle '" + idle +
					"' travels, using the stable loop '" + loop + "' instead");
				idle = loop;
				actor.Configure (data, initialSkin, idle, true);
			}
			if (!string.IsNullOrEmpty (idle))
				actor.Play (idle, true, 1f);

			// Fit to the tallest frame of the idle animation, not to the pose at time 0: the bbox grows
			// ~6% while the character bobs, which would push the feet under the button panel.
			Vector2 meshMin, meshMax;
			if (!MeasureIdleBounds (actor, idle, out meshMin, out meshMax)) {
				Debug.LogWarning ("[LocalReferenceDemo] " + name + ": no mesh to measure, keeping scale 1");
				meshMin = Vector2.zero;
				meshMax = Vector2.one;
			}
			float meshHeight = Mathf.Max (0.0001f, meshMax.y - meshMin.y);
			float meshWidth = Mathf.Max (0.0001f, meshMax.x - meshMin.x);
			referenceMeshSize = new Vector2 (meshWidth, meshHeight);
			referenceMeshCenter = (meshMin + meshMax) * 0.5f;
			float scale = Mathf.Clamp (targetHeightPixels * IdleFitMargin / meshHeight, 0.0001f, 1000f);
			rt.localScale = new Vector3 (scale, scale, 1f);

			// Keep the mesh visually centred on the requested point (the pivot is the rect centre)
			// and size the rect to the mesh so Canvas culling sees it.
			Vector2 meshCenter = (meshMin + meshMax) * 0.5f * scale;
			rt.anchoredPosition = anchoredPosition - meshCenter;
			rt.sizeDelta = new Vector2 (meshWidth * scale, meshHeight * scale);

			Debug.LogFormat ("[LocalReferenceDemo] actor {0}: mesh={1:0.#}x{2:0.#}px scale={3:0.###} -> {4:0.#}x{5:0.#}px on screen; multiRenderer={6} atlasMaterials={7} material={8} verts={9}",
				name, meshWidth, meshHeight, scale, meshWidth * scale, targetHeightPixels,
				actor.Graphic != null && actor.Graphic.allowMultipleCanvasRenderers,
				data.atlasAssets.Length == 0 ? 0 : data.atlasAssets[0].MaterialCount,
				actor.Graphic == null || actor.Graphic.material == null ? "null" : actor.Graphic.material.shader.name,
				actor.Graphic == null ? 0 : actor.Graphic.MeshGenerator.Buffers.vertexCount);
			Debug.LogFormat (
				"[LocalReferenceDemo] actor {0}: initialSkin={1} initialAnimation={2} loop=true height={3:0.#}px ({4:0.#}% of canvas) materials=[{5} | {6}]",
				name, initialSkin ?? "(none - no non-default skin)", idle ?? "(none)",
				targetHeightPixels, targetHeightPixels / CanvasHeight * 100f,
				normalMaterial == null ? "null" : normalMaterial.shader.name,
				grayMaterial == null ? "null" : grayMaterial.shader.name);
			return actor;
		}

		/// <summary>First skin that is not "default" (what the actor stores as Initial Skin).</summary>
		static string PickInitialSkin (SkeletonData data)
		{
			if (data == null) return null;
			foreach (var skin in data.Skins)
				if (skin != null && skin.Name != "default") return skin.Name;
			return null;
		}

		/// <summary>
		/// SpineUiActor keeps its materials in private [SerializeField] fields without setters.
		/// Setting them through SerializedObject keeps the scene references explicit (no magic
		/// fallback to Shader.Find at runtime) and persists them in Demo.unity.
		/// </summary>
		static void AssignMaterialFields (SpineUiActor actor, Material normalMaterial, Material grayMaterial)
		{
			var so = new SerializedObject (actor);
			var normal = so.FindProperty ("normalMaterial");
			var gray = so.FindProperty ("grayMaterial");
			if (normal == null || gray == null) {
				Debug.LogError ("[LocalReferenceDemo] SpineUiActor no longer has normalMaterial/grayMaterial " +
					"serialized fields; materials were not assigned.");
				return;
			}
			normal.objectReferenceValue = normalMaterial;
			gray.objectReferenceValue = grayMaterial;
			so.ApplyModifiedPropertiesWithoutUndo ();
		}

		/// <summary>Share of the target height used for the fit, leaving room for the idle breathing.</summary>
		const float IdleFitMargin = 0.97f;

		/// <summary>
		/// Bounds of the pose the user actually sees: changing the skin calls SetSlotsToSetupPose, so
		/// the setup pose is measured together with the first frame of the idle animation.
		/// </summary>
		static bool MeasureIdleBounds (SpineUiActor actor, string animation, out Vector2 min, out Vector2 max)
		{
			min = Vector2.zero;
			max = Vector2.zero;
			if (actor == null || actor.Graphic == null)
				return false;

			// Reset to the setup pose, then play the loop from t=0. Without the reset the measured box
			// depends on call order: the skin change leaves the setup pose, and animations that were
			// probed before (PickStableLoop) leave attachments that the next animation does not key.
			if (actor.Skeleton != null)
				actor.Skeleton.SetToSetupPose ();
			if (actor.AnimationState != null && !string.IsNullOrEmpty (animation))
				actor.Play (animation, true, 1f);

			if (!TryMeasureMesh (actor, out min, out max))
				return false;
			if (actor.AnimationState == null || string.IsNullOrEmpty (animation))
				return true;

			actor.Tick (0.1f);

			Vector2 firstMin, firstMax;
			if (!TryMeasureMesh (actor, out firstMin, out firstMax))
				return true;

			min = Vector2.Min (min, firstMin);
			max = Vector2.Max (max, firstMax);
			return true;
		}

		static bool TryMeasureMesh (SpineUiActor actor, out Vector2 min, out Vector2 max)
		{
			min = Vector2.zero;
			max = Vector2.zero;
			if (actor == null || actor.Graphic == null) return false;

			actor.Graphic.Update (0f);
			actor.Graphic.UpdateMesh ();

			var buffers = actor.Graphic.MeshGenerator.Buffers;
			int count = buffers.vertexCount;
			if (count == 0 || buffers.vertexBuffer == null) return false;

			min = new Vector2 (float.MaxValue, float.MaxValue);
			max = new Vector2 (float.MinValue, float.MinValue);
			for (int i = 0; i < count; i++) {
				var v = buffers.vertexBuffer[i];
				min = Vector2.Min (min, v);
				max = Vector2.Max (max, v);
			}
			return true;
		}

		static string PickWeaponSkin (SkeletonData data)
		{
			if (data == null) return null;

			// Weapon skins are grouped as "w_jian/w_jian_300108", "w_dao/...", ...
			foreach (var skin in data.Skins)
				if (skin != null && skin.Name.StartsWith ("w_jian/")) return skin.Name;
			foreach (var skin in data.Skins)
				if (skin != null && skin.Name != "default") return skin.Name;
			return null;
		}

		static string PickIdle (SkeletonData data)
		{
			string[] preferred = { "idle1", "idle", "stand", "stand2" };
			foreach (var p in preferred)
				foreach (var a in data.Animations)
					if (a.Name == p) return p;
			foreach (var a in data.Animations) {
				var n = a.Name.ToLowerInvariant ();
				if (n.Contains ("idle") || n.Contains ("stand")) return a.Name;
			}
			return data.Animations.Count > 0 ? data.Animations.Items[0].Name : null;
		}

		/// <summary>
		/// First idle/stand animation that loops in place. Candidates that make the bbox travel
		/// (the chibi idle1 rolls far sideways) are skipped: the demo fits the actor to a fixed
		/// height, so a travelling animation would either clip or shrink the character.
		/// Falls back to the preferred name when nothing is stable.
		/// </summary>
		static string PickStableLoop (SpineUiActor actor, SkeletonData data, string preferred)
		{
			var candidates = new List<string> ();
			string[] names = { preferred, "idle1", "idle", "stand", "stand2" };
			foreach (var name in names)
				if (!string.IsNullOrEmpty (name) && data.FindAnimation (name) != null && !candidates.Contains (name))
					candidates.Add (name);
			foreach (var animation in data.Animations) {
				var lower = animation.Name.ToLowerInvariant ();
				if ((lower.Contains ("idle") || lower.Contains ("stand")) && !candidates.Contains (animation.Name))
					candidates.Add (animation.Name);
			}

			foreach (var name in candidates)
				if (IsStableLoop (actor, name))
					return name;

			return preferred;
		}

		/// <summary>True when the bbox over one loop stays close to the first frame's bbox.</summary>
		static bool IsStableLoop (SpineUiActor actor, string animation)
		{
			if (actor == null || !actor.IsBuilt)
				return true;

			var entry = actor.Play (animation, true, 1f);
			if (entry == null || entry.Animation == null)
				return false;

			Vector2 firstMin, firstMax;
			if (!TryMeasureMesh (actor, out firstMin, out firstMax))
				return true;

			const int samples = 8;
			Vector2 min = firstMin;
			Vector2 max = firstMax;
			for (int i = 1; i <= samples; i++) {
				actor.SetProgress (i / (float) samples);
				Vector2 sampleMin, sampleMax;
				if (!TryMeasureMesh (actor, out sampleMin, out sampleMax))
					continue;
				min = Vector2.Min (min, sampleMin);
				max = Vector2.Max (max, sampleMax);
			}

			float firstWidth = Mathf.Max (0.0001f, firstMax.x - firstMin.x);
			float firstHeight = Mathf.Max (0.0001f, firstMax.y - firstMin.y);
			float widthGrowth = (max.x - min.x) / firstWidth;
			float heightGrowth = (max.y - min.y) / firstHeight;
			bool stable = widthGrowth <= 1.6f && heightGrowth <= 1.7f;

			Debug.LogFormat ("[LocalReferenceDemo] loop check {0}: growth {1:0.00}x{2:0.00}, stable={3}",
				animation, widthGrowth, heightGrowth, stable);
			return stable;
		}

		static void ApplyEditorSkin (SpineUiActor actor, string initialSkin)
		{
			if (actor == null || !actor.IsBuilt) return;
			var skeleton = actor.Skeleton;
			var groups = new Dictionary<string, List<Skin>> ();
			var order = new List<string> ();
			foreach (var skin in skeleton.Data.Skins) {
				if (skin == null || skin.Name == "default") continue;
				string key = skin.Name;
				int i = key.IndexOf ('_');
				if (i > 0) key = key.Substring (0, i);
				List<Skin> list;
				if (!groups.TryGetValue (key, out list)) {
					list = new List<Skin> ();
					groups.Add (key, list);
					order.Add (key);
				}
				list.Add (skin);
			}
			if (order.Count == 0) return;

			// Combined skin: variant index taken from the Initial Skin, so the saved preview and the
			// serialized field never disagree (the runtime controller builds the same combination).
			int variant = 0;
			if (!string.IsNullOrEmpty (initialSkin)) {
				foreach (var key in order) {
					int found = groups[key].FindIndex (s => s.Name == initialSkin);
					if (found >= 0) { variant = found; break; }
				}
			}

			var combined = new Skin ("demo_editor_combined");
			foreach (var key in order) {
				var list = groups[key];
				combined.AddSkin (list[variant % list.Count]);
			}

			skeleton.SetSkin ((Skin) null);
			skeleton.SetSkin (combined);
			skeleton.SetSlotsToSetupPose ();
		}

		// ------------------------------------------------------------------
		// UI helpers
		// ------------------------------------------------------------------

		static void CreateControls (Transform parent, SpineDemoController controller, DemoZoomPan zoom)
		{
			var panel = new GameObject ("Controls", typeof (RectTransform), typeof (Image));
			panel.transform.SetParent (parent, false);
			var panelRt = (RectTransform) panel.transform;
			panelRt.anchorMin = new Vector2 (0.5f, 0f);
			panelRt.anchorMax = new Vector2 (0.5f, 0f);
			panelRt.pivot = new Vector2 (0.5f, 0f);
			panelRt.anchoredPosition = new Vector2 (0f, 20f);
			panelRt.sizeDelta = new Vector2 (1830f, 84f);
			panel.GetComponent<Image> ().color = new Color (0.12f, 0.13f, 0.17f, 0.9f);

			CreateButton (panel.transform, "Next Skin", new Vector2 (-770f, 0f), controller.NextSkin);
			CreateButton (panel.transform, "Next Animation", new Vector2 (-550f, 0f), controller.NextAnimation);
			CreateButton (panel.transform, "Flip X", new Vector2 (-330f, 0f), controller.ToggleFlip);
			CreateButton (panel.transform, "Toggle Gray", new Vector2 (-110f, 0f), controller.ToggleGray);
			CreateButton (panel.transform, "Reset View", new Vector2 (110f, 0f), zoom.ResetView);
			CreateButton (panel.transform, "Mount", new Vector2 (330f, 0f), controller.ToggleMount);
			CreateButton (panel.transform, "Audit Skins", new Vector2 (550f, 0f), controller.AuditSkins);
			CreateButton (panel.transform, "Screenshot", new Vector2 (770f, 0f), controller.CaptureScreenshot);
		}

		static void CreateButton (Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
		{
			var go = new GameObject (label, typeof (RectTransform), typeof (Image), typeof (Button));
			go.transform.SetParent (parent, false);
			var rt = (RectTransform) go.transform;
			rt.sizeDelta = new Vector2 (200f, 56f);
			rt.anchoredPosition = pos;
			go.GetComponent<Image> ().color = new Color (0.24f, 0.26f, 0.33f, 1f);

			var text = CreateText (go.transform, "Text", label, 20, TextAnchor.MiddleCenter);
			text.rectTransform.anchorMin = Vector2.zero;
			text.rectTransform.anchorMax = Vector2.one;
			text.rectTransform.sizeDelta = Vector2.zero;

			var button = go.GetComponent<Button> ();
			UnityEventTools.AddPersistentListener (button.onClick, action);
		}

		static Text CreateText (Transform parent, string name, string value, int size, TextAnchor anchor)
		{
			var go = new GameObject (name, typeof (RectTransform), typeof (Text));
			go.transform.SetParent (parent, false);
			var text = go.GetComponent<Text> ();
			text.text = value;
			text.fontSize = size;
			text.alignment = anchor;
			text.color = Color.white;
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;

			var font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			if (font == null)
				font = Font.CreateDynamicFontFromOSFont ("Arial", size);
			text.font = font;
			return text;
		}

		// ------------------------------------------------------------------
		// Camera
		// ------------------------------------------------------------------

		/// <summary>Keeps the camera already in the scene; only creates one when the scene has none.</summary>
		static Camera KeepMainCamera ()
		{
			Camera found = null;
			var cameras = Object.FindObjectsOfType<Camera> ();
			for (int i = 0; i < cameras.Length; i++) {
				if (cameras[i].gameObject.name == "Main Camera") {
					found = cameras[i];
					break;
				}
				if (found == null)
					found = cameras[i];
			}

			GameObject go;
			if (found == null) {
				go = new GameObject ("Main Camera", typeof (Camera), typeof (AudioListener));
				Debug.Log ("[LocalReferenceDemo] no camera in the scene -> created Main Camera");
			} else {
				go = found.gameObject;
				if (go.GetComponent<AudioListener> () == null)
					go.AddComponent<AudioListener> ();
				Debug.Log ("[LocalReferenceDemo] kept existing camera '" + go.name + "'");
			}

			go.tag = "MainCamera";
			var camera = go.GetComponent<Camera> ();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color (0.07f, 0.08f, 0.11f, 1f);
			camera.orthographic = true;
			camera.orthographicSize = 5f;
			go.transform.position = new Vector3 (0f, 0f, -10f);
			return camera;
		}

		// ------------------------------------------------------------------
		// Verify (edit mode, runs at the end of the build)
		// ------------------------------------------------------------------

		static int Verify (
			Scene scene, Camera camera, Canvas canvas, CanvasScaler scaler,
			RectTransform actorRoot, DemoCanvasLayout layout, DemoZoomPan zoom,
			SpineUiActor full, SpineUiActor chibi, SpineUiActor third,
			Text info, SpineDemoController controller)
		{
			var errors = new List<string> ();

			if (camera == null) errors.Add ("no Main Camera in the scene");
			if (Object.FindObjectsOfType<Camera> ().Length != 1) errors.Add ("camera count = " + Object.FindObjectsOfType<Camera> ().Length);
			if (canvas == null) errors.Add ("no Canvas");
			if (scaler == null) errors.Add ("no CanvasScaler");
			if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
				errors.Add ("Canvas renderMode = " + canvas.renderMode + " (expected ScreenSpaceOverlay)");
			if (scaler != null) {
				if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
					errors.Add ("CanvasScaler mode = " + scaler.uiScaleMode);
				if (scaler.referenceResolution != new Vector2 (CanvasWidth, CanvasHeight))
					errors.Add ("CanvasScaler referenceResolution = " + scaler.referenceResolution);
			}
			if (Object.FindObjectOfType<EventSystem> () == null) errors.Add ("no EventSystem");

			CheckActor (full, "FULL BODY", FullBodyHeight, -1f, actorRoot, errors);
			CheckActor (chibi, "CHIBI", ChibiHeight, 1f, actorRoot, errors);
			CheckActor (third, "THIRD", ThirdHeight, 1f, actorRoot, errors);

			if (info == null) errors.Add ("controller.info = None");
			if (controller == null) {
				errors.Add ("no SpineDemoController");
			} else {
				if (controller.fullBody == null || controller.fullBody != full) errors.Add ("controller.fullBody is not the FullBody actor");
				if (controller.chibi == null || controller.chibi != chibi) errors.Add ("controller.chibi is not the Chibi actor");
				if (controller.third == null || controller.third != third) errors.Add ("controller.third is not the Third actor");
				if (controller.mountData == null) errors.Add ("controller.mountData = None");
				if (string.IsNullOrEmpty (controller.mountSkin)) errors.Add ("controller.mountSkin is empty");
				if (string.IsNullOrEmpty (controller.mountBone)) errors.Add ("controller.mountBone is empty");
			}

			CheckLayout (actorRoot, layout, zoom, full, chibi, third, errors);

			var buttons = canvas == null ? new Button[0] : canvas.GetComponentsInChildren<Button> (true);
			string[] required = { "Next Skin", "Next Animation", "Toggle Gray", "Flip X" };
			foreach (var label in required) {
				var button = FindButton (canvas, label);
				if (button == null) { errors.Add ("button missing: " + label); continue; }
				if (button.onClick.GetPersistentEventCount () == 0) errors.Add ("button has no listener: " + label);
			}

			foreach (var error in errors)
				Debug.LogError ("[LocalReferenceDemo][verify] " + error);

			Debug.LogFormat ("[LocalReferenceDemo][verify] scene={0} cameras={1} canvases={2} buttons={3} errors={4}",
				scene.name, Object.FindObjectsOfType<Camera> ().Length, Object.FindObjectsOfType<Canvas> ().Length,
				buttons.Length, errors.Count);
			return errors.Count;
		}

		/// <summary>
		/// Checks the aspect-independent layout: the same mesh box is mapped onto the live canvas rect,
		/// so 70%-/25%-of-height must hold for 16:9, 4:3 and 21:9 (the values are applied explicitly
		/// here because an edit-mode Canvas has no screen-driven rect).
		/// </summary>
		static void CheckLayout (
			RectTransform actorRoot, DemoCanvasLayout layout, DemoZoomPan zoom,
			SpineUiActor full, SpineUiActor chibi, SpineUiActor third, List<string> errors)
		{
			if (actorRoot == null) { errors.Add ("no ActorRoot"); return; }
			if (actorRoot.parent == null || actorRoot.parent.GetComponent<Canvas> () == null)
				errors.Add ("ActorRoot is not a child of the Canvas");
			if (layout == null) { errors.Add ("ActorRoot has no DemoCanvasLayout"); return; }
			if (zoom == null) errors.Add ("ActorRoot has no DemoZoomPan");
			if (Mathf.Abs (layout.FitMargin - IdleFitMargin) > 0.0001f)
				errors.Add ("DemoCanvasLayout fitMargin " + layout.FitMargin + " != builder margin " + IdleFitMargin);

			var slots = layout.Slots;
			if (slots.Length != 3) errors.Add ("DemoCanvasLayout slots = " + slots.Length);

			var expectedActors = new[] { full, chibi, third };
			for (int i = 0; i < slots.Length && i < expectedActors.Length; i++) {
				if (slots[i] == null || slots[i].actor != expectedActors[i])
					errors.Add ("layout slot " + i + " is not the expected actor");
				else if (slots[i].referenceMeshSize.y <= 0.5f)
					errors.Add ("layout slot " + i + " has no measured mesh size");
			}

			Vector2[] sizes = {
				new Vector2 (CanvasWidth, CanvasHeight),   // 16:9 reference
				new Vector2 (1663f, 1247f),                // 4:3
				new Vector2 (2217f, 935f)                  // 21:9
			};

			foreach (var size in sizes) {
				layout.ApplyForSize (size);
				for (int i = 0; i < slots.Length; i++) {
					var slot = slots[i];
					if (slot == null || slot.actor == null || !slot.actor.IsBuilt)
						continue;

					var rt = (RectTransform) slot.actor.transform;
					float height = slot.referenceMeshSize.y * rt.localScale.y;
					float ratio = height / size.y;
					float wanted = slot.heightFraction * layout.FitMargin;
					float centreX = rt.anchoredPosition.x + slot.referenceMeshCenter.x * rt.localScale.x;
					float wantedX = size.x * slot.sideFraction;

					Debug.LogFormat ("[LocalReferenceDemo][verify] layout {0:0}x{1:0} slot {2}: {3:0.#}% (wanted {4:0.#}%) x={5:0.#} (wanted {6:0.#})",
						size.x, size.y, i, ratio * 100f, wanted * 100f, centreX, wantedX);
					if (Mathf.Abs (ratio - wanted) > 0.005f)
						errors.Add (string.Format ("layout {0:0}x{1:0} slot {2}: {3:0.#}% != {4:0.#}%", size.x, size.y, i, ratio * 100f, wanted * 100f));
					if (Mathf.Abs (centreX - wantedX) > 1f)
						errors.Add (string.Format ("layout {0:0}x{1:0} slot {2}: x={3:0.#} != {4:0.#}", size.x, size.y, i, centreX, wantedX));
				}
			}

			// Back to the build reference so the saved scene keeps the 1920x1080 transforms.
			layout.ApplyForSize (new Vector2 (CanvasWidth, CanvasHeight));

			if (zoom != null) {
				zoom.SetZoom (2.5f);
				if (Mathf.Abs (actorRoot.localScale.x - 2.5f) > 0.01f)
					errors.Add ("zoom: ActorRoot scale = " + actorRoot.localScale.x + " after SetZoom(2.5)");
				zoom.ResetView ();
				if (Mathf.Abs (actorRoot.localScale.x - 1f) > 0.001f || actorRoot.anchoredPosition.sqrMagnitude > 0.001f)
					errors.Add ("zoom: Reset View did not restore scale/position");
			}
		}

		static void CheckActor (SpineUiActor actor, string label, float targetHeight, float side, RectTransform actorRoot, List<string> errors)
		{
			if (actor == null) { errors.Add (label + ": actor is None"); return; }

			var rt = actor.transform as RectTransform;
			if (rt == null) { errors.Add (label + ": no RectTransform"); return; }
			if (actorRoot == null || rt.parent != actorRoot)
				errors.Add (label + ": not a child of ActorRoot");

			if (!actor.IsBuilt) {
				errors.Add (label + ": actor is not built");
				return;
			}
			if (actor.Graphic == null) {
				errors.Add (label + ": Graphic is null");
			} else if (actor.Graphic.MeshGenerator.Buffers.vertexCount == 0) {
				errors.Add (label + ": mesh has 0 vertices");
			}

			var normal = SerializedObjectField<Material> (actor, "normalMaterial");
			var gray = SerializedObjectField<Material> (actor, "grayMaterial");
			if (normal == null) errors.Add (label + ": normalMaterial = None");
			else if (normal.shader == null || normal.shader.name != "Spine/SkeletonGraphic")
				errors.Add (label + ": normalMaterial shader = " + (normal.shader == null ? "null" : normal.shader.name));
			if (gray == null) errors.Add (label + ": grayMaterial = None");
			else if (gray.shader == null || gray.shader.name != "Spine/SkeletonGraphic Grayscale")
				errors.Add (label + ": grayMaterial shader = " + (gray.shader == null ? "null" : gray.shader.name));

			if (actor.Skeleton.Skin == null) errors.Add (label + ": no skin applied");
			if (actor.AnimationState.GetCurrent (0) == null) errors.Add (label + ": no animation playing");

			Vector2 min, max;
			if (!MeasureIdleBounds (actor, actor.InitialAnimation, out min, out max)) {
				errors.Add (label + ": mesh cannot be measured");
				return;
			}

			float height = (max.y - min.y) * rt.localScale.y;
			float ratio = height / CanvasHeight;
			float expected = targetHeight * IdleFitMargin / CanvasHeight;

			// The rect is offset by the measured mesh centre, so the side is judged by the mesh, not
			// by the rect position (the full body's setup pose sits ~120 mesh units left of its rect).
			float meshCentreX = rt.anchoredPosition.x + (min.x + max.x) * 0.5f * rt.localScale.x;
			if (side < 0f && meshCentreX >= 0f) errors.Add (label + ": mesh is not on the left half (x=" + meshCentreX + ")");
			if (side > 0f && meshCentreX <= 0f) errors.Add (label + ": mesh is not on the right half (x=" + meshCentreX + ")");
			Debug.LogFormat ("[LocalReferenceDemo][verify] {0}: {1:0.#}px tall = {2:0.#}% of canvas (requested {3:0.#}%, fit {4:0.#}%), meshX={5:0.#} verts={6}",
				label, height, ratio * 100f, targetHeight / CanvasHeight * 100f, expected * 100f, meshCentreX,
				actor.Graphic == null ? 0 : actor.Graphic.MeshGenerator.Buffers.vertexCount);
			if (Mathf.Abs (ratio - expected) > 0.03f)
				errors.Add (string.Format ("{0}: height {1:0.#}% != expected fit {2:0.#}%", label, ratio * 100f, expected * 100f));
		}

		static T SerializedObjectField<T> (Object target, string field) where T : Object
		{
			var property = new SerializedObject (target).FindProperty (field);
			return property == null ? null : property.objectReferenceValue as T;
		}

		static Button FindButton (Canvas canvas, string label)
		{
			if (canvas == null)
				return null;

			var buttons = canvas.GetComponentsInChildren<Button> (true);
			for (int i = 0; i < buttons.Length; i++) {
				var text = buttons[i].GetComponentInChildren<Text> (true);
				if (text != null && text.text == label)
					return buttons[i];
			}
			return null;
		}

		// ------------------------------------------------------------------
		// Play check (batch): -executeMethod ...PlayCheckBatch
		// ------------------------------------------------------------------

		const string PlayCheckPref = "MyClone.LocalReference.PlayCheck";
		static int s_PlayCheckFrame;

		/// <summary>
		/// Opens Demo.unity and enters play mode so Start() builds the actors and the first frames tick.
		/// <see cref="ResumePlayCheckAfterReload"/> finishes the check after the domain reload and exits
		/// with 0 (pass), 2 (checks failed) or 1 (timeout / could not enter play mode).
		/// </summary>
		public static void PlayCheckBatch ()
		{
			try {
				EditorPrefs.SetBool (PlayCheckPref, true);
				s_PlayCheckFrame = 0;
				EditorSceneManager.OpenScene (ScenePath, OpenSceneMode.Single);
				Debug.Log ("[LocalReferenceDemo][play] entering play mode with " + ScenePath);
				EditorApplication.EnterPlaymode ();
			} catch (Exception e) {
				Debug.LogError ("[LocalReferenceDemo][play] could not enter play mode: " + e);
				EditorPrefs.DeleteKey (PlayCheckPref);
				EditorApplication.Exit (1);
			}
		}

		[InitializeOnLoadMethod]
		static void ResumePlayCheckAfterReload ()
		{
			if (!EditorPrefs.GetBool (PlayCheckPref, false))
				return;

			double started = EditorApplication.timeSinceStartup;
			bool done = false;

			EditorApplication.update += Tick;

			void Tick ()
			{
				if (done)
					return;

				double elapsed = EditorApplication.timeSinceStartup - started;
				if (elapsed > 180.0) {
					done = true;
					Debug.LogError ("[LocalReferenceDemo][play] TIMEOUT waiting for play mode");
					EditorPrefs.DeleteKey (PlayCheckPref);
					EditorApplication.Exit (1);
					return;
				}

				if (!EditorApplication.isPlaying)
					return;

				s_PlayCheckFrame++;
				if (s_PlayCheckFrame < 20)
					return;

				done = true;
				bool ok;
				try {
					ok = RunPlayChecks ();
				} catch (Exception e) {
					Debug.LogError ("[LocalReferenceDemo][play] EXCEPTION: " + e);
					ok = false;
				}

				EditorPrefs.DeleteKey (PlayCheckPref);
				Debug.Log ("[LocalReferenceDemo][play] " + (ok ? "PASS" : "FAIL"));
				EditorApplication.Exit (ok ? 0 : 2);
			}
		}

		static bool RunPlayChecks ()
		{
			var errors = new List<string> ();
			var controller = Object.FindObjectOfType<SpineDemoController> ();
			if (controller == null) {
				Debug.LogError ("[LocalReferenceDemo][play] no SpineDemoController in " + ScenePath);
				return false;
			}

			var canvas = Object.FindObjectOfType<Canvas> ();
			if (canvas == null) errors.Add ("no Canvas");
			if (controller.fullBody == null) errors.Add ("controller.fullBody = None");
			if (controller.chibi == null) errors.Add ("controller.chibi = None");
			if (controller.third == null) errors.Add ("controller.third = None");
			if (controller.info == null) errors.Add ("controller.info = None");

			float canvasHeight = CanvasHeight;
			var canvasRect = canvas == null ? null : canvas.transform as RectTransform;
			if (canvasRect != null && canvasRect.rect.height > 1f)
				canvasHeight = canvasRect.rect.height;

			CheckRuntimeActor (controller.fullBody, "FULL BODY", 0.70f, canvasHeight, errors);
			CheckRuntimeActor (controller.chibi, "CHIBI", 0.25f, canvasHeight, errors);
			CheckRuntimeActor (controller.third, "THIRD", 0.25f, canvasHeight, errors);

			CheckLiveLayout (errors);

			if (controller.info != null && string.IsNullOrEmpty (controller.info.text))
				errors.Add ("info text is empty after Start()");

			// Capture the untouched first frames (idle, normal material, no flip) before the buttons
			// mutate the state, so the picture is what a user sees on pressing Play.
			string screenshot = controller.SaveScreenshot ();
			if (string.IsNullOrEmpty (screenshot) || !File.Exists (screenshot))
				errors.Add ("screenshot was not written");
			else
				Debug.Log ("[LocalReferenceDemo][play] screenshot: " + screenshot);

			string[] required = { "Next Skin", "Next Animation", "Toggle Gray", "Flip X", "Reset View" };
			foreach (var label in required) {
				var button = FindButton (canvas, label);
				if (button == null) { errors.Add ("button missing: " + label); continue; }

				int before = VertexCount (controller.fullBody) + VertexCount (controller.chibi) + VertexCount (controller.third);
				try {
					button.onClick.Invoke ();
				} catch (Exception e) {
					errors.Add ("button '" + label + "' threw " + e.GetType ().Name + ": " + e.Message);
				}
				int after = VertexCount (controller.fullBody) + VertexCount (controller.chibi) + VertexCount (controller.third);
				Debug.LogFormat ("[LocalReferenceDemo][play] button '{0}': verts {1} -> {2}", label, before, after);
				if (after <= 0) errors.Add ("button '" + label + "' left the actors with no mesh");
			}

			var graphic = controller.fullBody == null ? null : controller.fullBody.Graphic;
			if (graphic != null && graphic.material != null &&
				graphic.material.shader.name.IndexOf ("Grayscale", StringComparison.OrdinalIgnoreCase) < 0)
				errors.Add ("Toggle Gray did not switch to the grayscale shader (material=" + graphic.material.shader.name + ")");

			if (controller.info != null)
				Debug.Log ("[LocalReferenceDemo][play] info text:\n" + controller.info.text);

			foreach (var error in errors)
				Debug.LogError ("[LocalReferenceDemo][play] " + error);
			Debug.LogFormat ("[LocalReferenceDemo][play] errors = {0}", errors.Count);
			return errors.Count == 0;
		}

		/// <summary>
		/// Live aspect test: re-lays the actors out for 4:3 while the game is running and checks the
		/// same 70% / 25% shares, then restores the real canvas size and the 1x view.
		/// </summary>
		static void CheckLiveLayout (List<string> errors)
		{
			var layout = Object.FindObjectOfType<DemoCanvasLayout> ();
			if (layout == null) { errors.Add ("no DemoCanvasLayout in play"); return; }

			var actorRoot = (RectTransform) layout.transform;
			if (actorRoot.parent == null || actorRoot.parent.GetComponent<Canvas> () == null)
				errors.Add ("ActorRoot is not a child of the Canvas");

			var zoom = layout.GetComponent<DemoZoomPan> ();
			if (zoom == null) errors.Add ("no DemoZoomPan on ActorRoot");

			Vector2 live = actorRoot.rect.size;
			Vector2 test = new Vector2 (1663f, 1247f);   // 4:3
			layout.ApplyForSize (test);

			for (int i = 0; i < layout.Slots.Length; i++) {
				var slot = layout.Slots[i];
				if (slot == null || slot.actor == null || !slot.actor.IsBuilt)
					continue;

				var rt = (RectTransform) slot.actor.transform;
				float ratio = slot.referenceMeshSize.y * rt.localScale.y / test.y;
				float wanted = slot.heightFraction * layout.FitMargin;
				float x = rt.anchoredPosition.x + slot.referenceMeshCenter.x * rt.localScale.x;

				Debug.LogFormat ("[LocalReferenceDemo][play] layout 4:3 slot {0}: {1:0.#}% (wanted {2:0.#}%) x={3:0.#} (wanted {4:0.#})",
					i, ratio * 100f, wanted * 100f, x, test.x * slot.sideFraction);
				if (Mathf.Abs (ratio - wanted) > 0.005f)
					errors.Add (string.Format ("live layout slot {0}: {1:0.#}% != {2:0.#}%", i, ratio * 100f, wanted * 100f));
			}

			layout.ApplyForSize (live);   // what Update will keep on the next frame

			if (zoom != null) {
				zoom.SetZoom (2.5f);
				if (Mathf.Abs (actorRoot.localScale.x - 2.5f) > 0.01f)
					errors.Add ("zoom: SetZoom(2.5) did not apply");
				zoom.ResetView ();
				if (Mathf.Abs (actorRoot.localScale.x - 1f) > 0.001f || actorRoot.anchoredPosition.sqrMagnitude > 0.001f)
					errors.Add ("zoom: Reset View did not restore scale/position");
			}
		}

		static void CheckRuntimeActor (SpineUiActor actor, string label, float heightFraction, float canvasHeight, List<string> errors)
		{
			if (actor == null) { errors.Add (label + ": None"); return; }
			if (!actor.IsBuilt) { errors.Add (label + ": not built after Start()"); return; }
			if (actor.Graphic == null) { errors.Add (label + ": Graphic is null"); return; }

			int verts = actor.Graphic.MeshGenerator.Buffers.vertexCount;
			string shader = actor.Graphic.material == null ? "null" : actor.Graphic.material.shader.name;
			var current = actor.AnimationState.GetCurrent (0);
			string animation = current == null || current.Animation == null ? "-" : current.Animation.Name;
			float time = current == null ? 0f : current.TrackTime;

			Debug.LogFormat ("[LocalReferenceDemo][play] {0}: verts={1} shader={2} skin={3} animation={4} t={5:0.00} scale={6:0.###}",
				label, verts, shader, actor.Skeleton.Skin == null ? "default" : actor.Skeleton.Skin.Name,
				animation, time, actor.transform.localScale.x);

			if (verts == 0) errors.Add (label + ": 0 vertices");
			if (shader != "Spine/SkeletonGraphic") errors.Add (label + ": shader = " + shader);
			if (actor.Skeleton.Skin == null) errors.Add (label + ": no skin after Start()");
			if (current == null) errors.Add (label + ": no animation after Start()");
			if (!string.IsNullOrEmpty (actor.InitialAnimation) && animation != actor.InitialAnimation)
				errors.Add (label + ": playing '" + animation + "' instead of the initial '" + actor.InitialAnimation + "'");

			Vector2 min, max;
			if (!TryMeasureMesh (actor, out min, out max)) {
				errors.Add (label + ": mesh cannot be measured");
				return;
			}

			float height = (max.y - min.y) * actor.transform.localScale.y;
			float ratio = height / canvasHeight;
			float expected = heightFraction;
			Debug.LogFormat ("[LocalReferenceDemo][play] {0}: {1:0.#}px tall = {2:0.#}% of canvas (target {3:0.#}%)",
				label, height, ratio * 100f, expected * 100f);
			// The runtime bbox follows the animation, so it is checked against a band instead of an
			// exact value: it must not overflow the target (% + 2) and must stay recognisably large.
			if (ratio > expected + 0.02f)
				errors.Add (string.Format ("{0}: height {1:0.#}% is taller than target {2:0.#}%", label, ratio * 100f, expected * 100f));
			else if (ratio < expected * 0.85f)
				errors.Add (string.Format ("{0}: height {1:0.#}% is far below target {2:0.#}%", label, ratio * 100f, expected * 100f));
		}

		static int VertexCount (SpineUiActor actor)
		{
			return actor == null || actor.Graphic == null ? 0 : actor.Graphic.MeshGenerator.Buffers.vertexCount;
		}

		// ------------------------------------------------------------------
		// Cleanup
		// ------------------------------------------------------------------

		/// <summary>Deletes Assets/New SkeletonDataAsset.asset when nothing references it.</summary>
		static void RemoveUnusedNewSkeletonDataAsset ()
		{
			const string target = "Assets/New SkeletonDataAsset.asset";
			if (!File.Exists (target))
				return;

			if (IsReferenced (target)) {
				Debug.Log ("[LocalReferenceDemo] keeping " + target + " (referenced by another asset)");
				return;
			}

			if (AssetDatabase.DeleteAsset (target))
				Debug.Log ("[LocalReferenceDemo] deleted unused " + target);
		}

		static bool IsReferenced (string target)
		{
			string[] extensions = { ".unity", ".prefab", ".asset", ".mat", ".controller", ".anim" };
			foreach (var path in AssetDatabase.GetAllAssetPaths ()) {
				if (!path.StartsWith ("Assets/", StringComparison.Ordinal)) continue;
				if (path == target) continue;
				if (!extensions.Any (e => path.EndsWith (e, StringComparison.OrdinalIgnoreCase))) continue;

				var deps = AssetDatabase.GetDependencies (path, false);
				for (int i = 0; i < deps.Length; i++)
					if (deps[i] == target) return true;
			}
			return false;
		}
	}
}
