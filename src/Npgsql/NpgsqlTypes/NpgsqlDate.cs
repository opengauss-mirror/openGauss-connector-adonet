using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#pragma warning disable 1591

// ReSharper disable once CheckNamespace
namespace NpgsqlTypes
{
    [Obsolete(
        "For values outside the range of DateTime/DateOnly, consider using NodaTime (range -9998 to 9999), or read the value as an 'int'. " +
        "See https://www.npgsql.org/doc/types/datetime.html for more information.")]
    [Serializable]
    public readonly struct NpgsqlDate : IEquatable<NpgsqlDate>, IComparable<NpgsqlDate>, IComparable,
        IComparer<NpgsqlDate>, IComparer
    {
        //Number of days since January 1st CE (January 1st EV). 1 Jan 1 CE = 0, 2 Jan 1 CE = 1, 31 Dec 1 BCE = -1, etc.
        readonly int _daysSinceEra;
        readonly InternalType _type;

        #region Constants

        static readonly int[] CommonYearDays = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365 };
        static readonly int[] LeapYearDays = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366 };
        static readonly int[] CommonYearMaxes = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        static readonly int[] LeapYearMaxes = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Represents the date 1970-01-01
        /// </summary>
        public static readonly NpgsqlDate Epoch = new(1970, 1, 1);

        /// <summary>
        /// Represents the date 0001-01-01
        /// </summary>
        public static readonly NpgsqlDate Era = new(0);

        public const int MaxYear = 5874897;
        public const int MinYear = -4714;
        public static readonly NpgsqlDate MaxCalculableValue = new(MaxYear, 12, 31);
        public static readonly NpgsqlDate MinCalculableValue = new(MinYear, 11, 24);

        public static readonly NpgsqlDate Infinity = new(InternalType.Infinity);
        public static readonly NpgsqlDate NegativeInfinity = new(InternalType.NegativeInfinity);

        const int DaysInYear = 365; //Common years
        const int DaysIn4Years = 4 * DaysInYear + 1; //Leap year every 4 years.
        const int DaysInCentury = 25 * DaysIn4Years - 1; //Except no leap year every 100.
        const int DaysIn4Centuries = 4 * DaysInCentury + 1; //Except leap year every 400.

        #endregion

        #region Constructors

        NpgsqlDate(InternalType type)
        {
            _type = type;
            _daysSinceEra = 0;
        }

        internal NpgsqlDate(int days)
        {
            _type = InternalType.Finite;
            _daysSinceEra = days;
        }

        public NpgsqlDate(DateTime dateTime) : this((int)(dateTime.Ticks / TimeSpan.TicksPerDay)) {}

        public NpgsqlDate(NpgsqlDate copyFrom) : this(copyFrom._daysSinceEra) {}

        public NpgsqlDate(int year, int month, int day)
        {
            _type = InternalType.Finite;
            if (year == 0 || year < MinYear || year > MaxYear || month < 1 || month > 12 || day < 1 ||
                (day > (IsLeap(year) ? 366 : 365)))
            {
                throw new ArgumentOutOfRangeException();
            }

            _daysSinceEra = DaysForYears(year) + (IsLeap(year) ? LeapYearDays : CommonYearDays)[month - 1] + day - 1;
        }

        #endregion

        #region String Conversions

        public override string ToString()
            => _type switch
            {
                InternalType.Infinity         => "infinity",
                InternalType.NegativeInfinity => "-infinity",
                //Format of yyyy-MM-dd with " BC" for BCE and optional " AD" for CE which we omit here.
                _ => new StringBuilder(Math.Abs(Year).ToString("D4"))
                    .Append('-').Append(Month.ToString("D2"))
                    .Append('-').Append(Day.ToString("D2"))
                    .Append(_daysSinceEra < 0 ? " BC" : "").ToString()
            };

        public static NpgsqlDate Parse(string str)
        {

            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }

            if (str == "infinity")
                return Infinity;

            if (str == "-infinity")
                return NegativeInfinity;

            str = str.Trim();
            try {
                var idx = str.IndexOf('-');
                if (idx == -1) {
                    throw new FormatException();
                }
                var year = int.Parse(str.Substring(0, idx));
                var idxLast = idx + 1;
                if ((idx = str.IndexOf('-', idxLast)) == -1) {
                    throw new FormatException();
                }
                var month = int.Parse(str.Substring(idxLast, idx - idxLast));
                idxLast = idx + 1;
                if ((idx = str.IndexOf(' ', idxLast)) == -1) {
                    idx = str.Length;
                }
                var day = int.Parse(str.Substring(idxLast, idx - idxLast));
                if (str.Contains("BC")) {
                    year = -year;
                }
                return new NpgsqlDate(year, month, day);
            } catch (OverflowException) {
                throw;
            } catch (Exception) {
                throw new FormatException();
            }
        }

        public static bool TryParse(string str, out NpgsqlDate date)
        {
            try {
                date = Parse(str);
                return true;
            } catch {
                date = Era;
                return false;
            }
        }

        #endregion

        #region Public Properties

        public static NpgsqlDate Now => new(DateTime.Now);
        public static NpgsqlDate Today => Now;
        public static NpgsqlDate Yesterday => Now.AddDays(-1);
        public static NpgsqlDate Tomorrow => Now.AddDays(1);

        public int DayOfYear => _daysSinceEra - DaysForYears(Year) + 1;

        public int Year
        {
            get
            {
                var guess = (int)Math.Round(_daysSinceEra/365.2425);
                var test = guess - 1;
                while (DaysForYears(++test) <= _daysSinceEra) {}
                return test - 1;
            }
        }

        public int Month
        {
            get
            {
                var i = 1;
                var target = DayOfYear;
                var array = IsLeapYear ? LeapYearDays : CommonYearDays;
                while (target > array[i])
                {
                    ++i;
                }
                return i;
            }
        }

        public int Day => DayOfYear - (IsLeapYear ? LeapYearDays : CommonYearDays)[Month - 1];

        public DayOfWeek DayOfWeek => (DayOfWeek) ((_daysSinceEra + 1)%7);

        internal int DaysSinceEra => _daysSinceEra;

        public bool IsLeapYear => IsLeap(Year);

        public bool IsInfinity => _type == InternalType.Infinity;
        public bool IsNegativeInfinity => _type == InternalType.NegativeInfinity;

        public bool IsFinite
            => _type switch {
                InternalType.Finite           => true,
                InternalType.Infinity         => false,
                InternalType.NegativeInfinity => false,
                _ => throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {_type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.")
            };

        #endregion

        #region Internals

        static int DaysForYears(int years)
        {
            //Number of years after 1CE (0 for 1CE, -1 for 1BCE, 1 for 2CE).
            var calcYear = years < 1 ? years : years - 1;

            return calcYear / 400 * DaysIn4Centuries //Blocks of 400 years with their leap and common years
                   + calcYear % 400 / 100 * DaysInCentury //Remaining blocks of 100 years with their leap and common years
                   + calcYear % 100 / 4 * DaysIn4Years //Remaining blocks of 4 years with their leap and common years
                   + calcYear % 4 * DaysInYear //Remaining years, all common
                   + (calcYear < 0 ? -1 : 0); //And 1BCE is leap.
        }

        static bool IsLeap(int year)
        {
            //Every 4 years is a leap year
            //Except every 100 years isn't a leap year.
            //Except every 400 years is.
            if (year < 1)
            {
                year = year + 1;
            }
            return (year%4 == 0) && ((year%100 != 0) || (year%400 == 0));
        }

        #endregion

        #region Arithmetic

        public NpgsqlDate AddDays(int days)
            => _type switch
        {
            InternalType.Infinity         => Infinity,
            InternalType.NegativeInfinity => NegativeInfinity,
            InternalType.Finite           => new NpgsqlDate(_daysSinceEra + days),
            _ => throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {_type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.")
        };

        public NpgsqlDate AddYears(int years)
        {
            switch (_type) {
            case InternalType.Infinity:
                return Infinity;
            case InternalType.NegativeInfinity:
                return NegativeInfinity;
            case InternalType.Finite:
                break;
            default:
                throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {_type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.");
            }

            var newYear = Year + years;
            if (newYear >= 0 && _daysSinceEra < 0) //cross 1CE/1BCE divide going up
            {
                ++newYear;
            }
            else if (newYear <= 0 && _daysSinceEra >= 0) //cross 1CE/1BCE divide going down
            {
                --newYear;
            }
            return new NpgsqlDate(newYear, Month, Day);
        }

        public NpgsqlDate AddMonths(int months)
        {
            switch (_type) {
            case InternalType.Infinity:
                return Infinity;
            case InternalType.NegativeInfinity:
                return NegativeInfinity;
            case InternalType.Finite:
                break;
            default:
                throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {_type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.");
            }

            var newYear = Year;
            var newMonth = Month + months;

            while (newMonth > 12)
            {
                newMonth -= 12;
                newYear += 1;
            }
            while (newMonth < 1)
            {
                newMonth += 12;
                newYear -= 1;
            }
            var maxDay = (IsLeap(newYear) ? LeapYearMaxes : CommonYearMaxes)[newMonth - 1];
            var newDay = Day > maxDay ? maxDay : Day;
            return new NpgsqlDate(newYear, newMonth, newDay);

        }

        public NpgsqlDate Add(in NpgsqlTimeSpan interval)
        {
            switch (_type) {
            case InternalType.Infinity:
                return Infinity;
            case InternalType.NegativeInfinity:
                return NegativeInfinity;
            case InternalType.Finite:
                break;
            default:
                throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {_type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.");
            }

            return AddMonths(interval.Months).AddDays(interval.Days);
        }

        internal NpgsqlDate Add(in NpgsqlTimeSpan interval, int carriedOverflow)
        {
            switch (_type) {
            case InternalType.Infinity:
                return Infinity;
            case InternalType.NegativeInfinity:
                return NegativeInfinity;
            case InternalType.Finite:
                break;
            default:
                throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {_type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.");
            }

            return AddMonths(interval.Months).AddDays(interval.Days + carriedOverflow);
        }

        #endregion

        #region Comparison

        public int Compare(NpgsqlDate x, NpgsqlDate y) => x.CompareTo(y);

        public int Compare(object? x, object? y)
        {
            if (x == null)
            {
                return y == null ? 0 : -1;
            }
            if (y == null)
            {
                return 1;
            }
            if (!(x is IComparable) || !(y is IComparable))
            {
                throw new ArgumentException();
            }
            return ((IComparable) x).CompareTo(y);
        }

        public bool Equals(NpgsqlDate other)
            => _type switch
            {
                InternalType.Infinity         => other._type == InternalType.Infinity,
                InternalType.NegativeInfinity => other._type == InternalType.NegativeInfinity,
                InternalType.Finite           => other._type == InternalType.Finite && _daysSinceEra == other._daysSinceEra,
                _ => false
            };

        public override bool Equals(object? obj) => obj is NpgsqlDate date && Equals(date);

        public int CompareTo(NpgsqlDate other)
            => _type switch
            {
                InternalType.Infinity         => other._type == InternalType.Infinity ? 0 : 1,
                InternalType.NegativeInfinity => other._type == InternalType.NegativeInfinity ? 0 : -1,
                _ => other._type switch
                {
                    InternalType.Infinity         => -1,
                    InternalType.NegativeInfinity => 1,
                    _                             => _daysSinceEra.CompareTo(other._daysSinceEra)
                }
            };

        public int CompareTo(object? o)
            => o == null
                ? 1
                : o is NpgsqlDate npgsqlDate
                    ? CompareTo(npgsqlDate)
                    : throw new ArgumentException();

        public override int GetHashCode() => _daysSinceEra;

        #endregion

        #region Operators

        public static bool operator ==(NpgsqlDate x, NpgsqlDate y) => x.Equals(y);
        public static bool operator !=(NpgsqlDate x, NpgsqlDate y) => !(x == y);
        public static bool operator <(NpgsqlDate x, NpgsqlDate y)  => x.CompareTo(y) < 0;
        public static bool operator >(NpgsqlDate x, NpgsqlDate y)  => x.CompareTo(y) > 0;
        public static bool operator <=(NpgsqlDate x, NpgsqlDate y) => x.CompareTo(y) <= 0;
        public static bool operator >=(NpgsqlDate x, NpgsqlDate y) => x.CompareTo(y) >= 0;

        public static DateTime ToDateTime(NpgsqlDate date)
        {
            switch (date._type)
            {
            case InternalType.Infinity:
            case InternalType.NegativeInfinity:
                throw new InvalidCastException("Infinity values can't be cast to DateTime");
            case InternalType.Finite:
                try { return new DateTime(date._daysSinceEra * NpgsqlTimeSpan.TicksPerDay); }
                catch { throw new InvalidCastException(); }
            default:
                throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {date._type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.");
            }
        }

        public static explicit operator DateTime(NpgsqlDate date) => ToDateTime(date);

        public static NpgsqlDate ToNpgsqlDate(DateTime date)
            => new((int)(date.Ticks / NpgsqlTimeSpan.TicksPerDay));

        public static explicit operator NpgsqlDate(DateTime date) => ToNpgsqlDate(date);

        public static NpgsqlDate operator +(NpgsqlDate date, NpgsqlTimeSpan interval)
            => date.Add(interval);

        public static NpgsqlDate operator +(NpgsqlTimeSpan interval, NpgsqlDate date)
            => date.Add(interval);

        public static NpgsqlDate operator -(NpgsqlDate date, NpgsqlTimeSpan interval)
            => date.Subtract(interval);

        public NpgsqlDate Subtract(in NpgsqlTimeSpan interval) => Add(-interval);

        public static NpgsqlTimeSpan operator -(NpgsqlDate dateX, NpgsqlDate dateY)
        {
            if (dateX._type != InternalType.Finite || dateY._type != InternalType.Finite)
                throw new ArgumentException("Can't subtract infinity date values");

            return new NpgsqlTimeSpan(0, dateX._daysSinceEra - dateY._daysSinceEra, 0);
        }

        #endregion

#if NET6_0_OR_GREATER
        public NpgsqlDate(DateOnly date) : this(date.Year, date.Month, date.Day) {}

        public static DateOnly ToDateOnly(NpgsqlDate date)
        {
            switch (date._type)
            {
            case InternalType.Infinity:
            case InternalType.NegativeInfinity:
                throw new InvalidCastException("Infinity values can't be cast to DateTime");
            case InternalType.Finite:
                try { return new DateOnly(date.Year, date.Month, date.Day); }
                catch { throw new InvalidCastException(); }
            default:
                throw new InvalidOperationException($"Internal Npgsql bug: unexpected value {date._type} of enum {nameof(NpgsqlDate)}.{nameof(InternalType)}. Please file a bug.");
            }
        }

        public static explicit operator DateOnly(NpgsqlDate date) => ToDateOnly(date);

        public static NpgsqlDate ToNpgsqlDate(DateOnly date)
            => new(date.Year, date.Month, date.Day);

        public static explicit operator NpgsqlDate(DateOnly date) => ToNpgsqlDate(date);
#endif

        enum InternalType
        {
            Finite,
            Infinity,
            NegativeInfinity
        }
    }
}
