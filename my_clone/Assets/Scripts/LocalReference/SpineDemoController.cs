using System.Collections.Generic;
using System.IO;
using System.Text;
using MyClone.SpineSystem;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace MyClone.LocalReference
{
	/// <summary>
	/// Demo-only controller for Assets/_LocalReference/Demo.unity.
	/// It never loads game files by itself: the SkeletonDataAssets are wired in the scene
	/// by LocalReferenceDemoBuilder. Buttons call the public methods below.
	/// </summary>
	public class SpineDemoController : MonoBehaviour
	{
		public SpineUiActor fullBody;
		public SpineUiActor chibi;
		public SpineUiActor third;

		[Tooltip ("Standalone weapon skeleton shown for comparison; it is not cycled by the buttons.")]
		public SpineUiActor weapon;

		[Tooltip ("Disciple half-body portrait (2110101). Shares the 70% left slot with fullBody; " +
			"only one of the two is visible at a time.")]
		public SpineUiActor portrait;

		[Tooltip ("Caption of the button that swaps the 70% actor between the scene character and the portrait.")]
		public Text mainToggleLabel;

		public Text info;

		[Header ("Mounted component (game: dbcomponentcfg.followSlot)")]
		public SkeletonDataAsset mountData;
		public string mountSkin;
		public string mountBone = "youshou";

		int m_SkinCursor;
		int m_AnimationCursor;
		Canvas m_Canvas;
		SpineActorBase m_MountedActor;
		bool m_PortraitShown;
		static int s_ScreenshotIndex;

		public bool PortraitShown { get { return m_PortraitShown; } }

		void Start ()
		{
			// The scene is saved with built actors, but SpineUiActor keeps its renderer in a
			// non-serialized field: after a domain reload / scene load the actor reports
			// IsBuilt == false until Build() runs, so build here exactly like a runtime spawn.
			EnsureBuilt (fullBody);
			EnsureBuilt (chibi);
			EnsureBuilt (third);
			EnsureBuilt (weapon);   // keeps its configured skin/animation, so no ApplyCombinedSkin here
			EnsureBuilt (portrait);

			ApplyCombinedSkin (fullBody);
			ApplyCombinedSkin (chibi);
			ApplyCombinedSkin (third);
			ApplyCombinedSkin (portrait);
			PlayConfiguredAnimation (fullBody);
			PlayConfiguredAnimation (chibi);
			PlayConfiguredAnimation (third);
			PlayConfiguredAnimation (portrait);
			SetPortraitShown (false);
			RefreshLabel ();

			m_Canvas = GetComponentInParent<Canvas> ();
			if (m_Canvas == null)
				m_Canvas = Object.FindObjectOfType<Canvas> ();
		}

		static void EnsureBuilt (SpineUiActor actor)
		{
			if (actor == null || actor.IsBuilt)
				return;

			// The scene was saved after an editor build, so it still contains the preview
			// SpineGraphic child. SpineUiActor does not serialize its renderer reference, so that
			// child would sit next to the one Build() creates (an empty extra CanvasRenderer).
			// Demo-only cleanup; SpineUiActor is untouched.
			var stale = actor.GetComponentsInChildren<SpineSkeletonGraphic> (true);
			for (int i = 0; i < stale.Length; i++)
				if (stale[i] != null && stale[i].transform.parent == actor.transform)
					Object.Destroy (stale[i].gameObject);

			actor.Build ();
		}

		// ------------------------------------------------------------------
		// Buttons
		// ------------------------------------------------------------------

		public void NextSkin ()
		{
			m_SkinCursor++;
			ApplyCombinedSkin (fullBody);
			ApplyCombinedSkin (chibi);
			ApplyCombinedSkin (third);
			ApplyCombinedSkin (portrait);
			RefreshLabel ();
		}

		public void NextAnimation ()
		{
			m_AnimationCursor++;
			PlayCycledAnimation (fullBody, m_AnimationCursor);
			PlayCycledAnimation (chibi, m_AnimationCursor);
			PlayCycledAnimation (third, m_AnimationCursor);
			PlayCycledAnimation (portrait, m_AnimationCursor);
			RefreshLabel ();
		}

		public void ToggleFlip ()
		{
			if (fullBody != null) fullBody.SetFlipX (!fullBody.FlipX);
			if (chibi != null) chibi.SetFlipX (!chibi.FlipX);
			if (third != null) third.SetFlipX (!third.FlipX);
			if (portrait != null) portrait.SetFlipX (!portrait.FlipX);
		}

		public void ToggleGray ()
		{
			bool gray = fullBody != null && !fullBody.IsGray;
			if (fullBody != null) fullBody.SetGray (gray);
			if (chibi != null) chibi.SetGray (gray);
			if (third != null) third.SetGray (gray);
			if (portrait != null) portrait.SetGray (gray);
		}

		// ------------------------------------------------------------------
		// Main slot swap (scene character <-> portrait)
		// ------------------------------------------------------------------

		/// <summary>
		/// Both actors hold the same 70% left slot; visibility is Graphic alpha so the hidden one keeps
		/// its mesh (the layout/measurement code and the buttons still see a built actor).
		/// </summary>
		public void ToggleMainActor ()
		{
			SetPortraitShown (!m_PortraitShown);
			RefreshLabel ();
		}

		public void SetPortraitShown (bool show)
		{
			m_PortraitShown = show && portrait != null;
			SetAlpha (portrait, m_PortraitShown ? 1f : 0f);
			SetAlpha (fullBody, m_PortraitShown ? 0f : 1f);
			if (mainToggleLabel != null)
				mainToggleLabel.text = m_PortraitShown ? "Show Scene Char" : "Show Portrait";
		}

		static void SetAlpha (SpineUiActor actor, float alpha)
		{
			if (actor == null || !actor.IsBuilt || actor.Graphic == null)
				return;
			Color color = actor.Graphic.color;
			color.a = alpha;
			actor.SetColor (color);
		}

		/// <summary>
		/// Mounts/unmounts a second skeleton at a bone of the full-body actor, the same shape as the
		/// game's dbcomponentcfg.followSlot path (SpineActor.LoadSkeletonAttachment).
		/// </summary>
		public void ToggleMount ()
		{
			if (fullBody == null || !fullBody.IsBuilt) {
				Debug.LogWarning ("[SpineDemo] mount: full body actor not built");
				return;
			}

			if (m_MountedActor != null) {
				fullBody.UnMount ();
				Debug.Log ("[SpineDemo] unmounted component (renderer hidden, not destroyed)");
				m_MountedActor = null;
				RefreshLabel ();
				return;
			}

			if (mountData == null) {
				Debug.LogWarning ("[SpineDemo] mount: no mountData assigned (extract Weapon/ first)");
				return;
			}

			var mount = fullBody.Mount (mountData, mountSkin);
			if (mount == null) {
				Debug.LogWarning ("[SpineDemo] mount: Mount() failed for skin " + mountSkin);
				return;
			}

			bool attached = fullBody.MountToBone (mountBone);

			// The mount is a child of the actor root, which is scaled to fit the canvas height.
			// Cancel that scale so the mounted skeleton keeps its own pixel size.
			float parentScale = fullBody.transform.localScale.x;
			if (parentScale > 0.0001f)
				mount.transform.localScale = Vector3.one / parentScale;

			if (!mount.HasAnimation ("idle1") && mount.Skeleton.Data.Animations.Count > 0)
				mount.Play (mount.Skeleton.Data.Animations.Items[0].Name, true);
			else
				mount.Play ("idle1", true);

			m_MountedActor = mount;
			int renderers = fullBody.GetComponentsInChildren<SkeletonGraphic> (true).Length;
			Debug.LogFormat ("[SpineDemo] mounted {0} (skin {1}) at bone '{2}' ok={3}; SkeletonGraphics under full body = {4}",
				mountData.name, mountSkin, mountBone, attached, renderers);
			RefreshLabel ();
		}

		// ------------------------------------------------------------------
		// Skins
		// ------------------------------------------------------------------

		/// <summary>
		/// The game's characters are split into body/face/head skins. Build one combined skin
		/// by picking the cursor-th variant of each group so a single click changes the whole set.
		/// </summary>
		void ApplyCombinedSkin (SpineUiActor actor)
		{
			if (actor == null || !actor.IsBuilt)
				return;

			var skeleton = actor.Skeleton;
			var groups = new Dictionary<string, List<Skin>> ();
			var order = new List<string> ();
			GroupSkins (skeleton.Data, groups, order);

			if (order.Count == 0) {
				// single-skin skeleton (chibi "default") - nothing to combine
				if (m_SkinCursor % 2 == 1)
					Debug.Log ("[SpineDemo] " + actor.name + " has no extra skins to cycle.");
				return;
			}

			var combined = new Skin ("demo_combined_" + m_SkinCursor);
			foreach (var key in order) {
				var list = groups[key];
				combined.AddSkin (list[m_SkinCursor % list.Count]);
			}

			// Reset first: Skeleton.SetSkin(Skin) merges with the currently active skin.
			skeleton.SetSkin ((Skin)null);
			skeleton.SetSkin (combined);
			skeleton.SetSlotsToSetupPose ();
			if (actor.Graphic != null)
				actor.Graphic.SetVerticesDirty ();
		}

		static void GroupSkins (SkeletonData data, Dictionary<string, List<Skin>> groups, List<string> order)
		{
			if (data == null)
				return;

			foreach (var skin in data.Skins) {
				if (skin == null || skin.Name == "default")
					continue;

				string key = SkinGroupKey (skin.Name);

				List<Skin> list;
				if (!groups.TryGetValue (key, out list)) {
					list = new List<Skin> ();
					groups.Add (key, list);
					order.Add (key);
				}
				list.Add (skin);
			}
		}

		/// <summary>
		/// Group key for a skin name. Most groups are the prefix before the first underscore
		/// (body_/face_/hair_/hat_), but the portrait also has face_acc_*: splitting that at the
		/// first underscore would put it in the face group and the combined skin would show the
		/// accessory INSTEAD of the face.
		/// </summary>
		static string SkinGroupKey (string name)
		{
			if (name.StartsWith ("face_acc_", System.StringComparison.Ordinal))
				return "face_acc";

			int underscore = name.IndexOf ('_');
			return underscore > 0 ? name.Substring (0, underscore) : name;
		}

		// ------------------------------------------------------------------
		// Skin audit
		// ------------------------------------------------------------------

		/// <summary>
		/// Applies every skin variant of both actors and logs slot coverage, so it is visible which
		/// body/head variant combination is a complete character. Restores the current variant after.
		/// </summary>
		public void AuditSkins ()
		{
			AuditActor ("FULL BODY", fullBody);
			AuditActor ("CHIBI", chibi);
			ApplyCombinedSkin (fullBody);
			ApplyCombinedSkin (chibi);
			RefreshLabel ();
		}

		void AuditActor (string label, SpineUiActor actor)
		{
			if (actor == null || !actor.IsBuilt) {
				Debug.LogWarning ("[SpineDemo] audit " + label + ": actor not built");
				return;
			}

			var skeleton = actor.Skeleton;
			var groups = new Dictionary<string, List<Skin>> ();
			var order = new List<string> ();
			GroupSkins (skeleton.Data, groups, order);

			if (order.Count == 0) {
				Debug.Log ("[SpineDemo] audit " + label + ": no named skins (default only)");
				return;
			}

			int variants = 1;
			for (int i = 0; i < order.Count; i++)
				variants = Mathf.Max (variants, groups[order[i]].Count);

			var complete = new List<int> ();
			Debug.LogFormat ("[SpineDemo] audit {0}: groups={1} variants={2} slots={3}",
				label, string.Join ("/", order.ToArray ()), variants, skeleton.Slots.Count);

			for (int v = 0; v < variants; v++) {
				var combined = new Skin ("audit_" + v);
				var names = new List<string> ();
				for (int g = 0; g < order.Count; g++) {
					var list = groups[order[g]];
					var pick = list[v % list.Count];
					combined.AddSkin (pick);
					names.Add (pick.Name);
				}

				skeleton.SetSkin ((Skin) null);
				skeleton.SetSkin (combined);
				skeleton.SetSlotsToSetupPose ();

				var graphic = actor.Graphic;
				if (graphic != null)
					graphic.UpdateMesh ();

				int attached = 0;
				bool coreMissing = false;
				var missing = new List<string> ();
				for (int s = 0; s < skeleton.Slots.Count; s++) {
					var slot = skeleton.Slots.Items[s];
					if (slot.Attachment != null) {
						attached++;
						continue;
					}
					string slotName = slot.Data.Name;
					if (missing.Count < 6)
						missing.Add (slotName);
					if (IsCoreSlot (slotName))
						coreMissing = true;
				}

				Vector2 size = MeshSize (graphic);
				int verts = graphic == null ? 0 : graphic.MeshGenerator.Buffers.vertexCount;
				if (!coreMissing && verts > 0)
					complete.Add (v);

				Debug.LogFormat (
					"[SpineDemo] audit {0} variant {1}: skins=[{2}] slots={3}/{4} verts={5} mesh={6:0.#}x{7:0.#} core={8} missing=[{9}]",
					label, v, string.Join (",", names.ToArray ()), attached, skeleton.Slots.Count,
					verts, size.x, size.y, coreMissing ? "INCOMPLETE" : "ok", string.Join (",", missing.ToArray ()));
			}

			Debug.LogFormat ("[SpineDemo] audit {0}: complete variants = [{1}]",
				label, string.Join (",", complete.ConvertAll (c => c.ToString ()).ToArray ()));
		}

		static bool IsCoreSlot (string name)
		{
			return name == "body" || name == "head" || name == "face";
		}

		static Vector2 MeshSize (SkeletonGraphic graphic)
		{
			if (graphic == null)
				return Vector2.zero;

			var buffers = graphic.MeshGenerator.Buffers;
			int count = buffers.vertexCount;
			if (count == 0 || buffers.vertexBuffer == null)
				return Vector2.zero;

			Vector2 min = new Vector2 (float.MaxValue, float.MaxValue);
			Vector2 max = new Vector2 (float.MinValue, float.MinValue);
			for (int i = 0; i < count; i++) {
				Vector2 p = buffers.vertexBuffer[i];
				min = Vector2.Min (min, p);
				max = Vector2.Max (max, p);
			}
			return max - min;
		}

		// ------------------------------------------------------------------
		// Screenshot
		// ------------------------------------------------------------------

		public const int ScreenshotWidth = 1920;
		public const int ScreenshotHeight = 1080;

		/// <summary>Button entry point (UnityAction is void; <see cref="SaveScreenshot"/> returns the path).</summary>
		public void CaptureScreenshot ()
		{
			SaveScreenshot ();
		}

		public static string ScreenshotDirectory {
			get { return Path.Combine (Application.dataPath, "_LocalReference", "Screenshots"); }
		}

		/// <summary>
		/// Renders the canvas into a 1920x1080 PNG named after the current skins and animations.
		/// The overlay canvas is temporarily rendered through a throw-away orthographic camera so this
		/// works in play mode and from an editor script (no Game view needed).
		/// </summary>
		public string SaveScreenshot ()
		{
			if (m_Canvas == null)
				m_Canvas = Object.FindObjectOfType<Canvas> ();
			if (m_Canvas == null) {
				Debug.LogWarning ("[SpineDemo] screenshot: no Canvas in the scene");
				return null;
			}

			var cameraGo = new GameObject ("ScreenshotCamera");
			cameraGo.hideFlags = HideFlags.DontSave;
			var camera = cameraGo.AddComponent<Camera> ();
			camera.orthographic = true;
			camera.orthographicSize = ScreenshotHeight * 0.5f;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color (0.07f, 0.08f, 0.11f, 1f);
			camera.transform.position = new Vector3 (0f, 0f, -100f);
			camera.nearClipPlane = 0.3f;
			camera.farClipPlane = 1000f;

			var renderTexture = RenderTexture.GetTemporary (
				ScreenshotWidth, ScreenshotHeight, 24, RenderTextureFormat.ARGB32);
			camera.targetTexture = renderTexture;

			var previousMode = m_Canvas.renderMode;
			var previousCamera = m_Canvas.worldCamera;
			float previousPlane = m_Canvas.planeDistance;
			m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
			m_Canvas.worldCamera = camera;
			m_Canvas.planeDistance = 10f;

			Canvas.ForceUpdateCanvases ();
			var graphics = m_Canvas.GetComponentsInChildren<SkeletonGraphic> (true);
			for (int i = 0; i < graphics.Length; i++)
				graphics[i].UpdateMesh ();

			camera.Render ();

			RenderTexture.active = renderTexture;
			var texture = new Texture2D (ScreenshotWidth, ScreenshotHeight, TextureFormat.RGBA32, false);
			texture.ReadPixels (new Rect (0, 0, ScreenshotWidth, ScreenshotHeight), 0, 0);
			texture.Apply ();
			RenderTexture.active = null;

			m_Canvas.renderMode = previousMode;
			m_Canvas.worldCamera = previousCamera;
			m_Canvas.planeDistance = previousPlane;
			Canvas.ForceUpdateCanvases ();

			Directory.CreateDirectory (ScreenshotDirectory);
			string file = Path.Combine (ScreenshotDirectory, BuildScreenshotName ());
			File.WriteAllBytes (file, texture.EncodeToPNG ());

			if (Application.isPlaying) {
				Object.Destroy (texture);
				Object.Destroy (cameraGo);
			} else {
				Object.DestroyImmediate (texture);
				Object.DestroyImmediate (cameraGo);
			}
			RenderTexture.ReleaseTemporary (renderTexture);

			RefreshLabel ();
			if (info != null)
				info.text += "\nSaved #" + s_ScreenshotIndex + ": " + Path.GetFileName (file);
			Debug.Log ("[SpineDemo] screenshot saved: " + file);
			return file;
		}

		string BuildScreenshotName ()
		{
			s_ScreenshotIndex++;

			string animation = "none";
			if (fullBody != null && fullBody.IsBuilt) {
				var current = fullBody.AnimationState.GetCurrent (0);
				if (current != null && current.Animation != null)
					animation = current.Animation.Name;
			}

			return string.Format ("demo_{0:00}_{1}_variant{2}.png",
				s_ScreenshotIndex, Sanitize (animation), m_SkinCursor);
		}

		static string Sanitize (string value)
		{
			if (string.IsNullOrEmpty (value))
				return "none";

			var builder = new StringBuilder (value.Length);
			for (int i = 0; i < value.Length; i++) {
				char c = value[i];
				builder.Append (char.IsLetterOrDigit (c) || c == '-' || c == '_' ? c : '_');
			}
			return builder.ToString ();
		}

		// ------------------------------------------------------------------
		// Animations
		// ------------------------------------------------------------------

		/// <summary>
		/// The animation serialized on the actor (the builder picks a loop that stays in place, e.g.
		/// the chibi's stand instead of its travelling idle1) wins over the generic fallback list.
		/// </summary>
		void PlayConfiguredAnimation (SpineUiActor actor)
		{
			if (actor == null || !actor.IsBuilt)
				return;

			if (!string.IsNullOrEmpty (actor.InitialAnimation) && actor.HasAnimation (actor.InitialAnimation)) {
				actor.Play (actor.InitialAnimation, true, 1f);
				return;
			}

			PlayInitialAnimation (actor);
		}

		void PlayInitialAnimation (SpineUiActor actor)
		{
			if (actor == null || !actor.IsBuilt)
				return;

			string name = PickInitialAnimation (actor.Skeleton.Data);
			if (!string.IsNullOrEmpty (name))
				actor.Play (name, true, 1f);
		}

		static string PickInitialAnimation (SkeletonData data)
		{
			string[] preferred = { "idle1", "idle", "stand", "stand2" };
			for (int i = 0; i < preferred.Length; i++) {
				for (int a = 0; a < data.Animations.Count; a++) {
					if (data.Animations.Items[a].Name == preferred[i])
						return preferred[i];
				}
			}
			for (int a = 0; a < data.Animations.Count; a++) {
				var n = data.Animations.Items[a].Name.ToLowerInvariant ();
				if (n.Contains ("idle") || n.Contains ("stand"))
					return data.Animations.Items[a].Name;
			}
			return data.Animations.Count > 0 ? data.Animations.Items[0].Name : null;
		}

		void PlayCycledAnimation (SpineUiActor actor, int cursor)
		{
			if (actor == null || !actor.IsBuilt)
				return;

			var anims = actor.Skeleton.Data.Animations;
			if (anims.Count == 0)
				return;

			string name = anims.Items[cursor % anims.Count].Name;
			actor.Play (name, true, 1f);
		}

		// ------------------------------------------------------------------
		// Label
		// ------------------------------------------------------------------

		void RefreshLabel ()
		{
			if (info == null)
				return;

			info.text = Describe ("SCENE CHAR (fox)", fullBody) + "\n" + Describe ("PORTRAIT 2110101", portrait) +
				"\n" + Describe ("CHIBI", chibi);
			if (third != null)
				info.text += "\n" + Describe ("THIRD", third);
			info.text += "\n    shown: " + (m_PortraitShown ? "PORTRAIT 2110101" : "SCENE CHAR (fox)");
			if (m_MountedActor != null)
				info.text += "\n    mounted: " + (mountSkin ?? "-") + " @ " + mountBone;
		}

		static string Describe (string title, SpineUiActor actor)
		{
			if (actor == null || !actor.IsBuilt)
				return title + ": (not built)";

			var data = actor.Skeleton.Data;
			var current = actor.AnimationState.GetCurrent (0);
			string anim = current == null || current.Animation == null ? "-" : current.Animation.Name;
			return string.Format (
				"{0}: bones {1} slots {2} skins {3} anims {4} | {5} | {6}",
				title, data.Bones.Count, data.Slots.Count, data.Skins.Count,
				data.Animations.Count, actor.Skeleton.Skin == null ? "default" : actor.Skeleton.Skin.Name, anim);
		}
	}
}
