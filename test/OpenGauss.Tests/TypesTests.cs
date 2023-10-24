using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using OpenGauss.NET.Util;
using OpenGauss.NET.Types;
using NUnit.Framework;
using OpenGauss.NET;

#pragma warning disable 618 // OpenGaussDateTime, OpenGaussDate, OpenGaussTimeSpan are obsolete, remove in 7.0

namespace OpenGauss.Tests
{
    /// <summary>
    /// Tests OpenGauss.NET.Types.* independent of a database
    /// </summary>
    [TestFixture]
    public class TypesTests
    {
        [Test]
        public void OpenGaussIntervalParse()
        {
            string input;
            OpenGaussTimeSpan test;

            input = "1 day";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(1).Ticks, test.TotalTicks, input);

            input = "2 days";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(2).Ticks, test.TotalTicks, input);

            input = "2 days 3:04:05";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(new TimeSpan(2, 3, 4, 5).Ticks, test.TotalTicks, input);

            input = "-2 days";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(-2).Ticks, test.TotalTicks, input);

            input = "-2 days -3:04:05";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(new TimeSpan(-2, -3, -4, -5).Ticks, test.TotalTicks, input);

            input = "-2 days -0:01:02";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(new TimeSpan(-2, 0, -1, -2).Ticks, test.TotalTicks, input);

            input = "2 days -12:00";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(new TimeSpan(2, -12, 0, 0).Ticks, test.TotalTicks, input);

            input = "1 mon";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30).Ticks, test.TotalTicks, input);

            input = "2 mons";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(60).Ticks, test.TotalTicks, input);

            input = "1 mon -1 day";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(29).Ticks, test.TotalTicks, input);

            input = "1 mon -2 days";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(28).Ticks, test.TotalTicks, input);

            input = "-1 mon -2 days -3:04:05";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(new TimeSpan(-32, -3, -4, -5).Ticks, test.TotalTicks, input);

            input = "1 year";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*12).Ticks, test.TotalTicks, input);

            input = "2 years";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*24).Ticks, test.TotalTicks, input);

            input = "1 year -1 mon";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*11).Ticks, test.TotalTicks, input);

            input = "1 year -2 mons";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*10).Ticks, test.TotalTicks, input);

            input = "1 year -1 day";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*12 - 1).Ticks, test.TotalTicks, input);

            input = "1 year -2 days";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*12 - 2).Ticks, test.TotalTicks, input);

            input = "1 year -1 mon -1 day";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*11 - 1).Ticks, test.TotalTicks, input);

            input = "1 year -2 mons -2 days";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(TimeSpan.FromDays(30*10 - 2).Ticks, test.TotalTicks, input);

            input = "1 day 2:3:4.005";
            test = OpenGaussTimeSpan.Parse(input);
            Assert.AreEqual(new TimeSpan(1, 2, 3, 4, 5).Ticks, test.TotalTicks, input);

            var testCulture = new CultureInfo("fr-FR");
            Assert.AreEqual(",", testCulture.NumberFormat.NumberDecimalSeparator, "decimal seperator");
            using (TestUtil.SetCurrentCulture(testCulture))
            {
                input = "1 day 2:3:4.005";
                test = OpenGaussTimeSpan.Parse(input);
                Assert.AreEqual(new TimeSpan(1, 2, 3, 4, 5).Ticks, test.TotalTicks, input);
            }
        }

        [Test]
        public void OpenGaussIntervalConstructors()
        {
            OpenGaussTimeSpan test;

            test = new OpenGaussTimeSpan();
            Assert.AreEqual(0, test.Months, "Months");
            Assert.AreEqual(0, test.Days, "Days");
            Assert.AreEqual(0, test.Hours, "Hours");
            Assert.AreEqual(0, test.Minutes, "Minutes");
            Assert.AreEqual(0, test.Seconds, "Seconds");
            Assert.AreEqual(0, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(0, test.Microseconds, "Microseconds");

            test = new OpenGaussTimeSpan(1234567890);
            Assert.AreEqual(0, test.Months, "Months");
            Assert.AreEqual(0, test.Days, "Days");
            Assert.AreEqual(0, test.Hours, "Hours");
            Assert.AreEqual(2, test.Minutes, "Minutes");
            Assert.AreEqual(3, test.Seconds, "Seconds");
            Assert.AreEqual(456, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(456789, test.Microseconds, "Microseconds");

            test = new OpenGaussTimeSpan(new TimeSpan(1, 2, 3, 4, 5)).JustifyInterval();
            Assert.AreEqual(0, test.Months, "Months");
            Assert.AreEqual(1, test.Days, "Days");
            Assert.AreEqual(2, test.Hours, "Hours");
            Assert.AreEqual(3, test.Minutes, "Minutes");
            Assert.AreEqual(4, test.Seconds, "Seconds");
            Assert.AreEqual(5, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(5000, test.Microseconds, "Microseconds");

            test = new OpenGaussTimeSpan(3, 2, 1234567890);
            Assert.AreEqual(3, test.Months, "Months");
            Assert.AreEqual(2, test.Days, "Days");
            Assert.AreEqual(0, test.Hours, "Hours");
            Assert.AreEqual(2, test.Minutes, "Minutes");
            Assert.AreEqual(3, test.Seconds, "Seconds");
            Assert.AreEqual(456, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(456789, test.Microseconds, "Microseconds");

            test = new OpenGaussTimeSpan(1, 2, 3, 4);
            Assert.AreEqual(0, test.Months, "Months");
            Assert.AreEqual(1, test.Days, "Days");
            Assert.AreEqual(2, test.Hours, "Hours");
            Assert.AreEqual(3, test.Minutes, "Minutes");
            Assert.AreEqual(4, test.Seconds, "Seconds");
            Assert.AreEqual(0, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(0, test.Microseconds, "Microseconds");

            test = new OpenGaussTimeSpan(1, 2, 3, 4, 5);
            Assert.AreEqual(0, test.Months, "Months");
            Assert.AreEqual(1, test.Days, "Days");
            Assert.AreEqual(2, test.Hours, "Hours");
            Assert.AreEqual(3, test.Minutes, "Minutes");
            Assert.AreEqual(4, test.Seconds, "Seconds");
            Assert.AreEqual(5, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(5000, test.Microseconds, "Microseconds");

            test = new OpenGaussTimeSpan(1, 2, 3, 4, 5, 6);
            Assert.AreEqual(1, test.Months, "Months");
            Assert.AreEqual(2, test.Days, "Days");
            Assert.AreEqual(3, test.Hours, "Hours");
            Assert.AreEqual(4, test.Minutes, "Minutes");
            Assert.AreEqual(5, test.Seconds, "Seconds");
            Assert.AreEqual(6, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(6000, test.Microseconds, "Microseconds");

            test = new OpenGaussTimeSpan(1, 2, 3, 4, 5, 6, 7);
            Assert.AreEqual(14, test.Months, "Months");
            Assert.AreEqual(3, test.Days, "Days");
            Assert.AreEqual(4, test.Hours, "Hours");
            Assert.AreEqual(5, test.Minutes, "Minutes");
            Assert.AreEqual(6, test.Seconds, "Seconds");
            Assert.AreEqual(7, test.Milliseconds, "Milliseconds");
            Assert.AreEqual(7000, test.Microseconds, "Microseconds");
        }

        [Test]
        public void OpenGaussIntervalToString()
        {
            Assert.AreEqual("00:00:00", new OpenGaussTimeSpan().ToString());

            Assert.AreEqual("00:02:03.456789", new OpenGaussTimeSpan(1234567890).ToString());

            Assert.AreEqual("00:02:03.456789", new OpenGaussTimeSpan(1234567891).ToString());

            Assert.AreEqual("1 day 02:03:04.005",
                            new OpenGaussTimeSpan(new TimeSpan(1, 2, 3, 4, 5)).JustifyInterval().ToString());

            Assert.AreEqual("3 mons 2 days 00:02:03.456789", new OpenGaussTimeSpan(3, 2, 1234567890).ToString());

            Assert.AreEqual("1 day 02:03:04", new OpenGaussTimeSpan(1, 2, 3, 4).ToString());

            Assert.AreEqual("1 day 02:03:04.005", new OpenGaussTimeSpan(1, 2, 3, 4, 5).ToString());

            Assert.AreEqual("1 mon 2 days 03:04:05.006", new OpenGaussTimeSpan(1, 2, 3, 4, 5, 6).ToString());

            Assert.AreEqual("14 mons 3 days 04:05:06.007", new OpenGaussTimeSpan(1, 2, 3, 4, 5, 6, 7).ToString());

            Assert.AreEqual(new OpenGaussTimeSpan(0, 2, 3, 4, 5).ToString(), new OpenGaussTimeSpan(new TimeSpan(0, 2, 3, 4, 5)).ToString());

            Assert.AreEqual(new OpenGaussTimeSpan(1, 2, 3, 4, 5).ToString(), new OpenGaussTimeSpan(new TimeSpan(1, 2, 3, 4, 5)).ToString());
            const long moreThanAMonthInTicks = TimeSpan.TicksPerDay*40;
            Assert.AreEqual(new OpenGaussTimeSpan(moreThanAMonthInTicks).ToString(), new OpenGaussTimeSpan(new TimeSpan(moreThanAMonthInTicks)).ToString());

            var testCulture = new CultureInfo("fr-FR");
            Assert.AreEqual(",", testCulture.NumberFormat.NumberDecimalSeparator, "decimal seperator");
            using (TestUtil.SetCurrentCulture(testCulture))
            {
                Assert.AreEqual("14 mons 3 days 04:05:06.007", new OpenGaussTimeSpan(1, 2, 3, 4, 5, 6, 7).ToString());
            }
        }

        [Test]
        public void OpenGaussDateConstructors()
        {
            OpenGaussDate date;
            DateTime dateTime;
            System.Globalization.Calendar calendar = new System.Globalization.GregorianCalendar();

            date = new OpenGaussDate();
            Assert.AreEqual(1, date.Day);
            Assert.AreEqual(DayOfWeek.Monday, date.DayOfWeek);
            Assert.AreEqual(1, date.DayOfYear);
            Assert.AreEqual(false, date.IsLeapYear);
            Assert.AreEqual(1, date.Month);
            Assert.AreEqual(1, date.Year);

            dateTime = new DateTime(2009, 5, 31);
            date = new OpenGaussDate(dateTime);
            Assert.AreEqual(dateTime.Day, date.Day);
            Assert.AreEqual(dateTime.DayOfWeek, date.DayOfWeek);
            Assert.AreEqual(dateTime.DayOfYear, date.DayOfYear);
            Assert.AreEqual(calendar.IsLeapYear(2009), date.IsLeapYear);
            Assert.AreEqual(dateTime.Month, date.Month);
            Assert.AreEqual(dateTime.Year, date.Year);

            //Console.WriteLine(new DateTime(2009, 5, 31).Ticks);
            //Console.WriteLine((new DateTime(2009, 5, 31) - new DateTime(1, 1, 1)).TotalDays);
            // 2009-5-31
            dateTime = new DateTime(633793248000000000); // ticks since 1 Jan 1
            date = new OpenGaussDate(733557); // days since 1 Jan 1
            Assert.AreEqual(dateTime.Day, date.Day);
            Assert.AreEqual(dateTime.DayOfWeek, date.DayOfWeek);
            Assert.AreEqual(dateTime.DayOfYear, date.DayOfYear);
            Assert.AreEqual(calendar.IsLeapYear(2009), date.IsLeapYear);
            Assert.AreEqual(dateTime.Month, date.Month);
            Assert.AreEqual(dateTime.Year, date.Year);

            // copy previous value.  should get same result
            date = new OpenGaussDate(date);
            Assert.AreEqual(dateTime.Day, date.Day);
            Assert.AreEqual(dateTime.DayOfWeek, date.DayOfWeek);
            Assert.AreEqual(dateTime.DayOfYear, date.DayOfYear);
            Assert.AreEqual(calendar.IsLeapYear(2009), date.IsLeapYear);
            Assert.AreEqual(dateTime.Month, date.Month);
            Assert.AreEqual(dateTime.Year, date.Year);

#if NET6_0_OR_GREATER
            date = new OpenGaussDate(new DateOnly(2012, 3, 4));
            Assert.That(date.Year, Is.EqualTo(2012));
            Assert.That(date.Month, Is.EqualTo(3));
            Assert.That(date.Day, Is.EqualTo(4));
#endif
        }

        [Test]
        public void OpenGaussDateToString()
        {
            Assert.AreEqual("2009-05-31", new OpenGaussDate(2009, 5, 31).ToString());

            Assert.AreEqual("0001-05-07 BC", new OpenGaussDate(-1, 5, 7).ToString());

            var testCulture = new CultureInfo("fr-FR");
            Assert.AreEqual(",", testCulture.NumberFormat.NumberDecimalSeparator, "decimal seperator");
            using (TestUtil.SetCurrentCulture(testCulture))
                Assert.AreEqual("2009-05-31", new OpenGaussDate(2009, 5, 31).ToString());
        }

        [Test]
        public void SpecialDates()
        {
            OpenGaussDate date;
            DateTime dateTime;
            System.Globalization.Calendar calendar = new System.Globalization.GregorianCalendar();

            // a date after a leap year.
            dateTime = new DateTime(2008, 5, 31);
            date = new OpenGaussDate(dateTime);
            Assert.AreEqual(dateTime.Day, date.Day);
            Assert.AreEqual(dateTime.DayOfWeek, date.DayOfWeek);
            Assert.AreEqual(dateTime.DayOfYear, date.DayOfYear);
            Assert.AreEqual(calendar.IsLeapYear(2008), date.IsLeapYear);
            Assert.AreEqual(dateTime.Month, date.Month);
            Assert.AreEqual(dateTime.Year, date.Year);

            // A date that is a leap year day.
            dateTime = new DateTime(2000, 2, 29);
            date = new OpenGaussDate(2000, 2, 29);
            Assert.AreEqual(dateTime.Day, date.Day);
            Assert.AreEqual(dateTime.DayOfWeek, date.DayOfWeek);
            Assert.AreEqual(dateTime.DayOfYear, date.DayOfYear);
            Assert.AreEqual(calendar.IsLeapYear(2000), date.IsLeapYear);
            Assert.AreEqual(dateTime.Month, date.Month);
            Assert.AreEqual(dateTime.Year, date.Year);

            // A date that is not in a leap year.
            dateTime = new DateTime(1900, 3, 1);
            date = new OpenGaussDate(1900, 3, 1);
            Assert.AreEqual(dateTime.Day, date.Day);
            Assert.AreEqual(dateTime.DayOfWeek, date.DayOfWeek);
            Assert.AreEqual(dateTime.DayOfYear, date.DayOfYear);
            Assert.AreEqual(calendar.IsLeapYear(1900), date.IsLeapYear);
            Assert.AreEqual(dateTime.Month, date.Month);
            Assert.AreEqual(dateTime.Year, date.Year);

            // a date after a leap year.
            date = new OpenGaussDate(-1, 12, 31);
            Assert.AreEqual(31, date.Day);
            Assert.AreEqual(DayOfWeek.Sunday, date.DayOfWeek);
            Assert.AreEqual(366, date.DayOfYear);
            Assert.AreEqual(true, date.IsLeapYear);
            Assert.AreEqual(12, date.Month);
            Assert.AreEqual(-1, date.Year);
        }

        [Test]
        public void OpenGaussDateMath()
        {
            OpenGaussDate date;

            // add a day to the empty constructor
            date = new OpenGaussDate() + new OpenGaussTimeSpan(0, 1, 0);
            Assert.AreEqual(2, date.Day);
            Assert.AreEqual(DayOfWeek.Tuesday, date.DayOfWeek);
            Assert.AreEqual(2, date.DayOfYear);
            Assert.AreEqual(false, date.IsLeapYear);
            Assert.AreEqual(1, date.Month);
            Assert.AreEqual(1, date.Year);

            // add a day the same value as the empty constructor
            date = new OpenGaussDate(1, 1, 1) + new OpenGaussTimeSpan(0, 1, 0);
            Assert.AreEqual(2, date.Day);
            Assert.AreEqual(DayOfWeek.Tuesday, date.DayOfWeek);
            Assert.AreEqual(2, date.DayOfYear);
            Assert.AreEqual(false, date.IsLeapYear);
            Assert.AreEqual(1, date.Month);
            Assert.AreEqual(1, date.Year);

            var diff = new OpenGaussDate(1, 1, 1) - new OpenGaussDate(-1, 12, 31);
            Assert.AreEqual(new OpenGaussTimeSpan(0, 1, 0), diff);

            // Test of the addMonths method (positive values added)
            var dateForTestMonths = new OpenGaussDate(2008, 1, 1);
            Assert.AreEqual(dateForTestMonths.AddMonths(0), dateForTestMonths);
            Assert.AreEqual(dateForTestMonths.AddMonths(4), new OpenGaussDate(2008, 5, 1));
            Assert.AreEqual(dateForTestMonths.AddMonths(11), new OpenGaussDate(2008, 12, 1));
            Assert.AreEqual(dateForTestMonths.AddMonths(12), new OpenGaussDate(2009, 1, 1));
            Assert.AreEqual(dateForTestMonths.AddMonths(14), new OpenGaussDate(2009, 3, 1));
            dateForTestMonths = new OpenGaussDate(2008, 1, 31);
            Assert.AreEqual(dateForTestMonths.AddMonths(1), new OpenGaussDate(2008, 2, 29));
            Assert.AreEqual(dateForTestMonths.AddMonths(13), new OpenGaussDate(2009, 2, 28));

            // Test of the addMonths method (negative values added)
            dateForTestMonths = new OpenGaussDate(2009, 1, 1);
            Assert.AreEqual(dateForTestMonths.AddMonths(0), dateForTestMonths);
            Assert.AreEqual(dateForTestMonths.AddMonths(-4), new OpenGaussDate(2008, 9, 1));
            Assert.AreEqual(dateForTestMonths.AddMonths(-12), new OpenGaussDate(2008, 1, 1));
            Assert.AreEqual(dateForTestMonths.AddMonths(-13), new OpenGaussDate(2007, 12, 1));
            dateForTestMonths = new OpenGaussDate(2009, 3, 31);
            Assert.AreEqual(dateForTestMonths.AddMonths(-1), new OpenGaussDate(2009, 2, 28));
            Assert.AreEqual(dateForTestMonths.AddMonths(-13), new OpenGaussDate(2008, 2, 29));
        }

        [Test, IssueLink("https://github.com/opengauss/opengauss/issues/3019")]
        public void OpenGaussDateTimeMath()
        {
            // Note* OpenGaussTimespan treats 1 month as 30 days
            Assert.That(new OpenGaussDateTime(2020, 1, 1, 0, 0, 0).Add(new OpenGaussTimeSpan(1, 2, 0)),
                Is.EqualTo(new OpenGaussDateTime(2020, 2, 2, 0, 0, 0)));
            Assert.That(new OpenGaussDateTime(2020, 1, 1, 0, 0, 0).Add(new OpenGaussTimeSpan(0, -1, 0)),
                Is.EqualTo(new OpenGaussDateTime(2019, 12, 31, 0, 0, 0)));
            Assert.That(new OpenGaussDateTime(2020, 1, 1, 0, 0, 0).Add(new OpenGaussTimeSpan(0, 0, 0)),
                Is.EqualTo(new OpenGaussDateTime(2020, 1, 1, 0, 0, 0)));
            Assert.That(new OpenGaussDateTime(2020, 1, 1, 0, 0, 0).Add(new OpenGaussTimeSpan(0, 0, 10000000)),
                Is.EqualTo(new OpenGaussDateTime(2020, 1, 1, 0, 0, 1)));
            Assert.That(new OpenGaussDateTime(2020, 1, 1, 0, 0, 0).Subtract(new OpenGaussTimeSpan(1, 1, 0)),
                Is.EqualTo(new OpenGaussDateTime(2019, 12, 1, 0, 0, 0)));
            // Add 1 month = 2020-03-01 then add 30 days (1 month in opengaussTimespan = 30 days) = 2020-03-31
            Assert.That(new OpenGaussDateTime(2020, 2, 1, 0, 0, 0).AddMonths(1).Add(new OpenGaussTimeSpan(1, 0, 0)),
                Is.EqualTo(new OpenGaussDateTime(2020, 3, 31, 0, 0, 0)));
        }

        [Test]
        public void TsVector()
        {
            OpenGaussTsVector vec;

            vec = OpenGaussTsVector.Parse("a");
            Assert.AreEqual("'a'", vec.ToString());

            vec = OpenGaussTsVector.Parse("a ");
            Assert.AreEqual("'a'", vec.ToString());

            vec = OpenGaussTsVector.Parse("a:1A");
            Assert.AreEqual("'a':1A", vec.ToString());

            vec = OpenGaussTsVector.Parse(@"\abc\def:1a ");
            Assert.AreEqual("'abcdef':1A", vec.ToString());

            vec = OpenGaussTsVector.Parse(@"abc:3A 'abc' abc:4B 'hello''yo' 'meh\'\\':5");
            Assert.AreEqual(@"'abc':3A,4B 'hello''yo' 'meh''\\':5", vec.ToString());

            vec = OpenGaussTsVector.Parse(" a:12345C  a:24D a:25B b c d 1 2 a:25A,26B,27,28");
            Assert.AreEqual("'1' '2' 'a':24,25A,26B,27,28,12345C 'b' 'c' 'd'", vec.ToString());
        }

        [Test]
        public void TsQuery()
        {
            OpenGaussTsQuery query;

            query = new OpenGaussTsQueryLexeme("a", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B);
            query = new OpenGaussTsQueryOr(query, query);
            query = new OpenGaussTsQueryOr(query, query);

            var str = query.ToString();

            query = OpenGaussTsQuery.Parse("a & b | c");
            Assert.AreEqual("'a' & 'b' | 'c'", query.ToString());

            query = OpenGaussTsQuery.Parse("'a''':*ab&d:d&!c");
            Assert.AreEqual("'a''':*AB & 'd':D & !'c'", query.ToString());

            query = OpenGaussTsQuery.Parse("(a & !(c | d)) & (!!a&b) | c | d | e");
            Assert.AreEqual("( ( 'a' & !( 'c' | 'd' ) & !( !'a' ) & 'b' | 'c' ) | 'd' ) | 'e'", query.ToString());
            Assert.AreEqual(query.ToString(), OpenGaussTsQuery.Parse(query.ToString()).ToString());

            query = OpenGaussTsQuery.Parse("(((a:*)))");
            Assert.AreEqual("'a':*", query.ToString());

            query = OpenGaussTsQuery.Parse(@"'a\\b''cde'");
            Assert.AreEqual(@"a\b'cde", ((OpenGaussTsQueryLexeme)query).Text);
            Assert.AreEqual(@"'a\\b''cde'", query.ToString());

            query = OpenGaussTsQuery.Parse(@"a <-> b");
            Assert.AreEqual("'a' <-> 'b'", query.ToString());

            query = OpenGaussTsQuery.Parse("((a & b) <5> c) <-> !d <0> e");
            Assert.AreEqual("( ( 'a' & 'b' <5> 'c' ) <-> !'d' ) <0> 'e'", query.ToString());

            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("a b c & &"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("&"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("|"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("!"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("("));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse(")"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("()"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("<"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("<-"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("<->"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("a <->"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("<>"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("a <a> b"));
            Assert.Throws(typeof(FormatException), () => OpenGaussTsQuery.Parse("a <-1> b"));
        }

        [Test]
        public void TsQueryEquatibility()
        {
            //Debugger.Launch();
            AreEqual(
                new OpenGaussTsQueryLexeme("lexeme"),
                new OpenGaussTsQueryLexeme("lexeme"));

            AreEqual(
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B),
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B));

            AreEqual(
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B, true),
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B, true));

            AreEqual(
                new OpenGaussTsQueryNot(new OpenGaussTsQueryLexeme("not")),
                new OpenGaussTsQueryNot(new OpenGaussTsQueryLexeme("not")));

            AreEqual(
                new OpenGaussTsQueryAnd(new OpenGaussTsQueryLexeme("left"), new OpenGaussTsQueryLexeme("right")),
                new OpenGaussTsQueryAnd(new OpenGaussTsQueryLexeme("left"), new OpenGaussTsQueryLexeme("right")));

            AreEqual(
                new OpenGaussTsQueryOr(new OpenGaussTsQueryLexeme("left"), new OpenGaussTsQueryLexeme("right")),
                new OpenGaussTsQueryOr(new OpenGaussTsQueryLexeme("left"), new OpenGaussTsQueryLexeme("right")));

            AreEqual(
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("left"), 0, new OpenGaussTsQueryLexeme("right")),
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("left"), 0, new OpenGaussTsQueryLexeme("right")));

            AreEqual(
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("left"), 1, new OpenGaussTsQueryLexeme("right")),
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("left"), 1, new OpenGaussTsQueryLexeme("right")));

            AreEqual(
                new OpenGaussTsQueryEmpty(),
                new OpenGaussTsQueryEmpty());

            AreNotEqual(
                new OpenGaussTsQueryLexeme("lexeme a"),
                new OpenGaussTsQueryLexeme("lexeme b"));

            AreNotEqual(
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.D),
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B));

            AreNotEqual(
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B, true),
                new OpenGaussTsQueryLexeme("lexeme", OpenGaussTsQueryLexeme.Weight.A | OpenGaussTsQueryLexeme.Weight.B, false));

            AreNotEqual(
                new OpenGaussTsQueryNot(new OpenGaussTsQueryLexeme("not")),
                new OpenGaussTsQueryNot(new OpenGaussTsQueryLexeme("ton")));

            AreNotEqual(
                new OpenGaussTsQueryAnd(new OpenGaussTsQueryLexeme("right"), new OpenGaussTsQueryLexeme("left")),
                new OpenGaussTsQueryAnd(new OpenGaussTsQueryLexeme("left"), new OpenGaussTsQueryLexeme("right")));

            AreNotEqual(
                new OpenGaussTsQueryOr(new OpenGaussTsQueryLexeme("right"), new OpenGaussTsQueryLexeme("left")),
                new OpenGaussTsQueryOr(new OpenGaussTsQueryLexeme("left"), new OpenGaussTsQueryLexeme("right")));

            AreNotEqual(
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("right"), 0, new OpenGaussTsQueryLexeme("left")),
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("left"), 0, new OpenGaussTsQueryLexeme("right")));

            AreNotEqual(
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("left"), 0, new OpenGaussTsQueryLexeme("right")),
                new OpenGaussTsQueryFollowedBy(new OpenGaussTsQueryLexeme("left"), 1, new OpenGaussTsQueryLexeme("right")));

            void AreEqual(OpenGaussTsQuery left, OpenGaussTsQuery right)
            {
                Assert.True(left == right);
                Assert.False(left != right);
                Assert.AreEqual(left, right);
                Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
            }

            void AreNotEqual(OpenGaussTsQuery left, OpenGaussTsQuery right)
            {
                Assert.False(left == right);
                Assert.True(left != right);
                Assert.AreNotEqual(left, right);
                Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
            }
        }

        [Test]
        public void TsQueryOperatorPrecedence()
        {
            var query = OpenGaussTsQuery.Parse("!a <-> b & c | d & e");
            var expectedGrouping = OpenGaussTsQuery.Parse("((!(a) <-> b) & c) | (d & e)");
            Assert.AreEqual(expectedGrouping.ToString(), query.ToString());
        }

        [Test]
        public void Bug1011018()
        {
            var p = new OpenGaussParameter();
            p.OpenGaussDbType = OpenGaussDbType.Time;
            p.Value = DateTime.Now;
            var o = p.Value;
        }

#pragma warning disable 618
        [Test]
        [IssueLink("https://github.com/opengauss/opengauss/issues/750")]
        public void OpenGaussInet()
        {
            var v = new OpenGaussInet(IPAddress.Parse("2001:1db8:85a3:1142:1000:8a2e:1370:7334"), 32);
            Assert.That(v.ToString(), Is.EqualTo("2001:1db8:85a3:1142:1000:8a2e:1370:7334/32"));

#pragma warning disable CS8625
            Assert.That(v != null);  // #776
#pragma warning disable CS8625
        }
#pragma warning restore 618
    }
}
