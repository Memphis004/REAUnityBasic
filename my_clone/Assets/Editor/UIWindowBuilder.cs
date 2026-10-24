using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MyClone.SpineSystem;
using MyClone.UI;
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
	/// Builds Assets/_Generated/UI/DiscipleWindow.prefab from the UnityPy dump of the game's
	/// UIDiscipleRoleInfo2Win prefab, then builds Assets/_Generated/UIDemo.unity.
	///
	/// Menu:  Tools/UI/Build Disciple Window (Placeholder)
	///        Tools/UI/Build Disciple Window (Local Reference Sprites)
	/// Batch: -executeMethod MyClone.EditorTools.UIWindowBuilder.BuildBatch
	///        -executeMethod MyClone.EditorTools.UIWindowBuilder.BuildBatchLocalSprites
	///
	/// The dump (Assets/_Generated/Data/disciple_window.json) holds names, hierarchy,
	/// RectTransform values and UI component settings. Game-only components are skipped and
	/// listed in the build report. No art or code from the game is copied into the prefab:
	/// placeholder colours are used unless the editor preference asks for the comparison
	/// sprites under Assets/_LocalReference/UI (gitignored).
	/// </summary>
	public static class UIWindowBuilder
	{
		const string DataPath = "Assets/_Generated/Data/disciple_window.json";
		const string ReportPath = "Assets/_Generated/Data/disciple_window_build_report.json";
		const string PrefabDir = "Assets/_Generated/UI";
		const string PrefabPath = PrefabDir + "/DiscipleWindow.prefab";
		const string ScenePath = "Assets/_Generated/UIDemo.unity";

		const string PortraitDir = "Assets/_LocalReference/Portrait";
		const string PortraitDataPath = PortraitDir + "/2110101_SkeletonData.asset";
		const string PortraitMaterialPath = PortraitDir + "/2110101_Material.mat";
		const string PortraitGrayPath = PortraitDir + "/2110101_Material_Gray.mat";
		const string LocalSpriteDir = "Assets/_LocalReference/UI/Sprites";
		const string UseLocalSpritesKey = "MyClone.UI.UseLocalReferenceSprites";

		const int UILayer = 5;
		static readonly Vector2 ReferenceResolution = new Vector2 (1334f, 750f);

		// ------------------------------------------------------------------
		// JSON DTOs (JsonUtility needs public fields)
		// ------------------------------------------------------------------

		[Serializable] class WindowDoc
		{
			public SourceDto source;
			public CanvasDto canvas;
			public WindowInfoDto window;
			public BindingDto[] luaBinding;
			public long rootId;
			public NodeDto[] nodes;
		}

		[Serializable] class SourceDto
		{
			public string bundle;
			public int bundleSize;
			public string root;
			public int gameObjectCount;
			public string notes;
		}

		[Serializable] class CanvasDto
		{
			public string layerPath;
			public int canvasIndex;
			public string renderMode;
			public string camera;
			public float planeDistance;
			public int sortingLayerId;
			public int sortingOrder;
			public bool pixelPerfect;
			public int additionalShaderChannels;
			public ScalerDto scaler;
			public CameraDto cameraSettings;
		}

		[Serializable] class ScalerDto
		{
			public string uiScaleMode;
			public float[] referenceResolution;
			public string screenMatchMode;
			public float matchWidthOrHeight;
			public float referencePixelsPerUnit;
		}

		[Serializable] class CameraDto
		{
			public bool orthographic;
			public float orthographicSize;
			public float nearClip;
			public float farClip;
			public float depth;
			public int clearFlags;
			public int cullingMask;
			public float[] backgroundColor;
		}

		[Serializable] class WindowInfoDto
		{
			public int hideType;
			public float[] hideScale;
			public int canvasIndex;
			public int windowPositionStyle;
			public float windowKillTime;
			public string luaEventName;
			public int applySafeArea;
			public int exportSelfTransformToChild;
			public int childTransformCount;
		}

		[Serializable] class BindingDto
		{
			public int index;
			public string name;
			public string path;
		}

		[Serializable] class NodeDto
		{
			public long id;
			public string name;
			public string path;
			public long parentId;
			public int siblingIndex;
			public bool active;
			public RectDto rect;
			public ComponentDto[] components;
			public string[] skipped;
		}

		[Serializable] class RectDto
		{
			public float[] anchorMin;
			public float[] anchorMax;
			public float[] anchoredPosition;
			public float[] sizeDelta;
			public float[] pivot;
			public float[] localScale;
			public QuatDto localRotation;
		}

		[Serializable] class QuatDto
		{
			public float x, y, z, w;
		}

		[Serializable] class ComponentDto
		{
			public string type;
			// Image
			public string sprite;
			public string spriteBundle;
			public int imageType;
			public float[] color;
			public bool raycastTarget;
			public bool preserveAspect;
			public string placeholderKind;
			// Text
			public string text;
			public string sampleText;
			public int fontSize;
			public int fontStyle;
			public string alignment;
			public bool richText;
			public bool bestFit;
			public int horizontalOverflow;
			public int verticalOverflow;
			public string font;
			// Button
			public int transition;
			public bool interactable;
			public int persistentCalls;
			// CanvasGroup
			public float alpha;
			public bool blocksRaycasts;
			public bool ignoreParentGroups;
			// mask
			public bool showMaskGraphic;
			// layout
			public float[] padding;
			public int childAlignment;
			public float spacing;
			public bool childForceExpandWidth;
			public bool childForceExpandHeight;
			public bool childControlWidth;
			public bool childControlHeight;
			public float[] cellSize;
			public float[] spacingXY;
			public int startCorner;
			public int startAxis;
			public int constraint;
			public int constraintCount;
			public string horizontalFit;
			public string verticalFit;
			public bool ignoreLayout;
			public float minWidth, minHeight, preferredWidth, preferredHeight, flexibleWidth, flexibleHeight;
			// scroll rect
			public bool horizontal;
			public bool vertical;
			public int movementType;
			public float elasticity;
			public bool inertia;
			public float decelerationRate;
			public float scrollSensitivity;
			// Outline
			public float[] effectDistance;
			public bool useGraphicAlpha;
			// enabled flag shared by graphics
			public bool enabled;
		}

		// ------------------------------------------------------------------
		// Menu entries
		// ------------------------------------------------------------------

		[MenuItem ("Tools/UI/Build Disciple Window (Placeholder)", false, 20)]
		public static void BuildPlaceholderMenu ()
		{
			EditorPrefs.SetBool (UseLocalSpritesKey, false);
			BuildAll (false);
		}

		[MenuItem ("Tools/UI/Build Disciple Window (Local Reference Sprites)", false, 21)]
		public static void BuildLocalSpritesMenu ()
		{
			EditorPrefs.SetBool (UseLocalSpritesKey, true);
			BuildAll (true);
		}

		[MenuItem ("Tools/UI/Open UI Demo Scene", false, 40)]
		public static void OpenDemoScene ()
		{
			if (File.Exists (ScenePath))
				EditorSceneManager.OpenScene (ScenePath);
			else
				Debug.LogWarning ("[UIWindowBuilder] " + ScenePath + " not found. Build the window first.");
		}

		public static void BuildBatch ()
		{
			EditorPrefs.SetBool (UseLocalSpritesKey, false);
			RunBatch (false);
		}

		public static void BuildBatchLocalSprites ()
		{
			EditorPrefs.SetBool (UseLocalSpritesKey, true);
			RunBatch (true);
		}

		static void RunBatch (bool useLocalSprites)
		{
			try {
				BuildAll (useLocalSprites);
				EditorApplication.Exit (0);
			} catch (Exception e) {
				Debug.LogException (e);
				EditorApplication.Exit (1);
			}
		}

		// ------------------------------------------------------------------
		// Build
		// ------------------------------------------------------------------

		static void BuildAll (bool useLocalSprites)
		{
			var doc = LoadDocument ();
			var report = new BuildReport {
				dataPath = DataPath,
				spriteMode = useLocalSprites ? "local-reference" : "placeholder",
				prefabPath = PrefabPath,
				scenePath = ScenePath,
				sourceBundle = doc.source != null ? doc.source.bundle : "?",
				gameObjectCount = doc.nodes.Length,
			};

			var prefab = BuildPrefab (doc, useLocalSprites, report);
			BuildScene (doc, prefab, report);

			File.WriteAllText (ReportPath, JsonUtility.ToJson (report, true));
			AssetDatabase.ImportAsset (ReportPath);
			AssetDatabase.SaveAssets ();

			Debug.LogFormat ("[UIWindowBuilder] built {0} ({1} nodes, sprites={2}), scene {3}. Skipped component types: {4}",
				PrefabPath, doc.nodes.Length, report.spriteMode, ScenePath,
				string.Join (", ", report.skippedTypes.Distinct ().ToArray ()));
		}

		static WindowDoc LoadDocument ()
		{
			if (!File.Exists (DataPath))
				throw new FileNotFoundException ("Prefab dump not found: " + DataPath);
			var doc = JsonUtility.FromJson<WindowDoc> (File.ReadAllText (DataPath));
			if (doc == null || doc.nodes == null || doc.nodes.Length == 0)
				throw new InvalidDataException ("Prefab dump is empty: " + DataPath);
			return doc;
		}

		[Serializable] class BuildReport
		{
			public string dataPath;
			public string prefabPath;
			public string scenePath;
			public string sourceBundle;
			public string spriteMode;
			public int gameObjectCount;
			public int images;
			public int texts;
			public int buttons;
			public bool spineActor;
			public string spineNote;
			public string[] skippedTypes;
			public string[] unresolvedSprites;
			public string[] notes;
		}

		static GameObject BuildPrefab (WindowDoc doc, bool useLocalSprites, BuildReport report)
		{
			Directory.CreateDirectory (PrefabDir);

			var byId = new Dictionary<long, RectTransform> ();
			var byPath = new Dictionary<string, RectTransform> ();
			var skipped = new List<string> ();
			var unresolved = new List<string> ();
			var spriteCache = new Dictionary<string, Sprite> ();
			GameObject rootGo = null;

			foreach (var node in doc.nodes) {
				RectTransform parent = null;
				if (node.parentId != 0) {
					if (!byId.TryGetValue (node.parentId, out parent))
						throw new InvalidDataException ("Parent " + node.parentId + " of " + node.path + " missing.");
				}

				var go = new GameObject (node.name, typeof (RectTransform));
				go.layer = UILayer;
				var rt = (RectTransform)go.transform;
				rt.SetParent (parent, false);
				ApplyRect (rt, node.rect);

				byId[node.id] = rt;
				byPath[node.path] = rt;

				foreach (var c in node.components ?? new ComponentDto[0]) {
					switch (c.type) {
					case "Image":
						int images = BuildImage (go, c, useLocalSprites, spriteCache, unresolved);
						report.images += images;
						break;
					case "Text":
						BuildText (go, c, useLocalSprites);
						report.texts++;
						break;
					case "Button":
						BuildButton (go, c);
						report.buttons++;
						break;
					case "CanvasGroup":
						BuildCanvasGroup (go, c);
						break;
					case "Mask":
						BuildMask (go, c);
						break;
					case "RectMask2D":
						go.AddComponent<RectMask2D> ();
						break;
					case "Outline":
						BuildOutline (go, c);
						break;
					case "VerticalLayoutGroup":
					case "HorizontalLayoutGroup":
					case "GridLayoutGroup":
						BuildLayout (go, c);
						break;
					case "ContentSizeFitter":
						BuildFitter (go, c);
						break;
					case "LayoutElement":
						BuildLayoutElement (go, c);
						break;
					case "ScrollRect":
						BuildScrollRect (go, c, byPath, node.path);
						break;
					case "UIModelShow":
						// Replaced by MyClone.SpineSystem.SpineUiActor below.
						skipped.Add ("UIModelShow");
						break;
					default:
						skipped.Add (c.type);
						break;
					}
				}

				foreach (var s in node.skipped ?? new string[0])
					skipped.Add (s);

				// A Button added before its Graphic keeps a null target; wire it after all components.
				var button = go.GetComponent<Button> ();
				if (button != null && button.targetGraphic == null) {
					var graphic = go.GetComponent<Graphic> ();
					if (graphic != null)
						button.targetGraphic = graphic;
				}

				if (node.parentId == 0)
					rootGo = go;

				// Final active state (children keep their own flags once the tree is complete).
				go.SetActive (node.active);
			}

			if (rootGo == null)
				throw new InvalidDataException ("Root GameObject not found in dump.");

			// Root must be active for the prefab/scene instance to show.
			rootGo.SetActive (true);

			int built = rootGo.GetComponentsInChildren<Transform> (true).Length;
			if (built != doc.nodes.Length)
				throw new InvalidDataException ("Hierarchy has " + built + " transforms but the dump has " +
					doc.nodes.Length + " nodes.");

			BuildModelActor (doc, byPath, report);

			var window = rootGo.AddComponent<DiscipleWindow> ();
			BindWindow (window, byPath);
			window.mockDisciples = CreateMockDisciples ();

			var prefab = PrefabUtility.SaveAsPrefabAsset (rootGo, PrefabPath);
			Object.DestroyImmediate (rootGo);

			report.skippedTypes = skipped.Distinct ().OrderBy (s => s).ToArray ();
			report.unresolvedSprites = unresolved.Distinct ().OrderBy (s => s).ToArray ();
			report.notes = new[] {
				"Hierarchy/RectTransform values come from the UnityPy dump; nothing is instantiated from the game.",
				"Window root keeps its stretched RectTransform (anchors 0..1) and #root its 100x100 centre rect.",
				"CSGUIWindowLua / CSGUIImage / CSGUIBaseItem / CSGUICommonLayoutGroup / CSGUIEffect are not reimplemented; Unity's Outline is.",
			};

			if (prefab == null)
				throw new Exception ("SaveAsPrefabAsset returned null for " + PrefabPath);

			AssetDatabase.SaveAssets ();
			return prefab;
		}

		static float[] Vector2Or (float[] v, float x, float y)
		{
			return (v != null && v.Length >= 2) ? v : new[] { x, y };
		}

		static void ApplyRect (RectTransform rt, RectDto rect)
		{
			if (rect == null)
				return;

			var min = Vector2Or (rect.anchorMin, 0f, 0f);
			var max = Vector2Or (rect.anchorMax, 0f, 0f);
			var pos = Vector2Or (rect.anchoredPosition, 0f, 0f);
			var size = Vector2Or (rect.sizeDelta, 0f, 0f);
			var pivot = Vector2Or (rect.pivot, 0.5f, 0.5f);
			var scale = Vector2Or (rect.localScale, 1f, 1f);

			rt.anchorMin = new Vector2 (min[0], min[1]);
			rt.anchorMax = new Vector2 (max[0], max[1]);
			rt.anchoredPosition = new Vector2 (pos[0], pos[1]);
			rt.sizeDelta = new Vector2 (size[0], size[1]);
			rt.pivot = new Vector2 (pivot[0], pivot[1]);
			rt.localScale = new Vector3 (scale[0], scale[1], 1f);
			if (rect.localRotation != null)
				rt.localRotation = new Quaternion (rect.localRotation.x, rect.localRotation.y,
					rect.localRotation.z, rect.localRotation.w);
		}

		static Color ToColor (float[] rgba, Color fallback)
		{
			if (rgba == null || rgba.Length < 4)
				return fallback;
			return new Color (rgba[0], rgba[1], rgba[2], rgba[3]);
		}

		static int BuildImage (GameObject go, ComponentDto c, bool useLocalSprites,
			Dictionary<string, Sprite> cache, List<string> unresolved)
		{
			var image = go.GetComponent<Image> ();
			if (image == null)
				image = go.AddComponent<Image> ();

			var original = ToColor (c.color, Color.white);
			image.raycastTarget = c.raycastTarget;
			image.preserveAspect = c.preserveAspect;
			image.enabled = c.enabled;

			Sprite sprite = null;
			if (useLocalSprites && !string.IsNullOrEmpty (c.sprite)) {
				if (!cache.TryGetValue (c.sprite, out sprite) || sprite == null) {
					sprite = LoadLocalSprite (c.sprite);
					cache[c.sprite] = sprite;
				}
				if (sprite == null)
					unresolved.Add (c.sprite + " (" + c.spriteBundle + ")");
			}

			if (sprite != null) {
				image.sprite = sprite;
				image.color = original;
				image.type = c.imageType == 1 ? Image.Type.Sliced : Image.Type.Simple;
			} else {
				// Icons/avatars are drawn as circles when their rect is roughly square, so the
				// placeholder reads like the original badge art instead of a coloured box.
				var rt = (RectTransform)go.transform;
				if (c.placeholderKind == "avatar" && IsRoughlySquare (rt.sizeDelta))
					image.sprite = EnsureCircleSprite ();
				else
					image.sprite = null;
				image.type = Image.Type.Simple;
				image.color = PlaceholderColor (c.placeholderKind, original);
			}

			return 1;
		}

		static bool IsRoughlySquare (Vector2 size)
		{
			if (size.x <= 0f || size.y <= 0f)
				return false;
			float ratio = size.x / size.y;
			return ratio >= 0.8f && ratio <= 1.25f;
		}

		/// <summary>White anti-aliased disc, generated once into _Generated (not taken from the game).</summary>
		static Sprite EnsureCircleSprite ()
		{
			const string dir = PrefabDir + "/Generated";
			const string path = dir + "/round_placeholder.png";

			var existing = AssetDatabase.LoadAssetAtPath<Sprite> (path);
			if (existing != null)
				return existing;

			Directory.CreateDirectory (dir);
			const int size = 128;
			float radius = size * 0.5f - 1f;
			var center = new Vector2 (size * 0.5f - 0.5f, size * 0.5f - 0.5f);
			var pixels = new Color32[size * size];
			for (int y = 0; y < size; y++) {
				for (int x = 0; x < size; x++) {
					float distance = Vector2.Distance (new Vector2 (x, y), center);
					float alpha = Mathf.Clamp01 (radius - distance);
					pixels[y * size + x] = new Color32 (255, 255, 255, (byte)(alpha * 255f));
				}
			}

			var texture = new Texture2D (size, size, TextureFormat.RGBA32, false);
			texture.SetPixels32 (pixels);
			texture.Apply ();
			File.WriteAllBytes (path, texture.EncodeToPNG ());
			Object.DestroyImmediate (texture);
			AssetDatabase.ImportAsset (path, ImportAssetOptions.ForceSynchronousImport);

			var importer = AssetImporter.GetAtPath (path) as TextureImporter;
			if (importer != null) {
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Single;
				importer.alphaIsTransparency = true;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Bilinear;
				importer.wrapMode = TextureWrapMode.Clamp;
				importer.SaveAndReimport ();
			}

			return AssetDatabase.LoadAssetAtPath<Sprite> (path);
		}

		static Color PlaceholderColor (string kind, Color original)
		{
			Color c;
			switch (kind) {
			case "button":
				c = new Color (0.46f, 0.37f, 0.24f, 1f);
				break;
			case "card":
				c = new Color (0.25f, 0.29f, 0.39f, 1f);
				break;
			case "avatar":
				c = new Color (0.56f, 0.34f, 0.36f, 1f);
				break;
			case "nameplate":
				c = new Color (0.62f, 0.57f, 0.47f, 1f);
				break;
			case "invisible":
				c = new Color (1f, 1f, 1f, 0f);
				break;
			default:
				c = new Color (0.14f, 0.17f, 0.25f, 1f);
				break;
			}
			c.a = original.a; // keep fully transparent click zones transparent
			return c;
		}

		static Sprite LoadLocalSprite (string name)
		{
			string path = LocalSpriteDir + "/" + name + ".png";
			if (!File.Exists (path))
				return null;

			var importer = AssetImporter.GetAtPath (path) as TextureImporter;
			if (importer == null) {
				AssetDatabase.ImportAsset (path, ImportAssetOptions.ForceUpdate);
				importer = AssetImporter.GetAtPath (path) as TextureImporter;
				if (importer == null)
					return null;
			}

			bool dirty = importer.textureType != TextureImporterType.Sprite
				|| importer.mipmapEnabled
				|| importer.alphaIsTransparency == false
				|| importer.spriteImportMode != SpriteImportMode.Single;
			if (dirty) {
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Single;
				importer.mipmapEnabled = false;
				importer.alphaIsTransparency = true;
				importer.sRGBTexture = true;
				importer.wrapMode = TextureWrapMode.Clamp;
				importer.filterMode = FilterMode.Bilinear;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				importer.spritePixelsPerUnit = 100f;
				importer.SaveAndReimport ();
			}
			return AssetDatabase.LoadAssetAtPath<Sprite> (path);
		}

		static void BuildText (GameObject go, ComponentDto c, bool useLocalSprites)
		{
			var text = go.AddComponent<Text> ();
			text.font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			text.fontSize = c.fontSize > 0 ? c.fontSize : 24;
			text.fontStyle = (FontStyle)c.fontStyle;
			text.alignment = ParseAlignment (c.alignment);
			var color = ToColor (c.color, Color.white);
			// The original paints dark text on light art. With placeholder panels the same colour
			// would be unreadable, so brighten dark text only in placeholder mode.
			if (!useLocalSprites && Luminance (color) < 0.45f)
				color = new Color (0.93f, 0.90f, 0.84f, color.a);
			text.color = color;
			text.supportRichText = c.richText;
			text.resizeTextForBestFit = c.bestFit;
			text.horizontalOverflow = (HorizontalWrapMode)c.horizontalOverflow;
			text.verticalOverflow = (VerticalWrapMode)c.verticalOverflow;
			text.raycastTarget = c.raycastTarget;
			text.text = string.IsNullOrEmpty (c.sampleText) ? c.text : c.sampleText;
		}

		static float Luminance (Color c)
		{
			return 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
		}

		static TextAnchor ParseAlignment (string value)
		{
			if (string.IsNullOrEmpty (value))
				return TextAnchor.MiddleCenter;
			try {
				return (TextAnchor)Enum.Parse (typeof (TextAnchor), value);
			} catch {
				return TextAnchor.MiddleCenter;
			}
		}

		static void BuildButton (GameObject go, ComponentDto c)
		{
			var button = go.AddComponent<Button> ();
			button.transition = (Selectable.Transition)c.transition;
			button.interactable = c.interactable;
			var graphic = go.GetComponent<Graphic> ();
			if (graphic != null)
				button.targetGraphic = graphic;
		}

		static void BuildCanvasGroup (GameObject go, ComponentDto c)
		{
			var group = go.AddComponent<CanvasGroup> ();
			group.alpha = c.alpha;
			group.interactable = c.interactable;
			group.blocksRaycasts = c.blocksRaycasts;
			group.ignoreParentGroups = c.ignoreParentGroups;
		}

		static void BuildMask (GameObject go, ComponentDto c)
		{
			var mask = go.AddComponent<Mask> ();
			mask.showMaskGraphic = c.showMaskGraphic;
		}

		static RectOffset ToOffset (float[] padding)
		{
			if (padding == null || padding.Length < 4)
				return new RectOffset ();
			return new RectOffset ((int)padding[0], (int)padding[1], (int)padding[2], (int)padding[3]);
		}

		static void BuildLayout (GameObject go, ComponentDto c)
		{
			if (c.type == "VerticalLayoutGroup") {
				var layout = go.AddComponent<VerticalLayoutGroup> ();
				layout.padding = ToOffset (c.padding);
				layout.childAlignment = (TextAnchor)c.childAlignment;
				layout.spacing = c.spacing;
				layout.childForceExpandWidth = c.childForceExpandWidth;
				layout.childForceExpandHeight = c.childForceExpandHeight;
				layout.childControlWidth = c.childControlWidth;
				layout.childControlHeight = c.childControlHeight;
			} else if (c.type == "HorizontalLayoutGroup") {
				var layout = go.AddComponent<HorizontalLayoutGroup> ();
				layout.padding = ToOffset (c.padding);
				layout.childAlignment = (TextAnchor)c.childAlignment;
				layout.spacing = c.spacing;
				layout.childForceExpandWidth = c.childForceExpandWidth;
				layout.childForceExpandHeight = c.childForceExpandHeight;
				layout.childControlWidth = c.childControlWidth;
				layout.childControlHeight = c.childControlHeight;
			} else {
				var layout = go.AddComponent<GridLayoutGroup> ();
				layout.padding = ToOffset (c.padding);
				layout.childAlignment = (TextAnchor)c.childAlignment;
				layout.startCorner = (GridLayoutGroup.Corner)c.startCorner;
				layout.startAxis = (GridLayoutGroup.Axis)c.startAxis;
				layout.constraint = (GridLayoutGroup.Constraint)c.constraint;
				layout.constraintCount = c.constraintCount;
				layout.cellSize = c.cellSize != null && c.cellSize.Length >= 2
					? new Vector2 (c.cellSize[0], c.cellSize[1]) : new Vector2 (80f, 80f);
				layout.spacing = c.spacingXY != null && c.spacingXY.Length >= 2
					? new Vector2 (c.spacingXY[0], c.spacingXY[1]) : Vector2.zero;
			}
		}

		static void BuildFitter (GameObject go, ComponentDto c)
		{
			var fitter = go.AddComponent<ContentSizeFitter> ();
			fitter.horizontalFit = ParseFit (c.horizontalFit);
			fitter.verticalFit = ParseFit (c.verticalFit);
		}

		static ContentSizeFitter.FitMode ParseFit (string value)
		{
			if (string.IsNullOrEmpty (value))
				return ContentSizeFitter.FitMode.Unconstrained;
			try {
				return (ContentSizeFitter.FitMode)Enum.Parse (typeof (ContentSizeFitter.FitMode), value);
			} catch {
				return ContentSizeFitter.FitMode.Unconstrained;
			}
		}

		static void BuildLayoutElement (GameObject go, ComponentDto c)
		{
			var element = go.AddComponent<LayoutElement> ();
			element.ignoreLayout = c.ignoreLayout;
			element.minWidth = c.minWidth;
			element.minHeight = c.minHeight;
			element.preferredWidth = c.preferredWidth;
			element.preferredHeight = c.preferredHeight;
			element.flexibleWidth = c.flexibleWidth;
			element.flexibleHeight = c.flexibleHeight;
		}

		static void BuildOutline (GameObject go, ComponentDto c)
		{
			var outline = go.AddComponent<Outline> ();
			if (c.color != null && c.color.Length >= 4)
				outline.effectColor = new Color (c.color[0], c.color[1], c.color[2], c.color[3]);
			if (c.effectDistance != null && c.effectDistance.Length >= 2)
				outline.effectDistance = new Vector2 (c.effectDistance[0], c.effectDistance[1]);
			outline.useGraphicAlpha = c.useGraphicAlpha;
		}

		static void BuildScrollRect (GameObject go, ComponentDto c, Dictionary<string, RectTransform> byPath, string path)
		{
			var scroll = go.AddComponent<ScrollRect> ();
			scroll.horizontal = c.horizontal;
			scroll.vertical = c.vertical;
			scroll.movementType = (ScrollRect.MovementType)c.movementType;
			scroll.elasticity = c.elasticity;
			scroll.inertia = c.inertia;
			scroll.decelerationRate = c.decelerationRate;
			scroll.scrollSensitivity = c.scrollSensitivity;

			RectTransform viewport;
			if (byPath.TryGetValue (path + "/Viewport", out viewport))
				scroll.viewport = viewport;

			RectTransform content;
			if (byPath.TryGetValue (path + "/Viewport/#skillContent", out content))
				scroll.content = content;
		}

		// ------------------------------------------------------------------
		// Model actor (#discipleModelRoot)
		// ------------------------------------------------------------------

		static void BuildModelActor (WindowDoc doc, Dictionary<string, RectTransform> byPath, BuildReport report)
		{
			RectTransform modelRoot;
			if (!byPath.TryGetValue ("UIDiscipleRoleInfo2Win/#root/mask/#discipleModelRoot", out modelRoot)) {
				report.spineActor = false;
				report.spineNote = "#discipleModelRoot not found in the dump.";
				return;
			}

			var placeholder = CreateModelPlaceholder (modelRoot);
			var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset> (PortraitDataPath);

			if (data == null) {
				report.spineActor = false;
				report.spineNote = "No " + PortraitDataPath + "; #discipleModelRoot keeps a labelled placeholder.";
				return;
			}

			// The game puts UIModelShow on this node and that component creates a "SpineActor"
			// child. The clone's equivalent is SpineUiActor directly on the #discipleModelRoot slot;
			// it creates its own "SpineGraphic" child for the renderer.
			var actor = modelRoot.gameObject.AddComponent<SpineUiActor> ();
			actor.Configure (data, "body_2110101_4_1", "stand", true);

			var so = new SerializedObject (actor);
			var normal = AssetDatabase.LoadAssetAtPath<Material> (PortraitMaterialPath);
			var gray = AssetDatabase.LoadAssetAtPath<Material> (PortraitGrayPath);
			SetObjectIfFound (so, "normalMaterial", normal);
			SetObjectIfFound (so, "grayMaterial", gray);
			so.ApplyModifiedPropertiesWithoutUndo ();

			placeholder.SetActive (false);
			report.spineActor = true;
			report.spineNote = "SpineUiActor on #discipleModelRoot, " + PortraitDataPath +
				", initial skin body_2110101_4_1 / animation stand. Skins are combined per mock disciple at runtime.";

			// DiscipleWindow finds the placeholder by name during BindWindow.
		}

		static GameObject CreateModelPlaceholder (RectTransform parent)
		{
			var go = new GameObject ("ModelPlaceholder", typeof (RectTransform), typeof (Image));
			go.layer = UILayer;
			var rt = (RectTransform)go.transform;
			rt.SetParent (parent, false);
			rt.anchorMin = new Vector2 (0.5f, 0.5f);
			rt.anchorMax = new Vector2 (0.5f, 0.5f);
			rt.pivot = new Vector2 (0.5f, 0.5f);
			rt.sizeDelta = new Vector2 (parent.rect.width * 0.8f, parent.rect.height * 0.8f);
			rt.anchoredPosition = Vector2.zero;
			var image = go.GetComponent<Image> ();
			image.color = new Color (0.32f, 0.24f, 0.28f, 0.9f);
			image.raycastTarget = false;

			var labelGo = new GameObject ("Label", typeof (RectTransform), typeof (Text));
			labelGo.layer = UILayer;
			var lrt = (RectTransform)labelGo.transform;
			lrt.SetParent (rt, false);
			lrt.anchorMin = Vector2.zero;
			lrt.anchorMax = Vector2.one;
			lrt.offsetMin = Vector2.zero;
			lrt.offsetMax = Vector2.zero;
			var label = labelGo.GetComponent<Text> ();
			label.font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			label.fontSize = 20;
			label.alignment = TextAnchor.MiddleCenter;
			label.color = new Color (0.95f, 0.9f, 0.9f, 1f);
			label.text = "MODEL\n(#discipleModelRoot)";
			label.raycastTarget = false;
			return go;
		}

		static void SetObjectIfFound (SerializedObject so, string property, Object value)
		{
			if (value == null)
				return;
			var prop = so.FindProperty (property);
			if (prop != null)
				prop.objectReferenceValue = value;
		}

		// ------------------------------------------------------------------
		// Window bindings
		// ------------------------------------------------------------------

		static void BindWindow (DiscipleWindow window, Dictionary<string, RectTransform> byPath)
		{
			window.nameText = Find<Text> (byPath, "#discipleNameText");
			window.descText = Find<Text> (byPath, "#discipleDesc");
			window.fightText = Find<Text> (byPath, "#discipleFightTxt");

			window.injuryRoot = FindGo (byPath, "#injuryObj");
			window.injuryText = Find<Text> (byPath, "#injuryTxt");
			window.ldLockRoot = FindGo (byPath, "#ldLockObj");
			window.signRoot = FindGo (byPath, "#signBtn");
			window.switchRoot = FindGo (byPath, "#switchBtn");
			window.pdRoot = FindGo (byPath, "#pdRoot");
			window.fightRoot = FindGo (byPath, "#fightroot");

			window.orderButton = Find<Button> (byPath, "#orderBtn");
			window.orderIcon = Find<Image> (byPath, "#orderBtn");
			window.jobIcon = Find<Image> (byPath, "#discipleJobIcon");
			window.rolespButton = Find<Button> (byPath, "#rolespbtn");

			RectTransform modelRoot;
			if (byPath.TryGetValue ("UIDiscipleRoleInfo2Win/#root/mask/#discipleModelRoot", out modelRoot)) {
				window.modelRoot = modelRoot;
				window.modelActor = modelRoot.GetComponent<SpineUiActor> ();
				var placeholder = modelRoot.Find ("ModelPlaceholder");
				if (placeholder != null)
					window.modelPlaceholder = placeholder.gameObject;
			}

			RectTransform tianming;
			if (byPath.TryGetValue ("UIDiscipleRoleInfo2Win/#root/buttonRoot/#tmskillroot/#skillScrollView/Viewport/#skillContent/#tmskillList", out tianming))
				window.tianmingList = tianming;

			RectTransform daoyan;
			if (byPath.TryGetValue ("UIDiscipleRoleInfo2Win/#root/buttonRoot/#tmskillroot/#skillScrollView/Viewport/#skillContent/#dyskillList", out daoyan))
				window.daoyanList = daoyan;
		}

		static T Find<T> (Dictionary<string, RectTransform> byPath, string suffix) where T : Component
		{
			var go = FindGo (byPath, suffix);
			return go != null ? go.GetComponent<T> () : null;
		}

		static GameObject FindGo (Dictionary<string, RectTransform> byPath, string suffix)
		{
			foreach (var kv in byPath) {
				if (kv.Key.EndsWith ("/" + suffix, StringComparison.Ordinal) || kv.Key == suffix)
					return kv.Value.gameObject;
			}
			return null;
		}

		static DiscipleMock[] CreateMockDisciples ()
		{
			return new[] {
				new DiscipleMock {
					name = "Amy Robinson", job = "Sword Cultivator", fight = 128450,
					state = "Secluded cultivation", injury = 0, followed = true, skillCount = 5, skinVariant = 0,
				},
				new DiscipleMock {
					name = "Li Qingyao", job = "Alchemist", fight = 96780,
					state = "Guarding the sect gate", injury = 1, followed = false, skillCount = 3, skinVariant = 1,
				},
				new DiscipleMock {
					name = "Mo Yan", job = "Blade Dancer", fight = 154220,
					state = "On a mission", injury = 0, followed = true, skillCount = 4, skinVariant = 2,
				},
			};
		}

		// ------------------------------------------------------------------
		// Demo scene
		// ------------------------------------------------------------------

		static void BuildScene (WindowDoc doc, GameObject prefab, BuildReport report)
		{
			var scene = EditorSceneManager.NewScene (NewSceneSetup.EmptyScene, NewSceneMode.Single);

			var camGo = new GameObject ("UICamera", typeof (Camera));
			var cam = camGo.GetComponent<Camera> ();
			cam.orthographic = true;
			cam.orthographicSize = 5f;
			cam.nearClipPlane = 0.3f;
			cam.farClipPlane = 1000f;
			cam.depth = 15f;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color (0.10f, 0.11f, 0.14f, 1f);
			cam.cullingMask = 1 << UILayer;
			camGo.transform.position = Vector3.zero;

			var es = new GameObject ("EventSystem", typeof (EventSystem), typeof (StandaloneInputModule));
			es.layer = UILayer;

			var guiRoot = new GameObject ("GUI_ROOT", typeof (WindowManager));
			guiRoot.layer = UILayer;

			var bottom = new GameObject ("BottomUIContain", typeof (RectTransform));
			bottom.layer = UILayer;
			bottom.transform.SetParent (guiRoot.transform, false);

			var canvasGo = new GameObject ("Canvas_BottomFirstLayer",
				typeof (RectTransform), typeof (Canvas), typeof (CanvasScaler), typeof (GraphicRaycaster));
			canvasGo.layer = UILayer;
			canvasGo.transform.SetParent (bottom.transform, false);
			var canvasRt = (RectTransform)canvasGo.transform;
			canvasRt.anchorMin = Vector2.zero;
			canvasRt.anchorMax = Vector2.one;
			canvasRt.offsetMin = Vector2.zero;
			canvasRt.offsetMax = Vector2.zero;

			var canvas = canvasGo.GetComponent<Canvas> ();
			canvas.renderMode = RenderMode.ScreenSpaceCamera;
			canvas.worldCamera = cam;
			canvas.planeDistance = 100f;
			canvas.sortingOrder = 1000;
			canvas.pixelPerfect = false;
			// GUISettings Canvas_BottomFirstLayer stores 27 == TexCoord1|TexCoord2|Normal|Tangent.
			canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
				AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.Normal |
				AdditionalCanvasShaderChannels.Tangent;

			var scaler = canvasGo.GetComponent<CanvasScaler> ();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = ReferenceResolution;
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0f;
			scaler.referencePixelsPerUnit = 100f;

			var windowGo = (GameObject)PrefabUtility.InstantiatePrefab (prefab, canvasGo.transform);
			windowGo.layer = UILayer;
			windowGo.name = "UIDiscipleRoleInfo2Win";

			var window = windowGo.GetComponent<DiscipleWindow> ();

			var manager = guiRoot.GetComponent<WindowManager> ();
			var so = new SerializedObject (manager);
			var layersProp = so.FindProperty ("layers");
			layersProp.arraySize = 10;
			for (int i = 0; i < 10; i++)
				layersProp.GetArrayElementAtIndex (i).objectReferenceValue = null;
			layersProp.GetArrayElementAtIndex ((int)WindowLayer.BottomFirst).objectReferenceValue = canvasRt;
			so.FindProperty ("initialWindow").objectReferenceValue = window;
			so.FindProperty ("openInitialOnStart").boolValue = true;
			so.ApplyModifiedPropertiesWithoutUndo ();

			BuildDemoBar (window, manager);

			EditorSceneManager.MarkSceneDirty (scene);
			EditorSceneManager.SaveScene (scene, ScenePath);

			// Sanity check the saved scene.
			var saved = EditorSceneManager.OpenScene (ScenePath);
			var found = Object.FindObjectOfType<DiscipleWindow> ();
			if (found == null)
				throw new Exception ("Scene build failed: no DiscipleWindow in " + ScenePath);
			report.notes = (report.notes ?? new string[0]).Concat (new[] {
				"Scene: UICamera(ortho 5) + GUI_ROOT/BottomUIContain/Canvas_BottomFirstLayer (1334x750, match width) + window instance.",
				"DemoBar canvas (overlay, sorting 5000) is a test harness only and is not part of the original window.",
			}).ToArray ();
		}

		static void BuildDemoBar (DiscipleWindow window, WindowManager manager)
		{
			var barGo = new GameObject ("DemoBar",
				typeof (RectTransform), typeof (Canvas), typeof (CanvasScaler), typeof (GraphicRaycaster));
			barGo.layer = UILayer;
			var bar = barGo.GetComponent<Canvas> ();
			bar.renderMode = RenderMode.ScreenSpaceOverlay;
			bar.sortingOrder = 5000;

			var scaler = barGo.GetComponent<CanvasScaler> ();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = ReferenceResolution;
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0f;

			var panel = new GameObject ("Panel", typeof (RectTransform), typeof (Image));
			panel.layer = UILayer;
			var prt = (RectTransform)panel.transform;
			prt.SetParent (barGo.transform, false);
			prt.anchorMin = new Vector2 (1f, 0.5f);
			prt.anchorMax = new Vector2 (1f, 0.5f);
			prt.pivot = new Vector2 (1f, 0.5f);
			prt.anchoredPosition = new Vector2 (-24f, 0f);
			prt.sizeDelta = new Vector2 (360f, 340f);
			panel.GetComponent<Image> ().color = new Color (0.07f, 0.08f, 0.11f, 0.92f);

			CreateLabel (prt, "MOCK DISCIPLE (test harness)", 22, TextAnchor.MiddleCenter,
				new Vector2 (0f, 1f), new Vector2 (1f, 1f), new Vector2 (0f, -12f), new Vector2 (-24f, 32f), 0f);

			var mock = window != null ? window.mockDisciples : null;
			for (int i = 0; i < 3; i++) {
				string label = mock != null && i < mock.Length ? mock[i].name : "Disciple " + (i + 1);
				var button = CreateButton (prt, "SelectDisciple" + i, label,
					new Vector2 (0f, 1f), new Vector2 (1f, 1f),
					new Vector2 (0f, -60f - i * 52f), new Vector2 (-24f, 44f));
				if (window != null)
					UnityEventTools.AddIntPersistentListener (button.onClick, window.SelectDisciple, i);
			}

			var close = CreateButton (prt, "Close", "Close top window (Esc)",
				new Vector2 (0f, 1f), new Vector2 (1f, 1f), new Vector2 (0f, -228f), new Vector2 (-24f, 44f));
			UnityEventTools.AddPersistentListener (close.onClick, manager.CloseTop);

			var reopen = CreateButton (prt, "Reopen", "Reopen window",
				new Vector2 (0f, 1f), new Vector2 (1f, 1f), new Vector2 (0f, -280f), new Vector2 (-24f, 44f));
			UnityEventTools.AddPersistentListener (reopen.onClick, manager.OpenInitial);

			CreateLabel (prt, "Placeholder colours unless built with local sprites", 14, TextAnchor.MiddleCenter,
				new Vector2 (0f, 1f), new Vector2 (1f, 1f), new Vector2 (0f, -324f), new Vector2 (-24f, 24f), 0f);
		}

		static Text CreateLabel (RectTransform parent, string text, int size, TextAnchor anchor,
			Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta, float pivotY)
		{
			var go = new GameObject ("Label", typeof (RectTransform), typeof (Text));
			go.layer = UILayer;
			var rt = (RectTransform)go.transform;
			rt.SetParent (parent, false);
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.pivot = new Vector2 (0.5f, pivotY);
			rt.anchoredPosition = position;
			rt.sizeDelta = sizeDelta;
			var label = go.GetComponent<Text> ();
			label.font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			label.fontSize = size;
			label.alignment = anchor;
			label.color = new Color (0.93f, 0.94f, 0.97f, 1f);
			label.text = text;
			label.raycastTarget = false;
			return label;
		}

		static Button CreateButton (RectTransform parent, string name, string label,
			Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 sizeDelta)
		{
			var go = new GameObject (name, typeof (RectTransform), typeof (Image), typeof (Button));
			go.layer = UILayer;
			var rt = (RectTransform)go.transform;
			rt.SetParent (parent, false);
			rt.anchorMin = anchorMin;
			rt.anchorMax = anchorMax;
			rt.pivot = new Vector2 (0.5f, 1f);
			rt.anchoredPosition = position;
			rt.sizeDelta = sizeDelta;

			var image = go.GetComponent<Image> ();
			image.color = new Color (0.20f, 0.24f, 0.33f, 1f);

			var button = go.GetComponent<Button> ();
			button.targetGraphic = image;

			var textGo = new GameObject ("Text", typeof (RectTransform), typeof (Text));
			textGo.layer = UILayer;
			var trt = (RectTransform)textGo.transform;
			trt.SetParent (rt, false);
			trt.anchorMin = Vector2.zero;
			trt.anchorMax = Vector2.one;
			trt.offsetMin = new Vector2 (8f, 0f);
			trt.offsetMax = new Vector2 (-8f, 0f);
			var text = textGo.GetComponent<Text> ();
			text.font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			text.fontSize = 18;
			text.alignment = TextAnchor.MiddleCenter;
			text.color = Color.white;
			text.text = label;
			text.raycastTarget = false;

			return button;
		}
	}
}
