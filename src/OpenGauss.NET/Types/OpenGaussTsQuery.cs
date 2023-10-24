using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable CA1034

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET.Types
{
    /// <summary>
    /// Represents a PostgreSQL tsquery. This is the base class for the
    /// lexeme, not, or, and, and "followed by" nodes.
    /// </summary>
    public abstract class OpenGaussTsQuery : IEquatable<OpenGaussTsQuery>
    {
        /// <summary>
        /// Node kind
        /// </summary>
        public NodeKind Kind { get; }

        /// <summary>
        /// NodeKind
        /// </summary>
        public enum NodeKind
        {
            /// <summary>
            /// Represents the empty tsquery. Should only be used at top level.
            /// </summary>
            Empty = -1,
            /// <summary>
            /// Lexeme
            /// </summary>
            Lexeme = 0,
            /// <summary>
            /// Not operator
            /// </summary>
            Not = 1,
            /// <summary>
            /// And operator
            /// </summary>
            And = 2,
            /// <summary>
            /// Or operator
            /// </summary>
            Or = 3,
            /// <summary>
            /// "Followed by" operator
            /// </summary>
            Phrase = 4
        }

        /// <summary>
        /// Constructs an <see cref="OpenGaussTsQuery"/>.
        /// </summary>
        /// <param name="kind"></param>
        protected OpenGaussTsQuery(NodeKind kind) => Kind = kind;

        /// <summary>
        /// Writes the tsquery in PostgreSQL's text format.
        /// </summary>
        public void Write(StringBuilder stringBuilder) => WriteCore(stringBuilder, true);

        internal abstract void WriteCore(StringBuilder sb, bool first = false);

        /// <summary>
        /// Writes the tsquery in PostgreSQL's text format.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            Write(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Parses a tsquery in PostgreSQL's text format.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static OpenGaussTsQuery Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var valStack = new Stack<OpenGaussTsQuery>();
            var opStack = new Stack<OpenGaussTsQueryOperator>();

            var sb = new StringBuilder();
            var pos = 0;
            var expectingBinOp = false;

            var lastFollowedByOpDistance = -1;

            NextToken:
            if (pos >= value.Length)
                goto Finish;
            var ch = value[pos++];
            if (ch == '\'')
                goto WaitEndComplex;
            if ((ch == ')' || ch == '|' || ch == '&') && !expectingBinOp || (ch == '(' || ch == '!') && expectingBinOp)
                throw new FormatException("Syntax error in tsquery. Unexpected token.");

            if (ch == '<')
            {
                var endOfOperatorConsumed = false;
                var sbCurrentLength = sb.Length;

                while (pos < value.Length)
                {
                    var c = value[pos++];
                    if (c == '>')
                    {
                        endOfOperatorConsumed = true;
                        break;
                    }

                    sb.Append(c);
                }

                if (sb.Length == sbCurrentLength || !endOfOperatorConsumed)
                    throw new FormatException("Syntax error in tsquery. Malformed 'followed by' operator.");

                var followedByOpDistanceString = sb.ToString(sbCurrentLength, sb.Length - sbCurrentLength);
                if (followedByOpDistanceString == "-")
                {
                    lastFollowedByOpDistance = 1;
                }
                else if (!int.TryParse(followedByOpDistanceString, out lastFollowedByOpDistance)
                         || lastFollowedByOpDistance < 0)
                {
                    throw new FormatException("Syntax error in tsquery. Malformed distance in 'followed by' operator.");
                }

                sb.Length -= followedByOpDistanceString.Length;
            }

            if (ch == '(' || ch == '!' || ch == '&' || ch == '<')
            {
                opStack.Push(new OpenGaussTsQueryOperator(ch, lastFollowedByOpDistance));
                expectingBinOp = false;
                lastFollowedByOpDistance = 0;
                goto NextToken;
            }

            if (ch == '|')
            {
                if (opStack.Count > 0 && opStack.Peek() == '|')
                {
                    if (valStack.Count < 2)
                        throw new FormatException("Syntax error in tsquery");
                    var right = valStack.Pop();
                    var left = valStack.Pop();
                    valStack.Push(new OpenGaussTsQueryOr(left, right));
                    // Implicit pop and repush |
                }
                else
                    opStack.Push('|');
                expectingBinOp = false;
                goto NextToken;
            }

            if (ch == ')')
            {
                while (opStack.Count > 0 && opStack.Peek() != '(')
                {
                    if (valStack.Count < 2 || opStack.Peek() == '!')
                        throw new FormatException("Syntax error in tsquery");

                    var right = valStack.Pop();
                    var left = valStack.Pop();

                    var tsOp = opStack.Pop();
                    valStack.Push((char)tsOp switch
                    {
                        '&' => (OpenGaussTsQuery)new OpenGaussTsQueryAnd(left, right),
                        '|' => new OpenGaussTsQueryOr(left, right),
                        '<' => new OpenGaussTsQueryFollowedBy(left, tsOp.FollowedByDistance, right),
                        _   => throw new FormatException("Syntax error in tsquery")
                    });
                }
                if (opStack.Count == 0)
                    throw new FormatException("Syntax error in tsquery: closing parenthesis without an opening parenthesis");
                opStack.Pop();
                goto PushedVal;
            }

            if (ch == ':')
                throw new FormatException("Unexpected : while parsing tsquery");

            if (char.IsWhiteSpace(ch))
                goto NextToken;

            pos--;
            if (expectingBinOp)
                throw new FormatException("Unexpected lexeme while parsing tsquery");
            // Proceed to WaitEnd

            WaitEnd:
            if (pos >= value.Length || char.IsWhiteSpace(ch = value[pos]) || ch == '!' || ch == '&' || ch == '|' || ch == '(' || ch == ')')
            {
                valStack.Push(new OpenGaussTsQueryLexeme(sb.ToString()));
                goto PushedVal;
            }
            pos++;
            if (ch == ':')
            {
                valStack.Push(new OpenGaussTsQueryLexeme(sb.ToString()));
                sb.Clear();
                goto InWeightInfo;
            }
            if (ch == '\\')
            {
                if (pos >= value.Length)
                    throw new FormatException(@"Unexpected \ in end of value");
                ch = value[pos++];
            }
            sb.Append(ch);
            goto WaitEnd;

            WaitEndComplex:
            if (pos >= value.Length)
                throw new FormatException("Missing terminating ' in string literal");
            ch = value[pos++];
            if (ch == '\'')
            {
                if (pos < value.Length && value[pos] == '\'')
                {
                    ch = '\'';
                    pos++;
                }
                else
                {
                    valStack.Push(new OpenGaussTsQueryLexeme(sb.ToString()));
                    if (pos < value.Length && value[pos] == ':')
                    {
                        pos++;
                        goto InWeightInfo;
                    }
                    goto PushedVal;
                }
            }
            if (ch == '\\')
            {
                if (pos >= value.Length)
                    throw new FormatException(@"Unexpected \ in end of value");
                ch = value[pos++];
            }
            sb.Append(ch);
            goto WaitEndComplex;


            InWeightInfo:
            if (pos >= value.Length)
                goto Finish;
            ch = value[pos];
            if (ch == '*')
                ((OpenGaussTsQueryLexeme)valStack.Peek()).IsPrefixSearch = true;
            else if (ch == 'a' || ch == 'A')
                ((OpenGaussTsQueryLexeme)valStack.Peek()).Weights |= OpenGaussTsQueryLexeme.Weight.A;
            else if (ch == 'b' || ch == 'B')
                ((OpenGaussTsQueryLexeme)valStack.Peek()).Weights |= OpenGaussTsQueryLexeme.Weight.B;
            else if (ch == 'c' || ch == 'C')
                ((OpenGaussTsQueryLexeme)valStack.Peek()).Weights |= OpenGaussTsQueryLexeme.Weight.C;
            else if (ch == 'd' || ch == 'D')
                ((OpenGaussTsQueryLexeme)valStack.Peek()).Weights |= OpenGaussTsQueryLexeme.Weight.D;
            else
                goto PushedVal;
            pos++;
            goto InWeightInfo;

            PushedVal:
            sb.Clear();
            var processTightBindingOperator = true;
            while (opStack.Count > 0 && processTightBindingOperator)
            {
                var tsOp = opStack.Peek();
                switch (tsOp)
                {
                case '&':
                    if (valStack.Count < 2)
                        throw new FormatException("Syntax error in tsquery");
                    var andRight = valStack.Pop();
                    var andLeft = valStack.Pop();
                    valStack.Push(new OpenGaussTsQueryAnd(andLeft, andRight));
                    opStack.Pop();
                    break;

                case '!':
                    if (valStack.Count == 0)
                        throw new FormatException("Syntax error in tsquery");
                    valStack.Push(new OpenGaussTsQueryNot(valStack.Pop()));
                    opStack.Pop();
                    break;

                case '<':
                    if (valStack.Count < 2)
                        throw new FormatException("Syntax error in tsquery");
                    var followedByRight = valStack.Pop();
                    var followedByLeft = valStack.Pop();
                    valStack.Push(
                        new OpenGaussTsQueryFollowedBy(
                            followedByLeft,
                            tsOp.FollowedByDistance,
                            followedByRight));
                    opStack.Pop();
                    break;

                default:
                    processTightBindingOperator = false;
                    break;
                }
            }
            expectingBinOp = true;
            goto NextToken;

            Finish:
            while (opStack.Count > 0)
            {
                if (valStack.Count < 2)
                    throw new FormatException("Syntax error in tsquery");

                var right = valStack.Pop();
                var left = valStack.Pop();

                var tsOp = opStack.Pop();
                var query = (char)tsOp switch
                {
                    '&' => (OpenGaussTsQuery)new OpenGaussTsQueryAnd(left, right),
                    '|' => new OpenGaussTsQueryOr(left, right),
                    '<' => new OpenGaussTsQueryFollowedBy(left, tsOp.FollowedByDistance, right),
                    _   => throw new FormatException("Syntax error in tsquery")
                };
                valStack.Push(query);
            }
            if (valStack.Count != 1)
                throw new FormatException("Syntax error in tsquery");
            return valStack.Pop();
        }

        /// <inheritdoc/>
        public override int GetHashCode() =>
            throw new NotImplementedException();

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is OpenGaussTsQuery query && query.Equals(this);

        /// <summary>
        /// Returns a value indicating whether this instance and a specified <see cref="OpenGaussTsQuery"/> object represent the same value.
        /// </summary>
        /// <param name="other">An object to compare to this instance.</param>
        /// <returns><see langword="true"/> if g is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public abstract bool Equals(OpenGaussTsQuery? other);

        /// <summary>
        /// Indicates whether the values of two specified <see cref="OpenGaussTsQuery"/> objects are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator ==(OpenGaussTsQuery? left, OpenGaussTsQuery? right) =>
            left is null ? right is null : left.Equals(right);


        /// <summary>
        /// Indicates whether the values of two specified <see cref="OpenGaussTsQuery"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true"/> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=(OpenGaussTsQuery? left, OpenGaussTsQuery? right) =>
            left is null ? right is not null : !left.Equals(right);
    }

    readonly struct OpenGaussTsQueryOperator
    {
        public readonly char Char;
        public readonly int FollowedByDistance;

        public OpenGaussTsQueryOperator(char character, int followedByDistance)
        {
            Char = character;
            FollowedByDistance = followedByDistance;
        }

        public static implicit operator OpenGaussTsQueryOperator(char c) => new(c, 0);
        public static implicit operator char(OpenGaussTsQueryOperator o) => o.Char;
    }

    /// <summary>
    /// TsQuery Lexeme node.
    /// </summary>
    public sealed class OpenGaussTsQueryLexeme : OpenGaussTsQuery
    {
        string _text;

        /// <summary>
        /// Lexeme text.
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Text is null or empty string", nameof(value));

                _text = value;
            }
        }

        Weight _weights;

        /// <summary>
        /// Weights is a bitmask of the Weight enum.
        /// </summary>
        public Weight Weights
        {
            get => _weights;
            set
            {
                if (((byte)value >> 4) != 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Illegal weights");

                _weights = value;
            }
        }

        /// <summary>
        /// Prefix search.
        /// </summary>
        public bool IsPrefixSearch { get; set; }

        /// <summary>
        /// Creates a tsquery lexeme with only lexeme text.
        /// </summary>
        /// <param name="text">Lexeme text.</param>
        public OpenGaussTsQueryLexeme(string text) : this(text, Weight.None, false) { }

        /// <summary>
        /// Creates a tsquery lexeme with lexeme text and weights.
        /// </summary>
        /// <param name="text">Lexeme text.</param>
        /// <param name="weights">Bitmask of enum Weight.</param>
        public OpenGaussTsQueryLexeme(string text, Weight weights) : this(text, weights, false) { }

        /// <summary>
        /// Creates a tsquery lexeme with lexeme text, weights and prefix search flag.
        /// </summary>
        /// <param name="text">Lexeme text.</param>
        /// <param name="weights">Bitmask of enum Weight.</param>
        /// <param name="isPrefixSearch">Is prefix search?</param>
        public OpenGaussTsQueryLexeme(string text, Weight weights, bool isPrefixSearch)
            : base(NodeKind.Lexeme)
        {
            _text = text;
            Weights = weights;
            IsPrefixSearch = isPrefixSearch;
        }

        /// <summary>
        /// Weight enum, can be OR'ed together.
        /// </summary>
#pragma warning disable CA1714
        [Flags]
        public enum Weight
#pragma warning restore CA1714
        {
            /// <summary>
            /// None
            /// </summary>
            None = 0,
            /// <summary>
            /// D
            /// </summary>
            D = 1,
            /// <summary>
            /// C
            /// </summary>
            C = 2,
            /// <summary>
            /// B
            /// </summary>
            B = 4,
            /// <summary>
            /// A
            /// </summary>
            A = 8
        }

        internal override void WriteCore(StringBuilder sb, bool first = false)
        {
            sb.Append('\'').Append(Text.Replace(@"\", @"\\").Replace("'", "''")).Append('\'');
            if (IsPrefixSearch || Weights != Weight.None)
                sb.Append(':');
            if (IsPrefixSearch)
                sb.Append('*');
            if ((Weights & Weight.A) != Weight.None)
                sb.Append('A');
            if ((Weights & Weight.B) != Weight.None)
                sb.Append('B');
            if ((Weights & Weight.C) != Weight.None)
                sb.Append('C');
            if ((Weights & Weight.D) != Weight.None)
                sb.Append('D');
        }

        /// <inheritdoc/>
        public override bool Equals(OpenGaussTsQuery? other) =>
            other is OpenGaussTsQueryLexeme lexeme &&
            lexeme.Text == Text &&
            lexeme.Weights == Weights &&
            lexeme.IsPrefixSearch == IsPrefixSearch;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(Text, Weights, IsPrefixSearch);
    }

    /// <summary>
    /// TsQuery Not node.
    /// </summary>
    public sealed class OpenGaussTsQueryNot : OpenGaussTsQuery
    {
        /// <summary>
        /// Child node
        /// </summary>
        public OpenGaussTsQuery? Child { get; set; }

        /// <summary>
        /// Creates a not operator, with a given child node.
        /// </summary>
        /// <param name="child"></param>
        public OpenGaussTsQueryNot(OpenGaussTsQuery? child)
            : base(NodeKind.Not)
        {
            Child = child;
        }

        internal override void WriteCore(StringBuilder sb, bool first = false)
        {
            sb.Append('!');
            if (Child == null)
            {
                sb.Append("''");
            }
            else
            {
                if (Child.Kind != NodeKind.Lexeme)
                    sb.Append("( ");
                Child.WriteCore(sb, true);
                if (Child.Kind != NodeKind.Lexeme)
                    sb.Append(" )");
            }
        }

        /// <inheritdoc/>
        public override bool Equals(OpenGaussTsQuery? other) =>
            other is OpenGaussTsQueryNot not &&
            not.Child == Child;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Child?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Base class for TsQuery binary operators (&amp; and |).
    /// </summary>
    public abstract class OpenGaussTsQueryBinOp : OpenGaussTsQuery
    {
        /// <summary>
        /// Left child
        /// </summary>
        public OpenGaussTsQuery Left { get; set; }

        /// <summary>
        /// Right child
        /// </summary>
        public OpenGaussTsQuery Right { get; set; }

        /// <summary>
        /// Constructs a <see cref="OpenGaussTsQueryBinOp"/>.
        /// </summary>
        protected OpenGaussTsQueryBinOp(NodeKind kind, OpenGaussTsQuery left, OpenGaussTsQuery right)
            : base(kind)
        {
            Left = left;
            Right = right;
        }
    }

    /// <summary>
    /// TsQuery And node.
    /// </summary>
    public sealed class OpenGaussTsQueryAnd : OpenGaussTsQueryBinOp
    {
        /// <summary>
        /// Creates an and operator, with two given child nodes.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public OpenGaussTsQueryAnd(OpenGaussTsQuery left, OpenGaussTsQuery right)
            : base(NodeKind.And, left, right) {}

        internal override void WriteCore(StringBuilder sb, bool first = false)
        {
            Left.WriteCore(sb);
            sb.Append(" & ");
            Right.WriteCore(sb);
        }

        /// <inheritdoc/>
        public override bool Equals(OpenGaussTsQuery? other) =>
            other is OpenGaussTsQueryAnd and &&
            and.Left == Left &&
            and.Right == Right;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(Left, Right);
    }

    /// <summary>
    /// TsQuery Or Node.
    /// </summary>
    public sealed class OpenGaussTsQueryOr : OpenGaussTsQueryBinOp
    {
        /// <summary>
        /// Creates an or operator, with two given child nodes.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public OpenGaussTsQueryOr(OpenGaussTsQuery left, OpenGaussTsQuery right)
            : base(NodeKind.Or, left, right) {}

        internal override void WriteCore(StringBuilder sb, bool first = false)
        {
            // TODO: Figure out the nullability strategy here
            if (!first)
                sb.Append("( ");

            Left.WriteCore(sb);
            sb.Append(" | ");
            Right.WriteCore(sb);

            if (!first)
                sb.Append(" )");
        }

        /// <inheritdoc/>
        public override bool Equals(OpenGaussTsQuery? other) =>
            other is OpenGaussTsQueryOr or &&
            or.Left == Left &&
            or.Right == Right;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(Left, Right);
    }

    /// <summary>
    /// TsQuery "Followed by" Node.
    /// </summary>
    public sealed class OpenGaussTsQueryFollowedBy : OpenGaussTsQueryBinOp
    {
        /// <summary>
        /// The distance between the 2 nodes, in lexemes.
        /// </summary>
        public int Distance { get; set; }

        /// <summary>
        /// Creates a "followed by" operator, specifying 2 child nodes and the
        /// distance between them in lexemes.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="distance"></param>
        /// <param name="right"></param>
        public OpenGaussTsQueryFollowedBy(
            OpenGaussTsQuery left,
            int distance,
            OpenGaussTsQuery right)
            : base(NodeKind.Phrase, left, right)
        {
            if (distance < 0)
                throw new ArgumentOutOfRangeException(nameof(distance));

            Distance = distance;
        }

        internal override void WriteCore(StringBuilder sb, bool first = false)
        {
            // TODO: Figure out the nullability strategy here
            if (!first)
                sb.Append("( ");

            Left.WriteCore(sb);

            sb.Append(" <");
            if (Distance == 1) sb.Append("-");
            else sb.Append(Distance);
            sb.Append("> ");

            Right.WriteCore(sb);

            if (!first)
                sb.Append(" )");
        }

        /// <inheritdoc/>
        public override bool Equals(OpenGaussTsQuery? other) =>
            other is OpenGaussTsQueryFollowedBy followedBy &&
            followedBy.Left == Left &&
            followedBy.Right == Right &&
            followedBy.Distance == Distance;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(Left, Right, Distance);
    }

    /// <summary>
    /// Represents an empty tsquery. Shold only be used as top node.
    /// </summary>
    public sealed class OpenGaussTsQueryEmpty : OpenGaussTsQuery
    {
        /// <summary>
        /// Creates a tsquery that represents an empty query. Should not be used as child node.
        /// </summary>
        public OpenGaussTsQueryEmpty() : base(NodeKind.Empty) {}

        internal override void WriteCore(StringBuilder sb, bool first = false) { }

        /// <inheritdoc/>
        public override bool Equals(OpenGaussTsQuery? other) =>
            other is OpenGaussTsQueryEmpty;

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Kind.GetHashCode();
    }
}
