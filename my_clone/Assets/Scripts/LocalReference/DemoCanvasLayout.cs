using System;
using MyClone.SpineSystem;
using UnityEngine;

namespace MyClone.LocalReference
{
	/// <summary>
	/// Keeps the demo actors at a fixed share of the Canvas height for every window size / aspect.
	///
	/// The builder bakes the fit at the CanvasScaler reference resolution (1920x1080). A Canvas in
	/// Screen Space - Overlay is measured in reference pixels, so with another aspect one screen
	/// height is worth a different number of those pixels and 756 px stops being 70% of the canvas.
	/// This component re-applies the same fit (measured mesh box + target share + margin) against the
	/// live canvas rect, so the characters stay at 70% / 25% at any resolution.
	///
	/// Demo-only: nothing here touches SpineUiActor or game code.
	/// </summary>
	public class DemoCanvasLayout : MonoBehaviour
	{
		[Serializable]
		public class Slot
		{
			public SpineUiActor actor;

			[Tooltip ("Height as a share of the canvas height (0.70 / 0.25 as built by the scene builder).")]
			public float heightFraction = 0.70f;

			[Tooltip ("Horizontal centre as a share of the canvas width: -0.25 = left quarter, +0.25 = right quarter.")]
			public float sideFraction = -0.25f;

			[Tooltip ("Mesh box measured by the builder, in mesh units (independent of the actor scale).")]
			public Vector2 referenceMeshSize = new Vector2 (1f, 1f);

			[Tooltip ("Mesh box centre measured by the builder, in mesh units.")]
			public Vector2 referenceMeshCenter;
		}

		[SerializeField] Slot[] slots = new Slot[0];

		[Tooltip ("Feet baseline as a share of the canvas height (140 px of 1080 in the builder).")]
		[SerializeField] float feetFraction = 140f / 1080f;

		[Tooltip ("Same safety margin the builder used, so the idle bbox never reaches the button bar.")]
		[SerializeField] float fitMargin = 0.97f;

		[Tooltip ("Canvas pixels kept clear between the feet and the bottom edge (button bar is 104 px).")]
		[SerializeField] float bottomClearance = 128f;

		RectTransform m_Rect;
		Vector2 m_AppliedSize;
		int m_AppliedBuiltMask = -1;

		public Slot[] Slots { get { return slots; } }
		public float FitMargin { get { return fitMargin; } }

		/// <summary>Assigns the actor slots (called by the scene builder).</summary>
		public void Configure (params Slot[] newSlots)
		{
			slots = newSlots ?? new Slot[0];
			m_AppliedBuiltMask = -1;
		}

		void OnEnable ()
		{
			m_Rect = transform as RectTransform;
			ApplyCurrent ();
		}

		/// <summary>
		/// The Canvas rect changes on a window resize (and when the screenshot switches the canvas to
		/// Screen Space - Camera), so the fit is re-applied in the same frame, before rendering.
		/// </summary>
		void OnRectTransformDimensionsChange ()
		{
			if (m_Rect == null)
				m_Rect = transform as RectTransform;
			if (m_Rect == null)
				return;
			ApplyForSize (m_Rect.rect.size, BuiltMask ());
		}

		void Update ()
		{
			if (m_Rect == null)
				m_Rect = transform as RectTransform;
			if (m_Rect == null)
				return;

			Vector2 size = m_Rect.rect.size;
			int builtMask = BuiltMask ();
			if ((size - m_AppliedSize).sqrMagnitude > 0.01f || builtMask != m_AppliedBuiltMask)
				ApplyForSize (size, builtMask);
		}

		/// <summary>Lays the actors out for an explicit canvas size (used by tests/tools too).</summary>
		public void ApplyForSize (Vector2 canvasSize)
		{
			ApplyForSize (canvasSize, BuiltMask ());
		}

		void ApplyCurrent ()
		{
			if (m_Rect == null)
				return;
			ApplyForSize (m_Rect.rect.size, BuiltMask ());
		}

		void ApplyForSize (Vector2 canvasSize, int builtMask)
		{
			if (canvasSize.x < 1f || canvasSize.y < 1f)
				return;

			m_AppliedSize = canvasSize;
			m_AppliedBuiltMask = builtMask;

			// Feet sit at a fixed share of the height, but never behind the button bar.
			float feet = Mathf.Max (canvasSize.y * feetFraction, bottomClearance);

			for (int i = 0; i < slots.Length; i++) {
				var slot = slots[i];
				if (slot == null || slot.actor == null || !slot.actor.IsBuilt)
					continue;

				float meshHeight = Mathf.Max (0.0001f, slot.referenceMeshSize.y);
				float target = canvasSize.y * slot.heightFraction;
				float scale = Mathf.Clamp (target * fitMargin / meshHeight, 0.0001f, 1000f);

				var rt = (RectTransform) slot.actor.transform;
				rt.localScale = new Vector3 (scale, scale, 1f);
				rt.sizeDelta = slot.referenceMeshSize * scale;

				// Canvas centre is the anchor point of both actors (anchors 0.5/0.5).
				Vector2 targetCentre = new Vector2 (
					canvasSize.x * slot.sideFraction,
					feet - canvasSize.y * 0.5f + target * 0.5f);
				rt.anchoredPosition = targetCentre - slot.referenceMeshCenter * scale;
			}
		}

		int BuiltMask ()
		{
			int mask = 0;
			for (int i = 0; i < slots.Length; i++)
				if (slots[i] != null && slots[i].actor != null && slots[i].actor.IsBuilt)
					mask |= 1 << i;
			return mask;
		}
	}
}
