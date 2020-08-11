using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class InsightServer : InsightCommon {
		private readonly List<int> connectionsId = new List<int>();

		protected override void RegisterHandlers() {
			transport.OnServerConnected.AddListener(HandleConnect);
			transport.OnServerDisconnected.AddListener(HandleDisconnect);
			transport.OnServerDataReceived.AddListener(HandleData);
			transport.OnServerError.AddListener(OnError);
		}

		public override void StartInsight() {
			Debug.Log("[InsightServer] - Server started listening");

			transport.ServerStart();
			
			IsConnected = true;
		}

		public override void StopInsight() {
			Debug.Log("[InsightServer] - Server stopping");

			transport.ServerStop();

			IsConnected = false;
			
			connectionsId.Clear();
		}

		private void HandleConnect(int _connectionId) {
			Debug.Log($"[InsightServer] - Client connected connectionID: {_connectionId}");
			
			connectionsId.Add(_connectionId);
		}

		private void HandleDisconnect(int _connectionId) {
			Debug.Log($"[InsightServer] - Client disconnected connectionID: {_connectionId}");

			connectionsId.Remove(_connectionId);
		}

		private void HandleData(int _connectionId, ArraySegment<byte> _data, int _) {
			if (!connectionsId.Contains(_connectionId)) {
				Debug.LogError($"[InsightServer] - Unknown connection of data to handle : {_connectionId}");
				return;
			}
			
			var netMsg = new InsightNetworkMessage();
			netMsg.Deserialize(new NetworkReader(_data));
			netMsg.connectionId = _connectionId;

			HandleMessage(netMsg);
		}

		private void OnError(int _connectionId, Exception _exception) {
			// TODO Let's discuss how we will handle errors
			Debug.LogException(_exception);
		}

		public void NetworkSend(int _connectionId, InsightNetworkMessage _netMsg, CallbackHandler _callback = null) {
			if (!transport.ServerActive()) {
				Debug.LogError("[InsightServer] - Can't networking send with server inactive !");
				return;
			}

			_netMsg.connectionId = _connectionId;
			if(_netMsg.callbackId == 0) RegisterCallback(_netMsg, _callback);
			
			var writer = new NetworkWriter();
			_netMsg.Serialize(writer);

			transport.ServerSend(new List<int> {_connectionId}, 0, writer.ToArraySegment());
		}

		public void NetworkSend(int _connectionId, InsightMessageBase _message, CallbackHandler _callback = null) {
			NetworkSend(_connectionId, new InsightNetworkMessage(_message), _callback);
		}

		public void NetworkReply(int _connectionId, InsightNetworkMessage _netMsg) {
			Assert.AreNotEqual(CallbackStatus.Default, _netMsg.status);
			NetworkSend(_connectionId, _netMsg);
		}

		protected override void Resend(InsightMessage _insightMsg, CallbackHandler _callback) {
			if (_insightMsg is InsightNetworkMessage netMsg) {
				NetworkSend(netMsg.connectionId, netMsg, _callback);
			}
			else {
				InternalSend(_insightMsg, _callback);
			}
		}
	}
}