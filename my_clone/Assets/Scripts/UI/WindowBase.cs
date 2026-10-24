using UnityEngine;

namespace MyClone.UI
{
	/// <summary>
	/// Minimal window lifecycle, modelled on the observable behaviour of the game's
	/// CSGUIWindowBase/CSGUIWindowLua pair (Show / Close / SetActive + a CanvasGroup that
	/// blocks raycasts while the window is hidden).
	///
	/// Difference from the original: no DOTween transitions, no RenderTexture caching and no
	/// Lua binding. The three observable states (closed, open, initialised) are the same.
	/// </summary>
	[DisallowMultipleComponent]
	public abstract class WindowBase : MonoBehaviour
	{
		CanvasGroup m_CanvasGroup;

		public bool IsInitialized { get; private set; }
		public bool IsOpen { get; private set; }
		public virtual string WindowId { get { return GetType ().Name; } }

		protected CanvasGroup CanvasGroup {
			get {
				if (m_CanvasGroup == null) {
					m_CanvasGroup = GetComponent<CanvasGroup> ();
					if (m_CanvasGroup == null)
						m_CanvasGroup = gameObject.AddComponent<CanvasGroup> ();
				}
				return m_CanvasGroup;
			}
		}

		/// <summary>Idempotent one-time setup, called by <see cref="WindowManager"/> before the first Open.</summary>
		public void Init ()
		{
			if (IsInitialized)
				return;
			IsInitialized = true;
			OnInit ();
		}

		/// <summary>Shows the window and forwards the caller's payload.</summary>
		public void Open (object args = null)
		{
			Init ();
			gameObject.SetActive (true);
			CanvasGroup.alpha = 1f;
			CanvasGroup.interactable = true;
			CanvasGroup.blocksRaycasts = true;
			IsOpen = true;
			OnOpened (args);
		}

		/// <summary>Hides the window. The object is kept alive so it can be reopened from the stack.</summary>
		public void Close ()
		{
			if (!IsOpen)
				return;
			IsOpen = false;
			CanvasGroup.blocksRaycasts = false;
			OnClosed ();
			gameObject.SetActive (false);
		}

		protected virtual void OnInit () { }
		protected virtual void OnOpened (object args) { }
		protected virtual void OnClosed () { }
	}
}
