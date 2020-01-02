using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skywatch.AssetManagement.Pooling
{
    public class PoolBehaviour : MonoBehaviour
    {
        IPoolObject _poolObject;

        public void SetPoolObject(IPoolObject iPoolObject)
        {
            if (_poolObject != null)
            {
                _poolObject.AwakeFromPoolEvent -= OnAwakeFromPool;
                _poolObject.ReturnToPoolEvent -= OnReturnToPool;
                _poolObject = null;
            }

            _poolObject = iPoolObject;
            if (_poolObject != null)
            {
                _poolObject.AwakeFromPoolEvent += OnAwakeFromPool;
                _poolObject.ReturnToPoolEvent += OnReturnToPool;
            }
        }


        protected virtual void OnAwakeFromPool()
        {

        }

        protected virtual void OnReturnToPool()
        {

        }
    }
}
