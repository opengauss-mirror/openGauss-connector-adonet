using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.Internal;
using OpenGauss.NET.TypeMapping;
using OpenGauss.NET.Types;
using static OpenGauss.NET.Util.Statics;

namespace OpenGauss.NET
{
    /// <summary>
    /// A generic version of <see cref="OpenGaussParameter"/> which provides more type safety and
    /// avoids boxing of value types. Use <see cref="TypedValue"/> instead of <see cref="OpenGaussParameter.Value"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value that will be stored in the parameter.</typeparam>
    public sealed class OpenGaussParameter<T> : OpenGaussParameter
    {
        /// <summary>
        /// Gets or sets the strongly-typed value of the parameter.
        /// </summary>
        public T? TypedValue { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter. This delegates to <see cref="TypedValue"/>.
        /// </summary>
        public override object? Value
        {
            get => TypedValue;
            set => TypedValue = (T)value!;
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of <see cref="OpenGaussParameter{T}" />.
        /// </summary>
        public OpenGaussParameter() {}

        /// <summary>
        /// Initializes a new instance of <see cref="OpenGaussParameter{T}" /> with a parameter name and value.
        /// </summary>
        public OpenGaussParameter(string parameterName, T value)
        {
            ParameterName = parameterName;
            TypedValue = value;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OpenGaussParameter{T}" /> with a parameter name and type.
        /// </summary>
        public OpenGaussParameter(string parameterName, OpenGaussDbType opengaussDbType)
        {
            ParameterName = parameterName;
            OpenGaussDbType = opengaussDbType;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="OpenGaussParameter{T}" /> with a parameter name and type.
        /// </summary>
        public OpenGaussParameter(string parameterName, DbType dbType)
        {
            ParameterName = parameterName;
            DbType = dbType;
        }

        #endregion Constructors

        internal override void ResolveHandler(ConnectorTypeMapper typeMapper)
        {
            if (Handler is not null)
                return;

            // TODO: Better exceptions in case of cast failure etc.
            if (_opengaussDbType.HasValue)
                Handler = typeMapper.ResolveByOpenGaussDbType(_opengaussDbType.Value);
            else if (_dataTypeName is not null)
                Handler = typeMapper.ResolveByDataTypeName(_dataTypeName);
            else
                Handler = typeMapper.ResolveByValue(TypedValue);
        }

        internal override int ValidateAndGetLength()
        {
            if (TypedValue is null or DBNull)
                return 0;

            var lengthCache = LengthCache;
            var len = Handler!.ValidateAndGetLength(TypedValue, ref lengthCache, this);
            LengthCache = lengthCache;
            return len;
        }

        internal override Task WriteWithLength(OpenGaussWriteBuffer buf, bool async, CancellationToken cancellationToken = default)
            => Handler!.WriteWithLength(TypedValue, buf, LengthCache, this, async, cancellationToken);

        private protected override OpenGaussParameter CloneCore() =>
            // use fields instead of properties
            // to avoid auto-initializing something like type_info
            new OpenGaussParameter<T>
            {
                _precision = _precision,
                _scale = _scale,
                _size = _size,
                _opengaussDbType = _opengaussDbType,
                _dataTypeName = _dataTypeName,
                Direction = Direction,
                IsNullable = IsNullable,
                _name = _name,
                TrimmedName = TrimmedName,
                SourceColumn = SourceColumn,
                SourceVersion = SourceVersion,
                TypedValue = TypedValue,
                SourceColumnNullMapping = SourceColumnNullMapping,
            };
    }
}
