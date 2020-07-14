using System.Collections;
using Mirror;
using UnityEngine;

namespace Insight {
	public class ServerIdler : InsightModule {
		private Transport transport;
		private NetworkManager netManager;

		private IEnumerator exitCor;

		[SerializeField] private float maxMinutesOfIdle = 5f;

		public override void Initialize(InsightClient _client, ModuleManager _manager) {
			Debug.Log("[ServerIdler] - Initialization");
			
			transport = Transport.activeTransport;
			netManager = NetworkManager.singleton;

			RegisterHandlers();
			
			CheckIdle();
		}

		private void RegisterHandlers() {
			transport.OnServerConnected.AddListener(_ => CheckIdle());
			transport.OnServerDisconnected.AddListener(_ => CheckIdle());
		}

		private void CheckIdle() {
			if (NetworkServer.connections.Count > 0) {
				if(exitCor != null) {
					StopCoroutine(exitCor);
					exitCor = null;
				}
			}
			else {
				if (exitCor == null) {
					exitCor = WaitAndExitCor();
					StartCoroutine(exitCor);
				}
			}
		}

		private IEnumerator WaitAndExitCor() {
			yield return new WaitForSeconds(60*maxMinutesOfIdle);

			Debug.LogWarning("[ServerIdler] - No players connected within the allowed time. Shutting down server");

			netManager.StopServer();
			Application.Quit();
		}
	}
}