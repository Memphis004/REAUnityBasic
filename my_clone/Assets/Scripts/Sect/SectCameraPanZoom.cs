using UnityEngine;

namespace MyClone.Sect
{
	/// <summary>
	/// Minimal drag-to-pan / wheel-to-zoom camera for the sect map placeholder scene.
	/// Rewritten for this project; no game code is used. Bounds and zoom range come from
	/// the values the original game's map controller applies (map border + orthographic size).
	/// </summary>
	[RequireComponent (typeof (Camera))]
	public class SectCameraPanZoom : MonoBehaviour
	{
		public Vector2 borderMin = new Vector2 (-30f, -25.5f);
		public Vector2 borderMax = new Vector2 (30f, 18.5f);
		public float minOrthoSize = 4f;
		public float maxOrthoSize = 6f;
		public float zoomStep = 0.35f;

		Camera m_Camera;
		bool m_Dragging;
		Vector3 m_LastMouse;

		void Awake ()
		{
			m_Camera = GetComponent<Camera> ();
			m_Camera.orthographic = true;
		}

		void Update ()
		{
			if (m_Camera == null)
				return;

			if (Input.GetMouseButtonDown (0) || Input.GetMouseButtonDown (1)) {
				m_Dragging = true;
				m_LastMouse = Input.mousePosition;
			}
			if (Input.GetMouseButtonUp (0) || Input.GetMouseButtonUp (1))
				m_Dragging = false;

			if (m_Dragging && (Input.GetMouseButton (0) || Input.GetMouseButton (1))) {
				Vector3 delta = Input.mousePosition - m_LastMouse;
				m_LastMouse = Input.mousePosition;
				float unitsPerPixel = 2f * m_Camera.orthographicSize / Mathf.Max (1, Screen.height);
				transform.position -= new Vector3 (delta.x, delta.y, 0f) * unitsPerPixel;
			}

			float scroll = Input.mouseScrollDelta.y;
			if (Mathf.Abs (scroll) > 0.01f)
				SetOrthoSize (m_Camera.orthographicSize - scroll * zoomStep);

			ClampPosition ();
		}

		public void SetOrthoSize (float size)
		{
			if (m_Camera == null)
				m_Camera = GetComponent<Camera> ();
			if (m_Camera == null)
				return;
			m_Camera.orthographicSize = Mathf.Clamp (size, minOrthoSize, maxOrthoSize);
		}

		void ClampPosition ()
		{
			Vector3 p = transform.position;
			float halfH = m_Camera.orthographicSize;
			float halfW = halfH * m_Camera.aspect;
			p.x = ClampAxis (p.x, borderMin.x, borderMax.x, halfW);
			p.y = ClampAxis (p.y, borderMin.y, borderMax.y, halfH);
			transform.position = p;
		}

		/// <summary>
		/// Keep the view inside the border when it fits; otherwise keep the border inside the view
		/// (so the map never leaves the screen).
		/// </summary>
		static float ClampAxis (float value, float min, float max, float halfExtent)
		{
			if (max - min <= halfExtent * 2f)
				return (min + max) * 0.5f;
			return Mathf.Clamp (value, min + halfExtent, max - halfExtent);
		}
	}
}
