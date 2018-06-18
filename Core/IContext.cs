namespace wLib.Injection
{
    public interface IContext
    {
        T Create<T>();
    }
}