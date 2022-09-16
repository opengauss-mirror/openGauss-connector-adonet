using System;
using System.Reflection;
using System.Threading.Tasks;
using OpenGauss.NET.Internal.TypeHandling;

namespace OpenGauss.NET.Internal.TypeHandlers.CompositeHandlers
{
    abstract class CompositeParameterHandler
    {
        public OpenGaussTypeHandler Handler { get; }
        public Type ParameterType { get; }
        public int ParameterPosition { get; }

        public CompositeParameterHandler(OpenGaussTypeHandler handler, ParameterInfo parameterInfo)
        {
            Handler = handler;
            ParameterType = parameterInfo.ParameterType;
            ParameterPosition = parameterInfo.Position;
        }

        public async ValueTask<T> Read<T>(OpenGaussReadBuffer buffer, bool async)
        {
            await buffer.Ensure(sizeof(uint) + sizeof(int), async);

            var oid = buffer.ReadUInt32();
            var length = buffer.ReadInt32();
            if (length == -1)
                return default!;

            return NullableHandler<T>.Exists
                ? await NullableHandler<T>.ReadAsync(Handler, buffer, length, async)
                : await Handler.Read<T>(buffer, length, async);
        }

        public abstract ValueTask<object?> Read(OpenGaussReadBuffer buffer, bool async);
    }
}
