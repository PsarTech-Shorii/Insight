using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
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

	public abstract class InsightCommon : MonoBehaviour {
		protected struct CallbackData {
			public InsightMessage messageSent;
			public CallbackHandler callback;
			public float timeout;
		}

		public delegate void CallbackHandler(InsightMessage insightMsg);
		
		private const float CallbackTimeout = 30f;

		private int _callbackIdIndex; // 0 is a _special_ id - it represents _no callback_. 
		private readonly List<int> _abandonnedCallback = new List<int>();
		private readonly Dictionary<Type, InsightMessageDelegate> _messageHandlers =
			new Dictionary<Type, InsightMessageDelegate>();

		protected readonly Dictionary<int, CallbackData> callbacks = new Dictionary<int, CallbackData>();

		public Transport transport;
		
		public bool dontDestroy = true;
		public bool autoStart = true;
		public string networkAddress = "localhost";

		public ConnectState connectState = ConnectState.None;
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

		protected virtual void Start() {
			if (dontDestroy) {
				DontDestroyOnLoad(this);
			}

			Application.runInBackground = true;

			if (autoStart) {
				StartInsight();
			}
			
			RegisterHandlers();
		}

		protected virtual void Update() {
			CheckCallbackTimeouts();
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

		public void UnRegisterHandler<T>(InsightMessageDelegate handler) {
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
				callbacks.Add(callbackId, new CallbackData {
					messageSent = insightMsg,
					callback = callback,
					timeout = Time.realtimeSinceStartup + CallbackTimeout
				});
			}

			insightMsg.callbackId = callbackId;
		}

		protected void HandleMessage(InsightMessage insightMsg) {
			if (_abandonnedCallback.Contains(insightMsg.callbackId)) {
				_abandonnedCallback.Remove(insightMsg.callbackId);
			}
			else if (callbacks.ContainsKey(insightMsg.callbackId) && insightMsg.status != CallbackStatus.Default) {
				callbacks[insightMsg.callbackId].callback.Invoke(insightMsg);
				callbacks.Remove(insightMsg.callbackId);
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

		private void CheckCallbackTimeouts() {
			foreach (var callback in callbacks.Where(callback =>
				callback.Value.timeout < Time.realtimeSinceStartup)) {
				_abandonnedCallback.Add(callback.Key);
				callback.Value.callback.Invoke(new InsightMessage(new EmptyMessage()) {
					status = CallbackStatus.Timeout
				});
				Resend(callback.Value.messageSent, callback.Value.callback);
				callbacks.Remove(callback.Key);
				break;
			}
		}

		protected abstract void Resend(InsightMessage insightMsg, CallbackHandler callback);
		protected abstract void RegisterHandlers();
		public abstract void StartInsight();
		public abstract void StopInsight();
	}
}