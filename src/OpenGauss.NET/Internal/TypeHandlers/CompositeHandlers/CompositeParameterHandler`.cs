using System.Reflection;
using System.Threading.Tasks;
using OpenGauss.NET.Internal.TypeHandling;

namespace OpenGauss.NET.Internal.TypeHandlers.CompositeHandlers
{
    sealed class CompositeParameterHandler<T> : CompositeParameterHandler
    {
        public CompositeParameterHandler(OpenGaussTypeHandler handler, ParameterInfo parameterInfo)
            : base(handler, parameterInfo) { }

        public override ValueTask<object?> Read(OpenGaussReadBuffer buffer, bool async)
        {
            var task = Read<T>(buffer, async);
            return task.IsCompleted
                ? new ValueTask<object?>(task.Result)
                : AwaitTask(task);

            static async ValueTask<object?> AwaitTask(ValueTask<T> task) => await task;
        }
    }
}
