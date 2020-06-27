using System.Collections;
using Mirror;
using UnityEngine;

namespace Insight {
	public class ServerIdler : InsightModule {
		private Transport _networkManagerTransport;

		private IEnumerator _exitCor;
		
		[SerializeField] private float maxMinutesOfIdle = 5f;

		public override void Initialize(InsightClient client, ModuleManager manager) {
			_networkManagerTransport = Transport.activeTransport;
			
			Debug.Log("[ServerIdler] - Initialization");

			RegisterHandlers();
			
			StartCoroutine(CheckIdleCor());
		}

		private void RegisterHandlers() {
			_networkManagerTransport.OnServerConnected.AddListener(HandleConnection);
			_networkManagerTransport.OnServerDisconnected.AddListener(HandleConnection);
		}

		private void HandleConnection(int connectionId = -1) {
			StartCoroutine(CheckIdleCor());
		}

		private IEnumerator CheckIdleCor() {
			yield return new WaitForEndOfFrame();
			
			if (NetworkManager.singleton.numPlayers > 0) {
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

			NetworkManager.singleton.StopServer();
			Application.Quit();
		}
	}
}