using BenchmarkDotNet.Attributes;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.TypeMapping;
using OpenGauss.NET.Types;

namespace OpenGauss.Benchmarks
{
    [MemoryDiagnoser]
    public class ResolveHandler
    {
        OpenGaussConnection _conn = null!;
        ConnectorTypeMapper _typeMapper = null!;

        [Params(0, 1, 2)]
        public int NumPlugins { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _conn = BenchmarkEnvironment.OpenConnection();
            _typeMapper = (ConnectorTypeMapper)_conn.TypeMapper;

            if (NumPlugins > 0)
                _typeMapper.UseNodaTime();
            if (NumPlugins > 1)
                _typeMapper.UseNetTopologySuite();
        }

        [GlobalCleanup]
        public void Cleanup() => _conn.Dispose();

        [Benchmark]
        public OpenGaussTypeHandler ResolveOID()
            => _typeMapper.ResolveByOID(23); // int4

        [Benchmark]
        public OpenGaussTypeHandler ResolveOpenGaussDbType()
            => _typeMapper.ResolveByOpenGaussDbType(OpenGaussDbType.Integer);

        [Benchmark]
        public OpenGaussTypeHandler ResolveDataTypeName()
            => _typeMapper.ResolveByDataTypeName("integer");

        [Benchmark]
        public OpenGaussTypeHandler ResolveClrTypeNonGeneric()
            => _typeMapper.ResolveByValue((object)8);

        [Benchmark]
        public OpenGaussTypeHandler ResolveClrTypeGeneric()
            => _typeMapper.ResolveByValue(8);

    }
}
