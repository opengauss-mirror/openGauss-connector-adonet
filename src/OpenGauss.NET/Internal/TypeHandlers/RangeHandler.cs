using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandlers
{
    /// <summary>
    /// A type handler for PostgreSQL range types.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/rangetypes.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    /// <typeparam name="TSubtype">The range subtype.</typeparam>
    // NOTE: This cannot inherit from OpenGaussTypeHandler<OpenGaussRange<TSubtype>>, since that triggers infinite generic recursion in Native AOT
    public partial class RangeHandler<TSubtype> : OpenGaussTypeHandler, IOpenGaussTypeHandler<OpenGaussRange<TSubtype>>
    {
        /// <summary>
        /// The type handler for the subtype that this range type holds
        /// </summary>
        protected OpenGaussTypeHandler SubtypeHandler { get; }

        /// <inheritdoc />
        public RangeHandler(PostgresType rangePostgresType, OpenGaussTypeHandler subtypeHandler)
            : base(rangePostgresType)
            => SubtypeHandler = subtypeHandler;

        public override Type GetFieldType(FieldDescription? fieldDescription = null) => typeof(OpenGaussRange<TSubtype>);
        public override Type GetProviderSpecificFieldType(FieldDescription? fieldDescription = null) => typeof(OpenGaussRange<TSubtype>);

        /// <inheritdoc />
        public override OpenGaussTypeHandler CreateArrayHandler(PostgresArrayType pgArrayType, ArrayNullabilityMode arrayNullabilityMode)
            => new ArrayHandler<OpenGaussRange<TSubtype>>(pgArrayType, this, arrayNullabilityMode);

        /// <inheritdoc />
        public override OpenGaussTypeHandler CreateRangeHandler(PostgresType pgRangeType)
            => throw new NotSupportedException();

        /// <inheritdoc />
        public override OpenGaussTypeHandler CreateMultirangeHandler(PostgresMultirangeType pgMultirangeType)
            => throw new NotSupportedException();

        #region Read

        /// <inheritdoc />
        public ValueTask<OpenGaussRange<TSubtype>> Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
            => ReadRange<TSubtype>(buf, len, async, fieldDescription);

        protected internal async ValueTask<OpenGaussRange<TAnySubtype>> ReadRange<TAnySubtype>(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
        {
            await buf.Ensure(1, async);

            var flags = (RangeFlags)buf.ReadByte();
            if ((flags & RangeFlags.Empty) != 0)
                return OpenGaussRange<TAnySubtype>.Empty;

            var lowerBound = default(TAnySubtype);
            var upperBound = default(TAnySubtype);

            if ((flags & RangeFlags.LowerBoundInfinite) == 0)
                lowerBound = await SubtypeHandler.ReadWithLength<TAnySubtype>(buf, async);

            if ((flags & RangeFlags.UpperBoundInfinite) == 0)
                upperBound = await SubtypeHandler.ReadWithLength<TAnySubtype>(buf, async);

            return new OpenGaussRange<TAnySubtype>(lowerBound, upperBound, flags);
        }

        public override async ValueTask<object> ReadAsObject(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
            => await Read(buf, len, async, fieldDescription);

        #endregion

        #region Write

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussRange<TSubtype> value, [NotNullIfNotNull("lengthCache")] ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(value, ref lengthCache, parameter);

        protected internal int ValidateAndGetLengthRange<TAnySubtype>(OpenGaussRange<TAnySubtype> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
        {
            var totalLen = 1;
            var lengthCachePos = lengthCache?.Position ?? 0;
            if (!value.IsEmpty)
            {
                if (!value.LowerBoundInfinite)
                {
                    totalLen += 4;
                    if (value.LowerBound is not null)
                        totalLen += SubtypeHandler.ValidateAndGetLength(value.LowerBound, ref lengthCache, null);
                }

                if (!value.UpperBoundInfinite)
                {
                    totalLen += 4;
                    if (value.UpperBound is not null)
                        totalLen += SubtypeHandler.ValidateAndGetLength(value.UpperBound, ref lengthCache, null);
                }
            }

            // If we're traversing an already-populated length cache, rewind to first element slot so that
            // the elements' handlers can access their length cache values
            if (lengthCache != null && lengthCache.IsPopulated)
                lengthCache.Position = lengthCachePos;

            return totalLen;
        }

        /// <inheritdoc />
        public Task Write(OpenGaussRange<TSubtype> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteRange(value, buf, lengthCache, parameter, async, cancellationToken);

        protected internal async Task WriteRange<TAnySubtype>(OpenGaussRange<TAnySubtype> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 1)
                await buf.Flush(async, cancellationToken);

            buf.WriteByte((byte)value.Flags);

            if (value.IsEmpty)
                return;

            if (!value.LowerBoundInfinite)
                await SubtypeHandler.WriteWithLength(value.LowerBound, buf, lengthCache, null, async, cancellationToken);

            if (!value.UpperBoundInfinite)
                await SubtypeHandler.WriteWithLength(value.UpperBound, buf, lengthCache, null, async, cancellationToken);
        }

        #endregion
    }

    /// <summary>
    /// Type handler for PostgreSQL range types.
    /// </summary>
    /// <remarks>
    /// Introduced in PostgreSQL 9.2.
    /// https://www.postgresql.org/docs/current/static/rangetypes.html
    /// </remarks>
    /// <typeparam name="TSubtype1">The main range subtype.</typeparam>
    /// <typeparam name="TSubtype2">An alternative range subtype.</typeparam>
    public class RangeHandler<TSubtype1, TSubtype2> : RangeHandler<TSubtype1>, IOpenGaussTypeHandler<OpenGaussRange<TSubtype2>>
    {
        /// <inheritdoc />
        public RangeHandler(PostgresType rangePostgresType, OpenGaussTypeHandler subtypeHandler)
            : base(rangePostgresType, subtypeHandler) {}

        ValueTask<OpenGaussRange<TSubtype2>> IOpenGaussTypeHandler<OpenGaussRange<TSubtype2>>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadRange<TSubtype2>(buf, len, async, fieldDescription);

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussRange<TSubtype2> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthRange(value, ref lengthCache, parameter);

        /// <inheritdoc />
        public Task Write(OpenGaussRange<TSubtype2> value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteRange(value, buf, lengthCache, parameter, async, cancellationToken);

        public override int ValidateObjectAndGetLength(object? value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => value switch
            {
                OpenGaussRange<TSubtype1> converted => ((IOpenGaussTypeHandler<OpenGaussRange<TSubtype1>>)this).ValidateAndGetLength(converted, ref lengthCache, parameter),
                OpenGaussRange<TSubtype2> converted => ((IOpenGaussTypeHandler<OpenGaussRange<TSubtype2>>)this).ValidateAndGetLength(converted, ref lengthCache, parameter),

                DBNull => 0,
                null => 0,
                _ => throw new InvalidCastException($"Can't write CLR type {value.GetType()} with handler type RangeHandler<TElement>")
            };

        public override Task WriteObjectWithLength(object? value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => value switch
            {
                OpenGaussRange<TSubtype1> converted => WriteWithLength(converted, buf, lengthCache, parameter, async, cancellationToken),
                OpenGaussRange<TSubtype2> converted => WriteWithLength(converted, buf, lengthCache, parameter, async, cancellationToken),

                DBNull => WriteWithLength(DBNull.Value, buf, lengthCache, parameter, async, cancellationToken),
                null => WriteWithLength(DBNull.Value, buf, lengthCache, parameter, async, cancellationToken),
                _ => throw new InvalidCastException($"Can't write CLR type {value.GetType()} with handler type RangeHandler<TElement>")
            };
    }
}
