using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using OpenGauss.NET.Util;

#pragma warning disable 1591

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET.Types
{
    /// <summary>
    /// Represents a PostgreSQL point type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-geometric.html
    /// </remarks>
    public struct OpenGaussPoint : IEquatable<OpenGaussPoint>
    {
        static readonly Regex Regex = new(@"\((-?\d+.?\d*),(-?\d+.?\d*)\)");

        public double X { get; set; }
        public double Y { get; set; }

        public OpenGaussPoint(double x, double y)
            : this()
        {
            X = x;
            Y = y;
        }

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public bool Equals(OpenGaussPoint other) => X == other.X && Y == other.Y;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        public override bool Equals(object? obj)
            => obj is OpenGaussPoint point && Equals(point);

        public static bool operator ==(OpenGaussPoint x, OpenGaussPoint y) => x.Equals(y);

        public static bool operator !=(OpenGaussPoint x, OpenGaussPoint y) => !(x == y);

        public override int GetHashCode()
            => X.GetHashCode() ^ PGUtil.RotateShift(Y.GetHashCode(), PGUtil.BitsInInt / 2);

        public static OpenGaussPoint Parse(string s)
        {
            var m = Regex.Match(s);
            if (!m.Success) {
                throw new FormatException("Not a valid point: " + s);
            }
            return new OpenGaussPoint(double.Parse(m.Groups[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                                   double.Parse(m.Groups[2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat));
        }

        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "({0},{1})", X, Y);
    }

    /// <summary>
    /// Represents a PostgreSQL line type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-geometric.html
    /// </remarks>
    public struct OpenGaussLine : IEquatable<OpenGaussLine>
    {
        static readonly Regex Regex = new(@"\{(-?\d+.?\d*),(-?\d+.?\d*),(-?\d+.?\d*)\}");

        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }

        public OpenGaussLine(double a, double b, double c)
            : this()
        {
            A = a;
            B = b;
            C = c;
        }

        public static OpenGaussLine Parse(string s)
        {
            var m = Regex.Match(s);
            if (!m.Success)
                throw new FormatException("Not a valid line: " + s);
            return new OpenGaussLine(
                double.Parse(m.Groups[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse(m.Groups[2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse(m.Groups[3].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat)
            );
        }

        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "{{{0},{1},{2}}}", A, B, C);

        public override int GetHashCode() => A.GetHashCode() * B.GetHashCode() * C.GetHashCode();

        public bool Equals(OpenGaussLine other) => A == other.A && B == other.B && C == other.C;

        public override bool Equals(object? obj)
            => obj is OpenGaussLine line && Equals(line);

        public static bool operator ==(OpenGaussLine x, OpenGaussLine y) => x.Equals(y);
        public static bool operator !=(OpenGaussLine x, OpenGaussLine y) => !(x == y);
    }

    /// <summary>
    /// Represents a PostgreSQL Line Segment type.
    /// </summary>
    public struct OpenGaussLSeg : IEquatable<OpenGaussLSeg>
    {
        static readonly Regex Regex = new(@"\[\((-?\d+.?\d*),(-?\d+.?\d*)\),\((-?\d+.?\d*),(-?\d+.?\d*)\)\]");

        public OpenGaussPoint Start { get; set; }
        public OpenGaussPoint End { get; set; }

        public OpenGaussLSeg(OpenGaussPoint start, OpenGaussPoint end)
            : this()
        {
            Start = start;
            End = end;
        }

        public OpenGaussLSeg(double startx, double starty, double endx, double endy) : this()
        {
            Start = new OpenGaussPoint(startx, starty);
            End   = new OpenGaussPoint(endx,   endy);
        }

        public static OpenGaussLSeg Parse(string s)
        {
            var m = Regex.Match(s);
            if (!m.Success) {
                throw new FormatException("Not a valid line: " + s);
            }
            return new OpenGaussLSeg(
                double.Parse(m.Groups[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse(m.Groups[2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse(m.Groups[3].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse(m.Groups[4].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat)
            );

        }

        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "[{0},{1}]", Start, End);

        public override int GetHashCode()
            => Start.X.GetHashCode() ^
               PGUtil.RotateShift(Start.Y.GetHashCode(), PGUtil.BitsInInt / 4) ^
               PGUtil.RotateShift(End.X.GetHashCode(), PGUtil.BitsInInt / 2) ^
               PGUtil.RotateShift(End.Y.GetHashCode(), PGUtil.BitsInInt * 3 / 4);

        public bool Equals(OpenGaussLSeg other) => Start == other.Start && End == other.End;

        public override bool Equals(object? obj)
            => obj is OpenGaussLSeg seg && Equals(seg);

        public static bool operator ==(OpenGaussLSeg x, OpenGaussLSeg y) => x.Equals(y);
        public static bool operator !=(OpenGaussLSeg x, OpenGaussLSeg y) => !(x == y);
    }

    /// <summary>
    /// Represents a PostgreSQL box type.
    /// </summary>
    /// <remarks>
    /// See https://www.postgresql.org/docs/current/static/datatype-geometric.html
    /// </remarks>
    public struct OpenGaussBox : IEquatable<OpenGaussBox>
    {
        static readonly Regex Regex = new(@"\((-?\d+.?\d*),(-?\d+.?\d*)\),\((-?\d+.?\d*),(-?\d+.?\d*)\)");

        public OpenGaussPoint UpperRight { get; set; }
        public OpenGaussPoint LowerLeft { get; set; }

        public OpenGaussBox(OpenGaussPoint upperRight, OpenGaussPoint lowerLeft) : this()
        {
            UpperRight = upperRight;
            LowerLeft = lowerLeft;
        }

        public OpenGaussBox(double top, double right, double bottom, double left)
            : this(new OpenGaussPoint(right, top), new OpenGaussPoint(left, bottom)) { }

        public double Left => LowerLeft.X;
        public double Right => UpperRight.X;
        public double Bottom => LowerLeft.Y;
        public double Top => UpperRight.Y;
        public double Width => Right - Left;
        public double Height => Top - Bottom;

        public bool IsEmpty => Width == 0 || Height == 0;

        public bool Equals(OpenGaussBox other) => UpperRight == other.UpperRight && LowerLeft == other.LowerLeft;

        public override bool Equals(object? obj)
            => obj is OpenGaussBox box && Equals(box);

        public static bool operator ==(OpenGaussBox x, OpenGaussBox y) => x.Equals(y);
        public static bool operator !=(OpenGaussBox x, OpenGaussBox y) => !(x == y);
        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "{0},{1}", UpperRight, LowerLeft);

        public static OpenGaussBox Parse(string s)
        {
            var m = Regex.Match(s);
            return new OpenGaussBox(
                new OpenGaussPoint(double.Parse(m.Groups[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                                double.Parse(m.Groups[2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat)),
                new OpenGaussPoint(double.Parse(m.Groups[3].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                                double.Parse(m.Groups[4].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat))
            );
        }

        public override int GetHashCode()
            => Top.GetHashCode() ^
               PGUtil.RotateShift(Right.GetHashCode(), PGUtil.BitsInInt / 4) ^
               PGUtil.RotateShift(Bottom.GetHashCode(), PGUtil.BitsInInt / 2) ^
               PGUtil.RotateShift(LowerLeft.GetHashCode(), PGUtil.BitsInInt * 3 / 4);
    }

    /// <summary>
    /// Represents a PostgreSQL Path type.
    /// </summary>
    public struct OpenGaussPath : IList<OpenGaussPoint>, IEquatable<OpenGaussPath>
    {
        readonly List<OpenGaussPoint> _points;
        public bool Open { get; set; }

        public OpenGaussPath(IEnumerable<OpenGaussPoint> points, bool open) : this()
        {
            _points = new List<OpenGaussPoint>(points);
            Open = open;
        }

        public OpenGaussPath(IEnumerable<OpenGaussPoint> points) : this(points, false) {}
        public OpenGaussPath(params OpenGaussPoint[] points) : this(points, false) {}

        public OpenGaussPath(bool open) : this()
        {
            _points = new List<OpenGaussPoint>();
            Open = open;
        }

        public OpenGaussPath(int capacity, bool open) : this()
        {
            _points = new List<OpenGaussPoint>(capacity);
            Open = open;
        }

        public OpenGaussPath(int capacity) : this(capacity, false) {}

        public OpenGaussPoint this[int index]
        {
            get => _points[index];
            set => _points[index] = value;
        }

        public int Capacity => _points.Capacity;
        public int Count => _points.Count;
        public bool IsReadOnly => false;

        public int IndexOf(OpenGaussPoint item) => _points.IndexOf(item);
        public void Insert(int index, OpenGaussPoint item) => _points.Insert(index, item);
        public void RemoveAt(int index) => _points.RemoveAt(index);
        public void Add(OpenGaussPoint item) => _points.Add(item);
        public void Clear() =>  _points.Clear();
        public bool Contains(OpenGaussPoint item) => _points.Contains(item);
        public void CopyTo(OpenGaussPoint[] array, int arrayIndex) =>  _points.CopyTo(array, arrayIndex);
        public bool Remove(OpenGaussPoint item) =>  _points.Remove(item);
        public IEnumerator<OpenGaussPoint> GetEnumerator() =>  _points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(OpenGaussPath other)
        {
            if (Open != other.Open || Count != other.Count)
                return false;
            if (ReferenceEquals(_points, other._points))//Short cut for shallow copies.
                return true;
            for (var i = 0; i != Count; ++i)
                if (this[i] != other[i])
                    return false;
            return true;
        }

        public override bool Equals(object? obj)
            => obj is OpenGaussPath path && Equals(path);

        public static bool operator ==(OpenGaussPath x, OpenGaussPath y) => x.Equals(y);
        public static bool operator !=(OpenGaussPath x, OpenGaussPath y) => !(x == y);

        public override int GetHashCode()
        {
            var ret = 266370105;//seed with something other than zero to make paths of all zeros hash differently.
            foreach (var point in this)
            {
                //The ideal amount to shift each value is one that would evenly spread it throughout
                //the resultant bytes. Using the current result % 32 is essentially using a random value
                //but one that will be the same on subsequent calls.
                ret ^= PGUtil.RotateShift(point.GetHashCode(), ret % PGUtil.BitsInInt);
            }
            return Open ? ret : -ret;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Open ? '[' : '(');
            int i;
            for (i = 0; i < _points.Count; i++)
            {
                var p = _points[i];
                sb.AppendFormat(CultureInfo.InvariantCulture, "({0},{1})", p.X, p.Y);
                if (i < _points.Count - 1)
                    sb.Append(",");
            }
            sb.Append(Open ? ']' : ')');
            return sb.ToString();
        }

        public static OpenGaussPath Parse(string s)
        {
            var open = s[0] switch
            {
                '[' => true,
                '(' => false,
                _   => throw new Exception("Invalid path string: " + s)
            };
            Debug.Assert(s[s.Length - 1] == (open ? ']' : ')'));
            var result = new OpenGaussPath(open);
            var i = 1;
            while (true)
            {
                var i2 = s.IndexOf(')', i);
                result.Add(OpenGaussPoint.Parse(s.Substring(i, i2 - i + 1)));
                if (s[i2 + 1] != ',')
                    break;
                i = i2 + 2;
            }
            return result;
        }
    }

    /// <summary>
    /// Represents a PostgreSQL Polygon type.
    /// </summary>
    public struct OpenGaussPolygon : IList<OpenGaussPoint>, IEquatable<OpenGaussPolygon>
    {
        readonly List<OpenGaussPoint> _points;

        public OpenGaussPolygon(IEnumerable<OpenGaussPoint> points)
        {
            _points = new List<OpenGaussPoint>(points);
        }

        public OpenGaussPolygon(params OpenGaussPoint[] points) : this ((IEnumerable<OpenGaussPoint>) points) {}

        public OpenGaussPolygon(int capacity)
        {
            _points = new List<OpenGaussPoint>(capacity);
        }

        public OpenGaussPoint this[int index]
        {
            get => _points[index];
            set => _points[index] = value;
        }

        public int Capacity => _points.Capacity;
        public int Count => _points.Count;
        public bool IsReadOnly => false;

        public int IndexOf(OpenGaussPoint item) => _points.IndexOf(item);
        public void Insert(int index, OpenGaussPoint item) => _points.Insert(index, item);
        public void RemoveAt(int index) =>  _points.RemoveAt(index);
        public void Add(OpenGaussPoint item) =>  _points.Add(item);
        public void Clear() =>  _points.Clear();
        public bool Contains(OpenGaussPoint item) => _points.Contains(item);
        public void CopyTo(OpenGaussPoint[] array, int arrayIndex) => _points.CopyTo(array, arrayIndex);
        public bool Remove(OpenGaussPoint item) => _points.Remove(item);
        public IEnumerator<OpenGaussPoint> GetEnumerator() => _points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Equals(OpenGaussPolygon other)
        {
            if (Count != other.Count)
                return false;
            if (ReferenceEquals(_points, other._points))
                return true;
            for (var i = 0; i != Count; ++i)
                if (this[i] != other[i])
                    return false;
            return true;
        }

        public override bool Equals(object? obj)
            => obj is OpenGaussPolygon polygon && Equals(polygon);

        public static bool operator ==(OpenGaussPolygon x, OpenGaussPolygon y) => x.Equals(y);
        public static bool operator !=(OpenGaussPolygon x, OpenGaussPolygon y) => !(x == y);

        public override int GetHashCode()
        {
            var ret = 266370105;//seed with something other than zero to make paths of all zeros hash differently.
            foreach (var point in this)
            {
                //The ideal amount to shift each value is one that would evenly spread it throughout
                //the resultant bytes. Using the current result % 32 is essentially using a random value
                //but one that will be the same on subsequent calls.
                ret ^= PGUtil.RotateShift(point.GetHashCode(), ret % PGUtil.BitsInInt);
            }
            return ret;
        }

        public static OpenGaussPolygon Parse(string s)
        {
            var points = new List<OpenGaussPoint>();
            var i = 1;
            while (true)
            {
                var i2 = s.IndexOf(')', i);
                points.Add(OpenGaussPoint.Parse(s.Substring(i, i2 - i + 1)));
                if (s[i2 + 1] != ',')
                    break;
                i = i2 + 2;
            }
            return new OpenGaussPolygon(points);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('(');
            int i;
            for (i = 0; i < _points.Count; i++)
            {
                var p = _points[i];
                sb.AppendFormat(CultureInfo.InvariantCulture, "({0},{1})", p.X, p.Y);
                if (i < _points.Count - 1) {
                    sb.Append(",");
                }
            }
            sb.Append(')');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents a PostgreSQL Circle type.
    /// </summary>
    public struct OpenGaussCircle : IEquatable<OpenGaussCircle>
    {
        static readonly Regex Regex = new(@"<\((-?\d+.?\d*),(-?\d+.?\d*)\),(\d+.?\d*)>");

        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }

        public OpenGaussCircle(OpenGaussPoint center, double radius)
            : this()
        {
            X = center.X;
            Y = center.Y;
            Radius = radius;
        }

        public OpenGaussCircle(double x, double y, double radius) : this()
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public OpenGaussPoint Center
        {
            get => new(X, Y);
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public bool Equals(OpenGaussCircle other)
            => X == other.X && Y == other.Y && Radius == other.Radius;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        public override bool Equals(object? obj)
            => obj is OpenGaussCircle circle && Equals(circle);

        public static OpenGaussCircle Parse(string s)
        {
            var m = Regex.Match(s);
            if (!m.Success)
                throw new FormatException("Not a valid circle: " + s);

            return new OpenGaussCircle(
                double.Parse(m.Groups[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse(m.Groups[2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat),
                double.Parse(m.Groups[3].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat)
            );
        }

        public override string ToString()
            => string.Format(CultureInfo.InvariantCulture, "<({0},{1}),{2}>", X, Y, Radius);

        public static bool operator ==(OpenGaussCircle x, OpenGaussCircle y) => x.Equals(y);
        public static bool operator !=(OpenGaussCircle x, OpenGaussCircle y) => !(x == y);

        public override int GetHashCode()
            => X.GetHashCode() * Y.GetHashCode() * Radius.GetHashCode();
    }

    /// <summary>
    /// Represents a PostgreSQL inet type, which is a combination of an IPAddress and a
    /// subnet mask.
    /// </summary>
    /// <remarks>
    /// https://www.postgresql.org/docs/current/static/datatype-net-types.html
    /// </remarks>
    [Obsolete("Use ValueTuple<IPAddress, int> instead")]
    public struct OpenGaussInet : IEquatable<OpenGaussInet>
    {
        public IPAddress Address { get; set; }
        public int Netmask { get; set; }

        public OpenGaussInet(IPAddress address, int netmask)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                throw new ArgumentException("Only IPAddress of InterNetwork or InterNetworkV6 address families are accepted", nameof(address));

            Address = address;
            Netmask = netmask;
        }

        public OpenGaussInet(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                throw new ArgumentException("Only IPAddress of InterNetwork or InterNetworkV6 address families are accepted", nameof(address));

            Address = address;
            Netmask = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        }

        public OpenGaussInet(string addr)
        {
            if (addr.IndexOf('/') > 0)
            {
                var addrbits = addr.Split('/');
                if (addrbits.GetUpperBound(0) != 1) {
                    throw new FormatException("Invalid number of parts in CIDR specification");
                }
                Address = IPAddress.Parse(addrbits[0]);
                Netmask = int.Parse(addrbits[1]);
            }
            else
            {
                Address = IPAddress.Parse(addr);
                Netmask = 32;
            }
        }

        public override string ToString()
        {
            if ((Address.AddressFamily == AddressFamily.InterNetwork   && Netmask == 32) ||
                (Address.AddressFamily == AddressFamily.InterNetworkV6 && Netmask == 128))
            {
                return Address.ToString();
            }
            return $"{Address}/{Netmask}";
        }

        // ReSharper disable once InconsistentNaming
        public static IPAddress ToIPAddress(OpenGaussInet inet)
        {
            if (inet.Netmask != 32)
                throw new InvalidCastException("Cannot cast CIDR network to address");
            return inet.Address;
        }

        public static explicit operator IPAddress(OpenGaussInet inet) => ToIPAddress(inet);

        public static OpenGaussInet ToOpenGaussInet(IPAddress? ip)
            => ip is null ? default : new OpenGaussInet(ip);
            //=> ReferenceEquals(ip, null) ? default : new OpenGaussInet(ip);

        public static implicit operator OpenGaussInet(IPAddress ip) => ToOpenGaussInet(ip);

        public void Deconstruct(out IPAddress address, out int netmask)
        {
            address = Address;
            netmask = Netmask;
        }

        public bool Equals(OpenGaussInet other) => Address.Equals(other.Address) && Netmask == other.Netmask;

        public override bool Equals(object? obj)
            => obj is OpenGaussInet inet && Equals(inet);

        public override int GetHashCode()
            => PGUtil.RotateShift(Address.GetHashCode(), Netmask%32);

        public static bool operator ==(OpenGaussInet x, OpenGaussInet y) => x.Equals(y);
        public static bool operator !=(OpenGaussInet x, OpenGaussInet y) => !(x == y);
    }

    /// <summary>
    /// Represents a PostgreSQL tid value
    /// </summary>
    /// <remarks>
    /// https://www.postgresql.org/docs/current/static/datatype-oid.html
    /// </remarks>
    public readonly struct OpenGaussTid : IEquatable<OpenGaussTid>
    {
        /// <summary>
        /// Block number
        /// </summary>
        public uint BlockNumber { get; }

        /// <summary>
        /// Tuple index within block
        /// </summary>
        public ushort OffsetNumber { get; }

        public OpenGaussTid(uint blockNumber, ushort offsetNumber)
        {
            BlockNumber = blockNumber;
            OffsetNumber = offsetNumber;
        }

        public bool Equals(OpenGaussTid other)
            => BlockNumber == other.BlockNumber && OffsetNumber == other.OffsetNumber;

        public override bool Equals(object? o)
            => o is OpenGaussTid tid && Equals(tid);

        public override int GetHashCode() => (int)BlockNumber ^ OffsetNumber;
        public static bool operator ==(OpenGaussTid left, OpenGaussTid right) => left.Equals(right);
        public static bool operator !=(OpenGaussTid left, OpenGaussTid right) => !(left == right);
        public override string ToString() => $"({BlockNumber},{OffsetNumber})";
    }
}

#pragma warning restore 1591
