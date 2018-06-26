namespace wLib.Injection
{
    public interface IContext
    {
        T Create<T>();
        
        //TODO Add hirerachy contexts
//        IContext Parent { get; }
    }
}