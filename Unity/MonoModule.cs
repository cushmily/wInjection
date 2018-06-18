using UnityEngine;

namespace wLib.Injection
{
    public abstract class MonoModule : MonoBehaviour, IModule
    {
        public DiContainer Container { get; private set; }

        public void SetContainer(DiContainer container)
        {
            Container = container;
        }

        public abstract void ModuleBindings();
    }
}