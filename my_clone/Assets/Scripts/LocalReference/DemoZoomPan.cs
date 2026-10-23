using UnityEngine;

namespace MyClone.LocalReference
{
	/// <summary>
	/// Zoom / pan for the demo actor root: wheel zooms around the pointer, right-drag pans,
	/// Reset View goes back to 1x. The UI (title, info, buttons) is not a child of this transform,
	/// so it stays readable at any zoom.
	///
	/// Demo-only: it never touches SpineUiActor or game code.
	/// </summary>
	public class DemoZoomPan : MonoBehaviour
	{
		[SerializeField] float minZoom = 0.6f;
		[SerializeField] float maxZoom = 8f;
		[Tooltip ("Multiplier per wheel notch.")]
		[SerializeField] float wheelStep = 1.2f;
		[SerializeField] float dragSensitivity = 1f;

		RectTransform m_Rect;
		RectTransform m_CanvasRect;

		float m_Zoom = 1f;
		Vector2 m_Pan;
		bool m_Dragging;
		Vector2 m_LastPointer;

		public float Zoom { get { return m_Zoom; } }
		public Vector2 Pan { get { return m_Pan; } }
		public float MinZoom { get { return minZoom; } }
		public float MaxZoom { get { return maxZoom; } }

		void OnEnable ()
		{
			m_Rect = transform as RectTransform;
			Apply ();
		}

		void Update ()
		{
			if (m_Rect == null)
				return;

			HandleWheel ();
			HandleDrag ();
		}

		// ------------------------------------------------------------------
		// Public API (Reset View button, play check)
		// ------------------------------------------------------------------

		public void ResetView ()
		{
			m_Zoom = 1f;
			m_Pan = Vector2.zero;
			m_Dragging = false;
			Apply ();
		}

		/// <summary>Multiplies the current zoom, keeping the canvas centre in place.</summary>
		public void ZoomBy (float factor)
		{
			ZoomAt (m_Zoom * factor, Vector2.zero);
		}

		/// <summary>Sets an absolute zoom, keeping the canvas centre in place.</summary>
		public void SetZoom (float zoom)
		{
			ZoomAt (zoom, Vector2.zero);
		}

		// ------------------------------------------------------------------
		// Input
		// ------------------------------------------------------------------

		void HandleWheel ()
		{
			float wheel = Input.mouseScrollDelta.y;
			if (Mathf.Abs (wheel) < 0.01f)
				return;

			Vector2 pointer;
			if (!TryGetPointer (out pointer))
				return;

			ZoomAt (m_Zoom * Mathf.Pow (wheelStep, wheel), pointer);
		}

		void HandleDrag ()
		{
			if (Input.GetMouseButtonDown (1)) {
				m_Dragging = TryGetPointer (out m_LastPointer);
				return;
			}

			if (!m_Dragging)
				return;

			if (Input.GetMouseButtonUp (1)) {
				m_Dragging = false;
				return;
			}

			Vector2 pointer;
			if (!TryGetPointer (out pointer))
				return;

			m_Pan += (pointer - m_LastPointer) * dragSensitivity;
			m_LastPointer = pointer;
			Apply ();
		}

		/// <summary>Pointer position in canvas units, measured from the canvas centre.</summary>
		bool TryGetPointer (out Vector2 local)
		{
			local = Vector2.zero;
			if (m_CanvasRect == null) {
				var canvas = GetComponentInParent<Canvas> ();
				if (canvas == null)
					return false;
				m_CanvasRect = canvas.transform as RectTransform;
				if (m_CanvasRect == null)
					return false;
			}

			return RectTransformUtility.ScreenPointToLocalPointInRectangle (
				m_CanvasRect, Input.mousePosition, null, out local);
		}

		// ------------------------------------------------------------------
		// Apply
		// ------------------------------------------------------------------

		void ZoomAt (float zoom, Vector2 pivotFromCentre)
		{
			float clamped = Mathf.Clamp (zoom, minZoom, maxZoom);
			float ratio = clamped / Mathf.Max (0.0001f, m_Zoom);
			// Keep the point under the pointer fixed: p' = q - (q - p) * ratio.
			m_Pan = pivotFromCentre - (pivotFromCentre - m_Pan) * ratio;
			m_Zoom = clamped;
			Apply ();
		}

		void Apply ()
		{
			if (m_Rect == null)
				m_Rect = transform as RectTransform;
			if (m_Rect == null)
				return;

			m_Rect.localScale = new Vector3 (m_Zoom, m_Zoom, 1f);
			m_Rect.anchoredPosition = m_Pan;
		}
	}
}
