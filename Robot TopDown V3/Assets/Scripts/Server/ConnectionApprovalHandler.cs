using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ConnectionApprovalHandler : MonoBehaviour
{

    private void Start ()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        }
    }

	private void OnDestroy ()
	{
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        }
    }

	private void ApprovalCheck ( NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response )
    {
        response.Approved = false;
        response.Reason = "Testing the declined approval message";
    }

    private void OnClientDisconnectCallback ( ulong obj )
    {
        if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.DisconnectReason != string.Empty)
        {
            Debug.Log("Approval Declined Reason: " + NetworkManager.Singleton.DisconnectReason);
        }
    }
}
