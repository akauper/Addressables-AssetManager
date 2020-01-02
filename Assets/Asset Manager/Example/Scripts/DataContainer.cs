using System.Collections;
using System.Collections.Generic;
using Skywatch.AssetManagement;
using Skywatch.AssetManagement.Pooling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(menuName = "Skywatch/Example/Data Container")]
public class DataContainer : ScriptableObject
{
    [SerializeField] List<AssetReference> _audioClips;
    [SerializeField] List<AssetReference> _particleSystems;



    public void PlayRandomAudioClip(AudioSource source)
    {
        if (!Application.isPlaying)
            return;
        
        var reference = _audioClips[UnityEngine.Random.Range(0, _audioClips.Count)];

        if (AssetManager.TryGetOrLoadObjectAsync(reference, out AsyncOperationHandle<AudioClip> handle))
        {
            //The clip was already loaded
            PlayClip(source, handle.Result);
        }
        else
        {
            //The clip was not loaded. Wait for it to load, then play.
            handle.Completed += op =>
            {
                PlayClip(source, op.Result);
            };
        }
            
    }

    void PlayClip(AudioSource source, AudioClip clip)
    {
        source.PlayOneShot(clip);
    }

    public void InstantiateRandomParticleSystem(bool usePooling)
    {
        if (!Application.isPlaying)
            return;

        var reference = _particleSystems[UnityEngine.Random.Range(0, _particleSystems.Count)];

        if (usePooling)
        {
            var pool = AssetReferencePool<ParticleSystem>.GetOrCreate(reference, callback =>
            {
                callback.gameObject.AddComponent<EffectRecycler>();
            });
            
            if (pool.TryTake(Vector3.zero, Quaternion.identity, null, out var handle))
            {
                //The asset is loaded and the pool is ready.
                //handle.Result contains a reference to the instantiated instance
            }
            else
            {
                //Pool waits for asset to load, then calls Take.
                //handle.Result is not yet ready, but will contain a reference to the instantiated instance once it is complete.
            }
        }
        else
        {
            if (AssetManager.TryInstantiateOrLoadAsync(reference, Vector3.zero, Quaternion.identity, null, out AsyncOperationHandle<ParticleSystem> handle))
            {
                //The particle system has already been loaded.
                Destroy(handle.Result.gameObject, 5f);
            }
            else
            {
                //The particle system was not yet loaded.
                handle.Completed += op =>
                {
                    Destroy(op.Result.gameObject, 5f);
                };
            }
        }
    }

    public void DestroyAllInstances()
    {
        foreach (var assetReference in _particleSystems)
        {
            if (AssetManager.IsInstantiated(assetReference))
                AssetManager.DestroyAllInstances(assetReference);
        }
    }

    public void UnloadAllAssets()
    {
        foreach (var assetReference in _audioClips)
        {
            if (AssetManager.IsLoaded(assetReference) || AssetManager.IsLoading(assetReference))
                AssetManager.Unload(assetReference);
        }

        foreach (var assetReference in _particleSystems)
        {
            if (AssetManager.IsLoaded(assetReference) || AssetManager.IsLoading(assetReference))
                AssetManager.Unload(assetReference);
        }
    }
}
