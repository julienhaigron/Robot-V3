using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using System;

public class NetworkInteractions : NetworkBehaviour
{

	private void Start ()
	{

	}

	#region Logger

	[ServerRpc(RequireOwnership = false)]
	public void ServerSendMessage ( string message )
	{
		ObserversSendMessage(message);
	}

	[ObserversRpc]
	private void ObserversSendMessage ( string message )
	{
		//Logger.Info(message, printFile: false);
	}

	[ServerRpc(RequireOwnership = false)]
	public void ServerApplicationQuit ()
	{
		ObserversApplicationQuit();
	}

	[ObserversRpc]
	private void ObserversApplicationQuit ()
	{
		//Logger.Critical("Closing application. Forced by 'quit' command", Channel.Command);

#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
	}

	#endregion
}