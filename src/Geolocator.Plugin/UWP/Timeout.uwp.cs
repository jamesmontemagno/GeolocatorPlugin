using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Geolocator
{
    internal class Timeout
    {


        public Timeout(int timeout, Action timesup)
        {
            if (timeout == Infite)
                return; // nothing to do
            if (timeout < 0)
                throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            if (timesup == null)
                throw new ArgumentNullException("timesup");

            Task.Delay(TimeSpan.FromMilliseconds(timeout), canceller.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        timesup();
                });
        }

        public void Cancel()
        {
            canceller.Cancel();
        }

        private readonly CancellationTokenSource canceller = new CancellationTokenSource();

        public const int Infite = -1;
    }
}
