using System;
using System.Collections.Generic;
using System.Reflection;
using PRISM;

namespace FileContentUpdater
{
    public class UpdaterOptions
    {
        /// <summary>
        /// Program date
        /// </summary>
        public const string PROGRAM_DATE = "May 7, 2019";

        #region "Properties"

        [Option("I", "Input", ArgPosition = 1, HelpShowsDefault = false, HelpText = "Starting directory")]
        public string DirectoryPath { get; set; }

        [Option("F", "File", ArgPosition = 2, HelpShowsDefault = false, HelpText = "File to process (accepts wildcards); specify multiple files by separating with a semicolon")]
        public string InputFileNameFilter { get; set; }

        public List<string> InputFileNames { get; }

        [Option("S", "Recurse", HelpShowsDefault = false, HelpText = "Search in subdirectories below the starting directory")]
        public bool Recurse { get; set; }

        [Option("P", "Preview", HelpShowsDefault = false, HelpText = "Preview changes")]
        public bool Preview { get; set; }

        [Option("T", "Text", HelpShowsDefault = false, HelpText = "Tab-delimited file with two columns, specifying text to find and replacement text")]
        public string SearchReplaceFilePath { get; set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public UpdaterOptions()
        {
            DirectoryPath = string.Empty;
            InputFileNameFilter = string.Empty;
            InputFileNames = new List<string>();
            Recurse = false;
            Preview = false;
            SearchReplaceFilePath = string.Empty;
        }

        /// <summary>
        /// Get the program version
        /// </summary>
        /// <returns></returns>
        public static string GetAppVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version + " (" + PROGRAM_DATE + ")";

            return version;
        }

        /// <summary>
        /// Show the options at the console
        /// </summary>
        public void OutputSetOptions()
        {
            Console.WriteLine("Options:");

            Console.WriteLine(" Searching for files in: {0}", DirectoryPath);
            if (Recurse)
            {
                Console.WriteLine(" Also searching subdirectories");
            }

            if (InputFileNames.Count == 1)
            {
                Console.WriteLine(" Processing files named: {0}", InputFileNames[0]);
            }
            else if (InputFileNames.Count > 1)
            {
                Console.WriteLine(" Processing files named: {0}", string.Join(", ", InputFileNames));
            }

            Console.WriteLine(" Search/replace text file: {0}", SearchReplaceFilePath);

            if (Preview)
                Console.WriteLine(" Previewing changes");

            Console.WriteLine();

        }

        /// <summary>
        /// Validate the options
        /// </summary>
        /// <returns></returns>
        public bool ValidateArgs()
        {
            if (string.IsNullOrWhiteSpace(DirectoryPath))
            {
                Console.WriteLine("Use /I to specify the starting directory");
                return false;
            }

            if (string.IsNullOrWhiteSpace(InputFileNameFilter))
            {
                Console.WriteLine("Use /F to specify the file name (or names) to process");
                return false;
            }

            if (string.IsNullOrWhiteSpace(SearchReplaceFilePath))
            {
                Console.WriteLine("Use /T to specify the file with text to find and replacement text");
                return false;
            }

            InputFileNames.Clear();
            foreach (var filename in InputFileNameFilter.Split(';'))
            {
                InputFileNames.Add(filename);
            }

            return true;
        }

    }
}
