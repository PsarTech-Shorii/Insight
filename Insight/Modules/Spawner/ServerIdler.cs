using System.Collections;
using Mirror;
using UnityEngine;

namespace Insight {
	public class ServerIdler : InsightModule {
		private Transport _transport;
		private NetworkManager _netManager;

		private IEnumerator _exitCor;

		[SerializeField] private float maxMinutesOfIdle = 5f;

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_transport = Transport.activeTransport;
			_netManager = NetworkManager.singleton;
			
			Debug.Log("[ServerIdler] - Initialization");

			RegisterHandlers();
			
			CheckIdle();
		}

		private void RegisterHandlers() {
			_transport.OnServerConnected.AddListener(CheckIdle);
			_transport.OnServerDisconnected.AddListener(CheckIdle);
		}

		private void CheckIdle(int connectionId = -1) {
			if (NetworkServer.connections.Count > 0) {
				if(_exitCor != null) {
					StopCoroutine(_exitCor);
					_exitCor = null;
				}
			}
			else {
				if (_exitCor == null) {
					_exitCor = WaitAndExitCor();
					StartCoroutine(_exitCor);
				}
			}
		}

		private IEnumerator WaitAndExitCor() {
			yield return new WaitForSeconds(60*maxMinutesOfIdle);

			Debug.LogWarning("[ServerIdler] - No players connected within the allowed time. Shutting down server");

			_netManager.StopServer();
			Application.Quit();
		}
	}
}