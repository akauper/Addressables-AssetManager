/// <copyright file="LocalPoolObject.cs" company="Skywatch Games">
/// Copyright (c) Skywatch Games 2017. All rights reserved.
/// </copyright> 
/// <author>kaupe</author>
/// <date>2/4/2017 1:45:13 AM</date>

using System;
using System.Linq;
using UnityEngine;


namespace Skywatch.AssetManagement.Pooling
{
    public sealed class PoolObject : MonoBehaviour, IPoolObject
    {
        public event DelegateAwakeFromPool AwakeFromPoolEvent;
        public event DelegateReturnToPool ReturnToPoolEvent;

        [NonSerialized] Behaviour[] _behaviours;
        [NonSerialized] bool[] _behaviourStates;

        [NonSerialized] Collider[] _colliders;
        [NonSerialized] bool[] _colliderStates;

        [NonSerialized] Renderer[] _renderers;
        [NonSerialized] bool[] _rendererStates;

        [NonSerialized] ParticleSystem[] _particleSystems;

        [NonSerialized] AudioSource[] _audioSources;
        [NonSerialized] bool[] _audioSourceStates;

        [NonSerialized] bool[] _gameObjectStates;

        [NonSerialized] Rigidbody _rigidbody;
        [NonSerialized] bool _wasKinematic;

        public bool inPool { get; private set; }
        public IPool myPool { get; set; }

        bool _initialized;
        public bool initialized => _initialized;
        
        

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            if (_initialized) return;

            _behaviours = GetComponents<Behaviour>().Where(x => x != this).ToArray();
            _behaviourStates = new bool[_behaviours.Length];
            for (int i = 0; i < _behaviourStates.Length; i++)
                _behaviourStates[i] = _behaviours[i].enabled;

            _colliders = GetComponents<Collider>();
            _colliderStates = new bool[_colliders.Length];
            for (int i = 0; i < _colliderStates.Length; i++)
                _colliderStates[i] = _colliders[i].enabled;

            _renderers = GetComponents<Renderer>();
            _rendererStates = new bool[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _rendererStates[i] = _renderers[i].enabled;

            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            _audioSources = GetComponentsInChildren<AudioSource>(true);
            _audioSourceStates = new bool[_audioSources.Length];
            for (int i = 0; i < _audioSourceStates.Length; i++)
                _audioSourceStates[i] = _audioSources[i].enabled;

            _gameObjectStates = new bool[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                _gameObjectStates[i] = transform.GetChild(i).gameObject.activeSelf;

            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody)
                _wasKinematic = _rigidbody.isKinematic;

            var poolBehaviours = GetComponents<PoolBehaviour>();
            poolBehaviours.ForEach(x => x.SetPoolObject(this));
            
            _initialized = true;
        }

        public void AwakeFromPool()
        {
            for (int i = 0; i < _behaviours.Length; i++)
            {
                if (_behaviourStates[i])
                    _behaviours[i].enabled = true;
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliderStates[i])
                    _colliders[i].enabled = true;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_rendererStates[i])
                    _renderers[i].enabled = true;
            }

            //Maybe change to GetComponentsInChildren<Transform>(true); ??
            for (int i = 0; i < transform.childCount; i++)
            {
                if (i >= 0 && i < _gameObjectStates.Length && !_gameObjectStates[i])
                    continue;

                transform.GetChild(i).gameObject.SetActive(true);
            }

            foreach (var ps in _particleSystems)
            {
                ps.Stop(true);
                ps.Play(true);
            }

            for (int i = 0; i < _audioSources.Length; i++)
            {
                if (!_audioSourceStates[i]) continue;
                _audioSources[i].enabled = true;
                _audioSources[i].Stop();
                _audioSources[i].Play();
            }

            if (_rigidbody)
            {
                _rigidbody.isKinematic = _wasKinematic;
                _rigidbody.freezeRotation = false;
            }

            inPool = false;

            AwakeFromPoolEvent?.Invoke();
        }

        public void ReturnToPool()
        {
            if(!_initialized)
                Initialize();

            if (myPool != null)
                myPool.PoolObjectReturned(this);
            else
                Debug.LogError("I DONT HAVE A POOL!", this);

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            _behaviours.ForEach(x => x.enabled = false);
            _colliders.ForEach(x => x.enabled = false);
            _renderers.ForEach(x => x.enabled = false);

            //Maybe change to GetComponentsInChildren<Transform>(true); ??
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }

            _particleSystems.ForEach(x => x.Stop());
            _audioSources.ForEach(x => x.Stop());

            if (_rigidbody)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.rotation = Quaternion.identity;
                _rigidbody.freezeRotation = true;
                _rigidbody.Sleep();
            }

            inPool = true;

            ReturnToPoolEvent?.Invoke();
        }
    }
}