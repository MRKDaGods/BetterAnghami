namespace MRK
{
    public class ObjectReference<T>(T? initial = default)
    {
        public T? Value { get; set; } = initial;
    }
}
