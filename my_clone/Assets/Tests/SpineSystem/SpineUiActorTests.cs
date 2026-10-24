using System.Reflection;
using NUnit.Framework;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MyClone.SpineSystem.Tests
{
	public class SpineUiActorTests
	{
		GameObject m_CanvasGo;
		TestSpineAssets m_Assets;
		SpineUiActor m_Actor;

		[SetUp]
		public void SetUp ()
		{
			m_CanvasGo = new GameObject ("TestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			m_CanvasGo.GetComponent<Canvas> ().renderMode = RenderMode.ScreenSpaceOverlay;

			m_Assets = TestSpineAssets.Create ();
			m_Actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, m_Assets.Data);
		}

		[TearDown]
		public void TearDown ()
		{
			if (m_Actor != null)
				Object.DestroyImmediate (m_Actor.gameObject);
			if (m_CanvasGo != null)
				Object.DestroyImmediate (m_CanvasGo);
			if (m_Assets != null)
				m_Assets.Destroy ();
		}

		[Test]
		public void Build_CreatesValidGraphic ()
		{
			Assert.IsTrue (m_Actor.IsBuilt);
			Assert.IsNotNull (m_Actor.Graphic);
			Assert.IsTrue (m_Actor.Graphic.IsValid);
			Assert.IsNotNull (m_Actor.Skeleton);
			Assert.IsNotNull (m_Actor.AnimationState);
			Assert.IsNotNull (m_Actor.Graphic.canvasRenderer);
		}

		[Test]
		public void HasAnimation_ReflectsSkeletonData ()
		{
			Assert.IsTrue (m_Actor.HasAnimation ("idle"));
			Assert.IsTrue (m_Actor.HasAnimation ("once"));
			Assert.IsFalse (m_Actor.HasAnimation ("does-not-exist"));
		}

		[Test]
		public void Play_Loop_DoesNotRaiseComplete ()
		{
			int completed = 0;
			m_Actor.AnimationComplete += _ => completed++;

			var entry = m_Actor.Play ("once", true);

			Assert.IsNotNull (entry);
			Assert.IsTrue (entry.Loop);

			m_Actor.Tick (1f);

			Assert.AreEqual (0, completed);
		}

		[Test]
		public void Play_NonLoop_RaisesCompleteOnce ()
		{
			int completed = 0;
			m_Actor.AnimationComplete += _ => completed++;

			var entry = m_Actor.Play ("once", false);
			Assert.IsNotNull (entry);
			Assert.IsFalse (entry.Loop);

			m_Actor.Tick (0.5f);
			m_Actor.Tick (0.5f);

			Assert.AreEqual (1, completed);
		}

		[Test]
		public void SetProgress_SeeksInsideCurrentAnimation ()
		{
			m_Actor.Play ("idle", true, 1f);
			m_Actor.SetProgress (0.5f);

			var entry = m_Actor.AnimationState.GetCurrent (0);
			Assert.IsNotNull (entry);
			Assert.AreEqual (0.25f, entry.TrackTime, 0.01f);
		}

		[Test]
		public void SetSpeed_ChangesAnimationStateTimeScale ()
		{
			m_Actor.Play ("idle", true, 0.5f);
			Assert.AreEqual (0.5f, m_Actor.AnimationState.TimeScale, 0.0001f);

			m_Actor.SetSpeed (2f);
			Assert.AreEqual (2f, m_Actor.AnimationState.TimeScale, 0.0001f);
		}

		[Test]
		public void FlipXAndY_MirrorSkeletonTransform ()
		{
			m_Actor.SetFlipX (true);
			Assert.AreEqual (-1f, m_Actor.Graphic.transform.localScale.x, 0.0001f);

			m_Actor.SetFlipY (true);
			Assert.AreEqual (-1f, m_Actor.Graphic.transform.localScale.y, 0.0001f);

			m_Actor.SetFlipX (false);
			m_Actor.SetFlipY (false);
			Assert.AreEqual (Vector3.one, m_Actor.Graphic.transform.localScale);
		}

		[Test]
		public void SetSlotAttachment_WorksAndRejectsInvalidNames ()
		{
			Assert.IsTrue (m_Actor.SetSlotAttachment ("slot1", "img1"));
			Assert.AreEqual ("img1", m_Actor.GetSlotAttachment ("slot1"));

			Assert.IsFalse (m_Actor.SetSlotAttachment ("missing-slot", "img1"));
			Assert.IsFalse (m_Actor.SetSlotAttachment ("slot1", "missing-attachment"));

			Assert.IsTrue (m_Actor.ClearSlot ("slot1"));
			Assert.IsNull (m_Actor.GetSlotAttachment ("slot1"));
		}

	[Test]
	public void SetSkin_ChangesAttachmentLookup ()
	{
		Assert.IsTrue (m_Actor.SetSkin ("alt"));
		Assert.IsTrue (m_Actor.SetSlotAttachment ("slot1", "alt1"));
		Assert.AreEqual ("alt1", m_Actor.GetSlotAttachment ("slot1"));
	}

	[Test]
	public void Build_EmptyDefaultSkin_AutoPicksFirstSkinWithAttachments ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.EmptyDefaultSkeletonJson);
		try {
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] No skin configured. Automatically combining the first skin of each part group: look1.");

			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data);

			Assert.AreEqual ("combined:look1", actor.Skeleton.Skin.Name);
			Assert.AreEqual ("img1", actor.GetSlotAttachment ("slot1"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void Build_NoUsableSkin_WarnsNothingWillBeVisible ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.OnlyEmptySkinsJson);
		try {
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] Skin 'default' has no attachments: nothing will be visible. Pick another Initial Skin.");

			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data);

			Assert.IsTrue (actor.IsBuilt);
			Assert.IsNull (actor.GetSlotAttachment ("slot1"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void Build_InitialSkinWithoutAttachments_WarnsNothingWillBeVisible ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.EmptyDefaultSkeletonJson);
		try {
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] Skin 'empty' has no attachments: nothing will be visible. Pick another Initial Skin.");

			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "empty");

			Assert.IsTrue (actor.IsBuilt);
			Assert.AreEqual ("empty", actor.Skeleton.Skin.Name);
			Assert.IsNull (actor.GetSlotAttachment ("slot1"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void SetSkins_CombinesParts_AllSlotsHaveAttachments ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.PartSkinsSkeletonJson);
		try {
			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "body_a");

			var combined = actor.SetSkins ("body_a", "head_a", "face_a");

			Assert.IsNotNull (combined);
			Assert.AreEqual ("combined:body_a+head_a+face_a", combined.Name);
			Assert.AreEqual ("combined:body_a+head_a+face_a", actor.Skeleton.Skin.Name);
			Assert.AreEqual ("body_img", actor.GetSlotAttachment ("body_slot"));
			Assert.AreEqual ("head_img", actor.GetSlotAttachment ("head_slot"));
			Assert.AreEqual ("face_img", actor.GetSlotAttachment ("face_slot"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void SetSkins_MissingName_WarnsAndSkips ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.PartSkinsSkeletonJson);
		try {
			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "body_a");
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] skin 'nope' not found, skipping.");
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] Combined skin leaves 1 empty slot(s): face_slot (no candidates in the combined skin)");

			var combined = actor.SetSkins ("body_a", "nope", "head_a");

			Assert.IsNotNull (combined);
			// Only the existing parts made it into the combination.
			Assert.AreEqual ("combined:body_a+head_a", combined.Name);
			Assert.AreEqual ("body_img", actor.GetSlotAttachment ("body_slot"));
			Assert.AreEqual ("head_img", actor.GetSlotAttachment ("head_slot"));
			Assert.IsNull (actor.GetSlotAttachment ("face_slot"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void SetSkinParts_CombinesParts_AllSlotsHaveAttachments ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.PartSkinsSkeletonJson);
		try {
			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "body_a");

			actor.SetSkinParts (
				new SpineActorBase.SkinPart { part = "body", skin = "body_a" },
				new SpineActorBase.SkinPart { part = "head", skin = "head_a" },
				new SpineActorBase.SkinPart { part = "face", skin = "face_a" });

			Assert.AreEqual ("combined:body_a+head_a+face_a", actor.Skeleton.Skin.Name);
			Assert.AreEqual ("body_img", actor.GetSlotAttachment ("body_slot"));
			Assert.AreEqual ("head_img", actor.GetSlotAttachment ("head_slot"));
			Assert.AreEqual ("face_img", actor.GetSlotAttachment ("face_slot"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void SetSkinPart_HeadOnly_OthersUnchanged ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.PartSkinsSkeletonJson);
		try {
			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "body_a");
			actor.SetSkinParts (
				new SpineActorBase.SkinPart { part = "body", skin = "body_a" },
				new SpineActorBase.SkinPart { part = "head", skin = "head_a" },
				new SpineActorBase.SkinPart { part = "face", skin = "face_a" });

			actor.SetSkinPart ("head", "head_b");

			Assert.AreEqual ("combined:body_a+head_b+face_a", actor.Skeleton.Skin.Name);
			Assert.AreEqual ("body_img", actor.GetSlotAttachment ("body_slot"));
			Assert.AreEqual ("head_img2", actor.GetSlotAttachment ("head_slot"));
			Assert.AreEqual ("face_img", actor.GetSlotAttachment ("face_slot"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void SetSkinParts_SlotCollision_LaterPartWinsAndWarns ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.PartSkinsSkeletonJson);
		try {
			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "body_a");
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] combined skin: slot 'body_slot' attachment 'body_img' already provided by 'body_a'; later skin 'body_dup' wins");
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] Combined skin leaves 2 empty slot(s): head_slot (no candidates in the combined skin); face_slot (no candidates in the combined skin)");

			var combined = actor.SetSkins ("body_a", "body_dup");

			Assert.AreEqual ("combined:body_a+body_dup", combined.Name);

			// The later skin's attachment instance is the one kept in the combined skin.
			int bodySlotIndex = actor.Skeleton.FindSlot ("body_slot").Data.Index;
			var fromDup = actor.Skeleton.Data.FindSkin ("body_dup").GetAttachment (bodySlotIndex, "body_img");
			var fromA = actor.Skeleton.Data.FindSkin ("body_a").GetAttachment (bodySlotIndex, "body_img");
			Assert.AreSame (fromDup, combined.GetAttachment (bodySlotIndex, "body_img"));
			Assert.AreNotSame (fromA, combined.GetAttachment (bodySlotIndex, "body_img"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void Compose_EmptySlot_WarnsSlotAndCandidates ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.PartSkinsSkeletonJson);
		try {
			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "body_alt");
			LogAssert.Expect (LogType.Warning,
				"[SpineActorBase] Combined skin leaves 1 empty slot(s): body_slot (candidates: body_img2 - none matches setup attachment 'body_img')");

			actor.SetSkinParts (
				new SpineActorBase.SkinPart { part = "body", skin = "body_alt" },
				new SpineActorBase.SkinPart { part = "head", skin = "head_a" },
				new SpineActorBase.SkinPart { part = "face", skin = "face_a" });

			// Not auto-corrected: body_slot stays empty and the mismatch is only reported.
			Assert.IsNull (actor.GetSlotAttachment ("body_slot"));
			Assert.AreEqual ("head_img", actor.GetSlotAttachment ("head_slot"));
			Assert.AreEqual ("face_img", actor.GetSlotAttachment ("face_slot"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

	[Test]
	public void AutoFillEmptySlots_FillsSingleCandidate ()
	{
		var assets = TestSpineAssets.Create (skeletonJson: TestSpineAssets.PartSkinsSkeletonJson);
		try {
			var actor = SpineActorFactory.CreateUi (m_CanvasGo.transform, assets.Data, skin: "body_alt");
			SetPrivateField (actor, "autoFillEmptySlots", true);

			actor.SetSkinParts (
				new SpineActorBase.SkinPart { part = "body", skin = "body_alt" },
				new SpineActorBase.SkinPart { part = "head", skin = "head_a" },
				new SpineActorBase.SkinPart { part = "face", skin = "face_a" });

			Assert.AreEqual ("body_img2", actor.GetSlotAttachment ("body_slot"));
			Assert.AreEqual ("head_img", actor.GetSlotAttachment ("head_slot"));
			Assert.AreEqual ("face_img", actor.GetSlotAttachment ("face_slot"));

			Object.DestroyImmediate (actor.gameObject);
		} finally {
			assets.Destroy ();
		}
	}

		[Test]
		public void SetGray_SwapsMaterialShaderAndRestores ()
		{
			var normal = m_Actor.Graphic.material;
			Assert.IsNotNull (normal);
			Assert.AreEqual ("Spine/SkeletonGraphic", normal.shader.name);

			m_Actor.SetGray (true);
			Assert.IsTrue (m_Actor.IsGray);
			Assert.AreEqual ("Spine/SkeletonGraphic Grayscale", m_Actor.Graphic.material.shader.name);

			m_Actor.SetGray (false);
			Assert.IsFalse (m_Actor.IsGray);
			Assert.AreEqual ("Spine/SkeletonGraphic", m_Actor.Graphic.material.shader.name);
		}

		[Test]
		public void Mount_IsChildAndSurvivesUnMountUntilDestroyed ()
		{
			var mount = m_Actor.Mount (m_Assets.Data);

			Assert.IsNotNull (mount);
			Assert.AreSame (m_Actor.transform, mount.transform.parent);
			Assert.IsTrue (mount.IsBuilt);
			Assert.IsTrue (mount.gameObject.activeSelf);

			m_Actor.UnMount ();
			Assert.IsTrue (mount != null);
			Assert.IsFalse (mount.gameObject.activeSelf);

			m_Actor.DestroyMount ();
			Assert.IsTrue (m_Actor.MountActor == null);
			Assert.IsTrue (mount == null);
		}

		[Test]
		public void MountToBone_FollowsExistingBone ()
		{
			m_Actor.Mount (m_Assets.Data);
			Assert.IsTrue (m_Actor.MountToBone ("bone1"));
		}

		[Test]
		public void SetSeparatorSlots_CreatesParts ()
		{
			m_Actor.SetSeparatorSlots ("slot2");

			Assert.IsTrue (m_Actor.Graphic.enableSeparatorSlots);
			Assert.AreEqual (1, m_Actor.Graphic.separatorSlots.Count);
			Assert.GreaterOrEqual (m_Actor.Graphic.SeparatorParts.Count, 2);
			CollectionAssert.Contains (m_Actor.Graphic.GetSeparatorSlotNames (), "slot2");
		}

	[Test]
	public void Release_DestroysRendererAndAllowsRebuild ()
	{
		var first = m_Actor.Graphic;

		m_Actor.Release ();

		Assert.IsFalse (m_Actor.IsBuilt);
		Assert.IsTrue (first == null);
		Assert.IsTrue (m_Actor.Graphic == null);

		m_Actor.Build ();

		Assert.IsTrue (m_Actor.IsBuilt);
		Assert.IsNotNull (m_Actor.Graphic);
		Assert.IsTrue (m_Actor.Graphic.IsValid);
	}

	[Test]
	public void Start_BuildsAutomaticallyWhenUnbuilt ()
	{
		var go = new GameObject ("ManualActor", typeof (RectTransform));
		go.transform.SetParent (m_CanvasGo.transform, false);
		var actor = go.AddComponent<SpineUiActor> ();
		actor.Configure (m_Assets.Data);
		Assert.IsFalse (actor.IsBuilt);

		InvokeStart (actor);

		Assert.IsTrue (actor.IsBuilt);
		Assert.AreEqual (1, CountGraphicsChildren (actor.transform));

		Object.DestroyImmediate (go);
	}

	[Test]
	public void Start_DoesNotRebuildWhenAlreadyBuilt ()
	{
		Assert.IsTrue (m_Actor.IsBuilt);

		InvokeStart (m_Actor);

		Assert.AreEqual (1, CountGraphicsChildren (m_Actor.transform));
	}

	[Test]
	public void Rebuild_DoesNotStackRendererChildren ()
	{
		m_Actor.Build ();
		m_Actor.Build ();

		Assert.AreEqual (1, CountGraphicsChildren (m_Actor.transform));
	}

	[Test]
	public void Build_AfterReloadWithDifferentSkeleton_RemovesOrphanGraphic ()
	{
		// Simulate a duplicate or scene reload: the renderer GameObject survives in the
		// hierarchy but m_Graphic is not serialized, so the reference is gone.
		var orphan = m_Actor.Graphic.gameObject;
		SetPrivateField (m_Actor, "m_Graphic", null);

		var other = TestSpineAssets.Create ();
		m_Actor.Configure (other.Data);
		m_Actor.Build ();

		Assert.IsTrue (m_Actor.IsBuilt);
		Assert.IsTrue (orphan == null);
		Assert.AreEqual (1, CountGraphicsChildren (m_Actor.transform));

		other.Destroy ();
	}

	static int CountGraphicsChildren (Transform parent)
	{
		int count = 0;
		for (int i = 0; i < parent.childCount; i++) {
			if (parent.GetChild (i).GetComponent<SkeletonGraphic> () != null)
				count++;
		}
		return count;
	}

	static void SetPrivateField (object instance, string name, object value)
	{
		instance.GetType ()
			.GetField (name, BindingFlags.NonPublic | BindingFlags.Instance)
			.SetValue (instance, value);
	}

	/// <summary>
	/// Invokes SpineActorBase.Start via reflection: Unity blocks message dispatch of
	/// behaviour callbacks (SendMessage("Start")) in edit mode with a
	/// ShouldRunBehaviour assertion. Reflection matches what Unity does in play mode.
	/// </summary>
	static void InvokeStart (SpineActorBase actor)
	{
		typeof (SpineActorBase)
			.GetMethod ("Start", BindingFlags.NonPublic | BindingFlags.Instance)
			.Invoke (actor, null);
	}
}
}
