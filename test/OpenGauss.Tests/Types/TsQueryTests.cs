using System.Collections;
using OpenGauss.NET.Types;
using NUnit.Framework;

namespace OpenGauss.Tests.Types
{
    [TestFixture(MultiplexingMode.NonMultiplexing, false)]
    [TestFixture(MultiplexingMode.NonMultiplexing, true)]
    [TestFixture(MultiplexingMode.Multiplexing, false)]
    [TestFixture(MultiplexingMode.Multiplexing, true)]
    public sealed class TsQueryTests : TypeHandlerTestBase<OpenGaussTsQuery>
    {
        public TsQueryTests(MultiplexingMode multiplexingMode, bool useTypeName) : base(
            multiplexingMode,
            useTypeName ? null : OpenGaussDbType.TsQuery,
            useTypeName ? "tsquery" : null)
        { }

        public static IEnumerable TestCases() => new[]
        {
            new object[]
            {
                "$$'a'$$::tsquery",
                new OpenGaussTsQueryLexeme("a")
            },
            new object[]
            {
                "$$!'a'$$::tsquery",
                new OpenGaussTsQueryNot(
                    new OpenGaussTsQueryLexeme("a"))
            },
            new object[]
            {
                "$$'a' | 'b'$$::tsquery",
                new OpenGaussTsQueryOr(
                    new OpenGaussTsQueryLexeme("a"),
                    new OpenGaussTsQueryLexeme("b"))
            },
            new object[]
            {
                "$$'a' & 'b'$$::tsquery",
                new OpenGaussTsQueryAnd(
                    new OpenGaussTsQueryLexeme("a"),
                    new OpenGaussTsQueryLexeme("b"))
            },
            //new object[]
            //{
            //    "$$'a' <-> 'b'$$::tsquery",
            //    new OpenGaussTsQueryFollowedBy(
            //        new OpenGaussTsQueryLexeme("a"), 1, new OpenGaussTsQueryLexeme("b"))
            //},
            //new object[]
            //{
            //    "$$('a' & !('c' | 'd')) & (!!'a' & 'b') | 'ä' | 'x' <-> 'y' | 'x' <10> 'y' | 'd' <0> 'e' | 'f'$$::tsquery",
            //    new OpenGaussTsQueryOr(
            //        new OpenGaussTsQueryOr(
            //            new OpenGaussTsQueryOr(
            //                new OpenGaussTsQueryOr(
            //                    new OpenGaussTsQueryOr(
            //                        new OpenGaussTsQueryAnd(
            //                            new OpenGaussTsQueryAnd(
            //                                new OpenGaussTsQueryLexeme("a"),
            //                                new OpenGaussTsQueryNot(
            //                                    new OpenGaussTsQueryOr(
            //                                        new OpenGaussTsQueryLexeme("c"),
            //                                        new OpenGaussTsQueryLexeme("d")))),
            //                            new OpenGaussTsQueryAnd(
            //                                new OpenGaussTsQueryNot(
            //                                    new OpenGaussTsQueryNot(
            //                                        new OpenGaussTsQueryLexeme("a"))),
            //                                new OpenGaussTsQueryLexeme("b"))),
            //                        new OpenGaussTsQueryLexeme("ä")),
            //                    new OpenGaussTsQueryFollowedBy(
            //                        new OpenGaussTsQueryLexeme("x"), 1, new OpenGaussTsQueryLexeme("y"))),
            //                new OpenGaussTsQueryFollowedBy(
            //                    new OpenGaussTsQueryLexeme("x"), 10, new OpenGaussTsQueryLexeme("y"))),
            //            new OpenGaussTsQueryFollowedBy(
            //                new OpenGaussTsQueryLexeme("d"), 0, new OpenGaussTsQueryLexeme("e"))),
            //        new OpenGaussTsQueryLexeme("f"))
            //}
        };
    }
}
