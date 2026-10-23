using UnityEngine;
using UnityEngine.UI;

namespace MyClone.LocalReference
{
	/// <summary>
	/// Zoom / pan for the demo actor root: wheel zooms around the pointer, right-drag pans,
	/// Reset View goes back to 1x. Keyboard: +/= zoom in, - zoom out, 0 resets, arrow keys pan
	/// (hold Shift for 3x speed). A readout label shows the live zoom and pan if one is assigned.
	/// The UI (title, info, buttons, readout) is not a child of this transform, so it stays
	/// readable at any zoom.
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

		[Header ("Keyboard")]
		[Tooltip ("Multiplier for the + / - keys.")]
		[SerializeField] float keyZoomStep = 1.25f;
		[Tooltip ("Pan speed in canvas units per second while an arrow key is held.")]
		[SerializeField] float keyPanSpeed = 600f;
		[Tooltip ("Multiplier while Shift is held with an arrow key.")]
		[SerializeField] float keyPanBoost = 3f;

		[Header ("Readout")]
		[Tooltip ("Optional label that shows the live zoom / pan (\"Zoom 1.00x   pan 0, 0\").")]
		[SerializeField] Text zoomLabel;
		[SerializeField] string readoutFormat = "Zoom {0:0.00}x   pan {1:0}, {2:0}";

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
		public Text Label { get { return zoomLabel; } }

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
			HandleKeys ();
		}

		// ------------------------------------------------------------------
		// Public API (Reset View button, keyboard, play check)
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

		/// <summary>One keyboard/wheel notch in.</summary>
		public void ZoomIn ()
		{
			ZoomBy (keyZoomStep);
		}

		/// <summary>One keyboard/wheel notch out.</summary>
		public void ZoomOut ()
		{
			ZoomBy (1f / keyZoomStep);
		}

		/// <summary>Moves the actors by a delta in canvas units (same direction as a right-drag).</summary>
		public void PanBy (Vector2 delta)
		{
			m_Pan += delta;
			Apply ();
		}

		/// <summary>Assigns the readout label and refreshes it immediately.</summary>
		public void SetLabel (Text label)
		{
			zoomLabel = label;
			RefreshLabel ();
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

		void HandleKeys ()
		{
			if (Input.GetKeyDown (KeyCode.Equals) || Input.GetKeyDown (KeyCode.Plus) ||
				Input.GetKeyDown (KeyCode.KeypadPlus) || Input.GetKeyDown (KeyCode.RightBracket))
				ZoomIn ();

			if (Input.GetKeyDown (KeyCode.Minus) || Input.GetKeyDown (KeyCode.Underscore) ||
				Input.GetKeyDown (KeyCode.KeypadMinus) || Input.GetKeyDown (KeyCode.LeftBracket))
				ZoomOut ();

			if (Input.GetKeyDown (KeyCode.Alpha0) || Input.GetKeyDown (KeyCode.Keypad0))
				ResetView ();

			Vector2 direction = Vector2.zero;
			if (Input.GetKey (KeyCode.RightArrow)) direction.x += 1f;
			if (Input.GetKey (KeyCode.LeftArrow)) direction.x -= 1f;
			if (Input.GetKey (KeyCode.UpArrow)) direction.y += 1f;
			if (Input.GetKey (KeyCode.DownArrow)) direction.y -= 1f;

			if (direction == Vector2.zero)
				return;

			float speed = keyPanSpeed;
			if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift))
				speed *= keyPanBoost;

			PanBy (direction.normalized * speed * Time.unscaledDeltaTime);
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
			RefreshLabel ();
		}

		void RefreshLabel ()
		{
			if (zoomLabel == null || string.IsNullOrEmpty (readoutFormat))
				return;
			zoomLabel.text = string.Format (readoutFormat, m_Zoom, m_Pan.x, m_Pan.y);
		}
	}
}
