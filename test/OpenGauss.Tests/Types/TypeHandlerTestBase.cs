using System.Threading.Tasks;
using OpenGauss.NET.Types;
using NUnit.Framework;
using OpenGauss.NET;

namespace OpenGauss.Tests.Types
{
    public abstract class TypeHandlerTestBase<T> : MultiplexingTestBase
    {
        readonly OpenGaussDbType? _opengaussDbType;
        readonly string? _typeName;

        protected TypeHandlerTestBase(MultiplexingMode multiplexingMode, OpenGaussDbType? opengaussDbType, string? typeName)
            : base(multiplexingMode)
            => (_opengaussDbType, _typeName) = (opengaussDbType, typeName);

        [Test]
        [TestCaseSource("TestCases")]
        public async Task Read(string query, T expected)
        {
            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand($"SELECT {query}", conn);

            Assert.That(await cmd.ExecuteScalarAsync(), Is.EqualTo(expected));
        }

        [Test]
        [TestCaseSource("TestCases")]
        public async Task Write(string query, T expected)
        {
            var parameter = new OpenGaussParameter<T>("p", expected);

            if (_opengaussDbType != null)
                parameter.OpenGaussDbType = _opengaussDbType.Value;

            if (_typeName != null)
                parameter.DataTypeName = _typeName;

            using var conn = await OpenConnectionAsync();
            using var cmd = new OpenGaussCommand($"SELECT {query}::text = @p::text", conn)
            {
                Parameters = { parameter }
            };

            Assert.That(await cmd.ExecuteScalarAsync(), Is.True);
        }
    }
}
