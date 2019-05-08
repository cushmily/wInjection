using UnityEngine;

namespace wLib.Injection
{
    public abstract class MonoModule : MonoBehaviour, IModule
    {
        public IDependencyContainer Container { get; private set; }

        public void SetContainer(IDependencyContainer container)
        {
            Container = container;
        }

        public abstract void RegisterBindings();

        public abstract void Dispose();
    }
}