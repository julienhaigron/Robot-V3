using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndLevelPopup : AUIPopup
{
	[SerializeField] private TextMeshProUGUI m_texte;
	[SerializeField] private BaseButton m_continueButton;

	private void Awake ()
	{
		m_continueButton.onClick += OnClickContinue;
	}

	private void OnDestroy ()
	{
		m_continueButton.onClick -= OnClickContinue;
	}

	/*protected override void OnShowFinished ()
	{

	}*/

	protected override void OnHideFinished ()
	{
		base.OnHideFinished();
	}

	public void Init (bool _didWin = false)
	{
		m_texte.text = _didWin ? "You win" : "You loose";
	}

	private void OnClickContinue ()
	{
		Close();
		GameManager.Instance.GoBackToMainMenu();
	}

}
