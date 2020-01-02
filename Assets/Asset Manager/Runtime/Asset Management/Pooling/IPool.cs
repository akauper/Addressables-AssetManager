using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skywatch.AssetManagement.Pooling
{
    public interface IPool<in TPoolObjectType> where TPoolObjectType : IPoolObject
    {
        /// <summary>
        /// Allows a pool object to inform it's pool that is has been returned to the pool.
        /// </summary>
        /// <param name="poolObject">The pool object that has returned to the pool.</param>
        void PoolObjectReturned(TPoolObjectType poolObject);
    }

    public interface IPool
    {
        void PoolObjectReturned(PoolObject poolObject);
    }
}
