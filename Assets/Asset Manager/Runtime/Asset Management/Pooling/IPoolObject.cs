
namespace Skywatch.AssetManagement.Pooling
{
    public delegate void DelegateAwakeFromPool();
    public delegate void DelegateReturnToPool();

    public interface IPoolObject
    {
        event DelegateAwakeFromPool AwakeFromPoolEvent;
        event DelegateReturnToPool ReturnToPoolEvent;

        bool inPool { get; }
        IPool myPool { get; }

        void AwakeFromPool();
        void ReturnToPool();
    }
}
