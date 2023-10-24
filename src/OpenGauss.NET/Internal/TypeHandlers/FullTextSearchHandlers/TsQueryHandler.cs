using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

// TODO: Need to work on the nullability here
#nullable disable
#pragma warning disable CS8632

namespace OpenGauss.NET.Internal.TypeHandlers.FullTextSearchHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL tsquery data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-textsearch.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class TsQueryHandler : OpenGaussTypeHandler<OpenGaussTsQuery>,
        IOpenGaussTypeHandler<OpenGaussTsQueryEmpty>, IOpenGaussTypeHandler<OpenGaussTsQueryLexeme>,
        IOpenGaussTypeHandler<OpenGaussTsQueryNot>, IOpenGaussTypeHandler<OpenGaussTsQueryAnd>,
        IOpenGaussTypeHandler<OpenGaussTsQueryOr>, IOpenGaussTypeHandler<OpenGaussTsQueryFollowedBy>
    {
        // 1 (type) + 1 (weight) + 1 (is prefix search) + 2046 (max str len) + 1 (null terminator)
        const int MaxSingleTokenBytes = 2050;

        readonly Stack<OpenGaussTsQuery> _stack = new();

        public TsQueryHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        /// <inheritdoc />
        public override async ValueTask<OpenGaussTsQuery> Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
        {
            await buf.Ensure(4, async);
            var numTokens = buf.ReadInt32();
            if (numTokens == 0)
                return new OpenGaussTsQueryEmpty();

            OpenGaussTsQuery? value = null;
            var nodes = new Stack<Tuple<OpenGaussTsQuery, int>>();
            len -= 4;

            for (var tokenPos = 0; tokenPos < numTokens; tokenPos++)
            {
                await buf.Ensure(Math.Min(len, MaxSingleTokenBytes), async);
                var readPos = buf.ReadPosition;

                var isOper = buf.ReadByte() == 2;
                if (isOper)
                {
                    var operKind = (OpenGaussTsQuery.NodeKind)buf.ReadByte();
                    if (operKind == OpenGaussTsQuery.NodeKind.Not)
                    {
                        var node = new OpenGaussTsQueryNot(null);
                        InsertInTree(node, nodes, ref value);
                        nodes.Push(new Tuple<OpenGaussTsQuery, int>(node, 0));
                    }
                    else
                    {
                        var node = operKind switch
                        {
                            OpenGaussTsQuery.NodeKind.And    => (OpenGaussTsQuery)new OpenGaussTsQueryAnd(null, null),
                            OpenGaussTsQuery.NodeKind.Or     => new OpenGaussTsQueryOr(null, null),
                            OpenGaussTsQuery.NodeKind.Phrase => new OpenGaussTsQueryFollowedBy(null, buf.ReadInt16(), null),
                            _ => throw new InvalidOperationException($"Internal OpenGauss bug: unexpected value {operKind} of enum {nameof(OpenGaussTsQuery.NodeKind)}. Please file a bug.")
                        };

                        InsertInTree(node, nodes, ref value);

                        nodes.Push(new Tuple<OpenGaussTsQuery, int>(node, 1));
                        nodes.Push(new Tuple<OpenGaussTsQuery, int>(node, 2));
                    }
                }
                else
                {
                    var weight = (OpenGaussTsQueryLexeme.Weight)buf.ReadByte();
                    var prefix = buf.ReadByte() != 0;
                    var str = buf.ReadNullTerminatedString();
                    InsertInTree(new OpenGaussTsQueryLexeme(str, weight, prefix), nodes, ref value);
                }

                len -= buf.ReadPosition - readPos;
            }

            if (nodes.Count != 0)
                throw new InvalidOperationException("Internal OpenGauss bug, please report.");

            return value!;

            static void InsertInTree(OpenGaussTsQuery node, Stack<Tuple<OpenGaussTsQuery, int>> nodes, ref OpenGaussTsQuery? value)
            {
                if (nodes.Count == 0)
                    value = node;
                else
                {
                    var parent = nodes.Pop();
                    if (parent.Item2 == 0)
                        ((OpenGaussTsQueryNot)parent.Item1).Child = node;
                    else if (parent.Item2 == 1)
                        ((OpenGaussTsQueryBinOp)parent.Item1).Left = node;
                    else
                        ((OpenGaussTsQueryBinOp)parent.Item1).Right = node;
                }
            }
        }

        async ValueTask<OpenGaussTsQueryEmpty> IOpenGaussTypeHandler<OpenGaussTsQueryEmpty>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => (OpenGaussTsQueryEmpty)await Read(buf, len, async, fieldDescription);

        async ValueTask<OpenGaussTsQueryLexeme> IOpenGaussTypeHandler<OpenGaussTsQueryLexeme>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => (OpenGaussTsQueryLexeme)await Read(buf, len, async, fieldDescription);

        async ValueTask<OpenGaussTsQueryNot> IOpenGaussTypeHandler<OpenGaussTsQueryNot>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => (OpenGaussTsQueryNot)await Read(buf, len, async, fieldDescription);

        async ValueTask<OpenGaussTsQueryAnd> IOpenGaussTypeHandler<OpenGaussTsQueryAnd>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => (OpenGaussTsQueryAnd)await Read(buf, len, async, fieldDescription);

        async ValueTask<OpenGaussTsQueryOr> IOpenGaussTypeHandler<OpenGaussTsQueryOr>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => (OpenGaussTsQueryOr)await Read(buf, len, async, fieldDescription);

        async ValueTask<OpenGaussTsQueryFollowedBy> IOpenGaussTypeHandler<OpenGaussTsQueryFollowedBy>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => (OpenGaussTsQueryFollowedBy)await Read(buf, len, async, fieldDescription);

        #endregion Read

        #region Write

        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussTsQuery value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => value.Kind == OpenGaussTsQuery.NodeKind.Empty
                ? 4
                : 4 + GetNodeLength(value);

        int GetNodeLength(OpenGaussTsQuery node)
        {
            // TODO: Figure out the nullability strategy here
            switch (node.Kind)
            {
            case OpenGaussTsQuery.NodeKind.Lexeme:
                var strLen = Encoding.UTF8.GetByteCount(((OpenGaussTsQueryLexeme)node).Text);
                if (strLen > 2046)
                    throw new InvalidCastException("Lexeme text too long. Must be at most 2046 bytes in UTF8.");
                return 4 + strLen;
            case OpenGaussTsQuery.NodeKind.And:
            case OpenGaussTsQuery.NodeKind.Or:
                return 2 + GetNodeLength(((OpenGaussTsQueryBinOp)node).Left) + GetNodeLength(((OpenGaussTsQueryBinOp)node).Right);
            case OpenGaussTsQuery.NodeKind.Phrase:
                // 2 additional bytes for uint16 phrase operator "distance" field.
                return 4 + GetNodeLength(((OpenGaussTsQueryBinOp)node).Left) + GetNodeLength(((OpenGaussTsQueryBinOp)node).Right);
            case OpenGaussTsQuery.NodeKind.Not:
                return 2 + GetNodeLength(((OpenGaussTsQueryNot)node).Child);
            case OpenGaussTsQuery.NodeKind.Empty:
                throw new InvalidOperationException("Empty tsquery nodes must be top-level");
            default:
                throw new InvalidOperationException("Illegal node kind: " + node.Kind);
            }
        }

        /// <inheritdoc />
        public override async Task Write(OpenGaussTsQuery query, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            var numTokens = GetTokenCount(query);

            if (buf.WriteSpaceLeft < 4)
                await buf.Flush(async, cancellationToken);
            buf.WriteInt32(numTokens);

            if (numTokens == 0)
                return;

            _stack.Push(query);

            while (_stack.Count > 0)
            {
                if (buf.WriteSpaceLeft < 2)
                    await buf.Flush(async, cancellationToken);

                if (_stack.Peek().Kind == OpenGaussTsQuery.NodeKind.Lexeme && buf.WriteSpaceLeft < MaxSingleTokenBytes)
                    await buf.Flush(async, cancellationToken);

                var node = _stack.Pop();
                buf.WriteByte(node.Kind == OpenGaussTsQuery.NodeKind.Lexeme ? (byte)1 : (byte)2);
                if (node.Kind != OpenGaussTsQuery.NodeKind.Lexeme)
                {
                    buf.WriteByte((byte)node.Kind);
                    if (node.Kind == OpenGaussTsQuery.NodeKind.Not)
                        _stack.Push(((OpenGaussTsQueryNot)node).Child);
                    else
                    {
                        if (node.Kind == OpenGaussTsQuery.NodeKind.Phrase)
                            buf.WriteInt16(((OpenGaussTsQueryFollowedBy)node).Distance);

                        _stack.Push(((OpenGaussTsQueryBinOp)node).Left);
                        _stack.Push(((OpenGaussTsQueryBinOp)node).Right);
                    }
                }
                else
                {
                    var lexemeNode = (OpenGaussTsQueryLexeme)node;
                    buf.WriteByte((byte)lexemeNode.Weights);
                    buf.WriteByte(lexemeNode.IsPrefixSearch ? (byte)1 : (byte)0);
                    buf.WriteString(lexemeNode.Text);
                    buf.WriteByte(0);
                }
            }

            _stack.Clear();
        }

        int GetTokenCount(OpenGaussTsQuery node)
        {
            switch (node.Kind)
            {
            case OpenGaussTsQuery.NodeKind.Lexeme:
                return 1;
            case OpenGaussTsQuery.NodeKind.And:
            case OpenGaussTsQuery.NodeKind.Or:
            case OpenGaussTsQuery.NodeKind.Phrase:
                return 1 + GetTokenCount(((OpenGaussTsQueryBinOp)node).Left) + GetTokenCount(((OpenGaussTsQueryBinOp)node).Right);
            case OpenGaussTsQuery.NodeKind.Not:
                return 1 + GetTokenCount(((OpenGaussTsQueryNot)node).Child);
            case OpenGaussTsQuery.NodeKind.Empty:
                return 0;
            }
            return -1;
        }

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussTsQueryOr value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength((OpenGaussTsQuery)value, ref lengthCache, parameter);

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussTsQueryAnd value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength((OpenGaussTsQuery)value, ref lengthCache, parameter);

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussTsQueryNot value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength((OpenGaussTsQuery)value, ref lengthCache, parameter);

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussTsQueryLexeme value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength((OpenGaussTsQuery)value, ref lengthCache, parameter);

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussTsQueryEmpty value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength((OpenGaussTsQuery)value, ref lengthCache, parameter);

        /// <inheritdoc />
        public int ValidateAndGetLength(OpenGaussTsQueryFollowedBy value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength((OpenGaussTsQuery)value, ref lengthCache, parameter);

        /// <inheritdoc />
        public Task Write(OpenGaussTsQueryOr value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => Write((OpenGaussTsQuery)value, buf, lengthCache, parameter, async, cancellationToken);

        /// <inheritdoc />
        public Task Write(OpenGaussTsQueryAnd value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => Write((OpenGaussTsQuery)value, buf, lengthCache, parameter, async, cancellationToken);

        /// <inheritdoc />
        public Task Write(OpenGaussTsQueryNot value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => Write((OpenGaussTsQuery)value, buf, lengthCache, parameter, async, cancellationToken);

        /// <inheritdoc />
        public Task Write(OpenGaussTsQueryLexeme value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => Write((OpenGaussTsQuery)value, buf, lengthCache, parameter, async, cancellationToken);

        /// <inheritdoc />
        public Task Write(OpenGaussTsQueryEmpty value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => Write((OpenGaussTsQuery)value, buf, lengthCache, parameter, async, cancellationToken);

        /// <inheritdoc />
        public Task Write(
            OpenGaussTsQueryFollowedBy value,
            OpenGaussWriteBuffer buf,
            OpenGaussLengthCache? lengthCache,
            OpenGaussParameter? parameter,
            bool async,
            CancellationToken cancellationToken = default)
            => Write((OpenGaussTsQuery)value, buf, lengthCache, parameter, async, cancellationToken);

        #endregion Write
    }
}
