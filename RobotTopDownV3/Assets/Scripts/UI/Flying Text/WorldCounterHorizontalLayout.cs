using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class WorldCounterHorizontalLayout : MonoBehaviour
{
	[SerializeField] private TextMeshPro m_tmp;
	[SerializeField] private SpriteRenderer m_spriteRdr;
	[SerializeField] private float m_spacing = 0f;
	[SerializeField] private bool m_revertOrder = false;
	private float IconWidth => m_spriteRdr.gameObject.activeSelf ? m_spriteRdr.bounds.size.x : 0f;
	private float TMPWidth => m_tmp.bounds.size.x;
	private float Spacing => m_spriteRdr.gameObject.activeSelf ? m_spacing : 0f;

	private float m_totalWidth;
	private float m_startX;

	private void Awake ()
	{
		m_tmp.OnPreRenderText += OnTextChanged;
		UpdateLayout();
	}

	private void OnDestroy ()
	{
		m_tmp.OnPreRenderText -= OnTextChanged;
	}

	private void OnTextChanged ( TMP_TextInfo obj )
	{
		if (TMPWidth > 0f)
			UpdateLayout();
	}

	[Button]
	public void UpdateLayout ()
	{
		m_totalWidth = IconWidth + TMPWidth + Spacing;
		m_startX = -m_totalWidth * 0.5f;

		if (!m_revertOrder)
		{
			m_spriteRdr.transform.localPosition = new Vector3(m_startX + IconWidth * 0.5f, 0, 0);
			m_tmp.transform.localPosition = new Vector3(m_startX + IconWidth + Spacing + TMPWidth * 0.5f, 0, 0);
		}
		else
		{
			m_tmp.transform.localPosition = new Vector3(m_startX + TMPWidth * 0.5f, 0, 0);
			m_spriteRdr.transform.localPosition = new Vector3(m_startX + TMPWidth + Spacing + IconWidth * 0.5f, 0, 0);
		}
	}
}
