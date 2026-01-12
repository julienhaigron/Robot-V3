using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WallSelector : MonoBehaviour
{
	[SerializeField] private Wall m_linkedWall;
	public Wall LinkedWall => m_linkedWall;

	public void Link(Wall _wall )
	{
		m_linkedWall = _wall;
	}

#if UNITY_EDITOR


	[CustomEditor(typeof(WallSelector))]
	class WallSelectorEditor : Editor
	{
		//PathNode selectedNode = null;

		public override void OnInspectorGUI ()
		{
			DrawDefaultInspector();
			WallSelector wall = (WallSelector)target;
		}

		protected virtual void OnSceneGUI ()
		{
			WallSelector selector = (WallSelector)target;

			selector.LinkedWall.DisplayHandles();
		}
	}
#endif
}
