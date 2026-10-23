using NUnit.Framework;
using Spine;
using UnityEngine;
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
	}
}
