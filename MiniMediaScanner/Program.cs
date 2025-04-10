using CliFx;
using MiniMediaScanner.Jobs;
using Quartz;
using Quartz.Impl;

namespace MiniMediaScanner;

public class Program
{
    public static string[] ConsoleArguments { get; private set; }

    public static async Task Main(string[] args)
    {
        ATL.Settings.OutputStacktracesToConsole = false;
        ConsoleArguments = args;

        string? cronExpression = Environment.GetEnvironmentVariable("CRON");
        string? commandEnvironmentValue = Environment.GetEnvironmentVariable("COMMAND");
        
        if (!string.IsNullOrWhiteSpace(commandEnvironmentValue) && args?.Length == 0)
        {
            ConsoleArguments = [ commandEnvironmentValue ];
        }
        
        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            await CreateSchedulerAsync(cronExpression);
            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        else
        {
            try
            {
                await new CliApplicationBuilder()
                    .AddCommandsFromThisAssembly()
                    .Build()
                    .RunAsync(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    static async Task CreateSchedulerAsync(string cronExpression)
    {
        var factory = new StdSchedulerFactory();
        var scheduler = await factory.GetScheduler();
        await scheduler.Start();

        var job = JobBuilder.Create<CronJob>()
            .WithIdentity("cronJob", "group")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("trigger", "group")
            .WithCronSchedule(cronExpression)
            .Build();

        await scheduler.ScheduleJob(job, trigger);
    }
}