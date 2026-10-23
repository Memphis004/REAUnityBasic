using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyClone.Sect;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyClone.EditorTools
{
	/// <summary>
	/// Builds Assets/_Generated/SectMapPlaceholder.unity from the extracted sect-map data.
	///
	/// Menu:  Tools/Sect/Build Map Placeholder Scene
	/// Batch: -executeMethod MyClone.EditorTools.SectSceneBuilder.BuildBatch
	/// Verify:-executeMethod MyClone.EditorTools.SectSceneBuilder.VerifyBatch
	///
	/// Input (produced by ../extracted/build_sect_layout.py, never by hand):
	///   Assets/_Generated/Data/sect_layout.json
	///
	/// The scene is pure placeholder geometry (white sprite tinted per kind + TextMesh labels).
	/// No game art, prefab, script or asset is referenced; the JSON only carries numbers.
	/// </summary>
	public static class SectSceneBuilder
	{
		const string DataDir = "Assets/_Generated/Data";
		const string LayoutPath = DataDir + "/sect_layout.json";
		const string SpriteDir = "Assets/_Generated/SectMap";
		const string SpritePath = SpriteDir + "/White.png";
		const string VertexMaterialPath = SpriteDir + "/SectVertexColor.mat";
		const string ChunkDir = SpriteDir + "/Chunks";
		const float ChunkSizeUnits = 32f;
		const string ScenePath = "Assets/_Generated/SectMapPlaceholder.unity";
		const string ReportPath = DataDir + "/sect_scene_build_report.json";

		// เกมวาง background stage ไว้ที่ตำแหน่งนี้ (HexGrid/mapCamera/stageRoot + bgCamera)
		static readonly Vector3 StageOffset = new Vector3 (-1000f, -1000f, -4f);

		[MenuItem ("Tools/Sect/Build Map Placeholder Scene")]
		public static void BuildFromMenu ()
		{
			Build ();
			EditorUtility.DisplayDialog ("Sect Map",
				"Built " + ScenePath + "\n" + LastSummary, "OK");
		}

		public static void BuildBatch ()
		{
			try {
				Build ();
				EditorApplication.Exit (0);
			} catch (Exception e) {
				Debug.LogError ("[SectScene] FAILED: " + e);
				EditorApplication.Exit (1);
			}
		}

		static string LastSummary = "";

		// ------------------------------------------------------------------
		// entry
		// ------------------------------------------------------------------

		public static void Build ()
		{
			Debug.Log ("[SectScene] build start");

			if (!File.Exists (LayoutPath))
				throw new Exception ("missing " + LayoutPath + " (run extracted/build_sect_layout.py first)");

			var layout = JsonUtility.FromJson<SectLayout> (File.ReadAllText (LayoutPath, Encoding.UTF8));
			if (layout == null || layout.camera == null || layout.cells == null)
				throw new Exception ("cannot parse " + LayoutPath);
			if (layout.stageTiles == null) layout.stageTiles = new SectStageTile[0];
			if (layout.stagePlaces == null) layout.stagePlaces = new SectStagePlace[0];
			if (layout.groups == null) layout.groups = new SectGroup[0];
			if (layout.named == null) layout.named = new SectGroup[0];
			if (layout.sprites == null) layout.sprites = new SectSprite[0];

			EnsureSprite ();
			EnsureVertexMaterial ();
			ResetChunkFolder ();

			int addedLayers = EnsureSortingLayers (layout.sortingLayers);

			var scene = EditorSceneManager.NewScene (NewSceneSetup.EmptyScene, NewSceneMode.Single);
			scene.name = "SectMapPlaceholder";

			var root = new GameObject ("SectMap").transform;
			var camRoot = Child (root, "Cameras");
			var mapCamera = BuildCameras (camRoot, layout.camera);
			var hexGrid = BuildHexGrid (root, layout);
			var background = BuildBackground (root, layout);
			var parallax = BuildParallax (root, layout);

			if (mapCamera != null)
				Selection.activeGameObject = mapCamera.gameObject;

			var report = new BuildReport {
				layoutPath = LayoutPath,
				scenePath = ScenePath,
				sortingLayersAdded = addedLayers,
				cells = layout.cells.Length,
				entities = layout.entities.Length,
				stageTiles = layout.stageTiles.Length,
				stageTileChunks = s_ChunkCount,
				stagePlaces = layout.stagePlaces.Length,
				labelCount = CountLabels (root),
				spriteRendererCount = root.GetComponentsInChildren<SpriteRenderer> (true).Length,
				gameObjectCount = root.GetComponentsInChildren<Transform> (true).Length,
				hexGridChildren = hexGrid != null ? hexGrid.childCount : 0,
				backgroundChildren = background != null ? background.childCount : 0,
				parallaxChildren = parallax != null ? parallax.childCount : 0,
				skipped = new string[0],
			};

			Directory.CreateDirectory (DataDir);
			File.WriteAllText (ReportPath, JsonUtility.ToJson (report, true), Encoding.UTF8);

			AssetDatabase.SaveAssets ();
			EditorSceneManager.SaveScene (scene, ScenePath);
			AssetDatabase.Refresh ();

			LastSummary = string.Format (
				"{0} cells, {1} entities, {2} stage tiles, {3} stage places, {4} labels, {5} GameObjects, {6} sorting layers added",
				report.cells, report.entities, report.stageTiles, report.stagePlaces,
				report.labelCount, report.gameObjectCount, addedLayers);
			Debug.Log ("[SectScene] build done -> " + ScenePath + " : " + LastSummary);
		}

		// ------------------------------------------------------------------
		// cameras
		// ------------------------------------------------------------------

		static Transform BuildCameras (Transform parent, SectCamera c)
		{
			var map = new GameObject ("MapCamera");
			map.transform.SetParent (parent, false);
			map.transform.localPosition = new Vector3 (c.posX, c.posY, c.posZ);
			var cam = map.AddComponent<Camera> ();
			cam.orthographic = true;
			cam.orthographicSize = c.orthoSize;
			cam.nearClipPlane = c.nearClip;
			cam.farClipPlane = c.farClip;
			cam.depth = c.depth;
			cam.clearFlags = ClearFlags (c.clearFlags);
			Color bg;
			if (ColorUtility.TryParseHtmlString (c.bgColor, out bg))
				cam.backgroundColor = bg;
			cam.cullingMask = ~0;
			map.tag = "MainCamera";

			var pan = map.AddComponent<SectCameraPanZoom> ();
			pan.borderMin = new Vector2 (c.borderMinX, c.borderMinY);
			pan.borderMax = new Vector2 (c.borderMaxX, c.borderMaxY);
			pan.minOrthoSize = c.orthoMin;
			pan.maxOrthoSize = c.orthoMax;

			var bgGo = new GameObject ("BgCamera");
			bgGo.transform.SetParent (parent, false);
			bgGo.transform.localPosition = new Vector3 (c.bgPosX, c.bgPosY, c.bgPosZ);
			bgGo.transform.localRotation = Quaternion.Euler (c.bgRotX, 0f, 0f);
			var bgCam = bgGo.AddComponent<Camera> ();
			bgCam.orthographic = false;
			bgCam.fieldOfView = c.bgFov;
			bgCam.nearClipPlane = c.bgNear;
			bgCam.farClipPlane = c.bgFar;
			bgCam.depth = c.bgDepth;
			bgCam.clearFlags = ClearFlags (c.bgClearFlags);
			bgCam.cullingMask = ~0;

			return cam.transform;
		}

		static CameraClearFlags ClearFlags (int value)
		{
			// Unity: Skybox=1, Color=2, Depth=3, Nothing=4
			switch (value) {
			case 1: return CameraClearFlags.Skybox;
			case 2: return CameraClearFlags.SolidColor;
			case 3: return CameraClearFlags.Depth;
			case 4: return CameraClearFlags.Nothing;
			default: return CameraClearFlags.SolidColor;
			}
		}

		// ------------------------------------------------------------------
		// hex grid + cells + buildings
		// ------------------------------------------------------------------

		static readonly Dictionary<int, string> EntityKind = new Dictionary<int, string> {
			{ 1, "building" }, { 2, "plot" }, { 3, "gate" }, { 5, "start" }, { 6, "special" },
		};

		static Transform BuildHexGrid (Transform root, SectLayout layout)
		{
			var gridGo = new GameObject ("HexGrid", typeof (Grid));
			gridGo.transform.SetParent (root, false);
			var grid = gridGo.GetComponent<Grid> ();
			grid.cellSize = new Vector3 (layout.grid.cellW, layout.grid.cellH, 0f);
			grid.cellGap = Vector3.zero;
			grid.cellLayout = (GridLayout.CellLayout) layout.grid.cellLayout;
			grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

			// layer roots (tilemap ของเกมว่างใน prefab แต่มี sorting layer/ตำแหน่งจริง)
			var layersRoot = Child (gridGo.transform, "Layers");
			foreach (var l in layout.layerRoots) {
				var go = new GameObject (l.name);
				go.transform.SetParent (layersRoot, false);
				go.transform.localPosition = new Vector3 (l.x, l.y, l.z);
				float w = Mathf.Max (l.sizeX * layout.grid.cellW, layout.grid.cellW);
				float h = Mathf.Max (l.sizeY * layout.grid.cellH, layout.grid.cellH);
				Quad (go.transform, "extent", Vector3.zero, new Vector2 (w, h),
					new Color (0.35f, 0.55f, 0.75f, 0.10f),
					l.sortingLayer ?? DefaultLayer (layout), l.sortingOrder);
			}

			// cells (mjmap.map.zongmen1)
			var cellsRoot = Child (gridGo.transform, "Cells");
			float stepY = layout.grid.cellH * 0.75f;
			foreach (var c in layout.cells) {
				Vector2 pos = HexPosition (c.x, c.y, layout.grid.cellW, stepY);
				var color = CellColor (c);
				var go = Quad (cellsRoot, string.Format ("cell_{0:00}_{1:00}_id{2}", c.x, c.y, c.cellId),
					new Vector3 (pos.x, pos.y, 0f),
					new Vector2 (layout.grid.cellW * 0.95f, layout.grid.cellH * 0.95f),
					color, "ITGround1", 0);
				var label = Label (go.transform, string.Format ("{0},{1}", c.x, c.y), 0.16f, "ITGround1", 1);
				label.gameObject.SetActive (false); // 158 ป้ายจะทับกัน: เปิดเองได้ใน Hierarchy
			}

			// buildings / entities
			var entRoot = Child (gridGo.transform, "Entities");
			foreach (var e in layout.entities) {
				Vector2 pos = HexPosition (e.x, e.y, layout.grid.cellW, stepY);
				var color = EntityColor (e.type);
				string kind = EntityKind.ContainsKey (e.type) ? EntityKind[e.type] : "type" + e.type;
				var go = Quad (entRoot, string.Format ("{0}_{1}_{2:00}_{3:00}", kind, e.id, e.x, e.y),
					new Vector3 (pos.x, pos.y, -0.02f),
					new Vector2 (layout.grid.cellW * 0.9f, layout.grid.cellH * 0.9f),
					color, "ITBuilding", 0);
				string text = e.id + (string.IsNullOrEmpty (e.hint) ? "" : " " + e.hint);
				var label = Label (go.transform, text, 0.22f, "ITBuilding", 10);
				label.transform.localPosition = new Vector3 (0f, layout.grid.cellH * 0.6f, 0f);
			}

			return gridGo.transform;
		}

		static Vector2 HexPosition (int cx, int cy, float cellW, float stepY)
		{
			// hex pointy-top: คอลัมน์เว้นตาม cellW, แถวเว้น 3/4 ของความสูง, แถวคี่เลื่อนครึ่งช่อง
			return new Vector2 ((cx + 0.5f * (cy & 1)) * cellW, cy * stepY);
		}

		static Color CellColor (SectCell c)
		{
			float t = Mathf.Clamp01 ((c.cellId - 101) / 9f);
			var hue = Color.HSVToRGB (0.28f + t * 0.14f, 0.35f, 0.85f);
			hue.a = 0.75f;
			return hue;
		}

		static Color EntityColor (int type)
		{
			switch (type) {
			case 1: return new Color (0.75f, 0.31f, 0.30f, 0.95f);   // building
			case 2: return new Color (0.50f, 0.39f, 0.63f, 0.95f);   // plot / decoration
			case 3: return new Color (0.31f, 0.51f, 0.74f, 0.95f);   // gate
			case 5: return new Color (0.97f, 0.59f, 0.27f, 0.95f);   // spawn
			case 6: return new Color (0.61f, 0.73f, 0.35f, 0.95f);   // special
			default: return new Color (0.58f, 0.70f, 0.84f, 0.95f);
			}
		}

		// ------------------------------------------------------------------
		// background (mountain1) + parallax sprites
		// ------------------------------------------------------------------

		static int s_ChunkCount;

		static Transform BuildBackground (Transform root, SectLayout layout)
		{
			var stage = Child (root, "Background");
			stage.localPosition = StageOffset;

			// ช่องผิวของฉากหลัง: จาก TextAsset map_data_* ใน mountain1.ab (positionList/idList)
			float tw = Mathf.Max (layout.stage != null ? layout.stage.tileSizeX : 1f, 0.01f);
			float th = Mathf.Max (layout.stage != null ? layout.stage.tileSizeY : 0.5f, 0.01f);

			var tilesRoot = Child (stage, "Tiles");
			s_ChunkCount = BuildTileChunks (tilesRoot, layout, tw, th);
			Debug.Log ("[SectScene] stage tiles merged into " + s_ChunkCount + " chunk mesh(es) for " +
				layout.stageTiles.Length + " tiles");

			// วัตถุ/สิ่งปลูกสร้างที่วางบนฉากหลัง (placePositionList/placeIdList)
			var placesRoot = Child (stage, "Places");
			foreach (var p in layout.stagePlaces) {
				Quad (placesRoot, string.Format ("p{0}_{1}_id{2}", p.x, p.y, p.id),
					new Vector3 (p.wx, p.wy, -0.01f), new Vector2 (tw * 0.8f, th * 1.6f),
					StagePlaceColor (p.id), "ITGround1", 1);
			}

			// ป้ายระดับกลุ่ม (ไม่ใช่กล่อง): 5 กลุ่มหลักของ mountain1 + จุดที่แมปกับ entity ได้
			var markers = Child (stage, "GroupMarkers");
			foreach (var g in layout.groups) {
				var go = new GameObject (g.name);
				go.transform.SetParent (markers, false);
				go.transform.localPosition = new Vector3 ((g.minX + g.maxX) * 0.5f - StageOffset.x,
					(g.minY + g.maxY) * 0.5f - StageOffset.y, -0.1f);
				Label (go.transform, string.Format ("{0} ({1})", g.name, g.count), 1.0f, "ITGround1", 5);
			}
			foreach (var g in layout.named) {
				var go = new GameObject (g.name);
				go.transform.SetParent (markers, false);
				go.transform.localPosition = new Vector3 ((g.minX + g.maxX) * 0.5f - StageOffset.x,
					(g.minY + g.maxY) * 0.5f - StageOffset.y, -0.1f);
				Label (go.transform, g.name, 0.6f, "ITGround1", 6);
			}

			return stage;
		}

		// ------------------------------------------------------------------
		// stage tiles -> chunk meshes (แทน GameObject ต่อช่อง 5.5k ชิ้น)
		// ------------------------------------------------------------------

		static int BuildTileChunks (Transform parent, SectLayout layout, float tw, float th)
		{
			if (layout.stageTiles.Length == 0)
				return 0;

			var buckets = new Dictionary<Vector2Int, List<SectStageTile>> ();
			foreach (var t in layout.stageTiles) {
				var key = new Vector2Int (Mathf.FloorToInt (t.wx / ChunkSizeUnits),
					Mathf.FloorToInt (t.wy / ChunkSizeUnits));
				List<SectStageTile> list;
				if (!buckets.TryGetValue (key, out list)) {
					list = new List<SectStageTile> ();
					buckets.Add (key, list);
				}
				list.Add (t);
			}

			var material = AssetDatabase.LoadAssetAtPath<Material> (VertexMaterialPath);
			if (material == null)
				throw new Exception ("missing " + VertexMaterialPath);

			string layer = GroundLayer (layout);
			foreach (var kv in buckets) {
				var mesh = BuildChunkMesh (kv.Key, kv.Value, tw, th);
				string path = string.Format ("{0}/StageChunk_{1}_{2}.asset", ChunkDir, kv.Key.x, kv.Key.y);
				AssetDatabase.CreateAsset (mesh, path);

				var go = new GameObject (string.Format ("chunk_{0}_{1}", kv.Key.x, kv.Key.y));
				go.transform.SetParent (parent, false);
				go.AddComponent<MeshFilter> ().sharedMesh = mesh;
				var mr = go.AddComponent<MeshRenderer> ();
				mr.sharedMaterial = material;
				if (!string.IsNullOrEmpty (layer))
					mr.sortingLayerName = layer;
				mr.sortingOrder = 0;
			}
			return buckets.Count;
		}

		static Mesh BuildChunkMesh (Vector2Int key, List<SectStageTile> tiles, float tw, float th)
		{
			int n = tiles.Count;
			var verts = new Vector3[n * 4];
			var colors = new Color[n * 4];
			var uvs = new Vector2[n * 4];
			var tris = new int[n * 6];

			float hw = tw * 0.95f * 0.5f, hh = th * 0.95f * 0.5f;
			for (int i = 0; i < n; i++) {
				var t = tiles[i];
				var c = StageTileColor (t.id);
				int v = i * 4;
				verts[v + 0] = new Vector3 (t.wx - hw, t.wy - hh, 0f);
				verts[v + 1] = new Vector3 (t.wx - hw, t.wy + hh, 0f);
				verts[v + 2] = new Vector3 (t.wx + hw, t.wy + hh, 0f);
				verts[v + 3] = new Vector3 (t.wx + hw, t.wy - hh, 0f);
				uvs[v + 0] = new Vector2 (0f, 0f);
				uvs[v + 1] = new Vector2 (0f, 1f);
				uvs[v + 2] = new Vector2 (1f, 1f);
				uvs[v + 3] = new Vector2 (1f, 0f);
				colors[v + 0] = colors[v + 1] = colors[v + 2] = colors[v + 3] = c;
				int s = i * 6;
				tris[s + 0] = v + 0; tris[s + 1] = v + 1; tris[s + 2] = v + 2;
				tris[s + 3] = v + 0; tris[s + 4] = v + 2; tris[s + 5] = v + 3;
			}

			var mesh = new Mesh ();
			mesh.name = string.Format ("StageChunk_{0}_{1}", key.x, key.y);
			mesh.indexFormat = verts.Length > 65000
				? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
			mesh.vertices = verts;
			mesh.colors = colors;
			mesh.uv = uvs;
			mesh.triangles = tris;
			mesh.RecalculateBounds ();
			return mesh;
		}

		static string GroundLayer (SectLayout layout)
		{
			if (layout.layerRoots != null) {
				foreach (var l in layout.layerRoots) {
					if (l.name == "ground" && !string.IsNullOrEmpty (l.sortingLayer))
						return l.sortingLayer;
				}
			}
			return "ITGround1";
		}

		static void EnsureVertexMaterial ()
		{
			if (AssetDatabase.LoadAssetAtPath<Material> (VertexMaterialPath) != null)
				return;

			var shader = Shader.Find ("Sprites/Default");
			if (shader == null)
				throw new Exception ("shader Sprites/Default not found");
			var mat = new Material (shader) { name = "SectVertexColor" };
			if (m_White != null && m_White.texture != null) {
				mat.mainTexture = m_White.texture;
				mat.SetTexture ("_MainTex", m_White.texture);
			}
			AssetDatabase.CreateAsset (mat, VertexMaterialPath);
		}

		static void ResetChunkFolder ()
		{
			if (AssetDatabase.IsValidFolder (ChunkDir))
				AssetDatabase.DeleteAsset (ChunkDir);
			AssetDatabase.CreateFolder (SpriteDir, "Chunks");
		}

		static Color StageTileColor (int id)
		{
			if (id >= 1 && id <= 24) {
				var c = Color.HSVToRGB (0.22f + (id - 1) / 24f * 0.22f, 0.30f + (id % 4) * 0.05f, 0.80f);
				c.a = 0.90f;
				return c;
			}
			switch (id) {
			case 101: return new Color (0.72f, 0.66f, 0.55f, 0.95f); // ทาง/พื้นแข็ง
			case 102: return new Color (0.55f, 0.62f, 0.72f, 0.95f);
			case 104: return new Color (0.45f, 0.55f, 0.70f, 0.95f);
			case 105: return new Color (0.62f, 0.58f, 0.48f, 0.95f);
			default: return new Color (0.85f, 0.45f, 0.75f, 0.95f); // id หายาก
			}
		}

		static Color StagePlaceColor (int id)
		{
			float hue = Mathf.Repeat (id * 0.61803398875f, 1f);
			var c = Color.HSVToRGB (hue, 0.55f, 0.95f);
			c.a = 0.95f;
			return c;
		}

		static Transform BuildParallax (Transform root, SectLayout layout)
		{
			var parallax = Child (root, "Parallax");
			var byGroup = new Dictionary<string, Transform> ();
			foreach (var s in layout.sprites) {
				Transform group;
				if (!byGroup.TryGetValue (s.group, out group)) {
					group = Child (parallax, s.group);
					byGroup.Add (s.group, group);
				}
				Color color;
				if (!ColorUtility.TryParseHtmlString (s.color, out color))
					color = Color.white;
				if (s.kind == "cloud") color.a = 0.45f;
				else if (s.kind == "background") color.a = 0.6f;
				else if (s.kind == "surface") color.a = 0.75f;

				// ขนาด: Simple drawMode = sprite px / PPU * scale, อื่น ๆ ใช้ m_Size ที่ stretch ไว้
				float w = s.stretchW > 0f ? s.stretchW : (s.spritePixels != null && s.spritePixels.Length == 2
					? s.spritePixels[0] / SpritePPU * s.scaleX : 2f);
				float h = s.stretchH > 0f ? s.stretchH : (s.spritePixels != null && s.spritePixels.Length == 2
					? s.spritePixels[1] / SpritePPU * s.scaleY : 2f);
				var go = Quad (group, s.name, new Vector3 (s.x, s.y, s.z), new Vector2 (w, h),
					color, s.sortingLayer ?? DefaultLayer (layout), s.sortingOrder);
				if (s.kind != "cloud")
					Label (go.transform, s.group, 0.5f, s.sortingLayer ?? DefaultLayer (layout), s.sortingOrder + 5);
			}
			return parallax;
		}

		const float SpritePPU = 50f; // Sprite.m_PixelsToUnits ของ sprite เมฆในเกม

		// ------------------------------------------------------------------
		// primitives
		// ------------------------------------------------------------------

		static Sprite m_White;

		static void EnsureSprite ()
		{
			if (!Directory.Exists (SpriteDir))
				Directory.CreateDirectory (SpriteDir);

			if (!File.Exists (SpritePath)) {
				var tex = new Texture2D (8, 8, TextureFormat.RGBA32, false);
				var px = new Color32[64];
				for (int i = 0; i < px.Length; i++)
					px[i] = new Color32 (255, 255, 255, 255);
				tex.SetPixels32 (px);
				tex.Apply ();
				File.WriteAllBytes (SpritePath, tex.EncodeToPNG ());
				UnityEngine.Object.DestroyImmediate (tex);
				AssetDatabase.ImportAsset (SpritePath, ImportAssetOptions.ForceSynchronousImport);

				var importer = AssetImporter.GetAtPath (SpritePath) as TextureImporter;
				if (importer != null) {
					importer.textureType = TextureImporterType.Sprite;
					importer.spriteImportMode = SpriteImportMode.Single;
					importer.spritePixelsPerUnit = 100f;
					importer.filterMode = FilterMode.Point;
					importer.mipmapEnabled = false;
					importer.textureCompression = TextureImporterCompression.Uncompressed;
					importer.alphaIsTransparency = true;
					importer.SaveAndReimport ();
				}
			}

			m_White = AssetDatabase.LoadAssetAtPath<Sprite> (SpritePath);
			if (m_White == null)
				throw new Exception ("no Sprite at " + SpritePath);
		}

		static Transform Child (Transform parent, string name)
		{
			var go = new GameObject (name);
			go.transform.SetParent (parent, false);
			return go.transform;
		}

		static Transform Quad (Transform parent, string name, Vector3 localPos, Vector2 size, Color color,
			string sortingLayer, int sortingOrder)
		{
			var go = new GameObject (name);
			go.transform.SetParent (parent, false);
			go.transform.localPosition = localPos;
			go.transform.localScale = new Vector3 (size.x, size.y, 1f);
			var sr = go.AddComponent<SpriteRenderer> ();
			sr.sprite = m_White;
			sr.color = color;
			if (!string.IsNullOrEmpty (sortingLayer))
				sr.sortingLayerName = sortingLayer;
			sr.sortingOrder = sortingOrder;
			return go.transform;
		}

		static TextMesh Label (Transform parent, string text, float size, string sortingLayer, int order)
		{
			var go = new GameObject ("label_" + text);
			go.transform.SetParent (parent, false);
			go.transform.localPosition = new Vector3 (0f, 0f, -0.05f);
			var tm = go.AddComponent<TextMesh> ();
			tm.text = text;
			tm.anchor = TextAnchor.MiddleCenter;
			tm.alignment = TextAlignment.Center;
			tm.fontSize = 48;
			tm.characterSize = size * 0.1f;
			tm.color = Color.white;
			var font = BuiltinFont ();
			if (font != null) {
				tm.font = font;
				var mr = go.GetComponent<MeshRenderer> ();
				if (mr != null && font.material != null)
					mr.sharedMaterial = font.material;
			}
			var r = go.GetComponent<Renderer> ();
			if (r != null && !string.IsNullOrEmpty (sortingLayer)) {
				r.sortingLayerName = sortingLayer;
				r.sortingOrder = order;
			}
			return tm;
		}

		static Font BuiltinFont ()
		{
			// Unity 2021: Arial.ttf ; Unity 2022+: LegacyRuntime.ttf
			var font = Resources.GetBuiltinResource<Font> ("Arial.ttf");
			if (font == null)
				font = Resources.GetBuiltinResource<Font> ("LegacyRuntime.ttf");
			return font;
		}

		static int CountLabels (Transform root)
		{
			int n = 0;
			foreach (var t in root.GetComponentsInChildren<TextMesh> (true))
				n++;
			return n;
		}

		static string DefaultLayer (SectLayout layout)
		{
			if (layout.sortingLayers != null && layout.sortingLayers.Length > 0)
				return layout.sortingLayers[layout.sortingLayers.Length - 1];
			return "Default";
		}

		// ------------------------------------------------------------------
		// sorting layers
		// ------------------------------------------------------------------

		/// <summary>
		/// Adds the game's sorting layer names (same order as the game's TagManager) to this
		/// project's TagManager. Existing names are left untouched.
		/// </summary>
		static int EnsureSortingLayers (string[] names)
		{
			if (names == null || names.Length == 0)
				return 0;

			var assets = AssetDatabase.LoadAllAssetsAtPath ("ProjectSettings/TagManager.asset");
			if (assets == null || assets.Length == 0)
				throw new Exception ("cannot open ProjectSettings/TagManager.asset");

			var so = new SerializedObject (assets[0]);
			var arr = so.FindProperty ("m_SortingLayers");
			var existing = new HashSet<string> ();
			for (int i = 0; i < arr.arraySize; i++) {
				var el = arr.GetArrayElementAtIndex (i);
				existing.Add (el.FindPropertyRelative ("name").stringValue);
			}

			int added = 0;
			foreach (var name in names) {
				if (string.IsNullOrEmpty (name) || existing.Contains (name))
					continue;
				arr.InsertArrayElementAtIndex (arr.arraySize);
				var el = arr.GetArrayElementAtIndex (arr.arraySize - 1);
				el.FindPropertyRelative ("name").stringValue = name;
				// uniqueID เป็น UInt32: ต้องเขียนผ่าน longValue ไม่งั้นค่าที่มีบิตสูงจะถูกทิ้งเป็น 0
				el.FindPropertyRelative ("uniqueID").longValue = (uint) Hash (name);
				el.FindPropertyRelative ("locked").boolValue = false;
				existing.Add (name);
				added++;
			}

			if (added > 0) {
				so.ApplyModifiedProperties ();
				AssetDatabase.SaveAssets ();
				Debug.Log ("[SectScene] sorting layers added: " + added);
			} else {
				Debug.Log ("[SectScene] sorting layers already present (" + existing.Count + ")");
			}
			return added;
		}

		static int Hash (string s)
		{
			unchecked {
				uint h = 2166136261u;
				foreach (byte b in Encoding.UTF8.GetBytes (s))
					h = (h ^ b) * 16777619u;
				return (int) h;
			}
		}

		// ------------------------------------------------------------------
		// verify
		// ------------------------------------------------------------------

		public static void VerifyBatch ()
		{
			try {
				Verify ();
				EditorApplication.Exit (0);
			} catch (Exception e) {
				Debug.LogError ("[SectScene] VERIFY FAILED: " + e);
				EditorApplication.Exit (1);
			}
		}

		public static void Verify ()
		{
			var layout = JsonUtility.FromJson<SectLayout> (File.ReadAllText (LayoutPath, Encoding.UTF8));
			var scene = EditorSceneManager.OpenScene (ScenePath, OpenSceneMode.Single);
			int errors = 0;

			Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera> ();
			SpriteRenderer[] quads = UnityEngine.Object.FindObjectsOfType<SpriteRenderer> ();
			TextMesh[] labels = UnityEngine.Object.FindObjectsOfType<TextMesh> ();

			var map = Find ("SectMap/Cameras/MapCamera");
			var bg = Find ("SectMap/Cameras/BgCamera");
			if (map == null) { Debug.LogError ("missing MapCamera"); errors++; }
			if (bg == null) { Debug.LogError ("missing BgCamera"); errors++; }

			if (map != null) {
				var cam = map.GetComponent<Camera> ();
				if (!Mathf.Approximately (cam.orthographicSize, layout.camera.orthoSize)) {
					Debug.LogError ("MapCamera ortho size " + cam.orthographicSize + " != " + layout.camera.orthoSize);
					errors++;
				}
				if (cam.depth != layout.camera.depth) { Debug.LogError ("MapCamera depth mismatch"); errors++; }
				if (map.CompareTag ("MainCamera") == false) { Debug.LogError ("MapCamera tag"); errors++; }
				if (map.GetComponent<SectCameraPanZoom> () == null) { Debug.LogError ("MapCamera missing pan/zoom"); errors++; }
			}

			int expectCells = layout.cells.Length;
			int gridCells = 0, gridEntities = 0;
			var cellsRoot = Find ("SectMap/HexGrid/Cells");
			var entRoot = Find ("SectMap/HexGrid/Entities");
			if (cellsRoot != null) gridCells = cellsRoot.childCount;
			if (entRoot != null) gridEntities = entRoot.childCount;
			if (gridCells != expectCells) { Debug.LogError ("cells " + gridCells + " != " + expectCells); errors++; }
			if (gridEntities != layout.entities.Length) { Debug.LogError ("entities mismatch"); errors++; }

			int layerRoots = 0;
			var layerRoot = Find ("SectMap/HexGrid/Layers");
			if (layerRoot != null) layerRoots = layerRoot.childCount;
			if (layerRoots != layout.layerRoots.Length) { Debug.LogError ("layer roots mismatch"); errors++; }

			var stage = Find ("SectMap/Background");
			if (stage == null || (stage.localPosition - StageOffset).sqrMagnitude > 0.001f) {
				Debug.LogError ("Background stage offset wrong");
				errors++;
			}

			int stageTiles = 0, stagePlaces = 0, outOfBounds = 0, chunks = 0;
			var tilesRoot = Find ("SectMap/Background/Tiles");
			var placesRoot = Find ("SectMap/Background/Places");
			if (tilesRoot != null) {
				chunks = tilesRoot.childCount;
				foreach (var mf in tilesRoot.GetComponentsInChildren<MeshFilter> ()) {
					if (mf.sharedMesh != null)
						stageTiles += mf.sharedMesh.vertexCount / 4;
				}
			}
			if (placesRoot != null) stagePlaces = placesRoot.childCount;
			if (stageTiles != layout.stageTiles.Length) {
				Debug.LogError ("stage tiles (quads) " + stageTiles + " != " + layout.stageTiles.Length);
				errors++;
			}
			if (chunks == 0) {
				Debug.LogError ("no stage tile chunk meshes");
				errors++;
			}
			if (stagePlaces != layout.stagePlaces.Length) {
				Debug.LogError ("stage places " + stagePlaces + " != " + layout.stagePlaces.Length);
				errors++;
			}
			// ช่องของฉากหลังต้องอยู่ในกรอบ mesh ของ mountain1 (x −103.46…64.82, y −43.28…26.11)
			foreach (var t in layout.stageTiles) {
				if (t.wx < -104f || t.wx > 65f || t.wy < -44f || t.wy > 27f)
					outOfBounds++;
			}

			// sorting layer ของ "ground" ต้องมีจริงในโปรเจกต์นี้
			foreach (var l in layout.layerRoots) {
				if (!string.IsNullOrEmpty (l.sortingLayer) && SortingLayer.NameToID (l.sortingLayer) == 0 && l.sortingLayer != "Default") {
					Debug.LogError ("sorting layer missing: " + l.sortingLayer);
					errors++;
				}
			}

			var sb = new StringBuilder ();
			sb.AppendLine ("scene            : " + scene.path);
			sb.AppendLine ("cameras          : " + cams.Length + " (map ortho=" + (map != null ? map.GetComponent<Camera> ().orthographicSize : 0) + ")");
			sb.AppendLine ("sprite renderers : " + quads.Length);
			sb.AppendLine ("labels           : " + labels.Length);
			sb.AppendLine ("cells            : " + gridCells + " / " + expectCells);
			sb.AppendLine ("entities         : " + gridEntities);
			sb.AppendLine ("layer roots      : " + layerRoots);
			sb.AppendLine ("background stage : " + (stage != null ? stage.localPosition.ToString ("F1") : "missing"));
			sb.AppendLine ("stage tiles      : " + stageTiles + " / " + layout.stageTiles.Length + " quads in " + chunks + " chunk mesh(es), นอกกรอบ mesh " + outOfBounds);
			sb.AppendLine ("stage places     : " + stagePlaces + " / " + layout.stagePlaces.Length);
			sb.AppendLine ("errors           : " + errors);

			var report = new StringBuilder ();
			report.AppendLine ("{");
			report.AppendLine ("  \"errors\": " + errors + ",");
			report.AppendLine ("  \"cameras\": " + cams.Length + ",");
			report.AppendLine ("  \"spriteRenderers\": " + quads.Length + ",");
			report.AppendLine ("  \"labels\": " + labels.Length + ",");
			report.AppendLine ("  \"cells\": " + gridCells + ",");
			report.AppendLine ("  \"entities\": " + gridEntities + ",");
			report.AppendLine ("  \"layerRoots\": " + layerRoots + ",");
			report.AppendLine ("  \"stageTiles\": " + stageTiles + ",");
			report.AppendLine ("  \"stageTileChunks\": " + chunks + ",");
			report.AppendLine ("  \"stagePlaces\": " + stagePlaces + ",");
			report.AppendLine ("  \"stageTilesOutOfMeshBounds\": " + outOfBounds);
			report.AppendLine ("}");

			Directory.CreateDirectory (DataDir);
			File.WriteAllText (DataDir + "/sect_scene_verify_report.json", report.ToString (), Encoding.UTF8);
			Debug.Log ("[SectScene] verify\n" + sb);

			if (errors > 0)
				throw new Exception (errors + " verification error(s)");
		}

		static Transform Find (string path)
		{
			var go = GameObject.Find (path);
			return go != null ? go.transform : null;
		}

		// ------------------------------------------------------------------
		// data classes (field names == sect_layout.json keys)
		// ------------------------------------------------------------------

		[Serializable]
		public class SectLayout
		{
			public SectCamera camera;
			public SectGrid grid;
			public SectLayerRoot[] layerRoots;
			public SectCell[] cells;
			public SectEntity[] entities;
			public SectGroup[] groups;
			public SectGroup[] named;
			public SectStage stage;
			public SectStageTile[] stageTiles;
			public SectStagePlace[] stagePlaces;
			public SectSprite[] sprites;
			public string[] sortingLayers;
		}

		[Serializable]
		public class SectCamera
		{
			public float orthoSize, orthoMin, orthoMax;
			public float nearClip, farClip, depth;
			public int clearFlags;
			public string bgColor;
			public float posX, posY, posZ;
			public float bgFov, bgNear, bgFar, bgDepth, bgRotX, bgPosX, bgPosY, bgPosZ;
			public int bgClearFlags;
			public float borderMinX, borderMinY, borderMaxX, borderMaxY;
			public string borderSource;
		}

		[Serializable]
		public class SectGrid
		{
			public float cellW, cellH;
			public int cellLayout, cellSwizzle;
			public float[] isoCellSize;
			public float isoCellPosOffsetY;
			public int isoCellLayout;
			public string note;
		}

		[Serializable]
		public class SectLayerRoot
		{
			public string name;
			public float x, y, z;
			public int tiles;
			public float sizeX, sizeY;
			public string sortingLayer;
			public int sortingOrder;
		}

		[Serializable]
		public class SectCell
		{
			public int x, y, cellId, gridType, group;
			public float height;
		}

		[Serializable]
		public class SectEntity
		{
			public int x, y, type, id;
			public bool hidden;
			public string label, hint;
		}

		[Serializable]
		public class SectGroup
		{
			public string name, path;
			public int count;
			public float minX, maxX, minY, maxY;
		}

		[Serializable]
		public class SectStage
		{
			public string textAsset, source, mapping;
			public float tileSizeX, tileSizeY, offsetY;
		}

		[Serializable]
		public class SectStageTile
		{
			public int x, y, id;
			public float wx, wy;
		}

		[Serializable]
		public class SectStagePlace
		{
			public int x, y, id;
			public float wx, wy;
		}

		[Serializable]
		public class SectSprite
		{
			public string name, kind, group, sprite, sortingLayer, color;
			public float x, y, z, scaleX, scaleY, stretchW, stretchH;
			public float[] spritePixels;
			public int drawMode, sortingOrder;
		}

		[Serializable]
		public class BuildReport
		{
			public string layoutPath, scenePath;
			public int sortingLayersAdded, cells, entities, stageTiles, stageTileChunks, stagePlaces;
			public int labelCount, spriteRendererCount;
			public int gameObjectCount, hexGridChildren, backgroundChildren, parallaxChildren;
			public string[] skipped;
		}
	}
}
