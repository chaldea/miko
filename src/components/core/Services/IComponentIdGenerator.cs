namespace Miko
{
    public interface IComponentIdGenerator
    {
        string Generate(MikoDomComponentBase component);
    }
}
