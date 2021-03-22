using System;
using System.Threading;
using System.Threading.Tasks;

namespace BancoSimple.View.Utils
{
    public class BancoSimpleProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        private readonly TaskScheduler _taskScheduler;

        public BancoSimpleProgress(Action<T> handler)
        {
            _taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _handler = handler;
        }

        public void Report(T value)
        {
            Task.Factory.StartNew(
                () => _handler(value), //mesmo sendo um atributom eu passo um valor por ser uma action
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    _taskScheduler
                );
        }
    }
}
