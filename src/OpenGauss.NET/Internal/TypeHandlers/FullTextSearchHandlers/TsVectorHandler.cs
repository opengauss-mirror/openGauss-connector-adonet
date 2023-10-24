using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeHandlers.FullTextSearchHandlers
{
    /// <summary>
    /// A type handler for the PostgreSQL tsvector data type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-textsearch.html.
    ///
    /// The type handler API allows customizing OpenGauss's behavior in powerful ways. However, although it is public, it
    /// should be considered somewhat unstable, and may change in breaking ways, including in non-major releases.
    /// Use it at your own risk.
    /// </remarks>
    public partial class TsVectorHandler : OpenGaussTypeHandler<OpenGaussTsVector>
    {
        // 2561 = 2046 (max length lexeme string) + (1) null terminator +
        // 2 (num_pos) + sizeof(int16) * 256 (max_num_pos (positions/wegihts))
        const int MaxSingleLexemeBytes = 2561;

        public TsVectorHandler(PostgresType pgType) : base(pgType) {}

        #region Read

        /// <inheritdoc />
        public override async ValueTask<OpenGaussTsVector> Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
        {
            await buf.Ensure(4, async);
            var numLexemes = buf.ReadInt32();
            len -= 4;

            var lexemes = new List<OpenGaussTsVector.Lexeme>();
            for (var lexemePos = 0; lexemePos < numLexemes; lexemePos++)
            {
                await buf.Ensure(Math.Min(len, MaxSingleLexemeBytes), async);
                var posBefore = buf.ReadPosition;

                List<OpenGaussTsVector.Lexeme.WordEntryPos>? positions = null;

                var lexemeString = buf.ReadNullTerminatedString();
                int numPositions = buf.ReadInt16();
                for (var i = 0; i < numPositions; i++)
                {
                    var wordEntryPos = buf.ReadInt16();
                    if (positions == null)
                        positions = new List<OpenGaussTsVector.Lexeme.WordEntryPos>();
                    positions.Add(new OpenGaussTsVector.Lexeme.WordEntryPos(wordEntryPos));
                }

                lexemes.Add(new OpenGaussTsVector.Lexeme(lexemeString, positions, true));

                len -= buf.ReadPosition - posBefore;
            }

            return new OpenGaussTsVector(lexemes, true);
        }

        #endregion Read

        #region Write

        // TODO: Implement length cache
        /// <inheritdoc />
        public override int ValidateAndGetLength(OpenGaussTsVector value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => 4 + value.Sum(l => Encoding.UTF8.GetByteCount(l.Text) + 1 + 2 + l.Count * 2);

        /// <inheritdoc />
        public override async Task Write(OpenGaussTsVector vector, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
        {
            if (buf.WriteSpaceLeft < 4)
                await buf.Flush(async, cancellationToken);
            buf.WriteInt32(vector.Count);

            foreach (var lexeme in vector)
            {
                if (buf.WriteSpaceLeft < MaxSingleLexemeBytes)
                    await buf.Flush(async, cancellationToken);

                buf.WriteString(lexeme.Text);
                buf.WriteByte(0);
                buf.WriteInt16(lexeme.Count);
                for (var i = 0; i < lexeme.Count; i++)
                    buf.WriteInt16(lexeme[i].Value);
            }
        }

        #endregion Write
    }
}
