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
/// live canvas rect, so the characters stay at 70% / 25% at any resolution. On short ultrawide
/// canvases the tallest actor is clamped under the title band instead of overrunning it, and the
/// Info panel docks top-right (out of the actor row) from 2.4:1 upward.
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

			[Tooltip ("Optional cap as a share of the canvas width (0 = none). Wide props such as the weapon use it.")]
			public float maxWidthFraction;

			[Tooltip ("Roll in degrees. 90 turns a long horizontal prop (the weapon) upright; height then follows the long side.")]
			public float rotationDegrees;
		}

		[SerializeField] Slot[] slots = new Slot[0];

		[Tooltip ("Feet baseline as a share of the canvas height (140 px of 1080 in the builder).")]
		[SerializeField] float feetFraction = 140f / 1080f;

		[Tooltip ("Same safety margin the builder used, so the idle bbox never reaches the button bar.")]
		[SerializeField] float fitMargin = 0.97f;

		[Tooltip ("Canvas pixels kept clear between the feet and the bottom edge (button bar is 104 px).")]
		[SerializeField] float bottomClearance = 128f;

		[Tooltip ("Height reserved at the top for the title band. The tallest actor is clamped below it on short canvases.")]
		[SerializeField] float topReserve = 88f;

		[Header ("Info panel")]
		[Tooltip ("Panel that is docked bottom-left at normal aspects and moved out of the actor row on ultrawide canvases.")]
		[SerializeField] RectTransform infoPanel;
		[SerializeField] float infoMargin = 24f;
		[SerializeField] float infoBottom = 118f;
		[Tooltip ("Distance from the top edge when docked (keeps it under the zoom readout / hint).")]
		[SerializeField] float infoDockTop = 164f;
		[Tooltip ("Canvas width / height from which the panel docks top-right, clear of the actor row " +
			"(16:9 = 1.78 docks; 4:3 = 1.33 keeps it bottom-left because the actor head would sit under it).")]
		[SerializeField] float infoDockAspect = 1.7f;

		RectTransform m_Rect;
		Vector2 m_AppliedSize;
		int m_AppliedBuiltMask = -1;

		public Slot[] Slots { get { return slots; } }
		public float FitMargin { get { return fitMargin; } }
		public float TopReserve { get { return topReserve; } }
		public float FeetFraction { get { return feetFraction; } }
		public float BottomClearance { get { return bottomClearance; } }
		public float InfoDockAspect { get { return infoDockAspect; } }
		public float InfoMargin { get { return infoMargin; } }
		public float InfoBottom { get { return infoBottom; } }
		public float InfoDockTop { get { return infoDockTop; } }
		public RectTransform InfoPanel { get { return infoPanel; } }

		/// <summary>Assigns the actor slots (called by the scene builder).</summary>
		public void Configure (params Slot[] newSlots)
		{
			slots = newSlots ?? new Slot[0];
			m_AppliedBuiltMask = -1;
		}

		/// <summary>Assigns the info panel so it can be moved out of the actor row on ultrawide canvases.</summary>
		public void SetInfoPanel (RectTransform panel)
		{
			infoPanel = panel;
			m_AppliedSize = Vector2.zero;
			ApplyCurrent ();
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

			PlaceInfoPanel (canvasSize);

			// Feet sit at a fixed share of the height, but never behind the button bar.
			float feet = Mathf.Max (canvasSize.y * feetFraction, bottomClearance);
			// The title band is reserved: on short ultrawide canvases the tallest actor is clamped
			// below it instead of letting the head run into the title.
			float stageTop = Mathf.Max (feet, canvasSize.y - topReserve);

			for (int i = 0; i < slots.Length; i++) {
				var slot = slots[i];
				if (slot == null || slot.actor == null || !slot.actor.IsBuilt)
					continue;

				bool rotated = Mathf.Abs (slot.rotationDegrees) > 0.01f;
				// The fitted (vertical) and across extents swap when the prop is rolled 90 degrees.
				float fitExtent = Mathf.Max (0.0001f, rotated ? slot.referenceMeshSize.x : slot.referenceMeshSize.y);
				float acrossExtent = Mathf.Max (0.0001f, rotated ? slot.referenceMeshSize.y : slot.referenceMeshSize.x);
				float target = canvasSize.y * slot.heightFraction;
				float height = Mathf.Min (target * fitMargin, stageTop - feet);
				float scale = Mathf.Clamp (height / fitExtent, 0.0001f, 1000f);
				// Wide props (the standalone weapon) are width-capped so they stay inside their column.
				if (slot.maxWidthFraction > 0f)
					scale = Mathf.Min (scale, canvasSize.x * slot.maxWidthFraction / acrossExtent);
				scale = Mathf.Max (scale, 0.0001f);
				height = Mathf.Max (fitExtent * scale, 1f);

				var rt = (RectTransform) slot.actor.transform;
				rt.localScale = new Vector3 (scale, scale, 1f);
				rt.sizeDelta = slot.referenceMeshSize * scale;
				rt.localEulerAngles = new Vector3 (0f, 0f, slot.rotationDegrees);

				// Canvas centre is the anchor point of both actors (anchors 0.5/0.5).
				Vector2 targetCentre = new Vector2 (
					canvasSize.x * slot.sideFraction,
					feet - canvasSize.y * 0.5f + height * 0.5f);
				Vector2 meshOffset = slot.referenceMeshCenter * scale;
				if (rotated)
					meshOffset = Quaternion.Euler (0f, 0f, slot.rotationDegrees) * (Vector3) meshOffset;
				rt.anchoredPosition = targetCentre - meshOffset;
			}
		}

		bool InfoDocked (Vector2 canvasSize)
		{
			return canvasSize.y > 0f && canvasSize.x / canvasSize.y >= infoDockAspect;
		}

		/// <summary>
		/// The info panel rectangle for a canvas size, in canvas-centred units. The builder and the
		/// play check use this to prove the panel is clear of every actor on ultrawide canvases.
		/// </summary>
		public Rect InfoPanelRectFor (Vector2 canvasSize)
		{
			if (infoPanel == null)
				return new Rect (0f, 0f, 0f, 0f);

			Vector2 size = infoPanel.sizeDelta;
			if (InfoDocked (canvasSize)) {
				float right = canvasSize.x * 0.5f - infoMargin;
				float top = canvasSize.y * 0.5f - infoDockTop;
				return new Rect (right - size.x, top - size.y, size.x, size.y);
			}

			float left = -canvasSize.x * 0.5f + infoMargin;
			float bottom = -canvasSize.y * 0.5f + infoBottom;
			return new Rect (left, bottom, size.x, size.y);
		}

		void PlaceInfoPanel (Vector2 canvasSize)
		{
			if (infoPanel == null)
				return;

			if (InfoDocked (canvasSize)) {
				infoPanel.anchorMin = new Vector2 (1f, 1f);
				infoPanel.anchorMax = new Vector2 (1f, 1f);
				infoPanel.pivot = new Vector2 (1f, 1f);
				infoPanel.anchoredPosition = new Vector2 (-infoMargin, -infoDockTop);
			} else {
				infoPanel.anchorMin = Vector2.zero;
				infoPanel.anchorMax = Vector2.zero;
				infoPanel.pivot = Vector2.zero;
				infoPanel.anchoredPosition = new Vector2 (infoMargin, infoBottom);
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
