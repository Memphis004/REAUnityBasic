using NUnit.Framework;
using UnityEngine;

namespace MyClone.SpineSystem.Tests
{
	public class SpineWorldActorTests
	{
		GameObject m_Root;
		TestSpineAssets m_Assets;
		SpineWorldActor m_Actor;

		[SetUp]
		public void SetUp ()
		{
			m_Root = new GameObject ("WorldRoot");
			m_Assets = TestSpineAssets.Create ("Spine/Skeleton");
			m_Actor = SpineActorFactory.CreateWorld (m_Root.transform, m_Assets.Data);
		}

		[TearDown]
		public void TearDown ()
		{
			if (m_Actor != null)
				Object.DestroyImmediate (m_Actor.gameObject);
			if (m_Root != null)
				Object.DestroyImmediate (m_Root);
			if (m_Assets != null)
				m_Assets.Destroy ();
		}

		[Test]
		public void Build_CreatesValidSkeletonAnimation ()
		{
			Assert.IsTrue (m_Actor.IsBuilt);
			Assert.IsNotNull (m_Actor.Animation);
			Assert.IsTrue (m_Actor.Animation.valid);
			Assert.IsNotNull (m_Actor.Animation.GetComponent<MeshRenderer> ());
		}

		[Test]
		public void PlayAndFlip_WorkOnWorldActor ()
		{
			var entry = m_Actor.Play ("idle", true, 0.5f);

			Assert.IsNotNull (entry);
			Assert.AreEqual (0.5f, m_Actor.AnimationState.TimeScale, 0.0001f);
			Assert.IsTrue (m_Actor.HasAnimation ("once"));

			m_Actor.SetFlipX (true);
			Assert.AreEqual (-1f, m_Actor.Animation.transform.localScale.x, 0.0001f);
		}

		[Test]
		public void SetGray_UsesCustomMaterialOverride ()
		{
			Assert.AreEqual (0, m_Actor.Animation.CustomMaterialOverride.Count);

			m_Actor.SetGray (true);
			Assert.IsTrue (m_Actor.IsGray);
			Assert.Greater (m_Actor.Animation.CustomMaterialOverride.Count, 0);

			m_Actor.SetGray (false);
			Assert.AreEqual (0, m_Actor.Animation.CustomMaterialOverride.Count);
		}
	}
}
