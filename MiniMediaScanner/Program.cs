using ConsoleAppFramework;
using MiniMediaScanner.Commands;
using MiniMediaScanner.Jobs;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;

namespace MiniMediaScanner;

class Program
{
    public static string[] ConsoleArguments { get; private set; }
    public static ConsoleApp.ConsoleAppBuilder AppBuilder { get; private set; }
    
    static async Task Main(string[] args)
    {
        ConsoleArguments = args;
        AppBuilder = ConsoleApp.Create();
        AppBuilder.Add("import", ImportCommand.Import);
        AppBuilder.Add("updatemb", UpdateMBCommand.UpdateMB);
        AppBuilder.Add("missing", MissingCommand.Missing);
        AppBuilder.Add("deletedmedia", DeletedMediaCommand.DeletedMedia);
        AppBuilder.Add("convert", ConvertMediaCommand.ConvertMedia);
        AppBuilder.Add("fingerprint", FingerPrintMediaCommand.FingerPrintMedia);
        AppBuilder.Add("tagmissingmetadata", TagMissingMetadataCommand.TagMissingMetadata);
        AppBuilder.Add("deduplicate", DeDuplicateFileCommand.DeDuplicate);
        AppBuilder.Add("normalizefile", NormalizeFileCommand.NormalizeFile);
        AppBuilder.Add("equalizemediatag", EqualizeMediaTagCommand.EqualizeMediaTag);
        AppBuilder.Add("refreshmetadata", RefreshMetadataCommand.RefreshMetadata);
        AppBuilder.Add("fixversioning", FixVersioningCommand.FixVersioning);
        AppBuilder.Add("coverartarchive", CoverArtArchiveCommand.CoverArtArchive);
        
        string? importPath = Environment.GetEnvironmentVariable("IMPORT_PATH");
        string? cronExpression = Environment.GetEnvironmentVariable("CRON");
        string? connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING");
        
        if (!string.IsNullOrWhiteSpace(importPath))
        {
            ConsoleArguments = [ "import", "--connection-string", connectionString, "-p", importPath ];
        }
        
        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            await CreateSchedulerAsync(cronExpression);
        }
        else
        {
            try
            {
                AppBuilder.Run(ConsoleArguments);
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