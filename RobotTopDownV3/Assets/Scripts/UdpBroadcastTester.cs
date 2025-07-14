using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class UdpBroadcastTester : MonoBehaviour
{
    public bool isServer = false;
    private UdpClient udpClient;
    private IPEndPoint broadcastEndPoint;
    private const int broadcastPort = 47777; //
    private float broadcastInterval = 2f;
    private float timer;

    void Start ()
    {
        if (isServer)
        {
            udpClient = new UdpClient(broadcastPort);
            udpClient.EnableBroadcast = true;
            udpClient.BeginReceive(OnReceive, null);
            Debug.Log("[UDP Test] Serveur d'écoute lancé.");
        }
        else
        {
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            Debug.Log("[UDP Test] Client prêt à envoyer.");
        }
    }

    void Update ()
    {
        if (!isServer)
        {
            timer += Time.deltaTime;
            if (timer >= broadcastInterval)
            {
                timer = 0f;
                SendBroadcast();
            }
        }
    }

    void SendBroadcast ()
    {
        string message = $"Hello from {GetLocalIPAddress()} at {DateTime.Now}";
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length, broadcastEndPoint);
        Debug.Log("[UDP Test] Message envoyé : " + message);
    }

    void OnReceive ( IAsyncResult ar )
    {
        try
        {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, broadcastPort);
            byte[] data = udpClient.EndReceive(ar, ref sender);
            string message = Encoding.UTF8.GetString(data);
            Debug.Log($"[UDP Test] Message reçu de {sender}: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[UDP Test] Erreur lors de la réception : " + ex);
        }

        // Continuer à écouter
        udpClient.BeginReceive(OnReceive, null);
    }

    public string GetLocalIPAddress ()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
    }

    void OnDestroy ()
    {
        udpClient?.Close();
    }
}
