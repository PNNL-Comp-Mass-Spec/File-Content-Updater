﻿using PRISM;
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace FileContentUpdater
{
    internal class Program
    {
        private static DateTime mLastProgressTime;

        private static int Main(string[] args)
        {
            mLastProgressTime = DateTime.UtcNow;

            var asmName = typeof(Program).GetTypeInfo().Assembly.GetName();
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);       // Alternatively: System.AppDomain.CurrentDomain.FriendlyName
            var version = UpdaterOptions.GetAppVersion();

            var parser = new CommandLineParser<UpdaterOptions>(asmName.Name, version)
            {
                ProgramInfo = "This program searches for files in a directory that match the specified name, " +
                              "then updates the files to find and replace text in the files.",

                ContactInfo = "Program written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)" +
                              Environment.NewLine + Environment.NewLine +
                              "E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov" + Environment.NewLine +
                              "Website: https://github.com/PNNL-Comp-Mass-Spec/ or https://panomics.pnnl.gov/ or https://www.pnnl.gov/integrative-omics",

                UsageExamples = {
                        exeName + @" C:\WorkDir /F:runstart.txt /T:SearchReplace.txt /Preview",
                        exeName + @" C:\WorkDir /F:runstart.txt /T:SearchReplace.txt /S /Preview",
                        exeName + @" C:\WorkDir /F:runstart.txt;tic_front.csv /T:SearchReplace.txt /S",
                    }
            };

            var result = parser.ParseArgs(args);
            var options = result.ParsedResults;

            try
            {
                if (!result.Success)
                {
                    if (parser.CreateParamFileProvided)
                    {
                        return 0;
                    }

                    // Delay for 1500 msec in case the user double clicked this file from within Windows Explorer (or started the program via a shortcut)
                    Thread.Sleep(1500);
                    return -1;
                }

                if (!options.ValidateArgs(out var errorMessage))
                {
                    parser.PrintHelp();

                    Console.WriteLine();
                    ConsoleMsgUtils.ShowWarning("Validation error:");
                    ConsoleMsgUtils.ShowWarning(errorMessage);

                    Thread.Sleep(1500);
                    return -1;
                }

                options.OutputSetOptions();
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.Write($"Error running {exeName}");
                Console.WriteLine(e.Message);
                Console.WriteLine($"See help with {exeName} --help");
                return -1;
            }

            try
            {
                var processor = new FileContentUpdater(options);

                processor.ErrorEvent += Processor_ErrorEvent;
                processor.StatusEvent += Processor_StatusEvent;
                processor.WarningEvent += Processor_WarningEvent;

                var success = processor.ReplaceTextInFiles();

                if (success)
                {
                    Console.WriteLine();
                    Console.WriteLine("Search complete");
                    Thread.Sleep(1500);
                    return 0;
                }

                ConsoleMsgUtils.ShowWarning("Processing error");
                Thread.Sleep(2000);
                return -1;
            }
            catch (Exception ex)
            {
                ConsoleMsgUtils.ShowError("Error occurred in Program->Main", ex);
                Thread.Sleep(2000);
                return -1;
            }
        }

        private static void Processor_DebugEvent(string message)
        {
            ConsoleMsgUtils.ShowDebug(message);
        }

        private static void Processor_ErrorEvent(string message, Exception ex)
        {
            ConsoleMsgUtils.ShowErrorCustom(message, ex, false);
        }

        private static void Processor_StatusEvent(string message)
        {
            Console.WriteLine(message);
        }

        private static void Processor_ProgressUpdate(string progressMessage, float percentComplete)
        {
            if (DateTime.UtcNow.Subtract(mLastProgressTime).TotalSeconds < 5)
                return;

            Console.WriteLine();
            mLastProgressTime = DateTime.UtcNow;
            Processor_DebugEvent(percentComplete.ToString("0.0") + "%, " + progressMessage);
        }

        private static void Processor_WarningEvent(string message)
        {
            ConsoleMsgUtils.ShowWarning(message);
        }
    }
}
