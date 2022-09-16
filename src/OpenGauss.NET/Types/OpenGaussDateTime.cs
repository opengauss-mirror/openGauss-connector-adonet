using System;
using System.Collections;
using System.Collections.Generic;
using OpenGauss.NET.Util;

#pragma warning disable 1591

// ReSharper disable once CheckNamespace
namespace OpenGauss.NET.Types
{
    /// <summary>
    /// A struct similar to .NET DateTime but capable of storing PostgreSQL's timestamp and timestamptz types.
    /// DateTime is capable of storing values from year 1 to 9999 at 100-nanosecond precision,
    /// while PostgreSQL's timestamps store values from 4713BC to 5874897AD with 1-microsecond precision.
    /// </summary>
    [Obsolete(
        "For values outside the range of DateTime, consider using NodaTime (range -9998 to 9999), or read the value as a 'long'. " +
        "See https://www.opengauss.org/doc/types/datetime.html for more information.")]
    [Serializable]
    public readonly struct OpenGaussDateTime : IEquatable<OpenGaussDateTime>, IComparable<OpenGaussDateTime>, IComparable,
        IComparer<OpenGaussDateTime>, IComparer
    {
        #region Fields

        readonly OpenGaussDate _date;
        readonly TimeSpan _time;
        readonly InternalType _type;

        #endregion

        #region Constants

        public static readonly OpenGaussDateTime Epoch = new(OpenGaussDate.Epoch);
        public static readonly OpenGaussDateTime Era = new(OpenGaussDate.Era);

        public static readonly OpenGaussDateTime Infinity =
            new(InternalType.Infinity, OpenGaussDate.Era, TimeSpan.Zero);

        public static readonly OpenGaussDateTime NegativeInfinity =
            new(InternalType.NegativeInfinity, OpenGaussDate.Era, TimeSpan.Zero);

        // 9999-12-31
        const int MaxDateTimeDay = 3652058;

        #endregion

        #region Constructors

        OpenGaussDateTime(InternalType type, OpenGaussDate date, TimeSpan time)
        {
            if (!date.IsFinite && type != InternalType.Infinity && type != InternalType.NegativeInfinity)
                throw new ArgumentException("Can't construct an OpenGaussDateTime with a non-finite date, use Infinity and NegativeInfinity instead", nameof(date));

            _type = type;
            _date = date;
            _time = time;
        }

        public OpenGaussDateTime(OpenGaussDate date, TimeSpan time, DateTimeKind kind = DateTimeKind.Unspecified)
            : this(KindToInternalType(kind), date, time) {}

        public OpenGaussDateTime(OpenGaussDate date)
            : this(date, TimeSpan.Zero) {}

        public OpenGaussDateTime(int year, int month, int day, int hours, int minutes, int seconds, DateTimeKind kind=DateTimeKind.Unspecified)
            : this(new OpenGaussDate(year, month, day), new TimeSpan(0, hours, minutes, seconds), kind) {}

        public OpenGaussDateTime(int year, int month, int day, int hours, int minutes, int seconds, int milliseconds, DateTimeKind kind = DateTimeKind.Unspecified)
            : this(new OpenGaussDate(year, month, day), new TimeSpan(0, hours, minutes, seconds, milliseconds), kind) { }

        public OpenGaussDateTime(DateTime dateTime)
            : this(new OpenGaussDate(dateTime.Date), dateTime.TimeOfDay, dateTime.Kind) {}

        public OpenGaussDateTime(long ticks, DateTimeKind kind)
            : this(new DateTime(ticks, kind)) { }

        public OpenGaussDateTime(long ticks)
            : this(new DateTime(ticks, DateTimeKind.Unspecified)) { }

        #endregion

        #region Public Properties

        public OpenGaussDate Date => _date;
        public TimeSpan Time => _time;
        public int DayOfYear => _date.DayOfYear;
        public int Year => _date.Year;
        public int Month => _date.Month;
        public int Day => _date.Day;
        public DayOfWeek DayOfWeek => _date.DayOfWeek;
        public bool IsLeapYear => _date.IsLeapYear;

        public long Ticks => _date.DaysSinceEra * OpenGaussTimeSpan.TicksPerDay + _time.Ticks;
        public int Millisecond => _time.Milliseconds;
        public int Second => _time.Seconds;
        public int Minute => _time.Minutes;
        public int Hour => _time.Hours;
        public bool IsInfinity => _type == InternalType.Infinity;
        public bool IsNegativeInfinity => _type == InternalType.NegativeInfinity;

        public bool IsFinite
            => _type switch
            {
                InternalType.FiniteUnspecified => true,
                InternalType.FiniteUtc         => true,
                InternalType.FiniteLocal       => true,
                InternalType.Infinity          => false,
                InternalType.NegativeInfinity  => false,
                _ => throw new InvalidOperationException($"Internal OpenGauss bug: unexpected value {_type} of enum {nameof(OpenGaussDateTime)}.{nameof(InternalType)}. Please file a bug.")
            };

        public DateTimeKind Kind
            => _type switch
            {
                InternalType.FiniteUtc         => DateTimeKind.Utc,
                InternalType.FiniteLocal       => DateTimeKind.Local,
                InternalType.FiniteUnspecified => DateTimeKind.Unspecified,
                InternalType.Infinity          => DateTimeKind.Unspecified,
                InternalType.NegativeInfinity  => DateTimeKind.Unspecified,
                _ => throw new InvalidOperationException($"Internal OpenGauss bug: unexpected value {_type} of enum {nameof(DateTimeKind)}. Please file a bug.")
            };

        /// <summary>
        /// Cast of an <see cref="OpenGaussDateTime"/> to a <see cref="DateTime"/>.
        /// </summary>
        /// <returns>An equivalent <see cref="DateTime"/>.</returns>
        public DateTime ToDateTime()
        {
            if (!IsFinite)
                throw new InvalidCastException("Can't convert infinite timestamp values to DateTime");

            if (_date.DaysSinceEra < 0 || _date.DaysSinceEra > MaxDateTimeDay)
                throw new InvalidCastException("Out of the range of DateTime (year must be between 1 and 9999)");

            return new DateTime(Ticks, Kind);
        }

        /// <summary>
        /// Converts the value of the current <see cref="OpenGaussDateTime"/> object to Coordinated Universal Time (UTC).
        /// </summary>
        /// <remarks>
        /// See the MSDN documentation for DateTime.ToUniversalTime().
        /// <b>Note:</b> this method <b>only</b> takes into account the time zone's base offset, and does
        /// <b>not</b> respect daylight savings. See https://github.com/opengauss/opengauss/pull/684 for more
        /// details.
        /// </remarks>
        public OpenGaussDateTime ToUniversalTime()
        {
            switch (_type)
            {
            case InternalType.FiniteUnspecified:
                // Treat as Local
            case InternalType.FiniteLocal:
                if (_date.DaysSinceEra >= 1 && _date.DaysSinceEra <= MaxDateTimeDay - 1)
                {
                    // Day between 0001-01-02 and 9999-12-30, so we can use DateTime and it will always succeed
                    return new OpenGaussDateTime(Subtract(TimeZoneInfo.Local.GetUtcOffset(new DateTime(ToDateTime().Ticks, DateTimeKind.Local))).Ticks, DateTimeKind.Utc);
                }
                // Else there are no DST rules available in the system for outside the DateTime range, so just use the base offset
                var timeTicks = _time.Ticks - TimeZoneInfo.Local.BaseUtcOffset.Ticks;
                var date = _date;
                if (timeTicks < 0)
                {
                    timeTicks += OpenGaussTimeSpan.TicksPerDay;
                    date = date.AddDays(-1);
                }
                else if (timeTicks > OpenGaussTimeSpan.TicksPerDay)
                {
                    timeTicks -= OpenGaussTimeSpan.TicksPerDay;
                    date = date.AddDays(1);
                }
                return new OpenGaussDateTime(date, TimeSpan.FromTicks(timeTicks), DateTimeKind.Utc);
            case InternalType.FiniteUtc:
            case InternalType.Infinity:
            case InternalType.NegativeInfinity:
                return this;
            default:
                throw new InvalidOperationException($"Internal OpenGauss bug: unexpected value {_type} of enum {nameof(OpenGaussDateTime)}.{nameof(InternalType)}. Please file a bug.");
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="OpenGaussDateTime"/> object to local time.
        /// </summary>
        /// <remarks>
        /// See the MSDN documentation for DateTime.ToLocalTime().
        /// <b>Note:</b> this method <b>only</b> takes into account the time zone's base offset, and does
        /// <b>not</b> respect daylight savings. See https://github.com/opengauss/opengauss/pull/684 for more
        /// details.
        /// </remarks>
        public OpenGaussDateTime ToLocalTime()
        {
            switch (_type) {
            case InternalType.FiniteUnspecified:
                // Treat as UTC
            case InternalType.FiniteUtc:
                if (_date.DaysSinceEra >= 1 && _date.DaysSinceEra <= MaxDateTimeDay - 1)
                {
                    // Day between 0001-01-02 and 9999-12-30, so we can use DateTime and it will always succeed
                    return new OpenGaussDateTime(TimeZoneInfo.ConvertTime(new DateTime(ToDateTime().Ticks, DateTimeKind.Utc), TimeZoneInfo.Local));
                }
                // Else there are no DST rules available in the system for outside the DateTime range, so just use the base offset
                var timeTicks = _time.Ticks + TimeZoneInfo.Local.BaseUtcOffset.Ticks;
                var date = _date;
                if (timeTicks < 0)
                {
                    timeTicks += OpenGaussTimeSpan.TicksPerDay;
                    date = date.AddDays(-1);
                }
                else if (timeTicks > OpenGaussTimeSpan.TicksPerDay)
                {
                    timeTicks -= OpenGaussTimeSpan.TicksPerDay;
                    date = date.AddDays(1);
                }
                return new OpenGaussDateTime(date, TimeSpan.FromTicks(timeTicks), DateTimeKind.Local);
            case InternalType.FiniteLocal:
            case InternalType.Infinity:
            case InternalType.NegativeInfinity:
                return this;
            default:
                throw new InvalidOperationException($"Internal OpenGauss bug: unexpected value {_type} of enum {nameof(OpenGaussDateTime)}.{nameof(InternalType)}. Please file a bug.");
            }
        }

        public static OpenGaussDateTime Now => new(DateTime.Now);

        #endregion

        #region String Conversions

        public override string ToString()
            => _type switch
            {
                InternalType.Infinity         => "infinity",
                InternalType.NegativeInfinity => "-infinity",
                _                             => $"{_date} {_time}"
            };

        public static OpenGaussDateTime Parse(string str)
        {
            if (str == null) {
                throw new NullReferenceException();
            }
            switch (str = str.Trim().ToLowerInvariant()) {
            case "infinity":
                return Infinity;
            case "-infinity":
                return NegativeInfinity;
            default:
                try {
                    var idxSpace = str.IndexOf(' ');
                    var datePart = str.Substring(0, idxSpace);
                    if (str.Contains("bc")) {
                        datePart += " BC";
                    }
                    var idxSecond = str.IndexOf(' ', idxSpace + 1);
                    if (idxSecond == -1) {
                        idxSecond = str.Length;
                    }
                    var timePart = str.Substring(idxSpace + 1, idxSecond - idxSpace - 1);
                    return new OpenGaussDateTime(OpenGaussDate.Parse(datePart), TimeSpan.Parse(timePart));
                } catch (OverflowException) {
                    throw;
                } catch {
                    throw new FormatException();
                }
            }
        }

        #endregion

        #region Comparisons

        public bool Equals(OpenGaussDateTime other)
            => _type switch
            {
                InternalType.Infinity         => other._type == InternalType.Infinity,
                InternalType.NegativeInfinity => other._type == InternalType.NegativeInfinity,
                _                             => other._type == _type && _date.Equals(other._date) && _time.Equals(other._time)
            };

        public override bool Equals(object? obj)
            => obj is OpenGaussDateTime time && Equals(time);

        public override int GetHashCode()
            => _type switch
            {
                InternalType.Infinity         => int.MaxValue,
                InternalType.NegativeInfinity => int.MinValue,
                _ => _date.GetHashCode() ^ PGUtil.RotateShift(_time.GetHashCode(), 16)
            };

        public int CompareTo(OpenGaussDateTime other)
        {
            switch (_type) {
            case InternalType.Infinity:
                return other._type == InternalType.Infinity ? 0 : 1;
            case InternalType.NegativeInfinity:
                return other._type == InternalType.NegativeInfinity ? 0 : -1;
            default:
                switch (other._type) {
                case InternalType.Infinity:
                    return -1;
                case InternalType.NegativeInfinity:
                    return 1;
                default:
                    var cmp = _date.CompareTo(other._date);
                    return cmp == 0 ? _time.CompareTo(other._time) : cmp;
                }
            }
        }

        public int CompareTo(object? o)
            => o == null
                ? 1
                : o is OpenGaussDateTime opengaussDateTime
                    ? CompareTo(opengaussDateTime)
                    : throw new ArgumentException();

        public int Compare(OpenGaussDateTime x, OpenGaussDateTime y) => x.CompareTo(y);

        public int Compare(object? x, object? y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            if (y == null)
                return 1;
            if (!(x is IComparable) || !(y is IComparable))
                throw new ArgumentException();
            return ((IComparable)x).CompareTo(y);
        }

        #endregion

        #region Arithmetic

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the value of the specified <see cref="OpenGaussTimeSpan"/> to the value of this instance.
        /// </summary>
        /// <param name="value">An OpenGaussTimeSpan interval.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the time interval represented by value.</returns>
        public OpenGaussDateTime Add(in OpenGaussTimeSpan value) => AddTicks(value.UnjustifyInterval().TotalTicks);

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the value of the specified TimeSpan to the value of this instance.
        /// </summary>
        /// <param name="value">A positive or negative time interval.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the time interval represented by value.</returns>
        public OpenGaussDateTime Add(TimeSpan value) { return AddTicks(value.Ticks); }

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of years to the value of this instance.
        /// </summary>
        /// <param name="value">A number of years. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of years represented by value.</returns>
        public OpenGaussDateTime AddYears(int value)
            => _type switch
            {
                InternalType.Infinity         => this,
                InternalType.NegativeInfinity => this,
                _                             => new OpenGaussDateTime(_type, _date.AddYears(value), _time)
            };

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of months to the value of this instance.
        /// </summary>
        /// <param name="value">A number of months. The months parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and months.</returns>
        public OpenGaussDateTime AddMonths(int value)
            => _type switch
            {
                InternalType.Infinity         => this,
                InternalType.NegativeInfinity => this,
                _                             => new OpenGaussDateTime(_type, _date.AddMonths(value), _time)
            };

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of days to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional days. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of days represented by value.</returns>
        public OpenGaussDateTime AddDays(double value) => Add(TimeSpan.FromDays(value));

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of hours to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional hours. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of hours represented by value.</returns>
        public OpenGaussDateTime AddHours(double value) => Add(TimeSpan.FromHours(value));

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of minutes to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional minutes. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of minutes represented by value.</returns>
        public OpenGaussDateTime AddMinutes(double value) => Add(TimeSpan.FromMinutes(value));

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of minutes to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional minutes. The value parameter can be negative or positive.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of minutes represented by value.</returns>
        public OpenGaussDateTime AddSeconds(double value) => Add(TimeSpan.FromSeconds(value));

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of milliseconds to the value of this instance.
        /// </summary>
        /// <param name="value">A number of whole and fractional milliseconds. The value parameter can be negative or positive. Note that this value is rounded to the nearest integer.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the number of milliseconds represented by value.</returns>
        public OpenGaussDateTime AddMilliseconds(double value) => Add(TimeSpan.FromMilliseconds(value));

        /// <summary>
        /// Returns a new <see cref="OpenGaussDateTime"/> that adds the specified number of ticks to the value of this instance.
        /// </summary>
        /// <param name="value">A number of 100-nanosecond ticks. The value parameter can be positive or negative.</param>
        /// <returns>An object whose value is the sum of the date and time represented by this instance and the time represented by value.</returns>
        public OpenGaussDateTime AddTicks(long value)
            => _type switch
            {
                InternalType.Infinity         => this,
                InternalType.NegativeInfinity => this,
                _                             => new OpenGaussDateTime(Ticks + value, Kind),
            };

        public OpenGaussDateTime Subtract(in OpenGaussTimeSpan interval) =>  Add(-interval);

        public OpenGaussTimeSpan Subtract(OpenGaussDateTime timestamp)
        {
            switch (_type) {
            case InternalType.Infinity:
            case InternalType.NegativeInfinity:
                throw new InvalidOperationException("You cannot subtract infinity timestamps");
            }
            switch (timestamp._type) {
            case InternalType.Infinity:
            case InternalType.NegativeInfinity:
                throw new InvalidOperationException("You cannot subtract infinity timestamps");
            }
            return new OpenGaussTimeSpan(0, _date.DaysSinceEra - timestamp._date.DaysSinceEra, _time.Ticks - timestamp._time.Ticks);
        }

        #endregion

        #region Operators

        public static OpenGaussDateTime operator +(OpenGaussDateTime timestamp, OpenGaussTimeSpan interval)
            => timestamp.Add(interval);

        public static OpenGaussDateTime operator +(OpenGaussTimeSpan interval, OpenGaussDateTime timestamp)
            => timestamp.Add(interval);

        public static OpenGaussDateTime operator -(OpenGaussDateTime timestamp, OpenGaussTimeSpan interval)
            => timestamp.Subtract(interval);

        public static OpenGaussTimeSpan operator -(OpenGaussDateTime x, OpenGaussDateTime y) => x.Subtract(y);
        public static bool operator ==(OpenGaussDateTime x, OpenGaussDateTime y) => x.Equals(y);
        public static bool operator !=(OpenGaussDateTime x, OpenGaussDateTime y) => !(x == y);
        public static bool operator <(OpenGaussDateTime x, OpenGaussDateTime y) => x.CompareTo(y) < 0;
        public static bool operator >(OpenGaussDateTime x, OpenGaussDateTime y) => x.CompareTo(y) > 0;
        public static bool operator <=(OpenGaussDateTime x, OpenGaussDateTime y) => x.CompareTo(y) <= 0;
        public static bool operator >=(OpenGaussDateTime x, OpenGaussDateTime y) => x.CompareTo(y) >= 0;

        #endregion

        #region Casts

        /// <summary>
        /// Implicit cast of a <see cref="DateTime"/> to an <see cref="OpenGaussDateTime"/>
        /// </summary>
        /// <param name="dateTime">A <see cref="DateTime"/></param>
        /// <returns>An equivalent <see cref="OpenGaussDateTime"/>.</returns>
        public static implicit operator OpenGaussDateTime(DateTime dateTime) => ToOpenGaussDateTime(dateTime);
        public static OpenGaussDateTime ToOpenGaussDateTime(DateTime dateTime) => new(dateTime);

        /// <summary>
        /// Explicit cast of an <see cref="OpenGaussDateTime"/> to a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="opengaussDateTime">An <see cref="OpenGaussDateTime"/>.</param>
        /// <returns>An equivalent <see cref="DateTime"/>.</returns>
        public static explicit operator DateTime(OpenGaussDateTime opengaussDateTime)
            => opengaussDateTime.ToDateTime();

        #endregion

        public OpenGaussDateTime Normalize() => Add(OpenGaussTimeSpan.Zero);

        static InternalType KindToInternalType(DateTimeKind kind)
            => kind switch
            {
                DateTimeKind.Unspecified => InternalType.FiniteUnspecified,
                DateTimeKind.Utc         => InternalType.FiniteUtc,
                DateTimeKind.Local       => InternalType.FiniteLocal,
                _ => throw new InvalidOperationException($"Internal OpenGauss bug: unexpected value {kind} of enum {nameof(OpenGaussDateTime)}.{nameof(InternalType)}. Please file a bug.")
            };

        enum InternalType
        {
            FiniteUnspecified,
            FiniteUtc,
            FiniteLocal,
            Infinity,
            NegativeInfinity
        }
    }
}
