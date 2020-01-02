using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace Skywatch.AssetManagement
{
    /// <summary>
    /// Handles all loading, unloading, instantiating, and destroying of AssetReferences and their associated Objects.
    /// </summary>
    public static class AssetManager
    {
        const string _baseErr = "<color=#ffa500>" + nameof(AssetManager) + " Error:</color> ";

        public delegate void DelegateAssetLoaded(object key, AsyncOperationHandle handle);
        public static event DelegateAssetLoaded OnAssetLoaded;

        public delegate void DelegateAssetUnloaded(object runtimeKey);
        public static event DelegateAssetUnloaded OnAssetUnloaded;
        
        static readonly Dictionary<object, AsyncOperationHandle> _loadingAssets = new Dictionary<object, AsyncOperationHandle>(20);
        static readonly Dictionary<object, AsyncOperationHandle> _loadedAssets = new Dictionary<object, AsyncOperationHandle>(100);
        public static IReadOnlyList<object> LoadedAssets => _loadedAssets.Values.Select(x => x.Result).ToList();
        static readonly Dictionary<object, List<GameObject>> _instantiatedObjects = new Dictionary<object, List<GameObject>>(10);

        public static int loadedAssetsCount => _loadedAssets.Count;
        public static int loadingAssetsCount => _loadingAssets.Count;
        public static int instantiatedAssetsCount => _instantiatedObjects.Values.SelectMany(x => x).Count();

        #region Get
        public static bool IsLoaded(AssetReference aRef)
        {
            return _loadedAssets.ContainsKey(aRef.RuntimeKey);
        }
        static bool IsLoaded(object key)
        {
            return _loadedAssets.ContainsKey(key);
        }
        public static bool IsLoading(AssetReference aRef)
        {
            return _loadingAssets.ContainsKey(aRef.RuntimeKey);
        }
        static bool IsLoading(object key)
        {
            return _loadingAssets.ContainsKey(key);
        }
        public static bool IsInstantiated(AssetReference aRef)
        {
            return _instantiatedObjects.ContainsKey(aRef.RuntimeKey);
        }
        public static bool IsInstantiated(object key)
        {
            return _instantiatedObjects.ContainsKey(key);
        }
        public static int InstantiatedCount(AssetReference aRef)
        {
            return !IsInstantiated(aRef) ? 0 : _instantiatedObjects[aRef.RuntimeKey].Count;
        }
        #endregion
        
        #region Load/Unload
        /// <summary>
        /// DO NOT USE FOR <see cref="Component"/>s. Call <see cref="TryGetOrLoadComponentAsync{TComponentType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TComponentType})"/>
        ///
        /// Tries to get an already loaded <see cref="UnityEngine.Object"/> of type <see cref="TObjectType"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReference"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="TObjectType">The type of NON-COMPONENT object to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        public static bool TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef, out AsyncOperationHandle<TObjectType> handle) where TObjectType : Object
        {
            CheckRuntimeKey(aRef);

            var key = aRef.RuntimeKey;

            if (_loadedAssets.ContainsKey(key))
            {
                try
                {
                    handle = _loadedAssets[key].Convert<TObjectType>();
                }
                catch
                {
                    handle = Addressables.ResourceManager.CreateCompletedOperation(_loadedAssets[key].Result as TObjectType, string.Empty);
                }

                return true;
            }


            if (_loadingAssets.ContainsKey(key))
            {
                try
                {
                    handle = _loadingAssets[key].Convert<TObjectType>();
                }
                catch
                {
                    handle = Addressables.ResourceManager.CreateChainOperation(_loadingAssets[key], chainOp => Addressables.ResourceManager.CreateCompletedOperation(chainOp.Result as TObjectType, string.Empty));
                }
                return false;
            }
            

            handle = Addressables.LoadAssetAsync<TObjectType>(aRef);
            
            _loadingAssets.Add(key, handle);

            handle.Completed += op2 =>
            {
                _loadedAssets.Add(key, op2);
                _loadingAssets.Remove(key);
                
                OnAssetLoaded?.Invoke(key, op2);
            };
            
            return false;
        }
        /// <summary>
        /// DO NOT USE FOR <see cref="Component"/>s. Call <see cref="TryGetOrLoadComponentAsync{TComponentType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TComponentType})"/>
        ///
        /// Tries to get an already loaded <see cref="UnityEngine.Object"/> of type <see cref="TObjectType"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReferenceT{TObject}"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="TObjectType">The type of NON-COMPONENT object to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        public static bool TryGetOrLoadObjectAsync<TObjectType>(AssetReferenceT<TObjectType> aRef, out AsyncOperationHandle<TObjectType> handle) where TObjectType : Object
        {
            return TryGetOrLoadObjectAsync(aRef as AssetReference, out handle);
        }
        
        /// <summary>
        /// DO NOT USE FOR <see cref="UnityEngine.Object"/>s. Call <see cref="TryGetOrLoadObjectAsync{TObjectType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TObjectType})"/>
        ///
        /// Tries to get an already loaded <see cref="Component"/> of type <see cref="TComponentType"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReference"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="TComponentType">The type of Component to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        public static bool TryGetOrLoadComponentAsync<TComponentType>(AssetReference aRef, out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            CheckRuntimeKey(aRef);

            var key = aRef.RuntimeKey;

            if (_loadedAssets.ContainsKey(key))
            {
                handle = ConvertHandleToComponent<TComponentType>(_loadedAssets[key]);
                return true;
            }


            if (_loadingAssets.ContainsKey(key))
            {
                handle = Addressables.ResourceManager.CreateChainOperation(_loadingAssets[key], ConvertHandleToComponent<TComponentType>);
                return false;
            }
            

            var op = Addressables.LoadAssetAsync<GameObject>(aRef);
            
            _loadingAssets.Add(key, op);

            op.Completed += op2 =>
            {
                _loadedAssets.Add(key, op2);
                _loadingAssets.Remove(key);

                OnAssetLoaded?.Invoke(key, op2);
            };

            handle = Addressables.ResourceManager.CreateChainOperation<TComponentType, GameObject>(op, chainOp =>
            {
                var go = chainOp.Result;
                var comp = go.GetComponent<TComponentType>();
                return Addressables.ResourceManager.CreateCompletedOperation(comp, string.Empty);
            });
            return false;
        }
        /// <summary>
        /// DO NOT USE FOR <see cref="UnityEngine.Object"/>s. Call <see cref="TryGetOrLoadObjectAsync{TObjectType}(UnityEngine.AddressableAssets.AssetReference,out UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TObjectType})"/>
        ///
        /// Tries to get an already loaded <see cref="Component"/> of type <see cref="TComponentType"/>.
        /// Returns <value>true</value> if the object was loaded and sets <paramref name="handle"/> to the completed <see cref="AsyncOperationHandle{TObject}"/>
        /// If the object was not loaded returns <value>false</value>, loads the object and sets <paramref name="handle"/> to the un-completed <see cref="AsyncOperationHandle{TObject}"/>
        /// </summary>
        /// <param name="aRef">The <see cref="AssetReferenceT{TObject}"/> to load.</param>
        /// <param name="handle">The loading or completed <see cref="AsyncOperationHandle{TObject}"/></param>
        /// <typeparam name="TComponentType">The type of Component to load.</typeparam>
        /// <returns><value>true</value> if the object has already been loaded, false otherwise.</returns>
        public static bool TryGetOrLoadComponentAsync<TComponentType>(AssetReferenceT<TComponentType> aRef, out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            return TryGetOrLoadComponentAsync(aRef as AssetReference, out handle);
        }
        
        public static bool TryGetObjectSync<TObjectType>(AssetReference aRef, out TObjectType result) where TObjectType : Object
        {
            CheckRuntimeKey(aRef);
            var key = aRef.RuntimeKey;
            
            if (_loadedAssets.ContainsKey(key))
            {
                result = _loadedAssets[key].Convert<TObjectType>().Result;
                return true;
            }

            result = null;
            return false;
        }
        public static bool TryGetObjectSync<TObjectType>(AssetReferenceT<TObjectType> aRef, out TObjectType result) where TObjectType : Object
        {
            return TryGetObjectSync(aRef as AssetReference, out result);
        }

        public static bool TryGetComponentSync<TComponentType>(AssetReference aRef, out TComponentType result) where TComponentType : Component
        {
            CheckRuntimeKey(aRef);
            var key = aRef.RuntimeKey;

            if (_loadedAssets.ContainsKey(key))
            {
                var handle = _loadedAssets[key];
                result = null;
                var go = handle.Result as GameObject;
                if (!go)
                    throw new ConversionException($"Cannot convert {nameof(handle.Result)} to {nameof(GameObject)}.");
                result = go.GetComponent<TComponentType>();
                if(!result)
                    throw new ConversionException($"Cannot {nameof(go.GetComponent)} of Type {typeof(TComponentType)}.");
                return true;
            }

            result = null;
            return false;
        }
        public static bool TryGetComponentSync<TComponentType>(AssetReferenceT<TComponentType> aRef, out TComponentType result) where TComponentType : Component
        {
            return TryGetComponentSync(aRef as AssetReference, out result);
        }
        
        public static AsyncOperationHandle<List<AsyncOperationHandle<Object>>> LoadAssetsByLabelAsync(string label)
        {
            var handle = Addressables.ResourceManager.StartOperation(new LoadAssetsByLabelOperation(_loadedAssets, _loadingAssets, label, AssetLoadedCallback), default);
            return handle;
        }
        static void AssetLoadedCallback(object key, AsyncOperationHandle handle)
        {
            OnAssetLoaded?.Invoke(key, handle);
        }

        /// <summary>
        /// Unloads the given <paramref name="aRef"/> and calls <see cref="DestroyAllInstances"/> if it was Instantiated.
        /// </summary>
        /// <param name="aRef"></param>
        /// <returns></returns>
        public static void Unload(AssetReference aRef)
        {
            CheckRuntimeKey(aRef);

            var key = aRef.RuntimeKey;

            Unload(key);
        }
        static void Unload(object key)
        {
            CheckRuntimeKey(key);
            
            AsyncOperationHandle handle;
            if (_loadingAssets.ContainsKey(key))
            {
                handle = _loadingAssets[key];
                _loadingAssets.Remove(key);
            }
            else if (_loadedAssets.ContainsKey(key))
            {
                handle = _loadedAssets[key];
                _loadedAssets.Remove(key);
            }
            else
            {
                Debug.LogWarning($"{_baseErr}Cannot {nameof(Unload)} RuntimeKey '{key}': It is not loading or loaded.");
                return;
            }

            if (IsInstantiated(key))
                DestroyAllInstances(key);

            Addressables.Release(handle);
            
            OnAssetUnloaded?.Invoke(key);
        }

        public static void UnloadByLabel(string label)
        {
            if (string.IsNullOrEmpty(label) || string.IsNullOrWhiteSpace(label))
            {
                Debug.LogError("Label cannot be empty.");
                return;
            }
            
            var locationsHandle = Addressables.LoadResourceLocationsAsync(label);
            locationsHandle.Completed += op =>
            {
                if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Cannot Unload by label '{label}'");
                    return;
                }
                var keys = GetKeysFromLocations(op.Result);
                foreach (var key in keys)
                {
                    if (IsLoaded(key) || IsLoading(key))
                        Unload(key);
                }
            };

        }
        
        #endregion

        #region Instantiation

        public static bool TryInstantiateOrLoadAsync(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent,
            out AsyncOperationHandle<GameObject> handle)
        {
            if (TryGetOrLoadObjectAsync(aRef, out AsyncOperationHandle<GameObject> loadHandle))
            {
                var instance = InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                handle = Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
                return true;
            }

            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<GameObject>(null, $"Load Operation was invalid: {loadHandle}.");
                return false;
            }

            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var instance = InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                return Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
            });
            return false;
        }
        //Returns an AsyncOperationHandle<TComponentType> with the result set to an instantiated Component.
        public static bool TryInstantiateOrLoadAsync<TComponentType>(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent,
            out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            if (TryGetOrLoadComponentAsync(aRef, out AsyncOperationHandle<TComponentType> loadHandle))
            {
                var instance = InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                handle = Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
                return true;
            }

            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<TComponentType>(null, $"Load Operation was invalid: {loadHandle}.");
                return false;
            }
            
            //Create a chain that waits for loadHandle to finish, then instantiates and returns the instance GO.
            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var instance = InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                return Addressables.ResourceManager.CreateCompletedOperation(instance, string.Empty);
            });
            return false;
        }
        public static bool TryInstantiateOrLoadAsync<TComponentType>(AssetReferenceT<TComponentType> aRef, Vector3 position, Quaternion rotation, Transform parent,
            out AsyncOperationHandle<TComponentType> handle) where TComponentType : Component
        {
            return TryInstantiateOrLoadAsync(aRef as AssetReference, position, rotation, parent, out handle);
        }

        public static bool TryInstantiateMultiOrLoadAsync(AssetReference aRef, int count, Vector3 position, Quaternion rotation, Transform parent,
            out AsyncOperationHandle<List<GameObject>> handle)
        {
            if (TryGetOrLoadObjectAsync(aRef, out AsyncOperationHandle<GameObject> loadHandle))
            {
                var list = new List<GameObject>(count);
                for (int i = 0; i < count; i++)
                {
                    var instance = InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                    list.Add(instance);
                }

                handle = Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
                return true;
            }

            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<List<GameObject>>(null, $"Load Operation was invalid: {loadHandle}.");
                return false;
            }

            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var list = new List<GameObject>(count);
                for (int i = 0; i < count; i++)
                {
                    var instance = InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                    list.Add(instance);
                }

                return Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
            });
            return false;
        }
        public static bool TryInstantiateMultiOrLoadAsync<TComponentType>(AssetReference aRef, int count, Vector3 position, Quaternion rotation, Transform parent,
            out AsyncOperationHandle<List<TComponentType>> handle) where TComponentType : Component
        {
            if (TryGetOrLoadComponentAsync(aRef, out AsyncOperationHandle<TComponentType> loadHandle))
            {
                var list = new List<TComponentType>(count);
                for (int i = 0; i < count; i++)
                {
                    var instance = InstantiateInternal(aRef, loadHandle.Result, position, rotation, parent);
                    list.Add(instance);
                }

                handle = Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
                return true;
            }
            
            if (!loadHandle.IsValid())
            {
                Debug.LogError($"Load Operation was invalid: {loadHandle}.");
                handle = Addressables.ResourceManager.CreateCompletedOperation<List<TComponentType>>(null, $"Load Operation was invalid: {loadHandle}.");
                return false;
            }

            handle = Addressables.ResourceManager.CreateChainOperation(loadHandle, chainOp =>
            {
                var list = new List<TComponentType>(count);
                for (int i = 0; i < count; i++)
                {
                    var instance = InstantiateInternal(aRef, chainOp.Result, position, rotation, parent);
                    list.Add(instance);
                }

                return Addressables.ResourceManager.CreateCompletedOperation(list, string.Empty);
            });
            return false;
        }
        public static bool TryInstantiateMultiOrLoadAsync<TComponentType>(AssetReferenceT<TComponentType> aRef, int count, Vector3 position, Quaternion rotation,
            Transform parent, out AsyncOperationHandle<List<TComponentType>> handle) where TComponentType : Component
        {
            return TryInstantiateMultiOrLoadAsync(aRef as AssetReference, count, position, rotation, parent, out handle);
        }

        public static bool TryInstantiateSync(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent, out GameObject result)
        {
            if (!TryGetObjectSync(aRef, out GameObject loadResult))
            {
                result = null;
                return false;
            }

            result = InstantiateInternal(aRef, loadResult, position, rotation, parent);
            return true;
        }
        public static bool TryInstantiateSync<TComponentType>(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent,
            out TComponentType result) where TComponentType : Component
        {
            if (!TryGetComponentSync(aRef, out TComponentType loadResult))
            {
                result = null;
                return false;
            }

            result = InstantiateInternal(aRef, loadResult, position, rotation, parent);
            return true;
        }
        public static bool TryInstantiateSync<TComponentType>(AssetReferenceT<TComponentType> aRef, Vector3 position, Quaternion rotation, Transform parent,
            out TComponentType result) where TComponentType : Component
        {
            return TryInstantiateSync(aRef as AssetReference, position, rotation, parent, out result);
        }

        public static bool TryInstantiateMultiSync(AssetReference aRef, int count, Vector3 position, Quaternion rotation, Transform parent,
            out List<GameObject> result)
        {
            if (!TryGetObjectSync(aRef, out GameObject loadResult))
            {
                result = null;
                return false;
            }

            var list = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                var instance = InstantiateInternal(aRef, loadResult, position, rotation, parent);
                list.Add(instance);
            }

            result = list;
            return true;
        }
        public static bool TryInstantiateMultiSync<TComponentType>(AssetReference aRef, int count, Vector3 position, Quaternion rotation, Transform parent,
            out List<TComponentType> result) where TComponentType : Component
        {
            if (!TryGetComponentSync(aRef, out TComponentType loadResult))
            {
                result = null;
                return false;
            }

            var list = new List<TComponentType>(count);
            for (int i = 0; i < count; i++)
            {
                var instance = InstantiateInternal(aRef, loadResult, position, rotation, parent);
                list.Add(instance);
            }

            result = list;
            return true;
        }
        public static bool TryInstantiateMultiSync<TComponentType>(AssetReferenceT<TComponentType> aRef, int count, Vector3 position, Quaternion rotation, Transform parent,
            out List<TComponentType> result) where TComponentType : Component
        {
            return TryInstantiateMultiSync(aRef as AssetReference, count, position, rotation, parent, out result);
        }

        static TComponentType InstantiateInternal<TComponentType>(AssetReference aRef, TComponentType loadedAsset, Vector3 position, Quaternion rotation, Transform parent)
            where TComponentType : Component
        {
            var key = aRef.RuntimeKey;
            
            var instance = Object.Instantiate(loadedAsset, position, rotation, parent);
            if(!instance)
                throw new NullReferenceException($"Instantiated Object of type '{typeof(TComponentType)}' is null.");
            
            var monoTracker = instance.gameObject.AddComponent<MonoTracker>();
            monoTracker.key = key;
            monoTracker.OnDestroyed += TrackerDestroyed;

            if(!_instantiatedObjects.ContainsKey(key))
                _instantiatedObjects.Add(key, new List<GameObject>(20));
            _instantiatedObjects[key].Add(instance.gameObject);
            return instance;
        }
        static GameObject InstantiateInternal(AssetReference aRef, GameObject loadedAsset, Vector3 position, Quaternion rotation, Transform parent)
        {
            var key = aRef.RuntimeKey;
            
            var instance = Object.Instantiate(loadedAsset, position, rotation, parent);
            if(!instance)
                throw new NullReferenceException($"Instantiated Object of type '{typeof(GameObject)}' is null.");
            
            var monoTracker = instance.gameObject.AddComponent<MonoTracker>();
            monoTracker.key = key;
            monoTracker.OnDestroyed += TrackerDestroyed;
            
            if(!_instantiatedObjects.ContainsKey(key))
                _instantiatedObjects.Add(key, new List<GameObject>(20));
            _instantiatedObjects[key].Add(instance);
            return instance;
        }
        
        static void TrackerDestroyed(MonoTracker tracker)
        {
            if (_instantiatedObjects.TryGetValue(tracker.key, out var list))
                list.Remove(tracker.gameObject);
        }

        /// <summary>
        /// Destroys all instantiated instances of <paramref name="aRef"/>
        /// </summary>
        public static void DestroyAllInstances(AssetReference aRef)
        {
            CheckRuntimeKey(aRef);

            if (!_instantiatedObjects.ContainsKey(aRef.RuntimeKey))
            {
                Debug.LogWarning($"{nameof(AssetReference)} '{aRef}' has not been instantiated. 0 Instances destroyed.");
                return;
            }

            var key = aRef.RuntimeKey;

            DestroyAllInstances(key);
        }
        static void DestroyAllInstances(object key)
        {
            var instanceList = _instantiatedObjects[key];
            for (int i = instanceList.Count - 1; i >= 0; i--)
            {
                DestroyInternal(instanceList[i]);
            }
            _instantiatedObjects[key].Clear();
            _instantiatedObjects.Remove(key);
        }
        static void DestroyInternal(Object obj)
        {
            var c = obj as Component;
            if (c)
                Object.Destroy(c.gameObject);
            else
            {
                var go = obj as GameObject;
                if(go)
                    Object.Destroy(go);
            }
        }
        #endregion
        
        #region Utilities
        static void CheckRuntimeKey(AssetReference aRef)
        {
            if (!aRef.RuntimeKeyIsValid())
            {
                //Debug.Log(aRef.RuntimeKey);
                throw new InvalidKeyException($"{_baseErr}{nameof(aRef.RuntimeKey)} is not valid for '{aRef}'.");
            }
        }
        static bool CheckRuntimeKey(object key)
        {
            return Guid.TryParse(key.ToString(), out var result);
        }
        
        static AsyncOperationHandle<TComponentType> ConvertHandleToComponent<TComponentType>(AsyncOperationHandle handle) where TComponentType : Component
        {
            GameObject go = handle.Result as GameObject;
            if (!go)
                throw new ConversionException($"Cannot convert {nameof(handle.Result)} to {nameof(GameObject)}.");
            TComponentType comp = go.GetComponent<TComponentType>();
            if (!comp)
                throw new ConversionException($"Cannot {nameof(go.GetComponent)} of Type {typeof(TComponentType)}.");
            var result = Addressables.ResourceManager.CreateCompletedOperation(comp, string.Empty);

            return result;
        }
        
        static List<object> GetKeysFromLocations(IList<IResourceLocation> locations)
        {
            List<object> keys = new List<object>(locations.Count);
            
            foreach (var locator in Addressables.ResourceLocators)
            {
                foreach (var key in locator.Keys)
                {
                    bool isGUID = Guid.TryParse(key.ToString(), out var guid);
                    if (!isGUID)
                        continue;
                    
                    if (!TryGetKeyLocationID(locator, key, out var keyLocationID))
                        continue;

                    var locationMatched = locations.Select(x => x.InternalId).ToList().Exists(x => x == keyLocationID);
                    if (!locationMatched)
                        continue;
                    keys.Add(key);
                }
            }

            return keys;
        }
        static bool TryGetKeyLocationID(IResourceLocator locator, object key, out string internalID)
        {
            internalID = string.Empty;
            var hasLocation = locator.Locate(key, typeof(Object), out var keyLocations);
            if (!hasLocation)
                return false;
            if (keyLocations.Count == 0)
                return false;
            if (keyLocations.Count > 1)
                return false;

            internalID = keyLocations[0].InternalId;
            return true;
        }
        #endregion
    }
}

