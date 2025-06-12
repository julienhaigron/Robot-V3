using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

public class TileUIPlugin : MonoBehaviour
{
	[SerializeField] private Tile m_linkedTile;

    [SerializeField] private TextMeshProUGUI m_positionDisplay;
	[SerializeField] private Image m_outline;
	[SerializeField] private MeshRenderer m_groundMeshRendered;
	[SerializeField] private GameObject m_fow;

	#region base
	public void SetPosition(int _x, int _y )
	{
		m_positionDisplay.text = _x + "." + _y;
	}

	public void UpdateDistanceLabel (int _distance)
	{
		//m_positionDisplay.text = _distance == int.MaxValue ? "" :  _distance.ToString();
	}

	public void UpdateGroundMaterial ()
	{
		m_groundMeshRendered.material = GameAssets.current.material.tileGroundMaterials[m_linkedTile.GroundType];
	}

	public void ResetOutline ()
	{
		m_outline.enabled = false;
		m_outline.color = Color.black;
	}

	public void EnableOutline ()
	{
		m_outline.enabled = true;
	}

	public void EnableOutline ( Color color )
	{
		m_outline.color = color;
		m_outline.enabled = true;
	}
	#endregion

	#region FOW

	public void SetActiveFOW (bool _isActive = false, bool _isInstant = false)
	{
		m_fow.SetActive(_isActive);
	}

	#endregion

	public void SetAsInteractable(AEntityAction _correspondingAction )
	{
		m_outline.enabled = true;
		m_outline.color = Color.green;
	}
}
