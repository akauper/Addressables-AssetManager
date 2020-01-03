# Addressables-AssetManager
Pre-loading, Synchronicity, and Pooling for Unity Addressables


This project attempts to extend the Unity Addressables system for other common use cases.
The main file to look at is AssetManager.cs

## Getting Started

Open the project in Unity 2019.3. Alternatively, copy the Assets/Asset Manager folder into your project
Requires Addressable 1.5.0

## Use Cases
* Track loaded/unloaded assets.
* Load/Unload assets by label (Addressable Group labels, not gameobject labels)
* Track instantiated assets and destroy all instantiated assets by AssetReference.RuntimeKey
* Load/Instantiate Components directly. The default Addressable system currently only allows GameObjects or assets which derive from UnityEngine.Object (AudioClip, etc).
* Synchronous loading and instantiation
* Pooling of instantiated AssetReferences


## Code

* Load
  * AssetManager.TryGetOrLoadObjectAsync<TObjectType>(AssetReference aRef, out AsyncOperationHandle<TObjectType> handle)
    * Tries to get an already loaded UnityEngine.Object (not a component) and, if it doesnt exist, loads it.
    * Returns true if the object was already loaded, false otherwise.
    * handle contains an AsyncOperationHandle with Result as the requested object.
  * AssetManager.TryGetOrLoadComponentAsync<TComponentType>(AssetReference aRef, out AsyncOperationHandle<TComponentType> handle)
    * Same as above but used for components.
    * the handle result in this case will contain a reference to the requested Component attached to the GameObject.
  * AssetManager.TryGetObjectSync<TObjectType>(AssetReference aRef, out TObjectType result)
    * Tries to synchronously get a reference to an already loaded object.
    * Returns true if the object was already loaded, false otherwise.
    * If false, 'result' will be null.
    * This works similarly to TryGetOrLoadObjectAsync, but wont load the object if it wasn't already loaded.
  * AssetManager.TryGetComponentSync<TComponentType>(AssetReference aRef, out TComponentType result)
    * Same as above for components
  * AssetManager.LoadAssetsByLabelAsync(string label)
    * Loads all assets with the given Addressables label
  
* Unload
  * AssetManager.Unload(AssetReference aRef)
    * Unloads the given AssetReference
  * AssetManager.UnloadByLabel(string label)
    * Unloads all assets with the given Addressables label
    
* Get
  * AssetManager.IsLoaded(AssetReference aRef)
    * Returns true/false if the given AssetReference has already been loaded.
  * AssetManager.IsLoading(AssetReference aRef)
    * Returns true/false if the given AssetReference is currently loading.
  * AssetManager.IsInstantiated(AssetReference aRef)
    * Returns true/false if the given AssetReference has been instantiated.
  * AssetManager.InstantiatedCount(AssetReference aRef)
    * returns the total number of instantiated objects with the given AssetReference.

* Instantiate
  * AssetManager.TryInstantiateOrLoadAsync(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent, out AsyncOperationHandle<GameObject> handle)
    * Tries to instantiate the given AssetReference if it is already loaded. Otherwise, loads and instantiates it.
    * Returns true if the object was already loaded, false otherwise.
    * Important: The 'handle' field will contain a AsyncOperationHandle with a 'result' field containing THE INSTANTIATED OBJECT (not the loaded object).
  * AssetManager.TryInstantiateOrLoadAsync<TComponentType>(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent, out AsyncOperationHandle<TComponentType> handle)
    * Same as above but for components, rather than GameObjects.
  * AssetManager.TryInstantiateMultiOrLoadAsync(AssetReference aRef, int count, Vector3 position, Quaternion rotation, Transform parent, out AsyncOperationHandle<List<GameObject>> handle)
    * Same as TryInstantiateOrLoadAsync but instantiates multiple copies (determined by the count field).
  * AssetManager.TryInstantiateMultiOrLoadAsync<TComponentType>(AssetReference aRef, int count, Vector3 position, Quaternion rotation, Transform parent, out AsyncOperationHandle<List<TComponentType>> handle)
    * Same as above but for components, rather than GameObjects
  * Assetmanager.TryInstantiateSync(AssetReference aRef, Vector3 position, Quaternion rotation, Transform parent, out GameObject result)
    * Tries to instantiate the given AssetReference if it is already loaded.
    * Returns true if it was already loaded, false otherwise.
    * Similar to TryInstantiateOrLoadAsync but will not load the object if it was not already loaded.

* Destroy
  * AssetManager.DestroyAllInstances(AssetReference aRef)
    * Destroys all instantiated instances of the given AssetReference.

## Examples
Please see the examples folder included in the Unity project.
