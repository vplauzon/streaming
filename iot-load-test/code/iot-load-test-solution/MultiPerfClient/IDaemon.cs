using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiPerfClient
{
    public interface IDaemon
    {
        Task RunAsync();

        void Stop();
    }
}