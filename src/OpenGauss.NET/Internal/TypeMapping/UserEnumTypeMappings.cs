using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenGauss.NET.Internal.TypeHandlers;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;
using OpenGauss.NET.Types;

namespace OpenGauss.NET.Internal.TypeMapping
{
    public interface IUserEnumTypeMapping : IUserTypeMapping
    {
        IOpenGaussNameTranslator NameTranslator { get; }
    }

    class UserEnumTypeMapping<TEnum> : IUserEnumTypeMapping
        where TEnum : struct, Enum
    {
        public string PgTypeName { get; }
        public Type ClrType => typeof(TEnum);
        public IOpenGaussNameTranslator NameTranslator { get; }

        readonly Dictionary<TEnum, string> _enumToLabel = new();
        readonly Dictionary<string, TEnum> _labelToEnum = new();

        public UserEnumTypeMapping(string pgTypeName, IOpenGaussNameTranslator nameTranslator)
        {
            (PgTypeName, NameTranslator) = (pgTypeName, nameTranslator);

            foreach (var field in typeof(TEnum).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                var attribute = (PgNameAttribute?)field.GetCustomAttributes(typeof(PgNameAttribute), false).FirstOrDefault();
                var enumName = attribute is null
                    ? nameTranslator.TranslateMemberName(field.Name)
                    : attribute.PgName;
                var enumValue = (TEnum)field.GetValue(null)!;

                _enumToLabel[enumValue] = enumName;
                _labelToEnum[enumName] = enumValue;
            }
        }

        public OpenGaussTypeHandler CreateHandler(PostgresType postgresType, OpenGaussConnector connector)
            => new EnumHandler<TEnum>((PostgresEnumType)postgresType, _enumToLabel, _labelToEnum);
    }
}
