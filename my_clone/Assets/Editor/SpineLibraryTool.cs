using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace MyClone.EditorTools
{
	/// <summary>
	/// Imports a skeleton from the local reference library (extracted/spine_all) into
	/// Assets/_LocalReference/Spine/&lt;id&gt;/, and dumps runtime facts (bones/slots/skins/
	/// animations) for the catalogue.
	///
	/// Menu:  Tools/Local Reference/Import Spine By Id
	///        Tools/Local Reference/Spine Library/Dump Runtime Stats
	/// Code:  SpineLibraryTool.ImportById("body_1110901")
	///
	/// The tool never reads the game folder and never copies a DLL: the library folder is the
	/// only input. Import settings match the ones the Chibi reference was built with.
	/// </summary>
	public static class SpineLibraryTool
	{
		public const string ImportRoot = "Assets/_LocalReference/Spine";

		/// <summary>
		/// extracted/spine_all at the workspace root: my_clone/Assets -> my_clone -> workspace.
		/// The library deliberately lives outside the Unity project (never inside Assets).
		/// </summary>
		public static string LibraryRoot {
			get { return Path.GetFullPath (Path.Combine (Application.dataPath, "..", "..", "extracted", "spine_all")); }
		}

		[MenuItem ("Tools/Local Reference/Import Spine By Id")]
		public static void OpenImportWindow ()
		{
			ImportSpineWindow.ShowWindow ();
		}

		[MenuItem ("Tools/Local Reference/Spine Library/Dump Runtime Stats")]
		public static void DumpStatsFromMenu ()
		{
			string outPath = Path.Combine (LibraryRoot, "_runtime_stats.json");
			DumpRuntimeStats (outPath);
			EditorUtility.DisplayDialog ("Spine library",
				"Runtime stats written to:\n" + outPath, "OK");
		}

		// ------------------------------------------------------------------
		// Import
		// ------------------------------------------------------------------

		/// <summary>
		/// Imports one or more ids (comma separated). Returns the number of skeletons imported;
		/// every failure is logged with the reason and the closest library ids.
		/// </summary>
		public static int ImportById (string idList)
		{
			if (string.IsNullOrEmpty (idList)) {
				Debug.LogError ("[SpineLibrary] no id given. Usage: body_1110901 or id1,id2");
				return 0;
			}

			string[] raw = idList.Split (new[] { ',', ';', ' ', '\t', '\n', '\r' },
				StringSplitOptions.RemoveEmptyEntries);
			var ids = new List<string> ();
			for (int i = 0; i < raw.Length; i++) {
				string id = raw[i].Trim ();
				if (id.Length > 0 && !ids.Contains (id))
					ids.Add (id);
			}

			if (ids.Count == 0) {
				Debug.LogError ("[SpineLibrary] no valid id in '" + idList + "'");
				return 0;
			}

			int ok = 0;
			var failures = new List<string> ();
			for (int i = 0; i < ids.Count; i++) {
				string reason;
				if (ImportOne (ids[i], out reason))
					ok++;
				else
					failures.Add (ids[i] + " (" + reason + ")");
			}

			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();

			if (failures.Count == 0) {
				Debug.LogFormat ("[SpineLibrary] imported {0}/{1} skeleton(s): {2}",
					ok, ids.Count, string.Join (", ", ids.ToArray ()));
			} else {
				Debug.LogErrorFormat ("[SpineLibrary] imported {0}/{1}; failed: {2}",
					ok, ids.Count, string.Join ("; ", failures.ToArray ()));
			}
			return ok;
		}

		static bool ImportOne (string id, out string reason)
		{
			reason = null;
			string srcDir = Path.Combine (LibraryRoot, id);
			if (!Directory.Exists (srcDir)) {
				reason = "id not found in library " + LibraryRoot;
				Debug.LogErrorFormat (
					"[SpineLibrary] '{0}' is not in the library. Closest ids: {1}\nLibrary: {2}",
					id, string.Join (", ", SuggestIds (id).ToArray ()), LibraryRoot);
				return false;
			}

			try {
				string skeletonPath = FindSkeleton (srcDir, id);
				if (skeletonPath == null)
					throw new Exception ("no " + id + ".skel.bytes / " + id + ".json in " + srcDir);
				string atlasSrc = Path.Combine (srcDir, id + ".atlas.txt");
				if (!File.Exists (atlasSrc))
					throw new Exception ("missing " + id + ".atlas.txt");

				string atlasText = File.ReadAllText (atlasSrc, Encoding.UTF8);
				List<string> pages = AtlasPages (atlasText);
				if (pages.Count == 0)
					throw new Exception ("atlas lists no .png page");

				string srcJson = Path.Combine (srcDir, "source.json");
				bool premultiplied = true;   // game materials set _StraightAlphaInput = 0
				string alphaEvidence = "default (game material _StraightAlphaInput is 0)";
				if (File.Exists (srcJson)) {
					string json = File.ReadAllText (srcJson, Encoding.UTF8);
					if (json.IndexOf ("\"straightAlphaInput\": 1", StringComparison.Ordinal) >= 0) {
						premultiplied = false;
						alphaEvidence = "source.json material _StraightAlphaInput = 1";
					} else if (json.IndexOf ("\"straightAlphaInput\": 0", StringComparison.Ordinal) >= 0) {
						alphaEvidence = "source.json material _StraightAlphaInput = 0";
					}
				}

				string dstDir = Path.Combine (Application.dataPath, "_LocalReference", "Spine", id);
				Directory.CreateDirectory (dstDir);

				string skeletonAssetPath = CopyToProject (skeletonPath, dstDir);
				string atlasAssetPath = CopyToProject (atlasSrc, dstDir);
				var texPaths = new List<string> ();
				for (int p = 0; p < pages.Count; p++) {
					string page = pages[p];
					string pageSrc = Path.Combine (srcDir, page);
					if (!File.Exists (pageSrc))
						pageSrc = Path.Combine (LibraryRoot, "_shared", "textures", page);
					if (!File.Exists (pageSrc))
						throw new Exception ("texture page '" + page + "' is in neither " + srcDir +
							" nor " + Path.Combine (LibraryRoot, "_shared", "textures"));
					texPaths.Add (CopyToProject (pageSrc, dstDir));
				}

				// Import everything first: the importer must exist before its settings are applied.
				for (int i = 0; i < texPaths.Count; i++)
					AssetDatabase.ImportAsset (texPaths[i], ImportAssetOptions.ForceUpdate);
				AssetDatabase.ImportAsset (skeletonAssetPath, ImportAssetOptions.ForceUpdate);
				AssetDatabase.ImportAsset (atlasAssetPath, ImportAssetOptions.ForceUpdate);

				for (int i = 0; i < texPaths.Count; i++)
					ConfigureTexture (texPaths[i]);

				var atlasTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (atlasAssetPath);
				var skeletonTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset> (skeletonAssetPath);
				if (atlasTextAsset == null)
					throw new Exception ("atlas TextAsset did not import: " + atlasAssetPath);
				if (skeletonTextAsset == null)
					throw new Exception ("skeleton TextAsset did not import: " + skeletonAssetPath);

				Shader shader = Shader.Find ("Spine/SkeletonGraphic");
				if (shader == null)
					throw new Exception ("shader 'Spine/SkeletonGraphic' not found (Assets/Spine missing?)");

				string assetDir = ImportRoot + "/" + id;
				var materials = new Material[pages.Count];
				for (int i = 0; i < pages.Count; i++) {
					string matPath = assetDir + "/" + MaterialName (id, pages, i);
					var texture = AssetDatabase.LoadAssetAtPath<Texture2D> (texPaths[i]);
					if (texture == null)
						throw new Exception ("texture did not import: " + texPaths[i]);
					materials[i] = EnsureMaterial (matPath, shader, texture, premultiplied);
				}

				string atlasAssetOut = assetDir + "/" + id + "_Atlas.asset";
				var atlasAsset = AssetDatabase.LoadAssetAtPath<SpineAtlasAsset> (atlasAssetOut);
				if (atlasAsset == null) {
					atlasAsset = SpineAtlasAsset.CreateRuntimeInstance (atlasTextAsset, materials, false);
					AssetDatabase.CreateAsset (atlasAsset, atlasAssetOut);
				} else {
					atlasAsset.atlasFile = atlasTextAsset;
					atlasAsset.materials = materials;
					atlasAsset.Clear ();
					EditorUtility.SetDirty (atlasAsset);
				}

				string dataAssetOut = assetDir + "/" + id + "_SkeletonData.asset";
				var dataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset> (dataAssetOut);
				if (dataAsset == null) {
					dataAsset = SkeletonDataAsset.CreateRuntimeInstance (skeletonTextAsset, atlasAsset, false, 0.01f);
					AssetDatabase.CreateAsset (dataAsset, dataAssetOut);
				} else {
					dataAsset.skeletonJSON = skeletonTextAsset;
					dataAsset.atlasAssets = new AtlasAssetBase[] { atlasAsset };
					dataAsset.scale = 0.01f;
					dataAsset.Clear ();
				}
				dataAsset.defaultMix = 0.2f;
				EditorUtility.SetDirty (dataAsset);

				var skeletonData = dataAsset.GetSkeletonData (true);
				if (skeletonData == null)
					throw new Exception ("SkeletonDataAsset could not read the skeleton (see the 3.8.75 guard patch)");

				string grayPath = assetDir + "/" + GrayMaterialName (id, pages, 0);
				EnsureGray (grayPath, materials[0]);

				Debug.LogFormat (
					"[SpineLibrary] {0}: version={1} bones={2} slots={3} skins={4} animations={5} " +
					"pages={6} alpha={7} ({8}) -> {9}",
					id, skeletonData.Version, skeletonData.Bones.Count, skeletonData.Slots.Count,
					skeletonData.Skins.Count, skeletonData.Animations.Count, pages.Count,
					premultiplied ? "premultiplied" : "straight", alphaEvidence, assetDir);

				for (int i = 0; i < pages.Count; i++)
					Debug.LogFormat ("[SpineLibrary] {0}: page {1} -> {2}", id, pages[i], materials[i].name);
				return true;
			} catch (Exception e) {
				reason = e.Message;
				Debug.LogErrorFormat ("[SpineLibrary] '{0}' failed: {1}", id, e);
				return false;
			}
		}

		static string CopyToProject (string src, string dstDir)
		{
			string dst = Path.Combine (dstDir, Path.GetFileName (src));
			File.Copy (src, dst, true);
			return "Assets/_LocalReference/Spine/" + Path.GetFileName (dstDir) + "/" + Path.GetFileName (dst);
		}

		static string FindSkeleton (string dir, string id)
		{
			string binary = Path.Combine (dir, id + ".skel.bytes");
			if (File.Exists (binary))
				return binary;
			string json = Path.Combine (dir, id + ".json");
			if (File.Exists (json))
				return json;
			string[] any = Directory.GetFiles (dir, id + ".skel*");
			return any.Length > 0 ? any[0] : null;
		}

		static string MaterialName (string id, List<string> pages, int index)
		{
			if (index == 0)
				return id + "_Material.mat";
			return id + "_Material_" + Path.GetFileNameWithoutExtension (pages[index]) + ".mat";
		}

		static string GrayMaterialName (string id, List<string> pages, int index)
		{
			if (index == 0)
				return id + "_Material_Gray.mat";
			return id + "_Material_Gray_" + Path.GetFileNameWithoutExtension (pages[index]) + ".mat";
		}

		static List<string> AtlasPages (string atlasText)
		{
			var pages = new List<string> ();
			string[] lines = atlasText.Replace ("\r\n", "\n").Split ('\n');
			for (int i = 0; i < lines.Length; i++) {
				string s = lines[i].Trim ();
				if (s.Length > 0 && s.IndexOf (':') < 0 &&
					s.EndsWith (".png", StringComparison.OrdinalIgnoreCase))
					pages.Add (s);
			}
			return pages;
		}

		static void ConfigureTexture (string path)
		{
			var importer = AssetImporter.GetAtPath (path) as TextureImporter;
			if (importer == null)
				throw new Exception ("TextureImporter missing for " + path);

			// Same settings as the Chibi reference: Spine PMA atlas, no mips, clamp, bilinear.
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

		static Material EnsureMaterial (string path, Shader shader, Texture2D texture, bool premultiplied)
		{
			var material = AssetDatabase.LoadAssetAtPath<Material> (path);
			if (material == null) {
				material = new Material (shader);
				AssetDatabase.CreateAsset (material, path);
			}
			material.shader = shader;
			material.mainTexture = texture;
			material.SetTexture ("_MainTex", texture);
			if (material.HasProperty ("_StraightAlphaInput"))
				material.SetFloat ("_StraightAlphaInput", premultiplied ? 0f : 1f);
			if (premultiplied)
				material.DisableKeyword ("_STRAIGHT_ALPHA_INPUT");
			else
				material.EnableKeyword ("_STRAIGHT_ALPHA_INPUT");
			EditorUtility.SetDirty (material);
			return material;
		}

		static void EnsureGray (string path, Material normal)
		{
			Shader shader = Shader.Find ("Spine/SkeletonGraphic Grayscale");
			if (shader == null)
				throw new Exception ("shader 'Spine/SkeletonGraphic Grayscale' not found " +
					"(Assets/Scripts/SpineSystem/Shaders)");

			var gray = AssetDatabase.LoadAssetAtPath<Material> (path);
			if (gray == null) {
				gray = new Material (shader);
				AssetDatabase.CreateAsset (gray, path);
			}
			gray.shader = shader;
			gray.mainTexture = normal.mainTexture;
			gray.SetTexture ("_MainTex", normal.mainTexture);
			gray.SetFloat ("_StraightAlphaInput", normal.GetFloat ("_StraightAlphaInput"));
			gray.SetFloat ("_GrayAmount", 1f);
			if (normal.IsKeywordEnabled ("_STRAIGHT_ALPHA_INPUT"))
				gray.EnableKeyword ("_STRAIGHT_ALPHA_INPUT");
			else
				gray.DisableKeyword ("_STRAIGHT_ALPHA_INPUT");
			gray.DisableKeyword ("_CANVAS_GROUP_COMPATIBLE");
			EditorUtility.SetDirty (gray);
		}

		static List<string> SuggestIds (string id)
		{
			var result = new List<string> ();
			if (!Directory.Exists (LibraryRoot))
				return result;

			string family = id.Split ('_')[0];
			string prefix = id.Length > 3 ? id.Substring (0, Mathf.Min (6, id.Length)) : id;
			string[] needles = { id, family + "_", prefix };
			string[] dirs = Directory.GetDirectories (LibraryRoot);
			for (int n = 0; n < needles.Length && result.Count < 8; n++) {
				string needle = needles[n];
				if (needle.Length < 2)
					continue;
				for (int i = 0; i < dirs.Length && result.Count < 8; i++) {
					string name = Path.GetFileName (dirs[i]);
					if (name.StartsWith ("_", StringComparison.Ordinal) || result.Contains (name))
						continue;
					if (name.IndexOf (needle, StringComparison.OrdinalIgnoreCase) >= 0)
						result.Add (name);
				}
			}
			if (result.Count == 0) {
				// cheap fallback: first few ids so the user sees the expected format
				for (int i = 0; i < dirs.Length && result.Count < 5; i++) {
					string name = Path.GetFileName (dirs[i]);
					if (!name.StartsWith ("_", StringComparison.Ordinal))
						result.Add (name);
				}
			}
			return result;
		}

		// ------------------------------------------------------------------
		// Runtime stats (catalogue input)
		// ------------------------------------------------------------------

		/// <summary>
		/// Atlas texture loader that loads nothing: the catalogue only needs the atlas region table,
		/// not the pixels (the textures are not part of this parse).
		/// </summary>
		class NullTextureLoader : TextureLoader
		{
			public void Load (AtlasPage page, string path) { }
			public void Unload (object texture) { }
		}

		/// <summary>
		/// Fallback loader for the few skeletons whose atlas does not contain a region the skeleton
		/// references. It still builds every attachment so bones/slots/skins/animations can be read;
		/// only the UV data would be invalid, which a catalogue does not use.
		/// </summary>
		class LenientAttachmentLoader : AttachmentLoader
		{
			public RegionAttachment NewRegionAttachment (Skin skin, string name, string path) { return new RegionAttachment (name); }
			public MeshAttachment NewMeshAttachment (Skin skin, string name, string path) { return new MeshAttachment (name); }
			public BoundingBoxAttachment NewBoundingBoxAttachment (Skin skin, string name) { return new BoundingBoxAttachment (name); }
			public PathAttachment NewPathAttachment (Skin skin, string name) { return new PathAttachment (name); }
			public PointAttachment NewPointAttachment (Skin skin, string name) { return new PointAttachment (name); }
			public ClippingAttachment NewClippingAttachment (Skin skin, string name) { return new ClippingAttachment (name); }
		}

		[Serializable]
		public class StatRow
		{
			public string id;
			public string version;
			public int bones;
			public int slots;
			public int skins;
			public int animations;
			public string[] skinNames = new string[0];
			public string[] animationNames = new string[0];
			public string skeletonFile;
			public string error;
			public string atlasWarning;
		}

		[Serializable]
		public class StatDoc
		{
			public string generated;
			public int count;
			public StatRow[] rows = new StatRow[0];
		}

		static SkeletonData ReadSkeleton (byte[] bytes, AttachmentLoader loader)
		{
			using (var stream = new MemoryStream (bytes, false)) {
				var binary = new SkeletonBinary (loader);
				return binary.ReadSkeletonData (stream);
			}
		}

		/// <summary>
		/// Parses every skeleton in the library with the installed 3.8 runtime (read only: nothing
		/// is imported into Assets) and writes the facts the catalogue needs.
		/// </summary>
		public static int DumpRuntimeStats (string outPath)
		{
			var doc = new StatDoc ();
			doc.generated = DateTime.Now.ToString ("yyyy-MM-ddTHH:mm:ss");

			if (!Directory.Exists (LibraryRoot)) {
				Debug.LogError ("[SpineLibrary] library not found: " + LibraryRoot);
				return 0;
			}

			string[] dirs = Directory.GetDirectories (LibraryRoot);
			var rows = new List<StatRow> ();
			int ok = 0;
			for (int i = 0; i < dirs.Length; i++) {
				string name = Path.GetFileName (dirs[i]);
				if (name.StartsWith ("_", StringComparison.Ordinal))
					continue;

				var row = new StatRow { id = name };
				string skeletonPath = FindSkeleton (dirs[i], name);
				if (skeletonPath == null) {
					row.error = "no skeleton file";
					rows.Add (row);
					continue;
				}
				row.skeletonFile = Path.GetFileName (skeletonPath);
				try {
					byte[] bytes = File.ReadAllBytes (skeletonPath);
					string atlasPath = Path.Combine (dirs[i], name + ".atlas.txt");
					if (!File.Exists (atlasPath))
						throw new Exception ("missing " + name + ".atlas.txt");

					// The binary reader resolves region attachments through the atlas, so give it the
					// text atlas (no textures needed).
					var atlas = new Atlas (new StringReader (File.ReadAllText (atlasPath, Encoding.UTF8)),
						dirs[i], new NullTextureLoader ());
					SkeletonData data;
					try {
						data = ReadSkeleton (bytes, new AtlasAttachmentLoader (atlas));
					} catch (ArgumentException atlasError) {
						// Six game skeletons reference a region their atlas does not contain. Parse them
						// anyway so the catalogue is complete, and record the integrity warning.
						row.atlasWarning = atlasError.Message;
						data = ReadSkeleton (bytes, new LenientAttachmentLoader ());
					}
					if (data == null)
						throw new Exception ("ReadSkeletonData returned null");

					row.version = data.Version;
					row.bones = data.Bones.Count;
					row.slots = data.Slots.Count;
					row.skins = data.Skins.Count;
					row.animations = data.Animations.Count;

					var skins = new string[data.Skins.Count];
					for (int s = 0; s < skins.Length; s++)
						skins[s] = data.Skins.Items[s].Name;
					var anims = new string[data.Animations.Count];
					for (int a = 0; a < anims.Length; a++)
						anims[a] = data.Animations.Items[a].Name;
					row.skinNames = skins;
					row.animationNames = anims;
					ok++;
				} catch (Exception e) {
					row.error = e.GetType ().Name + ": " + e.Message;
				}
				rows.Add (row);

				if ((i + 1) % 500 == 0)
					Debug.LogFormat ("[SpineLibrary] stats {0}/{1} ({2} ok)", i + 1, dirs.Length, ok);
			}

			doc.rows = rows.ToArray ();
			doc.count = rows.Count;
			Directory.CreateDirectory (Path.GetDirectoryName (outPath));
			File.WriteAllText (outPath, JsonUtility.ToJson (doc, true), new UTF8Encoding (false));
			Debug.LogFormat ("[SpineLibrary] runtime stats: {0}/{1} parsed, {2} error(s) -> {3}",
				ok, rows.Count, rows.Count - ok, outPath);
			return ok;
		}
	}

	/// <summary>Tiny modal window: paste one or more ids (comma separated) and import.</summary>
	public class ImportSpineWindow : EditorWindow
	{
		string m_Ids = "";

		public static void ShowWindow ()
		{
			var window = GetWindow<ImportSpineWindow> (true, "Import Spine By Id");
			window.minSize = new Vector2 (560f, 130f);
			window.Show ();
		}

		void OnGUI ()
		{
			EditorGUILayout.LabelField ("Library", SpineLibraryTool.LibraryRoot);
			EditorGUILayout.HelpBox (
				"One id or several ids separated by commas, e.g. body_1110901,1123011\n" +
				"Copies into " + SpineLibraryTool.ImportRoot + "/<id>/ and builds " +
				"SkeletonDataAsset + Material.", MessageType.Info);
			m_Ids = EditorGUILayout.TextField ("Id(s)", m_Ids);
			bool hasIds = !string.IsNullOrEmpty (m_Ids.Trim ());
			using (new EditorGUI.DisabledScope (!hasIds)) {
				if (GUILayout.Button ("Import", GUILayout.Height (28f))) {
					SpineLibraryTool.ImportById (m_Ids);
					Close ();
				}
			}
		}
	}
}
