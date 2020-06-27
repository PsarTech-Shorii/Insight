using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Insight {
	[RequireComponent(typeof(InsightCommon))]
	public class ModuleManager : MonoBehaviour {
		private InsightClient _client;
		private InsightServer _server;

		public bool searchChildrenForModule = true;

		private Dictionary<Type, InsightModule> _modules;
		private HashSet<Type> _initializedModules;

		private bool _initializeComplete;
		private bool _cachedClientAutoStartValue;
		private bool _cachedServerAutoStartValue;

		private void Awake() {
			_client = GetComponent<InsightClient>();
			_server = GetComponent<InsightServer>();

			if (_client) {
				_cachedClientAutoStartValue = _client.autoStart;
				_client.autoStart = false; //Wait until modules are loaded to AutoStart
			}

			if (_server) {
				_cachedServerAutoStartValue = _server.autoStart;
				_server.autoStart = false; //Wait until modules are loaded to AutoStart
			}
		}

		private void Start() {
			_modules = new Dictionary<Type, InsightModule>();
			_initializedModules = new HashSet<Type>();
		}

		private void Update() {
			if (!_initializeComplete) {
				_initializeComplete = true;

				var modules = searchChildrenForModule
					? GetComponentsInChildren<InsightModule>()
					: FindObjectsOfType<InsightModule>();

				// Add modules
				foreach (var module in modules)
					AddModule(module);

				// Initialize modules
				InitializeModules(_client, _server);

				//Now that modules are loaded check for original AutoStart value
				if (_cachedServerAutoStartValue) {
					_server.autoStart = _cachedServerAutoStartValue;
					_server.StartInsight();
				}

				if (_cachedClientAutoStartValue) {
					_client.autoStart = _cachedClientAutoStartValue;
					_client.StartInsight();
				}
			}
		}

		public void AddModule(InsightModule module) {
			if (_modules.ContainsKey(module.GetType())) {
				throw new Exception($"A module already exists in the server: {module.GetType()}");
			}

			_modules[module.GetType()] = module;
		}

		public bool RemoveModule(InsightModule module) {
			return _modules.ContainsKey(module.GetType()) && _modules.Remove(module.GetType());
		}

		private bool InitializeModules(InsightClient client, InsightServer server) {
			var checkOptional = true;

			// Initialize modules
			while (true) {
				var changed = false;
				foreach (var entry in _modules) {
					// Module is already initialized
					if (_initializedModules.Contains(entry.Key))
						continue;

					// Not all dependencies have been initialized
					if (!entry.Value.Dependencies.All(d => _initializedModules.Any(d.IsAssignableFrom)))
						continue;

					// Not all OPTIONAL dependencies have been initialized
					if (checkOptional &&
					    !entry.Value.OptionalDependencies.All(d => _initializedModules.Any(d.IsAssignableFrom)))
						continue;

					// If we got here, we can initialize our module
					if (server) {
						entry.Value.Initialize(server, this);
						Debug.LogWarning($"[{gameObject.name}] Loaded InsightServer Module: {entry.Key}");
					}

					if (client) {
						entry.Value.Initialize(client, this);
						Debug.LogWarning($"[{gameObject.name}] Loaded InsightClient Module: {entry.Key}");
					}

					//Add the new module to the HashSet
					_initializedModules.Add(entry.Key);

					// Keep checking optional if something new was initialized
					checkOptional = true;

					changed = true;
				}

				// If we didn't change anything, and initialized all that we could
				// with optional dependencies in mind
				if (!changed && checkOptional) {
					// Initialize everything without checking optional dependencies
					checkOptional = false;
					continue;
				}

				// If we can no longer initialize anything
				if (!changed)
					return !GetUninitializedModules().Any();
			}
		}

		private IEnumerable<InsightModule> GetInitializedModules() {
			return _modules
				.Where(m => _initializedModules.Contains(m.Key))
				.Select(m => m.Value)
				.ToList();
		}

		private IEnumerable<InsightModule> GetUninitializedModules() {
			return _modules
				.Where(m => !_initializedModules.Contains(m.Key))
				.Select(m => m.Value)
				.ToList();
		}

		public T GetModule<T>() where T : InsightModule {
			_modules.TryGetValue(typeof(T), out var module);

			if (module == null) {
				// Try to find an assignable module
				module = _modules.Values.FirstOrDefault(m => m is T);
			}

			return module as T;
		}
	}
}