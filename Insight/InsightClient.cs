using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace Insight {
	public delegate void ConnectionDelegate();
	
	public class InsightClient : InsightCommon {
		private int serverConnId;

		private IEnumerator reconnectCor;
		private bool toReconnect;

		public bool autoReconnect = true;
		public float reconnectDelayInSeconds = 5f;

		public override bool IsConnected {
			protected set {
				connectState = value ? ConnectState.Connected : ConnectState.Disconnected;
				if(IsConnected) OnConnected?.Invoke();
				else OnDisconnected?.Invoke();
			}
		}

		public event ConnectionDelegate OnConnected;
		public event ConnectionDelegate OnDisconnected;

		protected override void RegisterHandlers() {
			transport.OnClientConnected.AddListener(() => IsConnected = true);
			transport.OnClientDisconnected.AddListener(() => IsConnected = false);
			transport.OnClientDataReceived.AddListener(HandleData);
			transport.OnClientError.AddListener(OnError);

			OnConnected += () => {
				Debug.Log($"[InsightClient] - Connecting to Insight Server: {networkAddress}");
				StopReconnect();
			};

			OnDisconnected += () => {
				Debug.Log("[InsightClient] - Disconnecting from Insight Server");
				if(toReconnect) StartReconnect();
			};
		}

		public override void StartInsight() {
			transport.ClientConnect(networkAddress);

			toReconnect = true;
			StartReconnect();
		}

		public override void StopInsight() {
			transport.ClientDisconnect();

			toReconnect = false;
		}

		private void HandleData(ArraySegment<byte> _data, int _) {
			var netMsg = new InsightNetworkMessage();
			netMsg.Deserialize(new NetworkReader(_data));

			HandleMessage(netMsg);
		}

		private void OnError(Exception _exception) {
			// TODO Let's discuss how we will handle errors
			Debug.LogException(_exception);
		}

		public void NetworkSend(InsightNetworkMessage _netMsg, CallbackHandler _callback = null) {
			if (!transport.ClientConnected()) {
				Debug.LogError("[InsightClient] - Client not connected!");
				return;
			}

			if(_netMsg.callbackId == 0) RegisterCallback(_netMsg, _callback);
			
			var writer = new NetworkWriter();
			_netMsg.Serialize(writer);

			transport.ClientSend(0, writer.ToArraySegment());
		}

		public void NetworkSend(InsightMessageBase _message, CallbackHandler _callback = null) {
			NetworkSend(new InsightNetworkMessage(_message), _callback);
		}

		public void NetworkReply(InsightNetworkMessage _netMsg) {
			Assert.AreNotEqual(CallbackStatus.Default, _netMsg.status);
			NetworkSend(_netMsg);
		}
		
		protected override void Resend(InsightMessage _insightMsg, CallbackHandler _callback) {
			if (_insightMsg is InsightNetworkMessage netMsg) {
				NetworkSend(netMsg, _callback);
			}
			else {
				InternalSend(_insightMsg, _callback);
			}
		}

		private void StartReconnect() {
			if (autoReconnect) {
				if (reconnectCor != null) {
					StopCoroutine(reconnectCor);
					reconnectCor = null;
				}

				reconnectCor = ReconnectCor();
				StartCoroutine(reconnectCor);
			}
		}

		private void StopReconnect() {
			if(reconnectCor != null) {
				StopCoroutine(reconnectCor);
				reconnectCor = null;
			}
		}
		
		private IEnumerator ReconnectCor() {
			Assert.IsTrue(autoReconnect);
			Assert.IsFalse(IsConnected);
				
			yield return new WaitForSeconds(reconnectDelayInSeconds);
				
			Debug.Log("[InsightClient] - Trying to reconnect...");
			StartInsight();
		}
	}
}