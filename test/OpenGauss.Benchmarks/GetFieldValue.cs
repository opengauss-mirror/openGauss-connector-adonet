using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace OpenGauss.Benchmarks
{
    [Config(typeof(Config))]
    public class GetFieldValue
    {
        readonly OpenGaussConnection _conn;
        readonly OpenGaussCommand _cmd;
        readonly OpenGaussDataReader _reader;

        public GetFieldValue()
        {
            _conn = BenchmarkEnvironment.OpenConnection();
            _cmd = new OpenGaussCommand("SELECT 0, 'str'", _conn);
            _reader = _cmd.ExecuteReader();
            _reader.Read();
        }

        [Benchmark]
        public void NullableField() => _reader.GetFieldValue<int?>(0);

        [Benchmark]
        public void ValueTypeField() => _reader.GetFieldValue<int>(0);

        [Benchmark]
        public void ReferenceTypeField() => _reader.GetFieldValue<string>(1);

        [Benchmark]
        public void ObjectField() => _reader.GetFieldValue<object>(1);

        class Config : ManualConfig
        {
            public Config() => AddColumn(StatisticColumn.OperationsPerSecond);
        }
    }
}
