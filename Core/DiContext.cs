namespace wLib.Injection
{
    public class DiContext : IContext
    {
        private readonly DiContainer _container = new DiContainer();

        public T Create<T>()
        {
            return _container.Resolve<T>();
        }

//        private static readonly Dictionary<string, DiContainer> _caches = new Dictionary<string, DiContainer>();

//        public static DiContainer Create(string id)
//        {
//            var container = new DiContainer();
//            _caches.Add(id, container);
//            return container;
//        }
//
//        public static DiContainer Get(string id)
//        {
//            DiContainer result;
//            if (!_caches.TryGetValue(id, out result)) { Debug.LogFormat("DiContainer with id [{0}] not found."); }
//
//            return result;
//        }
    }
}