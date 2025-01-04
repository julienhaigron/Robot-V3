using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seance.Networking
{
	public class NetworkCallbackManager : NetworkBehaviour
	{
		public delegate void NetworkCallbackResolvedHandler(string requestName);
		public event NetworkCallbackResolvedHandler NetworkCallbackResolved;

		private Dictionary<string, NetworkCallback> _networkCallbacks = new();

		[ServerRpc(RequireOwnership = false)]
		public void ServerCreateNetworkCallback(string callbackName, int goal)
		{
			if (!IsServerInitialized)
				return;

			if (_networkCallbacks.ContainsKey(callbackName))
            {
				_networkCallbacks.Remove(callbackName);
				Debug.Log($"Input request \"{callbackName}\" already exists and has been override");
            }

			_networkCallbacks.Add(callbackName, new NetworkCallback(callbackName, this));

			_networkCallbacks[callbackName].SetGoal(goal);

			_networkCallbacks[callbackName].ValidateGoal();
		}

		[ServerRpc(RequireOwnership = false)]
		public void ServerDeleteAllNetworkCallbacks()
		{
			_networkCallbacks.Clear();
		}

		public void ServerDeleteNetworkCallback(string callbackName)
		{
			if (!IsServerInitialized)
				return;

			if (!_networkCallbacks.ContainsKey(callbackName))
				return;

			_networkCallbacks.Remove(callbackName);
		}

		[ServerRpc(RequireOwnership = false)]
		public void ServerClearAllNetworkCallback()
        {
			if (!IsServerInitialized)
				return;

			ObserverCallAllNetworkCallback();
		}

		[ObserversRpc]
		public void ObserverCallAllNetworkCallback()
        {
			NetworkCallbackResolved = null;
			_networkCallbacks.Clear();
		}

		[ServerRpc(RequireOwnership = false)]
		public void ServerIncrementNetworkCallbackProgress(string callbackName, int progress)
		{
			if (!IsServerInitialized)
				return;

			if (!_networkCallbacks.ContainsKey(callbackName))
				return;

			_networkCallbacks[callbackName].IncrementProgress(progress);
		}
		
		void ResolveNetworkCallback(string callbackName)
		{
			_networkCallbacks.Remove(callbackName);

			ObserversResolveNetworkCallback(callbackName);
		}

		[ObserversRpc]
		void ObserversResolveNetworkCallback(string callbackName)
		{
			NetworkCallbackResolved?.Invoke(callbackName);
		}

		class NetworkCallback
		{
			readonly string _name;

			int _goal = 0;
			int _progress = 0;

			bool _validate = false;
			readonly NetworkCallbackManager _manager;

			public NetworkCallback(string name, NetworkCallbackManager manager)
			{
				_name = name;
				_manager = manager;
			}

			public void SetGoal(int value)
			{
				if (_validate)
					throw new Exception("Cannot set goal value after validation");

				_goal = value;
			}

			public void ValidateGoal()
			{
				_validate = true;
			}

			public void IncrementProgress(int value)
			{
				if (!_validate)
					throw new Exception("Can not increment delta value before validation");

				_progress += value;
				if (_progress >= _goal)
				{
					ResolveCallback();
				}
			}

			void ResolveCallback()
			{
				_manager.ResolveNetworkCallback(_name);
			}
		}
	}
}
