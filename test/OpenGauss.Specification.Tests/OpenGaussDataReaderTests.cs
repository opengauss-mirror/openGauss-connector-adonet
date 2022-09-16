using AdoNet.Specification.Tests;
using Xunit;

namespace OpenGauss.Specification.Tests
{
    public sealed class OpenGaussDataReaderTests : DataReaderTestBase<OpenGaussSelectValueFixture>
    {
        public OpenGaussDataReaderTests(OpenGaussSelectValueFixture fixture)
            : base(fixture) {}
    }
}
