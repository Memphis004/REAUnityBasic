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
	/// Audits one SkeletonDataAsset: which slots each skin covers, which skins can be
	/// combined (disjoint slot coverage) and which slots no skin covers.
	///
	/// Menu: Tools/Local Reference/Audit Skin Groups
	/// Output: notes/skin_groups_&lt;id&gt;.md at the workspace root.
	///
	/// Grouping rule: skins that share at least one slot form one group (they are
	/// variants of the same body part — pick at most one). Different groups cover
	/// disjoint slots, so one pick per group composes into a full character the same
	/// way dbbodyconfig's spineSkins[] does.
	/// </summary>
	public class SkinGroupAuditWindow : EditorWindow
	{
		SkeletonDataAsset m_Data;
		string m_Report;
		Vector2 m_Scroll;

		[MenuItem ("Tools/Local Reference/Audit Skin Groups")]
		public static void Open ()
		{
			var window = GetWindow<SkinGroupAuditWindow> ("Skin Groups");
			window.m_Data = Selection.activeObject as SkeletonDataAsset;
		}

		void OnGUI ()
		{
			m_Data = (SkeletonDataAsset)EditorGUILayout.ObjectField (
				"Skeleton Data Asset", m_Data, typeof (SkeletonDataAsset), false);

			using (new EditorGUI.DisabledScope (m_Data == null)) {
				if (GUILayout.Button ("Audit & Save"))
					m_Report = SkinGroupAudit.Audit (m_Data);
			}

			if (string.IsNullOrEmpty (m_Report))
				return;

			m_Scroll = EditorGUILayout.BeginScrollView (m_Scroll);
			EditorGUILayout.TextArea (m_Report, GUILayout.ExpandHeight (true));
			EditorGUILayout.EndScrollView ();
		}
	}

	public static class SkinGroupAudit
	{
		/// <summary>Notes live at the workspace root, outside the Unity project.</summary>
		static string NotesDirectory {
			get { return Path.GetFullPath (Path.Combine (Application.dataPath, "..", "..", "notes")); }
		}

		/// <summary>Runs the audit, writes notes/skin_groups_&lt;id&gt;.md and returns the report text.</summary>
		public static string Audit (SkeletonDataAsset asset)
		{
			if (asset == null)
				return null;

			var data = asset.GetSkeletonData (true);
			if (data == null) {
				Debug.LogError ("[SkinGroupAudit] Could not load skeleton data from " + asset.name);
				return null;
			}

			string id = asset.name.EndsWith ("_SkeletonData", StringComparison.Ordinal)
				? asset.name.Substring (0, asset.name.Length - "_SkeletonData".Length)
				: asset.name;

			var slotNames = new List<string> ();
			var slots = data.Slots;
			for (int i = 0; i < slots.Count; i++)
				slotNames.Add (slots.Items[i].Name);

			var skins = new List<Skin> ();
			foreach (var skin in data.Skins)
				if (skin != null)
					skins.Add (skin);

			// skin name -> slot names it has entries for
			var coverage = new Dictionary<string, HashSet<string>> ();
			foreach (var skin in skins) {
				var coveredSlots = new HashSet<string> ();
				foreach (var entry in skin.GetAttachments ()) {
					if (entry.SlotIndex >= 0 && entry.SlotIndex < slotNames.Count)
						coveredSlots.Add (slotNames[entry.SlotIndex]);
				}
				coverage[skin.Name] = coveredSlots;
			}

			// Connected components over non-default skins sharing slots.
			var named = skins.FindAll (skin => skin.Name != "default");
			var groupOf = new int[named.Count];
			for (int i = 0; i < groupOf.Length; i++)
				groupOf[i] = -1;
			var groups = new List<List<int>> ();
			for (int i = 0; i < named.Count; i++) {
				if (groupOf[i] >= 0)
					continue;

				int index = groups.Count;
				var members = new List<int> { i };
				groupOf[i] = index;
				var queue = new Queue<int> ();
				queue.Enqueue (i);
				while (queue.Count > 0) {
					int a = queue.Dequeue ();
					for (int b = 0; b < named.Count; b++) {
						if (groupOf[b] >= 0)
							continue;
						if (coverage[named[a].Name].Overlaps (coverage[named[b].Name])) {
							groupOf[b] = index;
							members.Add (b);
							queue.Enqueue (b);
						}
					}
				}
				groups.Add (members);
			}

			// Slots no skin covers at all.
			var anyCovered = new HashSet<string> ();
			foreach (var skin in skins)
				anyCovered.UnionWith (coverage[skin.Name]);

			var neverCovered = new List<string> ();
			var setupButUncovered = new List<string> ();
			for (int i = 0; i < slots.Count; i++) {
				string slotName = slotNames[i];
				if (anyCovered.Contains (slotName))
					continue;
				neverCovered.Add (slotName);
				if (!string.IsNullOrEmpty (slots.Items[i].AttachmentName))
					setupButUncovered.Add (slotName);
			}

			string report = BuildReport (id, asset, data, slotNames, skins, coverage,
				named, groups, neverCovered, setupButUncovered);

			Directory.CreateDirectory (NotesDirectory);
			string path = Path.Combine (NotesDirectory, "skin_groups_" + id + ".md");
			File.WriteAllText (path, report);
			Debug.Log ("[SkinGroupAudit] written " + path);
			return report;
		}

		static string BuildReport (string id, SkeletonDataAsset asset, SkeletonData data,
			List<string> slotNames, List<Skin> skins, Dictionary<string, HashSet<string>> coverage,
			List<Skin> named, List<List<int>> groups, List<string> neverCovered, List<string> setupButUncovered)
		{
			var builder = new StringBuilder ();
			builder.AppendLine ("# Skin groups — " + id);
			builder.AppendLine ();
			builder.AppendLine ("Source: `" + AssetDatabase.GetAssetPath (asset) + "`");
			builder.AppendLine ("Generated: " + DateTime.Now.ToString ("yyyy-MM-dd") +
				" (Tools/Local Reference/Audit Skin Groups)");
			builder.AppendLine ();
			builder.AppendLine ("Slots: " + slotNames.Count + " | Skins: " + skins.Count +
				" (named " + named.Count + ") | Animations: " + data.Animations.Count);
			builder.AppendLine ();

			builder.AppendLine ("## Per-skin slot coverage");
			builder.AppendLine ();
			builder.AppendLine ("| skin | attachments | slots covered |");
			builder.AppendLine ("|---|---:|---|");
			foreach (var skin in skins) {
				var slotsOfSkin = new List<string> (coverage[skin.Name]);
				slotsOfSkin.Sort (StringComparer.Ordinal);
				builder.AppendLine ("| " + skin.Name + " | " + skin.Attachments.Count + " | " +
					(slotsOfSkin.Count == 0 ? "(none)" : string.Join (", ", slotsOfSkin.ToArray ())) + " |");
			}
			builder.AppendLine ();

			builder.AppendLine ("## Combinable groups");
			builder.AppendLine ();
			builder.AppendLine ("Skins inside one group share slots (variants of the same part, pick at most one). " +
				"Groups are mutually disjoint, so one pick per group composes into one character.");
			builder.AppendLine ();
			if (groups.Count == 0) {
				builder.AppendLine ("(no named skins)");
			} else {
				for (int g = 0; g < groups.Count; g++) {
					var groupSlots = new HashSet<string> ();
					var memberNames = new List<string> ();
					foreach (int member in groups[g]) {
						var skin = named[member];
						memberNames.Add (skin.Name + " (" + skin.Attachments.Count + ")");
						groupSlots.UnionWith (coverage[skin.Name]);
					}
					var sorted = new List<string> (groupSlots);
					sorted.Sort (StringComparer.Ordinal);
					builder.AppendLine ("- group " + (g + 1) + " — slots: " +
						(sorted.Count == 0 ? "(none)" : string.Join (", ", sorted.ToArray ())));
					builder.AppendLine ("  - " + string.Join (" / ", memberNames.ToArray ()));
				}
			}
			builder.AppendLine ();

			builder.AppendLine ("## Slots covered by no skin");
			builder.AppendLine ();
			if (neverCovered.Count == 0) {
				builder.AppendLine ("(none — every slot is covered by at least one skin)");
			} else {
				foreach (string slot in neverCovered)
					builder.AppendLine ("- " + slot);
				if (setupButUncovered.Count > 0)
					builder.AppendLine ().AppendLine (
						"Of these, " + setupButUncovered.Count + " have a setup attachment name, so a " +
						"composed skin leaves them EMPTY: " + string.Join (", ", setupButUncovered.ToArray ()) +
						". The rest have no setup attachment (empty by design).");
			}

			return builder.ToString ();
		}
	}
}
