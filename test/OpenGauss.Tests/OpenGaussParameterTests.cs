
using OpenGauss.NET;
using OpenGauss.NET.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace OpenGauss.Tests
{
    public class OpenGaussParameterTest : TestBase
    {
        [Test, Description("Makes sure that when OpenGaussDbType or Value/OpenGaussValue are set, DbType and OpenGaussDbType are set accordingly")]
        public void Implicit_setting_of_DbType()
        {
            var p = new OpenGaussParameter("p", DbType.Int32);
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Integer));

            // As long as OpenGaussDbType/DbType aren't set explicitly, infer them from Value
            p = new OpenGaussParameter("p", 8);
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Integer));
            Assert.That(p.DbType, Is.EqualTo(DbType.Int32));

            p.Value = 3.0;
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Double));
            Assert.That(p.DbType, Is.EqualTo(DbType.Double));

            p.OpenGaussDbType = OpenGaussDbType.Bytea;
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Bytea));
            Assert.That(p.DbType, Is.EqualTo(DbType.Binary));

            p.Value = "dont_change";
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Bytea));
            Assert.That(p.DbType, Is.EqualTo(DbType.Binary));

            p = new OpenGaussParameter("p", new int[0]);
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Array | OpenGaussDbType.Integer));
            Assert.That(p.DbType, Is.EqualTo(DbType.Object));
        }

        [Test]
        public void DataTypeName()
        {
            using var conn = OpenConnection();
            using var cmd = new OpenGaussCommand("SELECT @p", conn);
            var p1 = new OpenGaussParameter { ParameterName = "p", Value = 8, DataTypeName = "integer" };
            cmd.Parameters.Add(p1);
            Assert.That(cmd.ExecuteScalar(), Is.EqualTo(8));
            // Purposefully try to send int as string, which should fail. This makes sure
            // the above doesn't work simply because of type inference from the CLR type.
            p1.DataTypeName = "text";
            Assert.That(() => cmd.ExecuteScalar(), Throws.Exception.TypeOf<InvalidCastException>());

            cmd.Parameters.Clear();

            var p2 = new OpenGaussParameter<int> { ParameterName = "p", TypedValue = 8, DataTypeName = "integer" };
            cmd.Parameters.Add(p2);
            Assert.That(cmd.ExecuteScalar(), Is.EqualTo(8));
            // Purposefully try to send int as string, which should fail. This makes sure
            // the above doesn't work simply because of type inference from the CLR type.
            p2.DataTypeName = "text";
            Assert.That(() => cmd.ExecuteScalar(), Throws.Exception.TypeOf<InvalidCastException>());
        }

        [Test]
        public void Positional_parameter_is_positional()
        {
            var p = new OpenGaussParameter(OpenGaussParameter.PositionalName, 1);
            Assert.That(p.IsPositional, Is.True);

            var p2 = new OpenGaussParameter(null, 1);
            Assert.That(p2.IsPositional, Is.True);
        }

        [Test]
        public void Infer_data_type_name_from_OpenGaussDbType()
        {
            var p = new OpenGaussParameter("par_field1", OpenGaussDbType.Varchar, 50);
            Assert.That(p.DataTypeName, Is.EqualTo("character varying"));
        }

        [Test]
        public void Infer_data_type_name_from_DbType()
        {
            var p = new OpenGaussParameter("par_field1", DbType.String, 50);
            Assert.That(p.DataTypeName, Is.EqualTo("text"));
        }

        [Test]
        public void Infer_data_type_name_from_OpenGaussDbType_for_array()
        {
            var p = new OpenGaussParameter("int_array", OpenGaussDbType.Array | OpenGaussDbType.Integer);
            Assert.That(p.DataTypeName, Is.EqualTo("integer[]"));
        }

        [Test]
        public void Infer_data_type_name_from_OpenGaussDbType_for_built_in_range()
        {
            var p = new OpenGaussParameter("numeric_range", OpenGaussDbType.Range | OpenGaussDbType.Numeric);
            Assert.That(p.DataTypeName, Is.EqualTo("numrange"));
        }

        [Test]
        public void Cannot_infer_data_type_name_from_OpenGaussDbType_for_unknown_range()
        {
            var p = new OpenGaussParameter("text_range", OpenGaussDbType.Range | OpenGaussDbType.Text);
            Assert.That(p.DataTypeName, Is.EqualTo(null));
        }

        [Test]
        public void Infer_data_type_name_from_ClrType()
        {
            var p = new OpenGaussParameter("p1", new Dictionary<string, string>());
            Assert.That(p.DataTypeName, Is.EqualTo("hstore"));
        }

        [Test]
        public void Setting_DbType_sets_OpenGaussDbType()
        {
            var p = new OpenGaussParameter();
            p.DbType = DbType.Binary;
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Bytea));
        }

        [Test]
        public void Setting_OpenGaussDbType_sets_DbType()
        {
            var p = new OpenGaussParameter();
            p.OpenGaussDbType = OpenGaussDbType.Bytea;
            Assert.That(p.DbType, Is.EqualTo(DbType.Binary));
        }

        [Test]
        public void Setting_value_does_not_change_DbType()
        {
            var p = new OpenGaussParameter { DbType = DbType.String, OpenGaussDbType = OpenGaussDbType.Bytea };
            p.Value = 8;
            Assert.That(p.DbType, Is.EqualTo(DbType.Binary));
            Assert.That(p.OpenGaussDbType, Is.EqualTo(OpenGaussDbType.Bytea));
        }

        // Older tests

        #region Constructors

        [Test]
        public void Constructor1()
        {
            var p = new OpenGaussParameter();
            Assert.AreEqual(DbType.Object, p.DbType, "DbType");
            Assert.AreEqual(ParameterDirection.Input, p.Direction, "Direction");
            Assert.IsFalse(p.IsNullable, "IsNullable");
            Assert.AreEqual(string.Empty, p.ParameterName, "ParameterName");
            Assert.AreEqual(0, p.Precision, "Precision");
            Assert.AreEqual(0, p.Scale, "Scale");
            Assert.AreEqual(0, p.Size, "Size");
            Assert.AreEqual(string.Empty, p.SourceColumn, "SourceColumn");
            Assert.AreEqual(DataRowVersion.Current, p.SourceVersion, "SourceVersion");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "OpenGaussDbType");
            Assert.IsNull(p.Value, "Value");
        }

        [Test]
        public void Constructor2_Value_DateTime()
        {
            var value = new DateTime(2004, 8, 24);

            var p = new OpenGaussParameter("address", value);
            Assert.AreEqual(DbType.DateTime2, p.DbType, "B:DbType");
            Assert.AreEqual(ParameterDirection.Input, p.Direction, "B:Direction");
            Assert.IsFalse(p.IsNullable, "B:IsNullable");
            Assert.AreEqual("address", p.ParameterName, "B:ParameterName");
            Assert.AreEqual(0, p.Precision, "B:Precision");
            Assert.AreEqual(0, p.Scale, "B:Scale");
            //Assert.AreEqual (0, p.Size, "B:Size");
            Assert.AreEqual(string.Empty, p.SourceColumn, "B:SourceColumn");
            Assert.AreEqual(DataRowVersion.Current, p.SourceVersion, "B:SourceVersion");
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "B:OpenGaussDbType");
            Assert.AreEqual(value, p.Value, "B:Value");
        }

        [Test]
        public void Constructor2_Value_DBNull()
        {
            var p = new OpenGaussParameter("address", DBNull.Value);
            Assert.AreEqual(DbType.Object, p.DbType, "B:DbType");
            Assert.AreEqual(ParameterDirection.Input, p.Direction, "B:Direction");
            Assert.IsFalse(p.IsNullable, "B:IsNullable");
            Assert.AreEqual("address", p.ParameterName, "B:ParameterName");
            Assert.AreEqual(0, p.Precision, "B:Precision");
            Assert.AreEqual(0, p.Scale, "B:Scale");
            Assert.AreEqual(0, p.Size, "B:Size");
            Assert.AreEqual(string.Empty, p.SourceColumn, "B:SourceColumn");
            Assert.AreEqual(DataRowVersion.Current, p.SourceVersion, "B:SourceVersion");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "B:OpenGaussDbType");
            Assert.AreEqual(DBNull.Value, p.Value, "B:Value");
        }

        [Test]
        public void Constructor2_Value_null()
        {
            var p = new OpenGaussParameter("address", null);
            Assert.AreEqual(DbType.Object, p.DbType, "A:DbType");
            Assert.AreEqual(ParameterDirection.Input, p.Direction, "A:Direction");
            Assert.IsFalse(p.IsNullable, "A:IsNullable");
            Assert.AreEqual("address", p.ParameterName, "A:ParameterName");
            Assert.AreEqual(0, p.Precision, "A:Precision");
            Assert.AreEqual(0, p.Scale, "A:Scale");
            Assert.AreEqual(0, p.Size, "A:Size");
            Assert.AreEqual(string.Empty, p.SourceColumn, "A:SourceColumn");
            Assert.AreEqual(DataRowVersion.Current, p.SourceVersion, "A:SourceVersion");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "A:OpenGaussDbType");
            Assert.IsNull(p.Value, "A:Value");
        }

        [Test]
        //.ctor (String, OpenGaussDbType, Int32, String, ParameterDirection, bool, byte, byte, DataRowVersion, object)
        public void Constructor7()
        {
            var p1 = new OpenGaussParameter("p1Name", OpenGaussDbType.Varchar, 20,
                "srcCol", ParameterDirection.InputOutput, false, 0, 0,
                DataRowVersion.Original, "foo");
            Assert.AreEqual(DbType.String, p1.DbType, "DbType");
            Assert.AreEqual(ParameterDirection.InputOutput, p1.Direction, "Direction");
            Assert.AreEqual(false, p1.IsNullable, "IsNullable");
            //Assert.AreEqual (999, p1.LocaleId, "#");
            Assert.AreEqual("p1Name", p1.ParameterName, "ParameterName");
            Assert.AreEqual(0, p1.Precision, "Precision");
            Assert.AreEqual(0, p1.Scale, "Scale");
            Assert.AreEqual(20, p1.Size, "Size");
            Assert.AreEqual("srcCol", p1.SourceColumn, "SourceColumn");
            Assert.AreEqual(false, p1.SourceColumnNullMapping, "SourceColumnNullMapping");
            Assert.AreEqual(DataRowVersion.Original, p1.SourceVersion, "SourceVersion");
            Assert.AreEqual(OpenGaussDbType.Varchar, p1.OpenGaussDbType, "OpenGaussDbType");
            //Assert.AreEqual (3210, p1.OpenGaussValue, "#");
            Assert.AreEqual("foo", p1.Value, "Value");
            //Assert.AreEqual ("database", p1.XmlSchemaCollectionDatabase, "XmlSchemaCollectionDatabase");
            //Assert.AreEqual ("name", p1.XmlSchemaCollectionName, "XmlSchemaCollectionName");
            //Assert.AreEqual ("schema", p1.XmlSchemaCollectionOwningSchema, "XmlSchemaCollectionOwningSchema");
        }

        [Test]
        public void Clone()
        {
            var expected = new OpenGaussParameter
            {
                Value = 42,
                ParameterName = "TheAnswer",

                DbType = DbType.Int32,
                OpenGaussDbType = OpenGaussDbType.Integer,
                DataTypeName = "integer",

                Direction = ParameterDirection.InputOutput,
                IsNullable = true,
                Precision = 1,
                Scale = 2,
                Size = 4,

                SourceVersion = DataRowVersion.Proposed,
                SourceColumn = "source",
                SourceColumnNullMapping = true,
            };
            var actual = expected.Clone();

            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.ParameterName, actual.ParameterName);

            Assert.AreEqual(expected.DbType, actual.DbType);
            Assert.AreEqual(expected.OpenGaussDbType, actual.OpenGaussDbType);
            Assert.AreEqual(expected.DataTypeName, actual.DataTypeName);

            Assert.AreEqual(expected.Direction, actual.Direction);
            Assert.AreEqual(expected.IsNullable, actual.IsNullable);
            Assert.AreEqual(expected.Precision, actual.Precision);
            Assert.AreEqual(expected.Scale, actual.Scale);
            Assert.AreEqual(expected.Size, actual.Size);

            Assert.AreEqual(expected.SourceVersion, actual.SourceVersion);
            Assert.AreEqual(expected.SourceColumn, actual.SourceColumn);
            Assert.AreEqual(expected.SourceColumnNullMapping, actual.SourceColumnNullMapping);
        }

        [Test]
        public void Clone_generic()
        {
            var expected = new OpenGaussParameter<int>
            {
                TypedValue = 42,
                ParameterName = "TheAnswer",

                DbType = DbType.Int32,
                OpenGaussDbType = OpenGaussDbType.Integer,
                DataTypeName = "integer",

                Direction = ParameterDirection.InputOutput,
                IsNullable = true,
                Precision = 1,
                Scale = 2,
                Size = 4,

                SourceVersion = DataRowVersion.Proposed,
                SourceColumn = "source",
                SourceColumnNullMapping = true,
            };
            var actual = (OpenGaussParameter<int>)expected.Clone();

            Assert.AreEqual(expected.Value, actual.Value);
            Assert.AreEqual(expected.TypedValue, actual.TypedValue);
            Assert.AreEqual(expected.ParameterName, actual.ParameterName);

            Assert.AreEqual(expected.DbType, actual.DbType);
            Assert.AreEqual(expected.OpenGaussDbType, actual.OpenGaussDbType);
            Assert.AreEqual(expected.DataTypeName, actual.DataTypeName);

            Assert.AreEqual(expected.Direction, actual.Direction);
            Assert.AreEqual(expected.IsNullable, actual.IsNullable);
            Assert.AreEqual(expected.Precision, actual.Precision);
            Assert.AreEqual(expected.Scale, actual.Scale);
            Assert.AreEqual(expected.Size, actual.Size);

            Assert.AreEqual(expected.SourceVersion, actual.SourceVersion);
            Assert.AreEqual(expected.SourceColumn, actual.SourceColumn);
            Assert.AreEqual(expected.SourceColumnNullMapping, actual.SourceColumnNullMapping);
        }

        #endregion

        [Test]
        [Ignore("")]
        public void InferType_invalid_throws()
        {
            var notsupported = new object[]
            {
                ushort.MaxValue,
                uint.MaxValue,
                ulong.MaxValue,
                sbyte.MaxValue,
                new OpenGaussParameter()
            };

            var param = new OpenGaussParameter();

            for (var i = 0; i < notsupported.Length; i++)
            {
                try
                {
                    param.Value = notsupported[i];
                    Assert.Fail("#A1:" + i);
                }
                catch (FormatException)
                {
                    // appears to be bug in .NET 1.1 while
                    // constructing exception message
                }
                catch (ArgumentException ex)
                {
                    // The parameter data type of ... is invalid
                    Assert.AreEqual(typeof(ArgumentException), ex.GetType(), "#A2");
                    Assert.IsNull(ex.InnerException, "#A3");
                    Assert.IsNotNull(ex.Message, "#A4");
                    Assert.IsNull(ex.ParamName, "#A5");
                }
            }
        }

        [Test] // bug #320196
        public void Parameter_null()
        {
            var param = new OpenGaussParameter("param", OpenGaussDbType.Numeric);
            Assert.AreEqual(0, param.Scale, "#A1");
            param.Value = DBNull.Value;
            Assert.AreEqual(0, param.Scale, "#A2");

            param = new OpenGaussParameter("param", OpenGaussDbType.Integer);
            Assert.AreEqual(0, param.Scale, "#B1");
            param.Value = DBNull.Value;
            Assert.AreEqual(0, param.Scale, "#B2");
        }

        [Test]
        [Ignore("")]
        public void Parameter_type()
        {
            OpenGaussParameter p;

            // If Type is not set, then type is inferred from the value
            // assigned. The Type should be inferred everytime Value is assigned
            // If value is null or DBNull, then the current Type should be reset to Text.
            p = new OpenGaussParameter();
            Assert.AreEqual(DbType.String, p.DbType, "#A1");
            Assert.AreEqual(OpenGaussDbType.Text, p.OpenGaussDbType, "#A2");
            p.Value = DBNull.Value;
            Assert.AreEqual(DbType.String, p.DbType, "#B1");
            Assert.AreEqual(OpenGaussDbType.Text, p.OpenGaussDbType, "#B2");
            p.Value = 1;
            Assert.AreEqual(DbType.Int32, p.DbType, "#C1");
            Assert.AreEqual(OpenGaussDbType.Integer, p.OpenGaussDbType, "#C2");
            p.Value = DBNull.Value;
            Assert.AreEqual(DbType.String, p.DbType, "#D1");
            Assert.AreEqual(OpenGaussDbType.Text, p.OpenGaussDbType, "#D2");
            p.Value = new byte[] { 0x0a };
            Assert.AreEqual(DbType.Binary, p.DbType, "#E1");
            Assert.AreEqual(OpenGaussDbType.Bytea, p.OpenGaussDbType, "#E2");
            p.Value = null;
            Assert.AreEqual(DbType.String, p.DbType, "#F1");
            Assert.AreEqual(OpenGaussDbType.Text, p.OpenGaussDbType, "#F2");
            p.Value = DateTime.Now;
            Assert.AreEqual(DbType.DateTime, p.DbType, "#G1");
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "#G2");
            p.Value = null;
            Assert.AreEqual(DbType.String, p.DbType, "#H1");
            Assert.AreEqual(OpenGaussDbType.Text, p.OpenGaussDbType, "#H2");

            // If DbType is set, then the OpenGaussDbType should not be
            // inferred from the value assigned.
            p = new OpenGaussParameter();
            p.DbType = DbType.DateTime;
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "#I1");
            p.Value = 1;
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "#I2");
            p.Value = null;
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "#I3");
            p.Value = DBNull.Value;
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "#I4");

            // If OpenGaussDbType is set, then the DbType should not be
            // inferred from the value assigned.
            p = new OpenGaussParameter();
            p.OpenGaussDbType = OpenGaussDbType.Bytea;
            Assert.AreEqual(OpenGaussDbType.Bytea, p.OpenGaussDbType, "#J1");
            p.Value = 1;
            Assert.AreEqual(OpenGaussDbType.Bytea, p.OpenGaussDbType, "#J2");
            p.Value = null;
            Assert.AreEqual(OpenGaussDbType.Bytea, p.OpenGaussDbType, "#J3");
            p.Value = DBNull.Value;
            Assert.AreEqual(OpenGaussDbType.Bytea, p.OpenGaussDbType, "#J4");
        }

        [Test]
        [Ignore("")]
        public void ParameterName()
        {
            var p = new OpenGaussParameter();
            p.ParameterName = "name";
            Assert.AreEqual("name", p.ParameterName, "#A:ParameterName");
            Assert.AreEqual(string.Empty, p.SourceColumn, "#A:SourceColumn");

            p.ParameterName = null;
            Assert.AreEqual(string.Empty, p.ParameterName, "#B:ParameterName");
            Assert.AreEqual(string.Empty, p.SourceColumn, "#B:SourceColumn");

            p.ParameterName = " ";
            Assert.AreEqual(" ", p.ParameterName, "#C:ParameterName");
            Assert.AreEqual(string.Empty, p.SourceColumn, "#C:SourceColumn");

            p.ParameterName = " name ";
            Assert.AreEqual(" name ", p.ParameterName, "#D:ParameterName");
            Assert.AreEqual(string.Empty, p.SourceColumn, "#D:SourceColumn");

            p.ParameterName = string.Empty;
            Assert.AreEqual(string.Empty, p.ParameterName, "#E:ParameterName");
            Assert.AreEqual(string.Empty, p.SourceColumn, "#E:SourceColumn");
        }

        [Test]
        public void ResetDbType()
        {
            OpenGaussParameter p;

            //Parameter with an assigned value but no DbType specified
            p = new OpenGaussParameter("foo", 42);
            p.ResetDbType();
            Assert.AreEqual(DbType.Int32, p.DbType, "#A:DbType");
            Assert.AreEqual(OpenGaussDbType.Integer, p.OpenGaussDbType, "#A:OpenGaussDbType");
            Assert.AreEqual(42, p.Value, "#A:Value");

            p.DbType = DbType.DateTime; //assigning a DbType
            Assert.AreEqual(DbType.DateTime, p.DbType, "#B:DbType1");
            Assert.AreEqual(OpenGaussDbType.TimestampTz, p.OpenGaussDbType, "#B:SqlDbType1");
            p.ResetDbType();
            Assert.AreEqual(DbType.Int32, p.DbType, "#B:DbType2");
            Assert.AreEqual(OpenGaussDbType.Integer, p.OpenGaussDbType, "#B:SqlDbtype2");

            //Parameter with an assigned OpenGaussDbType but no specified value
            p = new OpenGaussParameter("foo", OpenGaussDbType.Integer);
            p.ResetDbType();
            Assert.AreEqual(DbType.Object, p.DbType, "#C:DbType");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "#C:OpenGaussDbType");

            p.OpenGaussDbType = OpenGaussDbType.TimestampTz; //assigning a OpenGaussDbType
            Assert.AreEqual(DbType.DateTime, p.DbType, "#D:DbType1");
            Assert.AreEqual(OpenGaussDbType.TimestampTz, p.OpenGaussDbType, "#D:SqlDbType1");
            p.ResetDbType();
            Assert.AreEqual(DbType.Object, p.DbType, "#D:DbType2");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "#D:SqlDbType2");

            p = new OpenGaussParameter();
            p.Value = DateTime.MaxValue;
            Assert.AreEqual(DbType.DateTime2, p.DbType, "#E:DbType1");
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "#E:SqlDbType1");
            p.Value = null;
            p.ResetDbType();
            Assert.AreEqual(DbType.Object, p.DbType, "#E:DbType2");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "#E:SqlDbType2");

            p = new OpenGaussParameter("foo", OpenGaussDbType.Varchar);
            p.Value = DateTime.MaxValue;
            p.ResetDbType();
            Assert.AreEqual(DbType.DateTime2, p.DbType, "#F:DbType");
            Assert.AreEqual(OpenGaussDbType.Timestamp, p.OpenGaussDbType, "#F:OpenGaussDbType");
            Assert.AreEqual(DateTime.MaxValue, p.Value, "#F:Value");

            p = new OpenGaussParameter("foo", OpenGaussDbType.Varchar);
            p.Value = DBNull.Value;
            p.ResetDbType();
            Assert.AreEqual(DbType.Object, p.DbType, "#G:DbType");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "#G:OpenGaussDbType");
            Assert.AreEqual(DBNull.Value, p.Value, "#G:Value");

            p = new OpenGaussParameter("foo", OpenGaussDbType.Varchar);
            p.Value = null;
            p.ResetDbType();
            Assert.AreEqual(DbType.Object, p.DbType, "#G:DbType");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "#G:OpenGaussDbType");
            Assert.IsNull(p.Value, "#G:Value");
        }

        [Test]
        public void ParameterName_retains_prefix()
            => Assert.That(new OpenGaussParameter("@p", DbType.String).ParameterName, Is.EqualTo("@p"));

        [Test]
        [Ignore("")]
        public void SourceColumn()
        {
            var p = new OpenGaussParameter();
            p.SourceColumn = "name";
            Assert.AreEqual(string.Empty, p.ParameterName, "#A:ParameterName");
            Assert.AreEqual("name", p.SourceColumn, "#A:SourceColumn");

            p.SourceColumn = null;
            Assert.AreEqual(string.Empty, p.ParameterName, "#B:ParameterName");
            Assert.AreEqual(string.Empty, p.SourceColumn, "#B:SourceColumn");

            p.SourceColumn = " ";
            Assert.AreEqual(string.Empty, p.ParameterName, "#C:ParameterName");
            Assert.AreEqual(" ", p.SourceColumn, "#C:SourceColumn");

            p.SourceColumn = " name ";
            Assert.AreEqual(string.Empty, p.ParameterName, "#D:ParameterName");
            Assert.AreEqual(" name ", p.SourceColumn, "#D:SourceColumn");

            p.SourceColumn = string.Empty;
            Assert.AreEqual(string.Empty, p.ParameterName, "#E:ParameterName");
            Assert.AreEqual(string.Empty, p.SourceColumn, "#E:SourceColumn");
        }

        [Test]
        public void Bug1011100_OpenGaussDbType()
        {
            var p = new OpenGaussParameter();
            p.Value = DBNull.Value;
            Assert.AreEqual(DbType.Object, p.DbType, "#A:DbType");
            Assert.AreEqual(OpenGaussDbType.Unknown, p.OpenGaussDbType, "#A:OpenGaussDbType");

            // Now change parameter value.
            // Note that as we didn't explicitly specified a dbtype, the dbtype property should change when
            // the value changes...

            p.Value = 8;

            Assert.AreEqual(DbType.Int32, p.DbType, "#A:DbType");
            Assert.AreEqual(OpenGaussDbType.Integer, p.OpenGaussDbType, "#A:OpenGaussDbType");

            //Assert.AreEqual(3510, p.Value, "#A:Value");
            //p.OpenGaussDbType = OpenGaussDbType.Varchar;
            //Assert.AreEqual(DbType.String, p.DbType, "#B:DbType");
            //Assert.AreEqual(OpenGaussDbType.Varchar, p.OpenGaussDbType, "#B:OpenGaussDbType");
            //Assert.AreEqual(3510, p.Value, "#B:Value");
        }

        [Test]
        public void OpenGaussParameter_Clone()
        {
            var param = new OpenGaussParameter();

            param.Value = 5;
            param.Precision = 1;
            param.Scale = 1;
            param.Size = 1;
            param.Direction = ParameterDirection.Input;
            param.IsNullable = true;
            param.ParameterName = "parameterName";
            param.SourceColumn = "source_column";
            param.SourceVersion = DataRowVersion.Current;
            param.OpenGaussValue = 5;
            param.SourceColumnNullMapping = false;

            var newParam = param.Clone();

            Assert.AreEqual(param.Value, newParam.Value);
            Assert.AreEqual(param.Precision, newParam.Precision);
            Assert.AreEqual(param.Scale, newParam.Scale);
            Assert.AreEqual(param.Size, newParam.Size);
            Assert.AreEqual(param.Direction, newParam.Direction);
            Assert.AreEqual(param.IsNullable, newParam.IsNullable);
            Assert.AreEqual(param.ParameterName, newParam.ParameterName);
            Assert.AreEqual(param.TrimmedName, newParam.TrimmedName);
            Assert.AreEqual(param.SourceColumn, newParam.SourceColumn);
            Assert.AreEqual(param.SourceVersion, newParam.SourceVersion);
            Assert.AreEqual(param.OpenGaussValue, newParam.OpenGaussValue);
            Assert.AreEqual(param.SourceColumnNullMapping, newParam.SourceColumnNullMapping);
            Assert.AreEqual(param.OpenGaussValue, newParam.OpenGaussValue);

        }

        [Test]
        public void Precision_via_interface()
        {
            var parameter = new OpenGaussParameter();
            var paramIface = (IDbDataParameter)parameter;

            paramIface.Precision = 42;

            Assert.AreEqual((byte)42, paramIface.Precision);
        }

        [Test]
        public void Precision_via_base_class()
        {
            var parameter = new OpenGaussParameter();
            var paramBase = (DbParameter)parameter;

            paramBase.Precision = 42;

            Assert.AreEqual((byte)42, paramBase.Precision);
        }

        [Test]
        public void Scale_via_interface()
        {
            var parameter = new OpenGaussParameter();
            var paramIface = (IDbDataParameter)parameter;

            paramIface.Scale = 42;

            Assert.AreEqual((byte)42, paramIface.Scale);
        }

        [Test]
        public void Scale_via_base_class()
        {
            var parameter = new OpenGaussParameter();
            var paramBase = (DbParameter)parameter;

            paramBase.Scale = 42;

            Assert.AreEqual((byte)42, paramBase.Scale);
        }

        [Test]
        public void Null_value_throws()
        {
            using var connection = OpenConnection();
            using var command = new OpenGaussCommand("SELECT @p", connection)
            {
                Parameters = { new OpenGaussParameter("p", null) }
            };

            Assert.That(() => command.ExecuteReader(), Throws.InvalidOperationException);
        }

        [Test]
        public void Null_value_with_nullable_type()
        {
            using var connection = OpenConnection();
            using var command = new OpenGaussCommand("SELECT @p", connection)
            {
                Parameters = { new OpenGaussParameter<int?>("p", null) }
            };
            using var reader = command.ExecuteReader();

            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetFieldValue<int?>(0), Is.Null);
        }

#if NeedsPorting
        [Test]
        [Category ("NotWorking")]
        public void InferType_Char()
        {
            Char value = 'X';

            String string_value = "X";

            OpenGaussParameter p = new OpenGaussParameter ();
            p.Value = value;
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#A:OpenGaussDbType");
            Assert.AreEqual (DbType.String, p.DbType, "#A:DbType");
            Assert.AreEqual (string_value, p.Value, "#A:Value");

            p = new OpenGaussParameter ();
            p.Value = value;
            Assert.AreEqual (value, p.Value, "#B:Value1");
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#B:OpenGaussDbType");
            Assert.AreEqual (string_value, p.Value, "#B:Value2");

            p = new OpenGaussParameter ();
            p.Value = value;
            Assert.AreEqual (value, p.Value, "#C:Value1");
            Assert.AreEqual (DbType.String, p.DbType, "#C:DbType");
            Assert.AreEqual (string_value, p.Value, "#C:Value2");

            p = new OpenGaussParameter ("name", value);
            Assert.AreEqual (value, p.Value, "#D:Value1");
            Assert.AreEqual (DbType.String, p.DbType, "#D:DbType");
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#D:OpenGaussDbType");
            Assert.AreEqual (string_value, p.Value, "#D:Value2");

            p = new OpenGaussParameter ("name", 5);
            p.Value = value;
            Assert.AreEqual (value, p.Value, "#E:Value1");
            Assert.AreEqual (DbType.String, p.DbType, "#E:DbType");
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#E:OpenGaussDbType");
            Assert.AreEqual (string_value, p.Value, "#E:Value2");

            p = new OpenGaussParameter ("name", OpenGaussDbType.Text);
            p.Value = value;
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#F:OpenGaussDbType");
            Assert.AreEqual (value, p.Value, "#F:Value");
        }

        [Test]
        [Category ("NotWorking")]
        public void InferType_CharArray()
        {
            Char[] value = new Char[] { 'A', 'X' };

            String string_value = "AX";

            OpenGaussParameter p = new OpenGaussParameter ();
            p.Value = value;
            Assert.AreEqual (value, p.Value, "#A:Value1");
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#A:OpenGaussDbType");
            Assert.AreEqual (DbType.String, p.DbType, "#A:DbType");
            Assert.AreEqual (string_value, p.Value, "#A:Value2");

            p = new OpenGaussParameter ();
            p.Value = value;
            Assert.AreEqual (value, p.Value, "#B:Value1");
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#B:OpenGaussDbType");
            Assert.AreEqual (string_value, p.Value, "#B:Value2");

            p = new OpenGaussParameter ();
            p.Value = value;
            Assert.AreEqual (value, p.Value, "#C:Value1");
            Assert.AreEqual (DbType.String, p.DbType, "#C:DbType");
            Assert.AreEqual (string_value, p.Value, "#C:Value2");

            p = new OpenGaussParameter ("name", value);
            Assert.AreEqual (value, p.Value, "#D:Value1");
            Assert.AreEqual (DbType.String, p.DbType, "#D:DbType");
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#D:OpenGaussDbType");
            Assert.AreEqual (string_value, p.Value, "#D:Value2");

            p = new OpenGaussParameter ("name", 5);
            p.Value = value;
            Assert.AreEqual (value, p.Value, "#E:Value1");
            Assert.AreEqual (DbType.String, p.DbType, "#E:DbType");
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#E:OpenGaussDbType");
            Assert.AreEqual (string_value, p.Value, "#E:Value2");

            p = new OpenGaussParameter ("name", OpenGaussDbType.Text);
            p.Value = value;
            Assert.AreEqual (OpenGaussDbType.Text, p.OpenGaussDbType, "#F:OpenGaussDbType");
            Assert.AreEqual (value, p.Value, "#F:Value");
        }

        [Test]
        public void InferType_Object()
        {
            Object value = new Object();

            OpenGaussParameter param = new OpenGaussParameter();
            param.Value = value;
            Assert.AreEqual(OpenGaussDbType.Variant, param.OpenGaussDbType, "#1");
            Assert.AreEqual(DbType.Object, param.DbType, "#2");
        }

        [Test]
        public void LocaleId ()
        {
            OpenGaussParameter parameter = new OpenGaussParameter ();
            Assert.AreEqual (0, parameter.LocaleId, "#1");
            parameter.LocaleId = 15;
            Assert.AreEqual(15, parameter.LocaleId, "#2");
        }
#endif
    }
}
