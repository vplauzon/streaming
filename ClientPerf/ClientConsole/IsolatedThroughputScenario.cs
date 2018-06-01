using System;
using System.Threading.Tasks;

namespace ClientConsole
{
    public class IsolatedThroughputScenario : ScenarioBase
    {
        public IsolatedThroughputScenario(string connectionString, bool isAmqp) : base(connectionString, isAmqp)
        {
        }

        public override async Task RunAsync()
        {
            await Task.CompletedTask;
        }
    }
}