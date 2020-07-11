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

	public delegate void InsightMessageDelegate(InsightMessage insightMsg);
	public delegate void CallbackHandler(InsightMessage insightMsg);

	public abstract class InsightCommon : MonoBehaviour {
		private class CallbackData {
			public CallbackHandler callback;
			public IEnumerator expirationCor;
		}

		private const float CallbackTimeout = 30f;

		private int _callbackIdIndex; // 0 is a _special_ id - it represents _no callback_. 
		private readonly List<int> _expiredCallback = new List<int>();
		private readonly Dictionary<Type, InsightMessageDelegate> _messageHandlers =
			new Dictionary<Type, InsightMessageDelegate>();

		private readonly Dictionary<int, CallbackData> _callbacks = new Dictionary<int, CallbackData>();

		public Transport transport;
		
		public bool dontDestroy = true;
		public bool autoStart = true;
		public string networkAddress = "localhost";

		[HideInInspector] public ConnectState connectState = ConnectState.None;
		public bool IsConnected => connectState == ConnectState.Connected;

		private void OnValidate() {
			// add transport if there is none yet. makes upgrading easier.
			if (transport == null) {
				transport = GetComponent<Transport>();
				if (transport == null) {
					transport = gameObject.AddComponent<TelepathyTransport>();
					Debug.Log("NetworkManager: added default Transport because there was none yet.");
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

		public void RegisterHandler<T>(InsightMessageDelegate handler) {
			if (_messageHandlers.ContainsKey(typeof(T))) {
				Debug.Log($"NetworkConnection.RegisterHandler replacing {typeof(T)}");
			}

			_messageHandlers.Add(typeof(T), handler);
		}

		public void UnregisterHandler<T>(InsightMessageDelegate handler) {
			if (_messageHandlers.TryGetValue(typeof(T), out var handlerValue)) {
				if (handlerValue == handler) _messageHandlers.Remove(typeof(T));
			}
		}

		public void ClearHandlers() {
			_messageHandlers.Clear();
		}

		protected void RegisterCallback(InsightMessage insightMsg, CallbackHandler callback = null) {
			var callbackId = 0;
			if (callback != null) {
				callbackId = ++_callbackIdIndex;
				var callbackData = new CallbackData {
					callback = callback,
					expirationCor = ExpireCallbackCor(callbackId, insightMsg, callback)
				};
				
				_callbacks.Add(callbackId, callbackData);
				StartCoroutine(callbackData.expirationCor);
			}

			insightMsg.callbackId = callbackId;
		}

		protected void HandleMessage(InsightMessage insightMsg) {
			if (_expiredCallback.Contains(insightMsg.callbackId)) {
				_expiredCallback.Remove(insightMsg.callbackId);
			}
			else if (_callbacks.ContainsKey(insightMsg.callbackId) && insightMsg.status != CallbackStatus.Default) {
				var callbackData = _callbacks[insightMsg.callbackId];
				
				Assert.IsNotNull(callbackData.expirationCor);
				StopCoroutine(callbackData.expirationCor);
				
				callbackData.callback.Invoke(insightMsg);
				
				_callbacks.Remove(insightMsg.callbackId);
			}
			else {
				if (_messageHandlers.TryGetValue(insightMsg.MsgType, out var msgDelegate)) msgDelegate(insightMsg);
				else {
					//NOTE: this throws away the rest of the buffer. Need more error codes
					Debug.LogError($"Unknown message {insightMsg.MsgType}");
				}
			}
		}

		public void InternalSend(InsightMessage insightMsg, CallbackHandler callback = null) {
			if(insightMsg.callbackId == 0) RegisterCallback(insightMsg, callback);
			HandleMessage(insightMsg);
		}

		public void InternalSend(InsightMessageBase msg, CallbackHandler callback = null) {
			InternalSend(new InsightMessage(msg), callback);
		}

		public void InternalReply(InsightMessage insightMsg) {
			Assert.AreNotEqual(CallbackStatus.Default, insightMsg.status);
			InternalSend(insightMsg);
		}

		private IEnumerator ExpireCallbackCor(int callbackId, InsightMessage insightMsg, CallbackHandler callback) {
			yield return new WaitForSeconds(CallbackTimeout);
			
			_expiredCallback.Add(callbackId);
			callback.Invoke(new InsightMessage(new EmptyMessage()) {
				status = CallbackStatus.Timeout
			});
			Resend(insightMsg, callback);
			_callbacks.Remove(callbackId);
		}

		protected abstract void Resend(InsightMessage insightMsg, CallbackHandler callback);
		protected abstract void RegisterHandlers();
		public abstract void StartInsight();
		public abstract void StopInsight();
	}
}