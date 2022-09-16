using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.BackendMessages;

// ReSharper disable StaticMemberInGenericType
namespace OpenGauss.NET.Internal.TypeHandling
{
    delegate T ReadDelegate<T>(OpenGaussTypeHandler handler, OpenGaussReadBuffer buffer, int columnLength, FieldDescription? fieldDescription = null);
    delegate ValueTask<T> ReadAsyncDelegate<T>(OpenGaussTypeHandler handler, OpenGaussReadBuffer buffer, int columnLen, bool async, FieldDescription? fieldDescription = null);

    delegate int ValidateAndGetLengthDelegate<T>(OpenGaussTypeHandler handler, T value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter);
    delegate Task WriteAsyncDelegate<T>(OpenGaussTypeHandler handler, T value, OpenGaussWriteBuffer buffer, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default);

    static class NullableHandler<T>
    {
        public static readonly Type? UnderlyingType;
        public static readonly ReadDelegate<T> Read = null!;
        public static readonly ReadAsyncDelegate<T> ReadAsync = null!;
        public static readonly ValidateAndGetLengthDelegate<T> ValidateAndGetLength = null!;
        public static readonly WriteAsyncDelegate<T> WriteAsync = null!;

        public static bool Exists => UnderlyingType != null;

        static NullableHandler()
        {
            UnderlyingType = Nullable.GetUnderlyingType(typeof(T));

            if (UnderlyingType == null)
                return;

            Read = NullableHandler.CreateDelegate<ReadDelegate<T>>(UnderlyingType, NullableHandler.ReadMethod);
            ReadAsync = NullableHandler.CreateDelegate<ReadAsyncDelegate<T>>(UnderlyingType, NullableHandler.ReadAsyncMethod);
            ValidateAndGetLength = NullableHandler.CreateDelegate<ValidateAndGetLengthDelegate<T>>(UnderlyingType, NullableHandler.ValidateMethod);
            WriteAsync = NullableHandler.CreateDelegate<WriteAsyncDelegate<T>>(UnderlyingType, NullableHandler.WriteAsyncMethod);
        }
    }

    static class NullableHandler
    {
        internal static readonly MethodInfo ReadMethod = new ReadDelegate<int?>(Read<int>).Method.GetGenericMethodDefinition();
        internal static readonly MethodInfo ReadAsyncMethod = new ReadAsyncDelegate<int?>(ReadAsync<int>).Method.GetGenericMethodDefinition();
        internal static readonly MethodInfo ValidateMethod = new ValidateAndGetLengthDelegate<int?>(ValidateAndGetLength).Method.GetGenericMethodDefinition();
        internal static readonly MethodInfo WriteAsyncMethod = new WriteAsyncDelegate<int?>(WriteAsync).Method.GetGenericMethodDefinition();

        static T? Read<T>(OpenGaussTypeHandler handler, OpenGaussReadBuffer buffer, int columnLength, FieldDescription? fieldDescription)
            where T : struct
            => handler.Read<T>(buffer, columnLength, fieldDescription);

        static async ValueTask<T?> ReadAsync<T>(OpenGaussTypeHandler handler, OpenGaussReadBuffer buffer, int columnLength, bool async, FieldDescription? fieldDescription)
            where T : struct
            => await handler.Read<T>(buffer, columnLength, async, fieldDescription);

        static int ValidateAndGetLength<T>(OpenGaussTypeHandler handler, T? value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            where T : struct
            => value.HasValue ? handler.ValidateAndGetLength(value.Value, ref lengthCache, parameter) : 0;

        static Task WriteAsync<T>(OpenGaussTypeHandler handler, T? value, OpenGaussWriteBuffer buffer, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            where T : struct
            => value.HasValue
                ? handler.WriteWithLength(value.Value, buffer, lengthCache, parameter, async, cancellationToken)
                : handler.WriteWithLength(DBNull.Value, buffer, lengthCache, parameter, async, cancellationToken);

        internal static TDelegate CreateDelegate<TDelegate>(Type underlyingType, MethodInfo method)
            where TDelegate : Delegate
            => (TDelegate)method.MakeGenericMethod(underlyingType).CreateDelegate(typeof(TDelegate));
    }
}
