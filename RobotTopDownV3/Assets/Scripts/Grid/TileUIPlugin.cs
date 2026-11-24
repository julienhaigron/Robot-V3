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

	public void ClearPositionText ()
	{
		m_positionDisplay.text = string.Empty;
	}

	public void SetTargetIndexText ( string _text )
	{
		m_positionDisplay.text = _text;
	}

	public void UpdateGroundMaterial ()
	{
		m_groundMeshRendered.material = GameAssets.current.ui.tileGroundMaterials[m_linkedTile.GroundType];
	}

	public void ResetOutline ()
	{
		m_outline.enabled = false;
		m_outline.color = Color.black;
	}

	public void SetOutlineColor ( Color color )
	{
		m_outline.color = color;
		m_outline.enabled = true;
	}
	#endregion

	#region FOW

	private static readonly int s_fowMaskID = Shader.PropertyToID("_Color");
	private static MaterialPropertyBlock s_fowBlock;
	private Color? m_fowMask;

	public void SetFOW ( bool _isFogged, bool _hasThermal, bool _hasRadar )
	{
		Color mask = _isFogged ? new Color(1f, _hasThermal ? 1f : 0f, _hasRadar ? 1f : 0f) : Color.black;
		if (m_fowMask == mask)
			return;

		m_fowMask = mask;
		m_fow.SetActive(_isFogged);
		if (_isFogged)
		{
			s_fowBlock ??= new MaterialPropertyBlock();
			s_fowBlock.SetColor(s_fowMaskID, mask);
			m_fow.GetComponent<Renderer>().SetPropertyBlock(s_fowBlock);
		}

		FogOfWarRenderer.Instance.MarkDirty();
	}

	#endregion
}
