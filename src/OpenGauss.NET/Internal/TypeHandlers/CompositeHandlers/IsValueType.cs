namespace OpenGauss.NET.Internal.TypeHandlers.CompositeHandlers
{
    static class IsValueType<T>
    {
        public static readonly bool Value = typeof(T).IsValueType;
    }
}
