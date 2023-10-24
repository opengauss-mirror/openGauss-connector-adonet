using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using OpenGauss.NET;
using OpenGauss.NET.BackendMessages;
using OpenGauss.NET.Internal;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NetTopologySuite.Internal
{
    partial class NetTopologySuiteHandler : OpenGaussTypeHandler<Geometry>,
        IOpenGaussTypeHandler<Point>,
        IOpenGaussTypeHandler<LineString>,
        IOpenGaussTypeHandler<Polygon>,
        IOpenGaussTypeHandler<MultiPoint>,
        IOpenGaussTypeHandler<MultiLineString>,
        IOpenGaussTypeHandler<MultiPolygon>,
        IOpenGaussTypeHandler<GeometryCollection>
    {
        readonly PostGisReader _reader;
        readonly PostGisWriter _writer;
        readonly LengthStream _lengthStream = new();

        internal NetTopologySuiteHandler(PostgresType postgresType, PostGisReader reader, PostGisWriter writer)
            : base(postgresType)
        {
            _reader = reader;
            _writer = writer;
        }

        #region Read

        public override ValueTask<Geometry> Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription = null)
            => ReadCore<Geometry>(buf, len);

        ValueTask<Point> IOpenGaussTypeHandler<Point>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadCore<Point>(buf, len);

        ValueTask<LineString> IOpenGaussTypeHandler<LineString>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadCore<LineString>(buf, len);

        ValueTask<Polygon> IOpenGaussTypeHandler<Polygon>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadCore<Polygon>(buf, len);

        ValueTask<MultiPoint> IOpenGaussTypeHandler<MultiPoint>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadCore<MultiPoint>(buf, len);

        ValueTask<MultiLineString> IOpenGaussTypeHandler<MultiLineString>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadCore<MultiLineString>(buf, len);

        ValueTask<MultiPolygon> IOpenGaussTypeHandler<MultiPolygon>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadCore<MultiPolygon>(buf, len);

        ValueTask<GeometryCollection> IOpenGaussTypeHandler<GeometryCollection>.Read(OpenGaussReadBuffer buf, int len, bool async, FieldDescription? fieldDescription)
            => ReadCore<GeometryCollection>(buf, len);

        ValueTask<T> ReadCore<T>(OpenGaussReadBuffer buf, int len)
            where T : Geometry
            => new((T)_reader.Read(buf.GetStream(len, false)));

        #endregion

        #region ValidateAndGetLength

        public override int ValidateAndGetLength(Geometry value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLengthCore(value);

        int IOpenGaussTypeHandler<Point>.ValidateAndGetLength(Point value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength(value, ref lengthCache, parameter);

        int IOpenGaussTypeHandler<LineString>.ValidateAndGetLength(LineString value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength(value, ref lengthCache, parameter);

        int IOpenGaussTypeHandler<Polygon>.ValidateAndGetLength(Polygon value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength(value, ref lengthCache, parameter);

        int IOpenGaussTypeHandler<MultiPoint>.ValidateAndGetLength(MultiPoint value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength(value, ref lengthCache, parameter);

        int IOpenGaussTypeHandler<MultiLineString>.ValidateAndGetLength(MultiLineString value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength(value, ref lengthCache, parameter);

        int IOpenGaussTypeHandler<MultiPolygon>.ValidateAndGetLength(MultiPolygon value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength(value, ref lengthCache, parameter);

        int IOpenGaussTypeHandler<GeometryCollection>.ValidateAndGetLength(GeometryCollection value, ref OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter)
            => ValidateAndGetLength(value, ref lengthCache, parameter);

        int ValidateAndGetLengthCore(Geometry value)
        {
            _lengthStream.SetLength(0);
            _writer.Write(value, _lengthStream);
            return (int)_lengthStream.Length;
        }

        sealed class LengthStream : Stream
        {
            long _length;

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => _length;

            public override long Position
            {
                get => _length;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            { }

            public override int Read(byte[] buffer, int offset, int count)
                => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin)
                => throw new NotSupportedException();

            public override void SetLength(long value)
                => _length = value;

            public override void Write(byte[] buffer, int offset, int count)
                => _length += count;
        }

        #endregion

        #region Write

        public override Task Write(Geometry value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken = default)
            => WriteCore(value, buf);

        Task IOpenGaussTypeHandler<Point>.Write(Point value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken)
            => WriteCore(value, buf);

        Task IOpenGaussTypeHandler<LineString>.Write(LineString value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken)
            => WriteCore(value, buf);

        Task IOpenGaussTypeHandler<Polygon>.Write(Polygon value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken)
            => WriteCore(value, buf);

        Task IOpenGaussTypeHandler<MultiPoint>.Write(MultiPoint value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToke)
            => WriteCore(value, buf);

        Task IOpenGaussTypeHandler<MultiLineString>.Write(MultiLineString value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken)
            => WriteCore(value, buf);

        Task IOpenGaussTypeHandler<MultiPolygon>.Write(MultiPolygon value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken)
            => WriteCore(value, buf);

        Task IOpenGaussTypeHandler<GeometryCollection>.Write(GeometryCollection value, OpenGaussWriteBuffer buf, OpenGaussLengthCache? lengthCache, OpenGaussParameter? parameter, bool async, CancellationToken cancellationToken)
            => WriteCore(value, buf);

        Task WriteCore(Geometry value, OpenGaussWriteBuffer buf)
        {
            _writer.Write(value, buf.GetStream());
            return Task.CompletedTask;
        }

        #endregion
    }
}
