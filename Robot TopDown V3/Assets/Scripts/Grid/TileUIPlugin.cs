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

	public void SetPosition(int _x, int _y )
	{
		m_positionDisplay.text = _x + "." + _y;
	}

	public void UpdateDistanceLabel (int _distance)
	{
		m_positionDisplay.text = _distance.ToString();
	}

	public void DisableOutline ()
	{
		m_outline.enabled = false;
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
}
