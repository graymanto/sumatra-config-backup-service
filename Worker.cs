using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SumatraBackupService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher _fileWatcher;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Checking sumatra config for missing settings");

            var appDataPath = Environment.GetEnvironmentVariable("LocalAppData");
            var sumatraPath = Path.Combine(appDataPath, "SumatraPDF");
            var settingsFileName = "SumatraPDF-settings.txt";
            var syncFilePath = Path.Combine(sumatraPath, settingsFileName);
            var syncFileBackupPath = Path.Combine(sumatraPath, "SumatraPDF-settings.bck.txt");

            var fileContents = File.ReadAllText(syncFilePath);
            var matchRegEx = @"SessionData\s+\[\s*\]";
            var hasMissingSettings = Regex.IsMatch(fileContents, matchRegEx);

            if (hasMissingSettings)
            {
                var processNames = Process.GetProcessesByName("SumatraPDF");
                var running = processNames.Length > 0;

                if (running)
                {
                    Console.WriteLine("Empty Sumatra settings found but Sumatra is running. No action");
                }
                else
                {
                    if (File.Exists(syncFileBackupPath))
                    {
                        _logger.LogInformation("Sumatra settings missing. Copying last backup");
                        File.Copy(syncFileBackupPath, syncFilePath, true);
                    }

                }

            }
            else
            {
                _logger.LogInformation("Found correct sumatra settings on startup. No action");
            }

            _logger.LogInformation("Creating sumatra settings file watch");


            _fileWatcher = new FileSystemWatcher(sumatraPath);
            _fileWatcher.Filter = settingsFileName;

            _fileWatcher.Changed += (o, ea) =>
            {
                bool fileRead = false;

                while (!fileRead)
                {
                    try
                    {
                        fileContents = File.ReadAllText(syncFilePath);

                    }
                    catch (IOException)
                    {
                        _logger.LogInformation("Could not access settings file. Wait and try again");
                        Thread.Sleep(100);
                        continue;
                    }
                    fileRead = true;

                    hasMissingSettings = Regex.IsMatch(fileContents, matchRegEx);

                    if (!hasMissingSettings)
                    {
                        _logger.LogInformation("Configuration file changed. Making backup");
                        File.Copy(syncFilePath, syncFileBackupPath, true);
                    }
                    else
                    {
                        _logger.LogInformation("Configuration file changed but settings empty. No action");
                    }

                }
            };

            _fileWatcher.EnableRaisingEvents = true;

            await Task.CompletedTask;
        }
    }
}
