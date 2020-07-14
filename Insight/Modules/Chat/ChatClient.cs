using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public class ChatClient : InsightModule {
		private InsightClient client;
		
		public override void Initialize(InsightClient _client, ModuleManager _manager) {
			Debug.Log("[Client - Chat] - Initialization");
			client = _client;

			RegisterHandlers();
		}

		private void RegisterHandlers() {
			client.RegisterHandler<ChatMsg>(HandleChatMsg);
		}

		private void HandleChatMsg(InsightMessage _insightMsg) {
			Debug.Log("[Client - Chat] - Receive chatting");

			var message = (ChatMsg) _insightMsg.message;
			
			ReceiveMessage(message);
		}

		public void Chat(string _data) {
			Assert.IsTrue(client.IsConnected);
			client.NetworkSend(new ChatMsg {data = _data});
		}
	}
}