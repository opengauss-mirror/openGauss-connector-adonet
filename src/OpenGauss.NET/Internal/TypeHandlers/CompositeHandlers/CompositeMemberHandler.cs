using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OpenGauss.NET.Internal.TypeHandling;
using OpenGauss.NET.PostgresTypes;

namespace OpenGauss.NET.Internal.TypeHandlers.CompositeHandlers
{
    abstract class CompositeMemberHandler<TComposite>
    {
        public MemberInfo MemberInfo { get; }
        public PostgresType PostgresType { get; }

        protected CompositeMemberHandler(MemberInfo memberInfo, PostgresType postgresType)
        {
            MemberInfo = memberInfo;
            PostgresType = postgresType;
        }

        public abstract ValueTask Read(TComposite composite, OpenGaussReadBuffer buffer, bool async);

        public abstract ValueTask Read(ByReference<TComposite> composite, OpenGaussReadBuffer buffer, bool async);

        public abstract Task Write(TComposite composite, OpenGaussWriteBuffer buffer, OpenGaussLengthCache? lengthCache, bool async, CancellationToken cancellationToken = default);

        public abstract int ValidateAndGetLength(TComposite composite, [NotNullIfNotNull("lengthCache")] ref OpenGaussLengthCache? lengthCache);
    }
}
