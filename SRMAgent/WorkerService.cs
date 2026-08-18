namespace SRMAgent
{
    public class WorkerService : BackgroundService
    {
        private const int generalDelay = 1 * 10 * 1000; // 10 seconds

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(generalDelay, stoppingToken);
                await CallShelly();
                await CallCore();
            }
        }

        private static Task CallShelly()
        {
            Console.WriteLine("CallShelly");
            return Task.FromResult("Done");
        }

        private static Task CallCore()
        {
            Console.WriteLine("CallCore or whatever");
            return Task.FromResult("Done");
        }
    }
}
