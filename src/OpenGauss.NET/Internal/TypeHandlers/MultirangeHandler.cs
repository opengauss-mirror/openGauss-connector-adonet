using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandlers
{
    public partial class MultirangeHandler<TSubtype> : OpenGaussTypeHandler<OpenGaussRange<TSubtype>[]>,
        IOpenGaussTypeHandler<List<OpenGaussRange<TSubtype>>>
    {
        /// <summary>
        /// The type handler for the range that this multirange type holds
        /// </summary>
        protected RangeHandler<TSubtype> RangeHandler { get; }

        /// <inheritdoc />
        public MultirangeHandler(PostgresMultirangeType pgMultirangeType, RangeHandler<TSubtype> rangeHandler)
            : base(pgMultirangeType)
            => RangeHandler = rangeHandler;

        public override ValueTask<OpenGaussRange<TSubtype>[]> Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
            => ReadMultirangeArray<TSubtype>(buf, len, async, fieldDescription);

        protected async ValueTask<OpenGaussRange<TAnySubtype>[]> ReadMultirangeArray<TAnySubtype>(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
        {
            await buf.Ensure(4, async);
            var numRanges = buf.ReadInt32();
            var multirange = new OpenGaussRange<TAnySubtype>[numRanges];

            for (var i = 0; i < numRanges; i++)
            {
                await buf.Ensure(4, async);
                var rangeLen = buf.ReadInt32();
                multirange[i] = await RangeHandler.ReadRange<TAnySubtype>(buf, rangeLen, async, fieldDescription);
            }

            return multirange;
        }

        ValueTask<List<OpenGaussRange<TSubtype>>> IOpenGaussTypeHandler<List<OpenGaussRange<TSubtype>>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeList<TSubtype>(buf, len, async, fieldDescription);

        protected async ValueTask<List<OpenGaussRange<TAnySubtype>>> ReadMultirangeList<TAnySubtype>(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
        {
            await buf.Ensure(4, async);
            var numRanges = buf.ReadInt32();
            var multirange = new List<OpenGaussRange<TAnySubtype>>(numRanges);

            for (var i = 0; i < numRanges; i++)
            {
                await buf.Ensure(4, async);
                var rangeLen = buf.ReadInt32();
                multirange.Add(await RangeHandler.ReadRange<TAnySubtype>(buf, rangeLen, async, fieldDescription));
            }

            return multirange;
        }

        public override int ValidateAndGetLength(OpenGaussRange<TSubtype>[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(List<OpenGaussRange<TSubtype>> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        protected int ValidateAndGetLengthMultirange<TAnySubtype>(
            IList<OpenGaussRange<TAnySubtype>> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
        {
            lengthCache ??= new OpenGaussLengthCache(1);
            if (lengthCache.IsPopulated)
                return lengthCache.Get();

            // Leave empty slot for the entire array length, and go ahead an populate the element slots
            var pos = lengthCache.Position;
            lengthCache.Set(0);

            var sum = 4 + 4 * value.Count;
            for (var i = 0; i < value.Count; i++)
                sum += RangeHandler.ValidateAndGetLength(value[i], ref lengthCache, parameter);

            lengthCache.Lengths[pos] = sum;
            return sum;
        }

        public override Task Write(
            OpenGaussRange<TSubtype>[] value,
            OpenGaussWriteBuffer buf,
            OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter,
            bool async,
            CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(
            List<OpenGaussRange<TSubtype>> value,
            OpenGaussWriteBuffer buf,
            OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter,
            bool async,
            CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public async Task WriteMultirange<TAnySubtype>(
            IList<OpenGaussRange<TAnySubtype>> value,
            OpenGaussWriteBuffer buf,
            OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter,
            bool async,
            CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 4)
                await buf.Flush(async, cancellationToken);

            buf.WriteInt32(value.Count);

            for (var i = 0; i < value.Count; i++)
                await RangeHandler.WriteWithLength(value[i], buf, lengthCache, parameter: null, async, cancellationToken);
        }
    }

    public class MultirangeHandler<TSubtype1, TSubtype2> : MultirangeHandler<TSubtype1>,
        IOpenGaussTypeHandler<OpenGaussRange<TSubtype2>[]>, IOpenGaussTypeHandler<List<OpenGaussRange<TSubtype2>>>
    {
        /// <inheritdoc />
        public MultirangeHandler(PostgresMultirangeType pgMultirangeType, RangeHandler<TSubtype1, TSubtype2> rangeHandler)
            : base(pgMultirangeType, rangeHandler) {}

        ValueTask<OpenGaussRange<TSubtype2>[]> IOpenGaussTypeHandler<OpenGaussRange<TSubtype2>[]>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeArray<TSubtype2>(buf, len, async, fieldDescription);

        ValueTask<List<OpenGaussRange<TSubtype2>>> IOpenGaussTypeHandler<List<OpenGaussRange<TSubtype2>>>.Read(
            OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadMultirangeList<TSubtype2>(buf, len, async, fieldDescription);

        public int ValidateAndGetLength(List<OpenGaussRange<TSubtype2>> value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public int ValidateAndGetLength(OpenGaussRange<TSubtype2>[] value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthMultirange(value, ref lengthCache, parameter);

        public Task Write(
            List<OpenGaussRange<TSubtype2>> value,
            OpenGaussWriteBuffer buf,
            OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter,
            bool async,
            CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public Task Write(
            OpenGaussRange<TSubtype2>[] value,
            OpenGaussWriteBuffer buf,
            OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter,
            bool async,
            CancellationToken cancellationToken = default)
            => WriteMultirange(value, buf, lengthCache, parameter, async, cancellationToken);

        public override int ValidateObjectAndGetLength(object? value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => value switch
            {
                OpenGaussRange<TSubtype1>[] converted => ((IOpenGaussTypeHandler<OpenGaussRange<TSubtype1>[]>)this).ValidateAndGetLength(converted, ref lengthCache, parameter),
                OpenGaussRange<TSubtype2>[] converted => ((IOpenGaussTypeHandler<OpenGaussRange<TSubtype2>[]>)this).ValidateAndGetLength(converted, ref lengthCache, parameter),
                List<OpenGaussRange<TSubtype1>> converted => ((IOpenGaussTypeHandler<List<OpenGaussRange<TSubtype1>>>)this).ValidateAndGetLength(converted, ref lengthCache, parameter),
                List<OpenGaussRange<TSubtype2>> converted => ((IOpenGaussTypeHandler<List<OpenGaussRange<TSubtype2>>>)this).ValidateAndGetLength(converted, ref lengthCache, parameter),

                DBNull => 0,
                null => 0,
                _ => throw new InvalidCastException($"Can't write CLR type {value.GetType()} with handler type RangeHandler<TElement>")
            };

        public override Task WriteObjectWithLength(object? value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => value switch
            {
                OpenGaussRange<TSubtype1>[] converted => WriteWithLength(converted, buf, lengthCache, parameter, async, cancellationToken),
                OpenGaussRange<TSubtype2>[] converted => WriteWithLength(converted, buf, lengthCache, parameter, async, cancellationToken),
                List<OpenGaussRange<TSubtype1>> converted => WriteWithLength(converted, buf, lengthCache, parameter, async, cancellationToken),
                List<OpenGaussRange<TSubtype2>> converted => WriteWithLength(converted, buf, lengthCache, parameter, async, cancellationToken),

                DBNull => WriteWithLength(DBNull.Value, buf, lengthCache, parameter, async, cancellationToken),
                null => WriteWithLength(DBNull.Value, buf, lengthCache, parameter, async, cancellationToken),
                _ => throw new InvalidCastException($"Can't write CLR type {value.GetType()} with handler type RangeHandler<TElement>")
            };
    }
}
