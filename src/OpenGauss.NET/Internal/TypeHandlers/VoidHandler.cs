using System;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers
{
    /// <remarks>
    /// https://www.postgresql.org/docs/current/static/datatype-boolean.html
    /// </remarks>
    class VoidHandler : OpenGaussSimpleTypeHandler<DBNull>
    {
        public VoidHandler(PostgresType pgType) : base(pgType) {}

        public override DBNull Read(OpenGaussReadBuffer buf, int len, FieldDescription? fieldDescription = null)
            => DBNull.Value;

        public override int ValidateAndGetLength(DBNull value, OpenGaussParameter? parameter)
            => throw new NotSupportedException();

        public override void Write(DBNull value, OpenGaussWriteBuffer buf, OpenGaussParameter? parameter)
            => throw new NotSupportedException();

        public override int ValidateObjectAndGetLength(object? value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => value switch
            {
                DBNull => 0,
                null => 0,
                _ => throw new InvalidCastException($"Can't write CLR type {value.GetType()} with handler type {nameof(VoidHandler)}")
            };

        public override Task WriteObjectWithLength(object? value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => value switch
            {
                DBNull => WriteWithLength(DBNull.Value, buf, lengthCache, parameter, async, cancellationToken),
                null => WriteWithLength(DBNull.Value, buf, lengthCache, parameter, async, cancellationToken),
                _ => throw new InvalidCastException($"Can't write CLR type {value.GetType()} with handler type {nameof(VoidHandler)}")
            };
    }
}
