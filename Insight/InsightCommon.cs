using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Insight {
	public enum CallbackStatus : byte {
		Default,
		Success,
		Error,
		Timeout
	}
	
	public enum ConnectState {
		None,
		Connected,
		Disconnected,
	}

	public delegate void InsightMessageDelegate(InsightMessage _insightMsg);
	public delegate void CallbackHandler(InsightMessage _insightMsg);
	
	public abstract class InsightCommon : MonoBehaviour {
		private struct CallbackData {
			public CallbackHandler callback;
			public IEnumerator expirationCor;
		}

		private const float CallbackTimeout = 30f;

		private int callbackIdIndex; // 0 is a _special_ id - it represents _no callback_. 
		private readonly List<int> expiredCallback = new List<int>();
		private readonly Dictionary<Type, InsightMessageDelegate> messageHandlers =
			new Dictionary<Type, InsightMessageDelegate>();

		private readonly Dictionary<int, CallbackData> callbacks = new Dictionary<int, CallbackData>();

		public Transport transport;
		
		public bool dontDestroy = true;
		public bool autoStart = true;
		public string networkAddress = "localhost";

		[HideInInspector] public ConnectState connectState = ConnectState.None;
		public virtual bool IsConnected {
			get => connectState == ConnectState.Connected;
			protected set => connectState = value ? ConnectState.Connected : ConnectState.Disconnected;
		}

		private void OnValidate() {
			// add transport if there is none yet. makes upgrading easier.
			if (transport == null) {
				transport = GetComponent<Transport>();
				if (transport == null) {
					transport = gameObject.AddComponent<TelepathyTransport>();
					Debug.Log("InsightCommon: added default Transport because there was none yet.");
				}
#if UNITY_EDITOR
				UnityEditor.Undo.RecordObject(gameObject, "Added default Transport");
#endif
			}
		}

		private void Awake() {
			if (dontDestroy) DontDestroyOnLoad(this);
			Application.runInBackground = true;
		}

		private void Start() {
			RegisterHandlers();
			if (autoStart) StartInsight();
		}

		private void OnApplicationQuit() {
			StopInsight();
		}

		public void RegisterHandler<T>(InsightMessageDelegate _handler) {
			if (messageHandlers.ContainsKey(typeof(T))) {
				Debug.Log($"[Insight] - NetworkConnection.RegisterHandler replacing {typeof(T)}");
			}

			messageHandlers.Add(typeof(T), _handler);
		}

		public void UnregisterHandler<T>(InsightMessageDelegate _handler) {
			if (messageHandlers.TryGetValue(typeof(T), out var handlerValue)) {
				if (handlerValue == _handler) messageHandlers.Remove(typeof(T));
			}
		}

		public void ClearHandlers() {
			messageHandlers.Clear();
		}

		protected void RegisterCallback(InsightMessage _insightMsg, CallbackHandler _callback = null) {
			var callbackId = 0;
			if (_callback != null) {
				callbackId = ++callbackIdIndex;
				var callbackData = new CallbackData {
					callback = _callback,
					expirationCor = ExpireCallbackCor(callbackId, _insightMsg, _callback)
				};
				
				callbacks.Add(callbackId, callbackData);
				StartCoroutine(callbackData.expirationCor);
			}

			_insightMsg.callbackId = callbackId;
		}

		protected void HandleMessage(InsightMessage _insightMsg) {
			if (expiredCallback.Contains(_insightMsg.callbackId)) {
				expiredCallback.Remove(_insightMsg.callbackId);
			}
			else if (callbacks.ContainsKey(_insightMsg.callbackId) && _insightMsg.status != CallbackStatus.Default) {
				var callbackData = callbacks[_insightMsg.callbackId];
				
				Assert.IsNotNull(callbackData.expirationCor);
				StopCoroutine(callbackData.expirationCor);
				
				callbackData.callback.Invoke(_insightMsg);
				
				callbacks.Remove(_insightMsg.callbackId);
			}
			else {
				if (messageHandlers.TryGetValue(_insightMsg.MsgType, out var msgDelegate)) msgDelegate(_insightMsg);
				else {
					//NOTE: this throws away the rest of the buffer. Need more error codes
					Debug.LogError($"[Insight] - Unknown message {_insightMsg.MsgType}");
				}
			}
		}

		public void InternalSend(InsightMessage _insightMsg, CallbackHandler _callback = null) {
			if(_insightMsg.callbackId == 0) RegisterCallback(_insightMsg, _callback);
			HandleMessage(_insightMsg);
		}

		public void InternalSend(InsightMessageBase _message, CallbackHandler _callback = null) {
			InternalSend(new InsightMessage(_message), _callback);
		}

		public void InternalReply(InsightMessage _insightMsg) {
			Assert.AreNotEqual(CallbackStatus.Default, _insightMsg.status);
			InternalSend(_insightMsg);
		}

		private IEnumerator ExpireCallbackCor(int _callbackId, InsightMessage _insightMsg, CallbackHandler _callback) {
			yield return new WaitForSeconds(CallbackTimeout);
			
			expiredCallback.Add(_callbackId);
			_callback.Invoke(new InsightMessage(new EmptyMessage()) {
				status = CallbackStatus.Timeout
			});
			Resend(_insightMsg, _callback);
			callbacks.Remove(_callbackId);
		}

		protected abstract void Resend(InsightMessage _insightMsg, CallbackHandler _callback);
		protected abstract void RegisterHandlers();
		public abstract void StartInsight();
		public abstract void StopInsight();
	}
}