using System;
using BenchmarkDotNet.Attributes;
using OpenGauss.NET.Internal.TypeHandlers;

namespace OpenGauss.Benchmarks.TypeHandlers
{
    [Config(typeof(Config))]
    public class Uuid : TypeHandlerBenchmarks<Guid>
    {
        public Uuid() : base(new UuidHandler(GetPostgresType("uuid"))) { }
    }
}
