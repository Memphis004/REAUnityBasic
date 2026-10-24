using System.Reflection;
using NUnit.Framework;
using Spine.Unity;
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

	[Test]
	public void Start_BuildsAutomaticallyWhenUnbuilt ()
	{
		var go = new GameObject ("ManualWorldActor");
		go.transform.SetParent (m_Root.transform, false);
		var actor = go.AddComponent<SpineWorldActor> ();
		actor.Configure (m_Assets.Data);
		Assert.IsFalse (actor.IsBuilt);

		InvokeStart (actor);

		Assert.IsTrue (actor.IsBuilt);
		Assert.AreEqual (1, CountSkeletonRendererChildren (actor.transform));

		Object.DestroyImmediate (go);
	}

	[Test]
	public void Rebuild_DoesNotStackSkeletonAnimationChildren ()
	{
		m_Actor.Build ();
		m_Actor.Build ();

		Assert.AreEqual (1, CountSkeletonRendererChildren (m_Actor.transform));
	}

	[Test]
	public void Build_AfterReload_RemovesOrphanAnimation ()
	{
		// Simulate a duplicate or scene reload: the child GameObject survives but
		// m_Animation is not serialized, so the reference is gone.
		var orphan = m_Actor.Animation.gameObject;
		SetPrivateField (m_Actor, "m_Animation", null);

		m_Actor.Build ();

		Assert.IsTrue (m_Actor.IsBuilt);
		Assert.IsTrue (orphan == null);
		Assert.AreEqual (1, CountSkeletonRendererChildren (m_Actor.transform));
	}

	static int CountSkeletonRendererChildren (Transform parent)
	{
		int count = 0;
		for (int i = 0; i < parent.childCount; i++) {
			if (parent.GetChild (i).GetComponent<SkeletonRenderer> () != null)
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
