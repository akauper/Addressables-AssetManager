
using Skywatch.AssetManagement.Pooling;
using UnityEngine;

public class EffectRecycler : MonoBehaviour
{
    ParticleSystem _particleSystem;
    PoolObject _poolObject;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _poolObject = GetComponent<PoolObject>();
        if (!_particleSystem || !_poolObject)
        {
            Destroy(this);
            return;
        }
    }

    void Update()
    {
        if (!_particleSystem.IsAlive(true))
        {
            _poolObject.ReturnToPool();
        }
    }
}
