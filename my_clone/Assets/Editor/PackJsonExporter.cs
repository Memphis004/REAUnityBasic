// Exports one Spine skeleton as a web "anatomy" pack: a single JSON file where every
// slot that carries an attachment is rendered on its own, on one shared canvas and one
// shared scale, cropped to its alpha bounds and embedded as a data URI.
//
// Result format (consumed by the web viewer "Anatomy of a disciple"):
//   {"name": id, "canvas": {"w","h"},
//    "groups": [{"id","label","note"}],                 // order = top-to-bottom page order
//    "parts":  [{"id","label","group","x","y","w","h","rot","order","img"}]}
//
// x/y are the centre of the part's crop box on the canvas (canvas pixels, y down) and
// w/h are canvas pixels -- not image pixels. The embedded PNG can be larger than w/h by
// the render scale factor (2x = sharper on high-DPI screens, same canvas numbers).
//
// Nothing under Assets/ is modified: the skeleton instance, its material override, the
// camera and the render textures all live in a temporary scene that is closed again.
//
// Each export also rewrites "packs.json" next to the packs, so the web viewer lists the
// folder's packs by itself instead of hardcoding their ids.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyClone.EditorTools
{
	public class PackJsonExportOptions
	{
		public SkeletonDataAsset data;
		public string outputId = "body_1110901";
		/// <summary>Absolute path of the .pack.json to write.</summary>
		public string outputFile;
		/// <summary>Absolute path of the slot -&gt; group map (part_groups.json / sheet_groups.json).</summary>
		public string groupMapPath;
		/// <summary>
		/// Skins composed into one, in this order (later skins win on the same slot).
		/// Leave empty to let the exporter pick the "_1" body/head/face skins of this skeleton;
		/// when it has no named skins the skeleton's default skin is used as-is.
		/// </summary>
		public string[] skins;
		/// <summary>Canvas height in canvas pixels. Width follows the skeleton's bounds.</summary>
		public int canvasHeight = 1024;
		public int padding = 16;
		/// <summary>Image pixels per canvas pixel. Lowered automatically when the JSON is too big.</summary>
		public int renderScale = 2;
		/// <summary>Above this the exporter re-renders at a lower renderScale.</summary>
		public long maxJsonBytes = 25L * 1024 * 1024;
	}

	[Serializable]
	public class PackGroupEntry
	{
		public string name;
		public string id;
		public string label;
		public string note;
		public string[] slots;
		public string[] matchPrefixes;
	}

	[Serializable]
	public class PackGroupMapFile
	{
		public string fallbackGroup;
		public PackGroupEntry[] groups;
	}

	/// <summary>One "packs.json" entry: what the web viewer needs to list and pick a pack.</summary>
	[Serializable]
	public class PackIndexEntry
	{
		public string id;
		public string file;
		public int w;
		public int h;
		public int parts;
		public int groups;
		public long bytes;
	}

	[Serializable]
	public class PackIndexFile
	{
		public string generated;
		public string defaultPack;
		public PackIndexEntry[] packs;
	}

	/// <summary>One slot rendered alone and cropped to its alpha bounds.</summary>
	class PackPartRender
	{
		public string slotName;
		public string attachmentName;
		public string groupId;
		public int drawOrder;
		public float x, y, w, h;   // canvas pixels, x/y = centre of the crop box
		public byte[] png;
	}

	public static class PackJsonExporter
	{
		public const string MenuPath = "Tools/LocalReference/Export Pack JSON";
		const string TempSceneName = "PackJsonExportTemp";
		const string PackFileSuffix = ".pack.json";
		const string IndexFileName = "packs.json";
		const string PreferredDefaultId = "body_1110901";
		const string DefaultAssetPath = "Assets/_LocalReference/Spine/body_1110901/body_1110901_SkeletonData.asset";

		// ------------------------------------------------------------------
		// Entry points
		// ------------------------------------------------------------------

		[MenuItem (MenuPath)]
		public static void ExportFromMenu ()
		{
			var asset = Selection.activeObject as SkeletonDataAsset;
			if (asset == null) {
				EditorUtility.DisplayDialog (
					"Export Pack JSON",
					"Select the body SkeletonDataAsset in the Project window first.",
					"OK");
				return;
			}

			string report;
			try {
				report = Export (DefaultOptions (asset));
			} catch (Exception e) {
				Debug.LogError ("[PackJsonExporter] " + e);
				EditorUtility.DisplayDialog ("Export Pack JSON", "Export failed:\n" + e.Message, "OK");
				return;
			}

			Debug.Log ("[PackJsonExporter]\n" + report);
			EditorUtility.DisplayDialog ("Export Pack JSON", report, "OK");
		}

		/// <summary>
		/// Batchmode entry point for every skeleton under Assets/_LocalReference/Spine:
		/// -executeMethod MyClone.EditorTools.PackJsonExporter.ExportAll
		/// </summary>
		public static void ExportAll ()
		{
			const string root = "Assets/_LocalReference/Spine";
			if (!Directory.Exists (root)) {
				Debug.LogError ("[PackJsonExporter] missing folder: " + root);
				EditorApplication.Exit (1);
				return;
			}

			var folders = Directory.GetDirectories (root);
		Array.Sort (folders, StringComparer.OrdinalIgnoreCase);

			int exported = 0;
			var failures = new List<string> ();

			foreach (var folder in folders) {
				var assets = Directory.GetFiles (folder, "*_SkeletonData.asset");
				if (assets.Length == 0) continue;
				Array.Sort (assets, StringComparer.OrdinalIgnoreCase);

				var assetPath = assets[0].Replace ('\\', '/');
				var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset> (assetPath);
				if (asset == null) {
					failures.Add (assetPath + " (not a SkeletonDataAsset)");
					continue;
				}

				try {
					Debug.Log ("[PackJsonExporter]\n" + Export (DefaultOptions (asset)));
					exported++;
				} catch (Exception e) {
					failures.Add (assetPath + " -> " + e.Message);
					Debug.LogError ("[PackJsonExporter] " + assetPath + "\n" + e);
				}
			}

			Debug.Log ("[PackJsonExporter] exported " + exported + " pack(s), " + failures.Count + " failure(s)");
			foreach (var failure in failures) Debug.LogError ("[PackJsonExporter] failed: " + failure);
			EditorApplication.Exit (failures.Count == 0 ? 0 : 1);
		}

		/// <summary>Batchmode entry point: -executeMethod MyClone.EditorTools.PackJsonExporter.ExportDefault</summary>
		public static void ExportDefault ()
		{
			var asset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset> (DefaultAssetPath);
			if (asset == null) {
				Debug.LogError ("[PackJsonExporter] SkeletonDataAsset not found: " + DefaultAssetPath);
				EditorApplication.Exit (1);
				return;
			}

			try {
				Debug.Log ("[PackJsonExporter]\n" + Export (DefaultOptions (asset)));
				EditorApplication.Exit (0);
			} catch (Exception e) {
				Debug.LogError ("[PackJsonExporter] " + e);
				EditorApplication.Exit (1);
			}
		}

		public static PackJsonExportOptions DefaultOptions (SkeletonDataAsset asset)
		{
			var id = asset == null ? "body_1110901" : asset.name.Replace ("_SkeletonData", "");
			var projectDir = Directory.GetParent (Application.dataPath).FullName;
			var repoRoot = Directory.GetParent (projectDir).FullName;

			return new PackJsonExportOptions {
				data = asset,
				outputId = id,
				outputFile = Path.Combine (Path.Combine (Path.Combine (repoRoot, "extracted"), "web"), id + ".pack.json"),
				groupMapPath = FindGroupMap (repoRoot, id),
			};
		}

		/// <summary>
		/// This repo has no "part_groups.json" (whole-repo search = 0 hits). The part grouping
		/// that does exist lives in sheet_groups.json (part sheets) and layers_groups.json
		/// (reference sheets). part_groups.json is still preferred if it ever appears.
		/// </summary>
		static string FindGroupMap (string repoRoot, string id)
		{
			var extracted = Path.Combine (repoRoot, "extracted");
			var candidates = new[] {
				Path.Combine (Path.Combine (extracted, "web"), "part_groups.json"),
				Path.Combine (Path.Combine (Path.Combine (extracted, "part_sheets"), id), "part_groups.json"),
				Path.Combine (Path.Combine (Path.Combine (extracted, "ref_sheets"), id), "part_groups.json"),
				Path.Combine (Path.Combine (Path.Combine (extracted, "part_sheets"), id), "sheet_groups.json"),
				Path.Combine (Path.Combine (Path.Combine (extracted, "ref_sheets"), id), "layers_groups.json"),
			};

			for (int i = 0; i < candidates.Length; i++)
				if (File.Exists (candidates[i]))
					return candidates[i];

			return null;
		}

		// ------------------------------------------------------------------
		// Main export
		// ------------------------------------------------------------------

		public static string Export (PackJsonExportOptions o)
		{
			if (o == null) throw new ArgumentException ("options is null");
			if (o.data == null) throw new ArgumentException ("options.data is null");
			if (string.IsNullOrEmpty (o.outputFile)) throw new ArgumentException ("options.outputFile is empty");

			var warnings = new List<string> ();
			var groupMap = LoadGroupMap (o.groupMapPath, o.outputId, warnings);

			var previousActive = SceneManager.GetActiveScene ();
			// SceneManager.CreateScene only works in play mode and Unity refuses an additive editor
			// scene while the open scene is untitled, so fall back to working in the current scene
			// (every object created here is destroyed again in the finally block).
			Scene scene = previousActive;
			bool ownsScene = false;
			try {
				scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene (
					UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
					UnityEditor.SceneManagement.NewSceneMode.Additive);
				ownsScene = true;
				SceneManager.SetActiveScene (scene);
			} catch (InvalidOperationException) {
				scene = previousActive;
			}

			GameObject root = null;
			GameObject cameraObject = null;
			RenderTexture rt = null;
			var createdMaterials = new List<Material> ();
			SkeletonAnimation animation = null;

			try {
				// --- skeleton: setup pose + composed skins ---------------------
				root = new GameObject ("PackJsonExportRoot");
				animation = SkeletonAnimation.NewSkeletonAnimationGameObject (o.data);
				if (animation == null) throw new Exception ("SkeletonAnimation.NewSkeletonAnimationGameObject returned null");
				animation.transform.SetParent (root.transform, false);
				animation.Initialize (false);
				if (!animation.valid) throw new Exception ("SkeletonAnimation failed to initialize (check SkeletonDataAsset / atlas)");

				animation.UpdateMode = Spine.Unity.UpdateMode.FullUpdate;
				animation.updateWhenInvisible = Spine.Unity.UpdateMode.FullUpdate;

				var skeleton = animation.Skeleton;
				var data = skeleton.Data;

				var skinNames = (o.skins != null && o.skins.Length > 0) ? o.skins : ResolveSkins (data);
				var appliedNames = new List<string> ();
				var combined = new Skin ("packjson_combined");
				foreach (var skinName in skinNames) {
					var skin = data.FindSkin (skinName);
					if (skin == null) {
						warnings.Add ("skin not found, skipped: " + skinName);
						continue;
					}
					combined.AddSkin (skin);
					appliedNames.Add (skinName);
				}

				if (appliedNames.Count > 0) {
					skeleton.SetSkin (combined);
				} else if (o.skins != null && o.skins.Length > 0) {
					throw new Exception ("none of the requested skins exist: " + string.Join (", ", o.skins));
				} else {
					warnings.Add ("skeleton has no body/head/face named skin; using the default skin");
				}
				skeleton.SetSlotsToSetupPose ();
				skeleton.SetToSetupPose ();
				skeleton.UpdateWorldTransform ();
				animation.Update (0f);
				animation.LateUpdate ();

				BuildOverrideMaterials (o.data, animation, createdMaterials, warnings);

				// --- slots that actually draw --------------------------------
				var drawOrder = skeleton.DrawOrder;
				var baseAttachments = new Attachment[drawOrder.Count];
				for (int i = 0; i < drawOrder.Count; i++)
					baseAttachments[i] = drawOrder.Items[i].Attachment;

				var renderable = new List<int> ();
				var skipped = new List<string> ();
				float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

				for (int i = 0; i < drawOrder.Count; i++) {
					var slot = drawOrder.Items[i];
					var attachment = baseAttachments[i];
					if (attachment == null) {
						skipped.Add (slot.Data.Name + " (no attachment in the composed skins)");
						continue;
					}

					var verts = WorldVertices (slot, attachment);
					if (verts == null) {
						skipped.Add (slot.Data.Name + " (" + attachment.GetType ().Name + " draws no pixels)");
						continue;
					}

					renderable.Add (i);
					for (int v = 0; v < verts.Length; v += 2) {
						if (verts[v] < minX) minX = verts[v];
						if (verts[v] > maxX) maxX = verts[v];
						if (verts[v + 1] < minY) minY = verts[v + 1];
						if (verts[v + 1] > maxY) maxY = verts[v + 1];
					}
				}

				if (renderable.Count == 0) throw new Exception ("no renderable slot found");

				// --- one canvas, one scale for every slot --------------------
				float boundsWidth = Mathf.Max (maxX - minX, 0.0001f);
				float boundsHeight = Mathf.Max (maxY - minY, 0.0001f);
				int canvasHeight = Mathf.Max (o.canvasHeight, 64);
				int padding = Mathf.Max (o.padding, 0);

				float unitsToCanvas = (canvasHeight - 2 * padding) / boundsHeight;   // canvas px per skeleton unit
				int canvasWidth = Mathf.CeilToInt (boundsWidth * unitsToCanvas) + 2 * padding;

				float centerX = (minX + maxX) * 0.5f;
				float centerY = (minY + maxY) * 0.5f;

				// skeleton origin lands on the canvas centre, shifted so the bounds are centred
				animation.transform.localScale = new Vector3 (unitsToCanvas, unitsToCanvas, 1f);
				animation.transform.localPosition = new Vector3 (-centerX * unitsToCanvas, -centerY * unitsToCanvas, 0f);

				// --- render (lowering the render scale when the file is too big)
				int renderScale = Mathf.Max (1, o.renderScale);
				List<PackPartRender> parts = null;
				List<string> skippedFinal = null;
				byte[] fullPng = null;
				string json = null;
				long jsonBytes = 0;
				List<PackGroupEntry> usedGroups = null;
				List<string> unassigned = new List<string> ();

				while (true) {
					byte[] passFull;
					var passParts = new List<PackPartRender> ();
					var passSkipped = new List<string> ();

					passFull = RenderPass (animation, drawOrder, baseAttachments, renderable, passParts, passSkipped,
						canvasWidth, canvasHeight, renderScale, ref cameraObject, ref rt);

					unassigned = AssignGroups (passParts, groupMap);
					usedGroups = GroupsWithParts (groupMap, passParts, warnings);
					json = BuildJson (o.outputId, canvasWidth, canvasHeight, usedGroups, passParts);
					jsonBytes = Encoding.UTF8.GetByteCount (json);

					if (jsonBytes <= o.maxJsonBytes || renderScale <= 1) {
						parts = passParts;
						skippedFinal = passSkipped;
						fullPng = passFull;
						break;
					}

					warnings.Add ("pack is " + jsonBytes + " bytes at renderScale " + renderScale +
						" (limit " + o.maxJsonBytes + "), re-rendering at a lower scale");
					renderScale = Mathf.Max (1, renderScale / 2);
					ReleaseRenderTargets (ref rt, ref cameraObject);
				}

				// --- write ---------------------------------------------------
				var outDir = Path.GetDirectoryName (o.outputFile);
				if (!string.IsNullOrEmpty (outDir)) Directory.CreateDirectory (outDir);
				File.WriteAllText (o.outputFile, json, new UTF8Encoding (false));

				var fullPath = Path.Combine (outDir ?? ".", "_" + o.outputId + ".pack_full_ref.png");
				if (fullPng != null) File.WriteAllBytes (fullPath, fullPng);

				var indexPath = UpdateIndex (outDir, new PackIndexEntry {
					id = o.outputId,
					file = Path.GetFileName (o.outputFile),
					w = canvasWidth,
					h = canvasHeight,
					parts = parts.Count,
					groups = usedGroups.Count,
					bytes = jsonBytes,
				});

				// --- report --------------------------------------------------
				var groupCounts = new Dictionary<string, int> ();
				foreach (var part in parts) {
					int n;
					groupCounts.TryGetValue (part.groupId, out n);
					groupCounts[part.groupId] = n + 1;
				}

				var report = new StringBuilder ();
				report.AppendLine ("pack: " + o.outputFile);
				report.AppendLine ("pack index: " + (indexPath ?? "(not written)"));
				report.AppendLine ("group map: " + (o.groupMapPath ?? "(none)"));
				var mapFileName = string.IsNullOrEmpty (o.groupMapPath) ? "" : Path.GetFileName (o.groupMapPath);
				if (mapFileName.Length > 0 && mapFileName != "part_groups.json")
					report.AppendLine ("  (part_groups.json does not exist in this repo; using " + mapFileName + ")");
				report.AppendLine ("skins: " + (appliedNames.Count > 0 ? string.Join (" + ", appliedNames) : "(default skin)"));
				report.AppendLine ("slots in draw order (" + drawOrder.Count + "): " + string.Join (", ", SlotNames (drawOrder)));
				report.AppendLine (string.Format (CultureInfo.InvariantCulture,
					"canvas: {0}x{1} canvas px | scale {2:0.###} canvas px per skeleton unit | padding {3}",
					canvasWidth, canvasHeight, unitsToCanvas, padding));
				report.AppendLine ("render scale: " + renderScale + "x (image px per canvas px)");
				report.AppendLine ("parts: " + parts.Count + " | groups: " + usedGroups.Count);
				foreach (var group in usedGroups) {
					var id = GroupId (group);
					report.AppendLine ("  " + id + " [" + (group.label ?? LabelFor (id)) + "]: " +
						(groupCounts.ContainsKey (id) ? groupCounts[id] : 0) + " part(s)");
				}

				var dropped = new List<string> ();
				foreach (var group in groupMap.groups) {
					var id = GroupId (group);
					if (id == null || groupCounts.ContainsKey (id)) continue;
					dropped.Add (id);
				}
				if (dropped.Count > 0)
					report.AppendLine ("group(s) dropped (no renderable slot): " + string.Join (", ", dropped));

				report.AppendLine ("json size: " + jsonBytes + " bytes (" +
					(jsonBytes / 1048576.0).ToString ("0.00", CultureInfo.InvariantCulture) + " MB)");
				report.AppendLine ("full reference image: " + fullPath);
				if (unassigned.Count > 0)
					report.AppendLine ("slot(s) with no group rule -> '" + FallbackGroup (groupMap) + "': " + string.Join (", ", unassigned));
				var allSkipped = new List<string> (skipped);
				if (skippedFinal != null) allSkipped.AddRange (skippedFinal);
				if (allSkipped.Count > 0)
					report.AppendLine ("skipped: " + string.Join (", ", allSkipped));
				foreach (var warning in warnings)
					report.AppendLine ("warning: " + warning);

				return report.ToString ();
			} finally {
				ReleaseRenderTargets (ref rt, ref cameraObject);
				foreach (var material in createdMaterials)
					if (material != null) UnityEngine.Object.DestroyImmediate (material);
				if (root != null) UnityEngine.Object.DestroyImmediate (root);

				try {
					if (previousActive.IsValid ()) SceneManager.SetActiveScene (previousActive);
				} catch (Exception) {
					// the previously active scene may be gone (batchmode); nothing to restore
				}
				if (ownsScene && scene.IsValid ())
					UnityEditor.SceneManagement.EditorSceneManager.CloseScene (scene, true);
			}
		}

		/// <summary>
		/// Rewrites "packs.json" next to the packs, so the web viewer discovers packs by itself:
		/// every *.pack.json in the folder is listed, so a new export shows up without touching
		/// any id list. Entries keep their stored stats until that pack is exported again, and a
		/// pack whose file was deleted drops out of the index. Returns the index path.
		/// </summary>
		static string UpdateIndex (string outDir, PackIndexEntry justExported)
		{
			if (string.IsNullOrEmpty (outDir)) return null;

			var indexPath = Path.Combine (outDir, IndexFileName);
			var known = new Dictionary<string, PackIndexEntry> (StringComparer.OrdinalIgnoreCase);
			string previousDefault = null;

			if (File.Exists (indexPath)) {
				try {
					var existing = JsonUtility.FromJson<PackIndexFile> (File.ReadAllText (indexPath));
					if (existing != null) {
						previousDefault = existing.defaultPack;
						if (existing.packs != null) {
							foreach (var entry in existing.packs) {
								if (entry == null || string.IsNullOrEmpty (entry.id)) continue;
								known[entry.id] = entry;
							}
						}
					}
				} catch (Exception e) {
					Debug.LogWarning ("[PackJsonExporter] could not read " + indexPath +
						", rebuilding it: " + e.Message);
				}
			}

			if (justExported != null) known[justExported.id] = justExported;

			var entries = new List<PackIndexEntry> ();
			var files = Directory.Exists (outDir) ? Directory.GetFiles (outDir, "*" + PackFileSuffix) : new string[0];
			Array.Sort (files, StringComparer.OrdinalIgnoreCase);

			foreach (var path in files) {
				var fileName = Path.GetFileName (path);
				var id = fileName.Substring (0, fileName.Length - PackFileSuffix.Length);

				PackIndexEntry entry;
				if (!known.TryGetValue (id, out entry) || entry == null)
					entry = new PackIndexEntry { id = id, bytes = new FileInfo (path).Length };

				entry.id = id;
				entry.file = fileName;
				entries.Add (entry);
			}

			entries.Sort ((a, b) => string.CompareOrdinal (a.id, b.id));

			// keep the stored default while its pack still exists, else prefer the disciple body
			string defaultPack = null;
			if (!string.IsNullOrEmpty (previousDefault) && entries.Exists (e => SameId (e.id, previousDefault)))
				defaultPack = previousDefault;
			else if (entries.Exists (e => SameId (e.id, PreferredDefaultId)))
				defaultPack = PreferredDefaultId;
			else if (entries.Count > 0)
				defaultPack = entries[0].id;

			if (defaultPack != null) {
				int at = entries.FindIndex (e => SameId (e.id, defaultPack));
				if (at > 0) {
					var first = entries[at];
					entries.RemoveAt (at);
					entries.Insert (0, first);
				}
			}

			var json = new StringBuilder ();
			json.AppendLine ("{");
			json.AppendLine ("  \"note\": \"Generated by Tools/LocalReference/Export Pack JSON -- every *.pack.json in this folder is listed here. The web viewer reads only this file; re-export to refresh it.\",");
			json.AppendLine ("  \"generated\": " + JsonString (DateTime.Now.ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)) + ",");
			json.AppendLine ("  \"defaultPack\": " + JsonString (defaultPack ?? "") + ",");
			json.AppendLine ("  \"packs\": [");

			for (int i = 0; i < entries.Count; i++) {
				var e = entries[i];
				json.Append ("    { \"id\": " + JsonString (e.id) +
					", \"file\": " + JsonString (e.file ?? e.id + PackFileSuffix) +
					", \"w\": " + e.w.ToString (CultureInfo.InvariantCulture) +
					", \"h\": " + e.h.ToString (CultureInfo.InvariantCulture) +
					", \"parts\": " + e.parts.ToString (CultureInfo.InvariantCulture) +
					", \"groups\": " + e.groups.ToString (CultureInfo.InvariantCulture) +
					", \"bytes\": " + e.bytes.ToString (CultureInfo.InvariantCulture) + " }");
				json.AppendLine (i < entries.Count - 1 ? "," : "");
			}

			json.AppendLine ("  ]");
			json.AppendLine ("}");

			File.WriteAllText (indexPath, json.ToString (), new UTF8Encoding (false));
			return indexPath;
		}

		static bool SameId (string a, string b)
		{
			return string.Equals (a, b, StringComparison.OrdinalIgnoreCase);
		}

		static string JsonString (string value)
		{
			return value == null ? "null" : "\"" + Escape (value) + "\"";
		}

		// ------------------------------------------------------------------
		// Rendering
		// ------------------------------------------------------------------

		/// <summary>Renders the full character and then one image per slot, same camera and scale.</summary>
		static byte[] RenderPass (
			SkeletonAnimation animation,
			ExposedList<Slot> drawOrder,
			Attachment[] baseAttachments,
			List<int> renderable,
			List<PackPartRender> parts,
			List<string> skipped,
			int canvasWidth, int canvasHeight, int renderScale,
			ref GameObject cameraObject,
			ref RenderTexture rt)
		{
			var camera = EnsureCamera (ref cameraObject, ref rt, canvasWidth, canvasHeight, renderScale);

			// full character in real draw order
			for (int i = 0; i < drawOrder.Count; i++)
				drawOrder.Items[i].Attachment = baseAttachments[i];
			animation.LateUpdate ();
			camera.Render ();

			var fullPixels = ReadRenderTexture (rt);
			var fullPng = EncodePng (fullPixels, rt.width, rt.height);

			int rtWidth = rt.width;
			int rtHeight = rt.height;

			foreach (int slotIndex in renderable) {
				// all other slots hidden on this instance only (the asset is never touched)
				for (int i = 0; i < drawOrder.Count; i++)
					drawOrder.Items[i].Attachment = i == slotIndex ? baseAttachments[i] : null;
				animation.LateUpdate ();
				camera.Render ();

				var pixels = ReadRenderTexture (rt);

				byte[] png;
				int left, top, cropWidth, cropHeight;
				if (!CropAndEncode (pixels, rtWidth, rtHeight, out png, out left, out top, out cropWidth, out cropHeight)) {
					skipped.Add (drawOrder.Items[slotIndex].Data.Name + " (rendered no pixels)");
					continue;
				}

				var slot = drawOrder.Items[slotIndex];
				float scale = renderScale;
				var part = new PackPartRender {
					slotName = slot.Data.Name,
					attachmentName = baseAttachments[slotIndex].Name,
					drawOrder = slotIndex,
					png = png,
					w = cropWidth / scale,
					h = cropHeight / scale,
				};
				// crop box centre in canvas pixels (y down)
				part.x = (2f * left + cropWidth) / (2f * scale);
				part.y = (2f * top + cropHeight) / (2f * scale);
				parts.Add (part);
			}

			for (int i = 0; i < drawOrder.Count; i++)
				drawOrder.Items[i].Attachment = baseAttachments[i];
			animation.LateUpdate ();

			return fullPng;
		}

		static Camera EnsureCamera (ref GameObject cameraObject, ref RenderTexture rt, int canvasWidth, int canvasHeight, int renderScale)
		{
			ReleaseRenderTargets (ref rt, ref cameraObject);

			rt = new RenderTexture (canvasWidth * renderScale, canvasHeight * renderScale, 24, RenderTextureFormat.ARGB32);
			rt.antiAliasing = 1;
			rt.useMipMap = false;
			rt.filterMode = FilterMode.Point;
			rt.wrapMode = TextureWrapMode.Clamp;
			rt.Create ();

			cameraObject = new GameObject ("PackJsonExportCamera");
			var cam = cameraObject.AddComponent<Camera> ();
			cam.orthographic = true;
			cam.orthographicSize = canvasHeight * 0.5f;   // 1 world unit = 1 canvas pixel
			cam.transform.position = new Vector3 (0f, 0f, -10f);
			cam.transform.rotation = Quaternion.identity;
			cam.nearClipPlane = 0.1f;
			cam.farClipPlane = 100f;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = new Color (0f, 0f, 0f, 0f);
			cam.allowMSAA = false;
			cam.allowHDR = false;
			cam.targetTexture = rt;
			return cam;
		}

		static void ReleaseRenderTargets (ref RenderTexture rt, ref GameObject cameraObject)
		{
			if (cameraObject != null) {
				UnityEngine.Object.DestroyImmediate (cameraObject);
				cameraObject = null;
			}
			if (rt != null) {
				rt.Release ();
				UnityEngine.Object.DestroyImmediate (rt);
				rt = null;
			}
		}

		/// <summary>Reads the render target, un-premultiplies it, returns bottom-up pixels.</summary>
		static Color32[] ReadRenderTexture (RenderTexture rt)
		{
			var previous = RenderTexture.active;
			RenderTexture.active = rt;

			var tex = new Texture2D (rt.width, rt.height, TextureFormat.RGBA32, false, false);
			tex.hideFlags = HideFlags.HideAndDontSave;
			tex.ReadPixels (new Rect (0, 0, rt.width, rt.height), 0, 0, false);
			tex.Apply (false, false);

			var pixels = tex.GetPixels32 ();
			UnityEngine.Object.DestroyImmediate (tex);

			RenderTexture.active = previous;

			// The Spine shaders blend (One, OneMinusSrcAlpha) over a transparent clear, so the
			// target holds premultiplied colour; PNG data URIs are straight alpha.
			for (int i = 0; i < pixels.Length; i++) {
				var c = pixels[i];
				if (c.a == 0) {
					c.r = 0; c.g = 0; c.b = 0;
				} else if (c.a < 255) {
					int a = c.a;
					c.r = (byte)Mathf.Min (255, (c.r * 255 + (a >> 1)) / a);
					c.g = (byte)Mathf.Min (255, (c.g * 255 + (a >> 1)) / a);
					c.b = (byte)Mathf.Min (255, (c.b * 255 + (a >> 1)) / a);
				}
				pixels[i] = c;
			}

			return pixels;
		}

		/// <summary>
		/// Crops a bottom-up pixel buffer to its alpha bounds. Reports the box in top-down
		/// pixels (left, top) plus its size in pixels.
		/// </summary>
		static bool CropAndEncode (Color32[] src, int width, int height, out byte[] png,
			out int left, out int top, out int cropWidth, out int cropHeight)
		{
			png = null;
			left = top = cropWidth = cropHeight = 0;

			int minColumn = width, maxColumn = -1, minRow = height, maxRow = -1;
			for (int y = 0; y < height; y++) {
				int row = y * width;
				for (int x = 0; x < width; x++) {
					if (src[row + x].a == 0) continue;
					if (x < minColumn) minColumn = x;
					if (x > maxColumn) maxColumn = x;
					if (y < minRow) minRow = y;
					if (y > maxRow) maxRow = y;
				}
			}
			if (maxColumn < 0) return false;

			cropWidth = maxColumn - minColumn + 1;
			cropHeight = maxRow - minRow + 1;
			left = minColumn;
			top = height - 1 - maxRow;   // bottom-up rows -> distance from the canvas top

			var cropped = new Color32[cropWidth * cropHeight];
			for (int y = 0; y < cropHeight; y++)
				Array.Copy (src, (minRow + y) * width + minColumn, cropped, y * cropWidth, cropWidth);

			var tex = new Texture2D (cropWidth, cropHeight, TextureFormat.RGBA32, false, false);
			tex.hideFlags = HideFlags.HideAndDontSave;
			tex.SetPixels32 (cropped);
			tex.Apply (false, false);
			png = tex.EncodeToPNG ();
			UnityEngine.Object.DestroyImmediate (tex);
			return png != null && png.Length > 0;
		}

		/// <summary>Encodes a bottom-up pixel buffer; Texture2D index 0 is the bottom row.</summary>
		static byte[] EncodePng (Color32[] src, int width, int height)
		{
			var tex = new Texture2D (width, height, TextureFormat.RGBA32, false, false);
			tex.hideFlags = HideFlags.HideAndDontSave;
			tex.SetPixels32 (src);
			tex.Apply (false, false);
			var png = tex.EncodeToPNG ();
			UnityEngine.Object.DestroyImmediate (tex);
			return png;
		}

		static float[] WorldVertices (Slot slot, Attachment attachment)
		{
			var region = attachment as RegionAttachment;
			if (region != null) {
				var verts = new float[8];
				region.ComputeWorldVertices (slot.Bone, verts, 0, 2);
				return verts;
			}

			var mesh = attachment as MeshAttachment;
			if (mesh != null) {
				int length = mesh.WorldVerticesLength;
				if (length < 4) return null;
				var verts = new float[length];
				mesh.ComputeWorldVertices (slot, 0, length, verts, 0, 2);
				return verts;
			}

			return null;   // BoundingBox / Path / Point / Clipping: no pixels
		}

		static void BuildOverrideMaterials (SkeletonDataAsset asset, SkeletonAnimation animation,
			List<Material> created, List<string> warnings)
		{
			var shader = Shader.Find ("Spine/Skeleton");
			if (shader == null) {
				warnings.Add ("shader 'Spine/Skeleton' not found, rendering with the atlas material as-is");
				return;
			}

			var overrides = animation.CustomMaterialOverride;
			overrides.Clear ();

			var atlases = asset.atlasAssets;
			if (atlases == null) return;

			for (int i = 0; i < atlases.Length; i++) {
				var atlas = atlases[i];
				if (atlas == null) continue;

				foreach (var source in atlas.Materials) {
					if (source == null) continue;

					var material = new Material (shader);
					material.name = source.name + " (PackJson)";
					material.hideFlags = HideFlags.HideAndDontSave;
					if (source.HasProperty ("_MainTex"))
						material.SetTexture ("_MainTex", source.GetTexture ("_MainTex"));
					material.SetFloat ("_StencilRef", 1f);
					material.SetFloat ("_StencilComp", (float)UnityEngine.Rendering.CompareFunction.Always);
					material.DisableKeyword ("_STRAIGHT_ALPHA_INPUT");   // the atlas page is premultiplied

					overrides[source] = material;
					created.Add (material);
				}
			}
		}

		// ------------------------------------------------------------------
		// Groups
		// ------------------------------------------------------------------

		/// <summary>Loads the slot -&gt; group map; a missing file degrades to a single 'other' group.</summary>
		static PackGroupMapFile LoadGroupMap (string path, string id, List<string> warnings)
		{
			if (!string.IsNullOrEmpty (path) && File.Exists (path)) {
				var map = JsonUtility.FromJson<PackGroupMapFile> (File.ReadAllText (path, Encoding.UTF8));
				if (map == null || map.groups == null || map.groups.Length == 0)
					throw new Exception ("group map has no groups: " + path);
				return map;
			}

			warnings.Add ("no group map for " + id + ": every part falls back to 'other'. " +
				"Write extracted/part_sheets/" + id + "/part_groups.json (\"groups\": [{\"name\",\"label\",\"note\",\"slots\"}], \"fallbackGroup\": \"other\") and run again.");

			return new PackGroupMapFile {
				fallbackGroup = "other",
				groups = new[] {
					new PackGroupEntry {
						name = "other",
						label = "Other",
						note = "no group rules for this skeleton yet",
						slots = new string[0],
						matchPrefixes = new string[0],
					},
				},
			};
		}

		/// <summary>
		/// Picks the base skins of a composed disciple: the "_1" variant of each body / head /
		/// face part. Empty when the skeleton only has a default skin (then nothing is composed).
		/// </summary>
		static string[] ResolveSkins (SkeletonData data)
		{
			var parts = new[] { "body", "head", "face" };
			var found = new List<string> ();

			foreach (var part in parts) {
				string best = null;
				foreach (var skin in data.Skins) {
					var name = skin.Name;
					if (string.IsNullOrEmpty (name)) continue;
					if (!name.StartsWith (part + "_", StringComparison.OrdinalIgnoreCase)) continue;
					if (!name.EndsWith ("_1", StringComparison.Ordinal)) continue;
					if (best == null || name.Length < best.Length) best = name;
				}
				if (best != null) found.Add (best);
			}

			return found.ToArray ();
		}

		static string FallbackGroup (PackGroupMapFile map)
		{
			return string.IsNullOrEmpty (map.fallbackGroup) ? "other" : map.fallbackGroup;
		}

		static string GroupId (PackGroupEntry entry)
		{
			if (entry == null) return null;
			if (!string.IsNullOrEmpty (entry.name)) return entry.name;
			if (!string.IsNullOrEmpty (entry.id)) return entry.id;
			return null;
		}

		static Dictionary<string, string> SlotToGroup (PackGroupMapFile map)
		{
			var bySlot = new Dictionary<string, string> (StringComparer.Ordinal);
			foreach (var group in map.groups) {
				var id = GroupId (group);
				if (id == null) continue;
				foreach (var slot in group.slots ?? new string[0])
					if (!string.IsNullOrEmpty (slot) && !bySlot.ContainsKey (slot))
						bySlot[slot] = id;
			}
			return bySlot;
		}

		static string ResolveGroup (string slotName, Dictionary<string, string> bySlot, PackGroupMapFile map)
		{
			string id;
			if (bySlot.TryGetValue (slotName, out id)) return id;

			foreach (var group in map.groups) {
				var groupId = GroupId (group);
				if (groupId == null) continue;
				foreach (var prefix in group.matchPrefixes ?? new string[0]) {
					if (string.IsNullOrEmpty (prefix)) continue;
					if (slotName.StartsWith (prefix, StringComparison.OrdinalIgnoreCase)) return groupId;
				}
			}

			return FallbackGroup (map);
		}

		/// <summary>Assigns a group to every part and returns the slots that fell back.</summary>
		static List<string> AssignGroups (List<PackPartRender> parts, PackGroupMapFile map)
		{
			var bySlot = SlotToGroup (map);
			var fallback = FallbackGroup (map);
			var unassigned = new List<string> ();

			foreach (var part in parts) {
				var id = ResolveGroup (part.slotName, bySlot, map);
				part.groupId = id;
				if (id == fallback && !bySlot.ContainsKey (part.slotName))
					unassigned.Add (part.slotName);
			}

			return unassigned;
		}

		/// <summary>Groups in map order, keeping only the ones that received a part.</summary>
		static List<PackGroupEntry> GroupsWithParts (PackGroupMapFile map, List<PackPartRender> parts, List<string> warnings)
		{
			var used = new List<string> ();
			foreach (var part in parts)
				if (!used.Contains (part.groupId)) used.Add (part.groupId);

			var ordered = new List<PackGroupEntry> ();
			foreach (var group in map.groups) {
				var id = GroupId (group);
				if (id == null || !used.Contains (id)) continue;
				ordered.Add (group);
				used.Remove (id);
			}

			// anything left (e.g. the fallback group when the map does not declare it)
			foreach (var id in used) {
				ordered.Add (new PackGroupEntry { name = id, note = "" });
				warnings.Add ("group '" + id + "' is not declared in the group map; appended at the end");
			}

			return ordered;
		}

		static List<string> SlotNames (ExposedList<Slot> drawOrder)
		{
			var names = new List<string> (drawOrder.Count);
			for (int i = 0; i < drawOrder.Count; i++) names.Add (drawOrder.Items[i].Data.Name);
			return names;
		}

		static string LabelFor (string id)
		{
			switch (id) {
			case "hair_front": return "Hair (front)";
			case "hair_back": return "Hair (back)";
			case "head": return "Head";
			case "face_open": return "Face (open)";
			case "face_alt": return "Face (alt)";
			case "torso": return "Torso";
			case "lower_body": return "Lower body";
			case "arm_L": return "Left arm";
			case "arm_R": return "Right arm";
			case "leg_L": return "Left leg";
			case "leg_R": return "Right leg";
			case "sash_or_ribbon": return "Sash / ribbon";
			case "accessory": return "Accessory";
			case "other": return "Other";
			}

			var sb = new StringBuilder (id.Length);
			bool upper = true;
			foreach (char ch in id) {
				if (ch == '_' || ch == '-') {
					sb.Append (' ');
					upper = true;
					continue;
				}
				sb.Append (upper ? char.ToUpperInvariant (ch) : ch);
				upper = false;
			}
			return sb.ToString ();
		}

		// ------------------------------------------------------------------
		// JSON
		// ------------------------------------------------------------------

		static string BuildJson (string id, int canvasWidth, int canvasHeight,
			List<PackGroupEntry> groups, List<PackPartRender> parts)
		{
			var culture = CultureInfo.InvariantCulture;
			var sb = new StringBuilder (16 * 1024 * 1024);

			sb.Append ("{\"name\":\"").Append (Escape (id)).Append ("\",");
			sb.Append ("\"canvas\":{\"w\":").Append (canvasWidth).Append (",\"h\":").Append (canvasHeight).Append ("},");
			sb.Append ("\n\"groups\":[");

			for (int i = 0; i < groups.Count; i++) {
				var group = groups[i];
				var groupId = GroupId (group);
				sb.Append (i == 0 ? "\n  " : ",\n  ");
				sb.Append ("{\"id\":\"").Append (Escape (groupId)).Append ("\",");
				sb.Append ("\"label\":\"").Append (Escape (string.IsNullOrEmpty (group.label) ? LabelFor (groupId) : group.label)).Append ("\",");
				sb.Append ("\"note\":\"").Append (Escape (group.note ?? "")).Append ("\"}");
			}

			sb.Append ("\n],\n\"parts\":[");

			for (int i = 0; i < parts.Count; i++) {
				var part = parts[i];
				sb.Append (i == 0 ? "\n  " : ",\n  ");
				sb.Append ("{\"id\":\"").Append (Escape (part.slotName)).Append ("\",");
				sb.Append ("\"label\":\"").Append (Escape (part.slotName + ":" + part.attachmentName)).Append ("\",");
				sb.Append ("\"group\":\"").Append (Escape (part.groupId)).Append ("\",");
				sb.Append ("\"x\":").Append (Number (part.x, culture));
				sb.Append (",\"y\":").Append (Number (part.y, culture));
				sb.Append (",\"w\":").Append (Number (part.w, culture));
				sb.Append (",\"h\":").Append (Number (part.h, culture));
				sb.Append (",\"rot\":0");
				sb.Append (",\"order\":").Append (part.drawOrder);
				sb.Append (",\"img\":\"data:image/png;base64,");
				sb.Append (Convert.ToBase64String (part.png));
				sb.Append ("\"}");
			}

			sb.Append ("\n]}\n");
			return sb.ToString ();
		}

		static string Number (float value, CultureInfo culture)
		{
			return Math.Round (value, 4).ToString ("0.####", culture);
		}

		static string Escape (string value)
		{
			if (string.IsNullOrEmpty (value)) return "";
			var sb = new StringBuilder (value.Length + 8);
			foreach (char ch in value) {
				switch (ch) {
				case '"': sb.Append ("\\\""); break;
				case '\\': sb.Append ("\\\\"); break;
				case '\n': sb.Append ("\\n"); break;
				case '\r': sb.Append ("\\r"); break;
				case '\t': sb.Append ("\\t"); break;
				default:
					if (ch < 0x20) sb.Append ("\\u").Append (((int)ch).ToString ("x4", CultureInfo.InvariantCulture));
					else sb.Append (ch);
					break;
				}
			}
			return sb.ToString ();
		}
	}
}
