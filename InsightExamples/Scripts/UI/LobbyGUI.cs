using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Insight;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI {
	public class LobbyGUI : MonoBehaviour {
		private const float GameJoinerMargin = 75f;
		private readonly List<GameObject> _gameJoiners = new List<GameObject>();

		[Header("Module")]
		[SerializeField] private ClientGameManager clientGameManager;
		[SerializeField] private ClientMatchMaker clientMatchMaker;

		[Header("Interface")]
		[SerializeField] private GameObject gameCreationPopup;
		[SerializeField] private Button gameCreationButton;
		[SerializeField] private Button matchGameButton;
		[SerializeField] private TextMeshProUGUI gameNameText;
		[SerializeField] private TextMeshProUGUI playerCountText;
		[SerializeField] private Slider playerCountSlider;
		[SerializeField] private GameObject gameJoinerPrefabs;
		[SerializeField] private Transform gameJoinerParent;


		public UnityEvent toActivate;
		
		private void Start() {
			clientGameManager.onResponse.AddListener(OnReceiveGameList);
			clientGameManager.onReceive.AddListener(OnUpdateGameList);
			clientGameManager.onResponse.AddListener(OnChangeServer);
			clientGameManager.onReceive.AddListener(OnChangeServer);
			
			playerCountSlider.onValueChanged.AddListener(OnPlayerCountChanged);
			
			gameObject.SetActive(false);
		}

		private void OnReceiveGameList(InsightMessageBase messageBase, CallbackStatus status) {
			if(!(messageBase is GameListMsg message)) return;

			switch (status) {
				case CallbackStatus.Success: {
					foreach (var gameJoiner in _gameJoiners) Destroy(gameJoiner);
					_gameJoiners.Clear();

					foreach (var game in message.gamesArray) {
						var gameJoinerObject = Instantiate(gameJoinerPrefabs, gameJoinerParent);
						var gameJoiner = gameJoinerObject.GetComponent<GameJoiner>();
						gameJoiner.Initialize(game);
						gameJoiner.joinEvent.AddListener(clientGameManager.JoinGame);
						_gameJoiners.Add(gameJoinerObject);
					}
			
					GameJoinersPositioning();
				}
					break;
				case CallbackStatus.Error:
				case CallbackStatus.Timeout:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void OnUpdateGameList(InsightMessageBase messageBase) {
			if(!(messageBase is GameListStatusMsg message)) return;
			UpdateGameList(message);
			GameJoinersPositioning();
		}

		private void OnChangeServer(InsightMessageBase messageBase) {
			OnChangeServer(messageBase, CallbackStatus.Default);
		}
		
		private void OnChangeServer(InsightMessageBase messageBase, CallbackStatus status) {
			if(!(messageBase is ChangeServerMsg)) return;

			gameCreationButton.interactable = true;
			matchGameButton.interactable = true;

			switch (status) {
				case CallbackStatus.Default:
				case CallbackStatus.Success: {
					toActivate?.Invoke();
					gameObject.SetActive(false);
					break;
				}
				case CallbackStatus.Error:
				case CallbackStatus.Timeout:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, null);
			}
		}

		private void UpdateGameList(GameListStatusMsg message) {
			switch (message.operation) {
				case Operation.Add: {
					var gameJoinerObject = Instantiate(gameJoinerPrefabs, gameJoinerParent);
					var gameJoiner = gameJoinerObject.GetComponent<GameJoiner>();
					gameJoiner.Initialize(message.game);
					gameJoiner.joinEvent.AddListener(clientGameManager.JoinGame);
					_gameJoiners.Add(gameJoinerObject);
					break;
				}
				case Operation.Remove: {
					var gameJoinerObject = _gameJoiners.Find(e =>
						e.GetComponent<GameJoiner>().Is(message.game.uniqueId));
					Destroy(gameJoinerObject);
					_gameJoiners.Remove(gameJoinerObject);
					break;
				}
				case Operation.Update: {
					var gameJoinerObject = _gameJoiners.Find(e =>
						e.GetComponent<GameJoiner>().Is(message.game.uniqueId));
					
					gameJoinerObject.GetComponent<GameJoiner>().UpdatePlayerCount(new GameStatusMsg {
							currentPlayers = message.game.currentPlayers,
							hasStarted = message.game.hasStarted
						}
					);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void GameJoinersPositioning() {
			var i = 0;
			var position = gameJoinerPrefabs.GetComponent<RectTransform>().anchoredPosition;
			foreach (var gameJoiner in _gameJoiners.Where(e => e.activeInHierarchy)) {
				gameJoiner.GetComponent<RectTransform>().anchoredPosition = position + Vector2.down * GameJoinerMargin * i;
				i++;
			}
		}

		private void OnPlayerCountChanged(float playerCount) {
			playerCountText.text = $"Minimum player count : {playerCount}";
		}

		public void CreateGame() {
			gameCreationButton.interactable = false;
			clientGameManager.CreateGame(new CreateGameMsg {
				sceneName = Settings.ScenesRoot + "Game",
				gameName = gameNameText.text,
				minPlayers = (int) playerCountSlider.value
			});
			gameNameText.text = "";
		}

		public void MatchGame() {
			if (_gameJoiners.Exists(e => e.activeInHierarchy)) {
				matchGameButton.interactable = false;
				clientMatchMaker.MatchGame();
			}
			else {
				gameCreationPopup.SetActive(true);
			}
		}

		public void LeaveGame() {
			clientGameManager.LeaveGame(Settings.ScenesRoot + "Lobby");
		}
	}
}