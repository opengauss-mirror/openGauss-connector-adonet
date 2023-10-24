using System.Threading.Tasks;
using OpenGauss.NET.Types;
using NUnit.Framework;
using OpenGauss.NET;

namespace OpenGauss.Tests.Types
{
    /// <summary>
    /// Tests on PostgreSQL geometric types
    /// </summary>
    /// <remarks>
    /// https://www.postgresql.org/docs/current/static/datatype-geometric.html
    /// </remarks>
    class GeometricTypeTests : MultiplexingTestBase
    {
        [Test]
        public async Task Point()
        {
            using var conn = await OpenConnectionAsync();
            var expected = new OpenGaussPoint(1.2, 3.4);
            var cmd = new OpenGaussCommand("SELECT @p1, @p2", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Point) {Value = expected};
            var p2 = new OpenGaussParameter {ParameterName = "p2", Value = expected};
            Assert.That(p2.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Point));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(OpenGaussPoint)));
                var actual = reader.GetFieldValue<OpenGaussPoint>(i);
                AssertPointsEqual(actual, expected);
            }
        }

        [Test]
        public async Task LineSegment()
        {
            using var conn = await OpenConnectionAsync();
            var expected = new OpenGaussLSeg(1, 2, 3, 4);
            var cmd = new OpenGaussCommand("SELECT @p1, @p2", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.LSeg) {Value = expected};
            var p2 = new OpenGaussParameter {ParameterName = "p2", Value = expected};
            Assert.That(p2.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.LSeg));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(OpenGaussLSeg)));
                var actual = reader.GetFieldValue<OpenGaussLSeg>(i);
                AssertPointsEqual(actual.Start, expected.Start);
                AssertPointsEqual(actual.End, expected.End);
            }
        }

        [Test]
        public async Task Box()
        {
            using var conn = await OpenConnectionAsync();
            var expected = new OpenGaussBox(2, 4, 1, 3);
            var cmd = new OpenGaussCommand("SELECT @p1, @p2", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Box) {Value = expected};
            var p2 = new OpenGaussParameter {ParameterName = "p2", Value = expected};
            Assert.That(p2.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Box));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(OpenGaussBox)));
                var actual = reader.GetFieldValue<OpenGaussBox>(i);
                AssertPointsEqual(actual.UpperRight, expected.UpperRight);
            }
        }

        [Test]
        public async Task Path()
        {
            using var conn = await OpenConnectionAsync();
            var expectedOpen = new OpenGaussPath(new[] {new OpenGaussPoint(1, 2), new OpenGaussPoint(3, 4)}, true);
            var expectedClosed = new OpenGaussPath(new[] {new OpenGaussPoint(1, 2), new OpenGaussPoint(3, 4)}, false);
            var cmd = new OpenGaussCommand("SELECT @p1, @p2, @p3", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Path) {Value = expectedOpen};
            var p2 = new OpenGaussParameter("p2", OpenGaussDbType.Path) {Value = expectedClosed};
            var p3 = new OpenGaussParameter {ParameterName = "p3", Value = expectedClosed};
            Assert.That(p3.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Path));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            cmd.Parameters.Add(p3);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                var expected = i == 0 ? expectedOpen : expectedClosed;
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(OpenGaussPath)));
                var actual = reader.GetFieldValue<OpenGaussPath>(i);
                Assert.That(actual.Open, Is.EqualTo(expected.Open));
                Assert.That(actual, Has.Count.EqualTo(expected.Count));
                for (var j = 0; j < actual.Count; j++)
                    AssertPointsEqual(actual[j], expected[j]);
            }
        }

        [Test]
        public async Task Polygon()
        {
            using var conn = await OpenConnectionAsync();
            var expected = new OpenGaussPolygon(new OpenGaussPoint(1, 2), new OpenGaussPoint(3, 4));
            var cmd = new OpenGaussCommand("SELECT @p1, @p2", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Polygon) {Value = expected};
            var p2 = new OpenGaussParameter {ParameterName = "p2", Value = expected};
            Assert.That(p2.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Polygon));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(OpenGaussPolygon)));
                var actual = reader.GetFieldValue<OpenGaussPolygon>(i);
                Assert.That(actual, Has.Count.EqualTo(expected.Count));
                for (var j = 0; j < actual.Count; j++)
                    AssertPointsEqual(actual[j], expected[j]);
            }
        }

        [Test]
        public async Task Circle()
        {
            using var conn = await OpenConnectionAsync();
            var expected = new OpenGaussCircle(1, 2, 0.5);
            var cmd = new OpenGaussCommand("SELECT @p1, @p2", conn);
            var p1 = new OpenGaussParameter("p1", OpenGaussDbType.Circle) {Value = expected};
            var p2 = new OpenGaussParameter {ParameterName = "p2", Value = expected};
            Assert.That(p2.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Circle));
            cmd.Parameters.Add(p1);
            cmd.Parameters.Add(p2);
            using var reader = await cmd.ExecuteReaderAsync();
            reader.Read();

            for (var i = 0; i < cmd.Parameters.Count; i++)
            {
                Assert.That(reader.GetFieldType(i), Is.EqualTo(typeof(OpenGaussCircle)));
                var actual = reader.GetFieldValue<OpenGaussCircle>(i);
                Assert.That(actual.X, Is.EqualTo(expected.X).Within(1).Ulps);
                Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(1).Ulps);
                Assert.That(actual.Radius, Is.EqualTo(expected.Radius).Within(1).Ulps);
            }
        }

        void AssertPointsEqual(OpenGaussPoint actual, OpenGaussPoint expected)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(1).Ulps);
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(1).Ulps);
        }

        public GeometricTypeTests(MultiplexingMode multiplexingMode) : base(multiplexingMode) {}
    }
}
