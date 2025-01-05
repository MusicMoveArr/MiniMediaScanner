using Quartz;

namespace MiniMediaScanner.Jobs;

[DisallowConcurrentExecution]
public class CronJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        try
        {
            Program.AppBuilder.Run(Program.ConsoleArguments);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return Task.CompletedTask;
    }
}