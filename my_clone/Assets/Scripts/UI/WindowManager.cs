using System.Collections.Generic;
using UnityEngine;

namespace MyClone.UI
{
	/// <summary>
	/// Canvas layer indices observed in the shipped GUISettings prefab
	/// (GUISettings.UICanvas[3] == GUI_ROOT/BottomUIContain/Canvas_BottomFirstLayer).
	/// </summary>
	public enum WindowLayer
	{
		Scene = 0,
		BottomFirst = 3,
		BottomSecond = 4,
		BottomThird = 5,
		WindowFirst = 6,
		WindowSecond = 7,
		WindowThird = 8,
		Dialog = 9,
	}

	/// <summary>
	/// Small window manager: parents a window to the canvas for its layer, keeps a stack and
	/// routes Back/Escape to the top window. This mirrors the observable game behaviour
	/// (CSGUIManager.AddUIToCanvas(index) + UIManager's active window stack) without the Lua
	/// layer, prefab cache or tweening.
	/// </summary>
	[DisallowMultipleComponent]
	public class WindowManager : MonoBehaviour
	{
		public static WindowManager Instance { get; private set; }

		[Tooltip ("Index = WindowLayer value. Filled by UIWindowBuilder.")]
		[SerializeField] Transform[] layers = new Transform[10];

		[SerializeField] WindowBase initialWindow;
		[SerializeField] bool openInitialOnStart = true;
		[SerializeField] bool closeTopOnEscape = true;

		readonly List<WindowBase> m_Stack = new List<WindowBase> ();

		public IReadOnlyList<WindowBase> Stack { get { return m_Stack; } }
		public WindowBase Top { get { return m_Stack.Count == 0 ? null : m_Stack[m_Stack.Count - 1]; } }

		void Awake ()
		{
			Instance = this;
		}

		void OnDestroy ()
		{
			if (Instance == this)
				Instance = null;
		}

		void Start ()
		{
			if (openInitialOnStart)
				OpenInitial ();
		}

		void Update ()
		{
			if (closeTopOnEscape && Input.GetKeyDown (KeyCode.Escape))
				CloseTop ();
		}

		public Transform GetLayer (WindowLayer layer)
		{
			int index = (int)layer;
			if (layers != null && index >= 0 && index < layers.Length && layers[index] != null)
				return layers[index];
			return transform;
		}

		/// <summary>Opens the window assigned in the inspector (used by the demo scene).</summary>
		public void OpenInitial ()
		{
			if (initialWindow != null)
				Open (initialWindow);
		}

		public void Open (WindowBase window, object args = null, WindowLayer? layer = null)
		{
			if (window == null)
				return;

			var parent = GetLayer (layer ?? WindowLayer.BottomFirst);
			if (window.transform.parent != parent)
				window.transform.SetParent (parent, false);

			m_Stack.Remove (window);
			m_Stack.Add (window);
			window.Open (args);
		}

		public void Close (WindowBase window)
		{
			if (window == null)
				return;
			m_Stack.Remove (window);
			window.Close ();
		}

		/// <summary>Back action: closes the most recently opened window.</summary>
		public void CloseTop ()
		{
			if (m_Stack.Count == 0)
				return;
			var top = m_Stack[m_Stack.Count - 1];
			m_Stack.RemoveAt (m_Stack.Count - 1);
			if (top != null)
				top.Close ();
		}
	}
}
