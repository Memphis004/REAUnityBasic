using System;
using System.Collections.Generic;
using MyClone.SpineSystem;
using Spine;
using UnityEngine;
using UnityEngine.UI;

namespace MyClone.UI
{
	[Serializable]
	public class DiscipleMock
	{
		public string name = "Disciple";
		public string job = "Sword Cultivator";
		public int fight = 100000;
		public string state = "Secluded cultivation";
		public int injury;
		public bool followed;
		[Range (0, 5)] public int skillCount = 3;
		[Range (0, 2)] public int skinVariant;
	}

	/// <summary>
	/// Rebuilt Disciple role-info window (UIDiscipleRoleInfo2Win).
	///
	/// Behaviour taken from the observed Lua window (gui.windows.disciple.uidiscipleroleinfo2win):
	///   OnShow(guid) -> refreshName / refreshDiscipleInfo / refreshFightView / refreshOrderBtn,
	///   setChildInSideModel(#discipleModelRoot, guid, 0.85, anim stand, args.bgFisrt).
	///
	/// This clone has no net/guid data, so it takes a mock list instead and swaps the portrait's
	/// combined skin when the selection changes. Everything else (which nodes are shown) follows
	/// the same rules the Lua applies: injury row only when injured, sign/switch/zhenfa buttons
	/// hidden unless their game state is active, shuwu PD row hidden for normal disciples.
	/// </summary>
	public class DiscipleWindow : WindowBase
	{
		[Header ("Bound window children")]
		public Text nameText;
		public Text descText;
		public Text fightText;

		public GameObject injuryRoot;
		public Text injuryText;
		public GameObject ldLockRoot;
		public GameObject signRoot;
		public GameObject switchRoot;
		public GameObject pdRoot;
		public GameObject fightRoot;

		public Button orderButton;
		public Image orderIcon;
		public Image jobIcon;
		public Button rolespButton;

		[Header ("Model")]
		public RectTransform modelRoot;
		public SpineUiActor modelActor;
		public GameObject modelPlaceholder;
		[Tooltip ("Portrait height as a fraction of #discipleModelRoot's height.")]
		public float modelHeightFraction = 0.98f;

		[Header ("Skill lists")]
		public Transform tianmingList;
		public Transform daoyanList;

		[Header ("Mock data")]
		public DiscipleMock[] mockDisciples;

		static readonly Color FollowedColor = new Color (0.95f, 0.78f, 0.35f, 1f);
		static readonly Color NotFollowedColor = new Color (0.55f, 0.58f, 0.65f, 1f);
		static readonly Color[] SkillTints = {
			new Color (0.42f, 0.62f, 0.90f, 1f),
			new Color (0.55f, 0.80f, 0.48f, 1f),
			new Color (0.90f, 0.62f, 0.40f, 1f),
			new Color (0.72f, 0.55f, 0.90f, 1f),
			new Color (0.90f, 0.55f, 0.62f, 1f),
		};

		int m_Index;

		public int SelectedIndex { get { return m_Index; } }
		public DiscipleMock Selected {
			get {
				var list = Disciples;
				return list.Length == 0 ? null : list[Mathf.Clamp (m_Index, 0, list.Length - 1)];
			}
		}

		DiscipleMock[] Disciples {
			get {
				if (mockDisciples == null || mockDisciples.Length == 0)
					mockDisciples = CreateDefaults ();
				return mockDisciples;
			}
		}

		static DiscipleMock[] CreateDefaults ()
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

		protected override void OnInit ()
		{
			if (orderButton != null)
				orderButton.onClick.AddListener (ToggleFollow);
			if (rolespButton != null)
				rolespButton.onClick.AddListener (PlayVoicePlaceholder);
		}

		protected override void OnOpened (object args)
		{
			if (args is int index)
				SelectDisciple (index);
			else
				SelectDisciple (m_Index);
		}

		/// <summary>Selects a mock disciple and refreshes every bound widget.</summary>
		public void SelectDisciple (int index)
		{
			var list = Disciples;
			if (list.Length == 0)
				return;

			m_Index = Mathf.Clamp (index, 0, list.Length - 1);
			var d = list[m_Index];

			if (nameText != null)
				nameText.text = d.name;
			if (descText != null)
				descText.text = d.state;
			if (fightText != null)
				fightText.text = d.fight.ToString ("N0");

			if (injuryRoot != null)
				injuryRoot.SetActive (d.injury > 0);
			if (injuryText != null)
				injuryText.text = d.injury > 0 ? "<color=#FD7474>" + InjuryName (d.injury) + "</color>" : string.Empty;
			if (ldLockRoot != null)
				ldLockRoot.SetActive (false);
			if (signRoot != null)
				signRoot.SetActive (false);
			if (switchRoot != null)
				switchRoot.SetActive (false);

			// Lua: isShuWuDisciple -> #pdRoot else #fightroot
			if (pdRoot != null)
				pdRoot.SetActive (false);
			if (fightRoot != null)
				fightRoot.SetActive (true);

			if (orderIcon != null)
				orderIcon.color = d.followed ? FollowedColor : NotFollowedColor;
			if (jobIcon != null)
				jobIcon.color = JobColor (d.job);

			ApplySkills (d.skillCount);
			ApplyModel (d.skinVariant);
		}

		static string InjuryName (int injury)
		{
			switch (injury) {
			case 1: return "Injured";
			case 2: return "Dying";
			default: return "Injured";
			}
		}

		static Color JobColor (string job)
		{
			if (string.IsNullOrEmpty (job))
				return new Color (0.60f, 0.60f, 0.66f, 1f);
			switch (job[0]) {
			case 'A': return new Color (0.42f, 0.72f, 0.86f, 1f);
			case 'B': return new Color (0.78f, 0.55f, 0.42f, 1f);
			case 'S': return new Color (0.50f, 0.78f, 0.55f, 1f);
			default: return new Color (0.62f, 0.62f, 0.70f, 1f);
			}
		}

		void ToggleFollow ()
		{
			var d = Selected;
			if (d == null)
				return;
			d.followed = !d.followed;
			if (orderIcon != null)
				orderIcon.color = d.followed ? FollowedColor : NotFollowedColor;
			Debug.Log ("[DiscipleWindow] " + d.name + " followed = " + d.followed);
		}

		void PlayVoicePlaceholder ()
		{
			var d = Selected;
			Debug.Log ("[DiscipleWindow] role voice on click (mock): " + (d != null ? d.name : "?"));
		}

		// ------------------------------------------------------------------
		// Skill lists
		// ------------------------------------------------------------------

		void ApplySkills (int count)
		{
			ApplySkillRows (tianmingList, Mathf.Clamp (count, 0, 5));
			// The game shows #dyskillList only when the disciple has DaoYan skills.
			if (daoyanList != null)
				daoyanList.gameObject.SetActive (false);
		}

		static void ApplySkillRows (Transform list, int count)
		{
			if (list == null)
				return;

			for (int i = 0; i < list.childCount; i++) {
				var item = list.GetChild (i);
				bool on = i < count;
				item.gameObject.SetActive (on);
				if (!on)
					continue;

				var icon = item.Find ("skillicon");
				if (icon != null) {
					var img = icon.GetComponent<Image> ();
					if (img != null)
						img.color = SkillTints[i % SkillTints.Length];
				}
				var lockGo = item.Find ("lock");
				if (lockGo != null)
					lockGo.gameObject.SetActive (false);
				var reddot = item.Find ("reddot");
				if (reddot != null)
					reddot.gameObject.SetActive (false);
			}
		}

		// ------------------------------------------------------------------
		// Model (SpineUiActor in #discipleModelRoot)
		// ------------------------------------------------------------------

		void ApplyModel (int skinVariant)
		{
			// The prefab points at Assets/_LocalReference/Portrait, which is gitignored: in a clean
			// checkout the reference is missing, so fall back to the labelled placeholder instead
			// of letting SpineActorBase log a build error.
			if (modelActor == null || modelActor.Data == null) {
				if (modelPlaceholder != null)
					modelPlaceholder.SetActive (true);
				return;
			}

			if (!modelActor.IsBuilt)
				modelActor.Build ();

			if (!modelActor.IsBuilt) {
				if (modelPlaceholder != null)
					modelPlaceholder.SetActive (true);
				return;
			}

			if (modelPlaceholder != null)
				modelPlaceholder.SetActive (false);

			ApplyCombinedSkin (skinVariant);
			FitModel ();
		}

		void ApplyCombinedSkin (int variant)
		{
			var skeleton = modelActor.Skeleton;
			if (skeleton == null || skeleton.Data == null)
				return;

			var groups = new Dictionary<string, List<Skin>> ();
			var order = new List<string> ();
			foreach (var skin in skeleton.Data.Skins) {
				if (skin == null || skin.Name == "default")
					continue;
				string key = SkinGroupKey (skin.Name);
				List<Skin> bucket;
				if (!groups.TryGetValue (key, out bucket)) {
					bucket = new List<Skin> ();
					groups.Add (key, bucket);
					order.Add (key);
				}
				bucket.Add (skin);
			}

			if (order.Count == 0)
				return;

			var combined = new Skin ("clone_variant_" + variant);
			foreach (var key in order) {
				var bucket = groups[key];
				combined.AddSkin (bucket[variant % bucket.Count]);
			}

			// SetSkin(Skin) merges with the active skin, so clear it first (same as the demo).
			skeleton.SetSkin ((Skin)null);
			skeleton.SetSkin (combined);
			skeleton.SetSlotsToSetupPose ();
			if (modelActor.Graphic != null)
				modelActor.Graphic.SetVerticesDirty ();
		}

		static string SkinGroupKey (string name)
		{
			if (name.StartsWith ("face_acc_", StringComparison.Ordinal))
				return "face_acc";
			int underscore = name.IndexOf ('_');
			return underscore > 0 ? name.Substring (0, underscore) : name;
		}

		void FitModel ()
		{
			if (modelActor == null || modelRoot == null)
				return;

			modelActor.MatchRectTransformToBounds ();

			var graphic = modelActor.Graphic;
			if (graphic == null)
				return;

			graphic.rectTransform.anchoredPosition = Vector2.zero;
			float meshHeight = graphic.rectTransform.rect.height;
			if (meshHeight <= 1f)
				return;

			float target = modelRoot.rect.height * modelHeightFraction;
			float scale = target / meshHeight;
			modelActor.transform.localScale = new Vector3 (scale, scale, 1f);
			modelActor.transform.localPosition = Vector3.zero;
		}
	}
}
