using AdoNet.Specification.Tests;

namespace OpenGauss.Specification.Tests
{
    public sealed class OpenGaussCommandTests : CommandTestBase<OpenGaussDbFactoryFixture>
    {
        public OpenGaussCommandTests(OpenGaussDbFactoryFixture fixture)
            : base(fixture)
        {
        }

        // PostgreSQL only supports a single transaction on a given connection at a given time. As a result,
        // OpenGauss completely ignores DbCommand.Transaction.
        public override void ExecuteReader_throws_when_transaction_required() {}
        public override void ExecuteReader_throws_when_transaction_mismatched() {}
    }
}
