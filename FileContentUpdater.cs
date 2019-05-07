using System;
using System.Collections.Generic;
using System.IO;
using PRISM;

namespace FileContentUpdater
{
    public class FileContentUpdater : EventNotifier
    {
        private UpdaterOptions Options { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options"></param>
        public FileContentUpdater(UpdaterOptions options)
        {
            Options = options;
        }

        private Dictionary<string, string> ReadSearchReplaceFile(FileSystemInfo searchReplaceFile)
        {
            var searchReplaceList = new Dictionary<string, string>();

            try
            {
                OnStatusEvent("Reading search/replace items from: " + searchReplaceFile.FullName);

                using (var reader = new StreamReader(new FileStream(searchReplaceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var lineNumber = 0;

                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        lineNumber++;

                        if (string.IsNullOrWhiteSpace(dataLine))
                            continue;

                        var lineParts = dataLine.Split('\t');
                        if (lineParts.Length < 2)
                        {
                            OnWarningEvent(string.Format("Ignoring line {0}, which has fewer than two columns", lineNumber));
                            continue;
                        }

                        if (searchReplaceList.Count == 0)
                        {
                            if (lineParts[0].Equals("TextToFind", StringComparison.OrdinalIgnoreCase) &&
                                lineParts[1].Equals("ReplacementText", StringComparison.OrdinalIgnoreCase))
                            {
                                // Header line; skip it
                                continue;
                            }
                        }

                        if (searchReplaceList.ContainsKey(lineParts[0]))
                        {
                            // Duplicate key; skip it
                            OnWarningEvent(string.Format(
                                                            "Ignoring line {0}, which has search text already defined on a previous line: {1}",
                                                            lineNumber, dataLine));
                            continue;
                        }

                        searchReplaceList.Add(lineParts[0], lineParts[1]);
                    }
                }

                return searchReplaceList;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in ReadSearchReplaceFile", ex);
                return searchReplaceList;
            }

        }

        /// <summary>
        /// Search for the specified file names in the given search directory (and subdirectories)
        /// Open matching files, and replace text in the files using the data in the Search/Replace file
        /// </summary>
        /// <returns></returns>
        public bool ReplaceTextInFiles()
        {
            var success = ReplaceTextInFiles(Options.SearchReplaceFilePath, Options.DirectoryPath, Options.InputFileNames);
            return success;
        }

        /// <summary>
        /// Search for the specified file names in the given search directory (and subdirectories)
        /// Open matching files, and replace text in the files using the data in the Search/Replace file
        /// </summary>
        /// <param name="searchReplaceFilePath"></param>
        /// <param name="directoryPath"></param>
        /// <param name="inputFileNames">Files to process (supports wildcards)</param>
        /// <returns></returns>
        public bool ReplaceTextInFiles(string searchReplaceFilePath, string directoryPath, List<string> inputFileNames)
        {
            try
            {
                var searchDirectory = new DirectoryInfo(directoryPath);
                if (!searchDirectory.Exists)
                {
                    OnWarningEvent("Directory not found: " + directoryPath);
                    return false;
                }

                var searchReplaceFile = new FileInfo(searchReplaceFilePath);
                if (!searchReplaceFile.Exists)
                {
                    OnWarningEvent("File not found: " + searchReplaceFilePath);
                    return false;
                }

                var searchReplaceList = ReadSearchReplaceFile(searchReplaceFile);

                OnStatusEvent("Searching for files to update in: " + searchDirectory.FullName);

                var success = ReplaceTextInFiles(searchDirectory, inputFileNames, searchReplaceList);
                if (success)
                    return true;

                OnWarningEvent("Error processing " + searchDirectory.FullName);
                return false;

            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in ReplaceTextInFiles", ex);
                return false;
            }

        }

        private bool ReplaceTextInFiles(DirectoryInfo searchDirectory, ICollection<string> fileNamesToFind, Dictionary<string, string> searchReplaceList)
        {

            try
            {

                // Find files to update
                foreach (var fileNameSpec in fileNamesToFind)
                {
                    foreach (var candidateFile in searchDirectory.GetFiles(fileNameSpec))
                    {
                        var success = ReplaceTextInFile(candidateFile, searchReplaceList);
                    }
                }

                if (!Options.Recurse)
                    return true;

                // Process subdirectories
                foreach (var subDirectory in searchDirectory.GetDirectories())
                {
                    OnStatusEvent(" ... " + subDirectory.FullName.Replace(searchDirectory.FullName, string.Empty));
                    var subDirSuccess = ReplaceTextInFiles(subDirectory, fileNamesToFind, searchReplaceList);

                    if (!subDirSuccess)
                    {
                        OnWarningEvent("Error processing " + subDirectory.FullName);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in ReplaceTextInFiles", ex);
                return false;
            }

        }

        private bool ReplaceTextInFile(FileSystemInfo fileToUpdate, Dictionary<string, string> searchReplaceList)
        {
            try
            {
                var fileChanged = false;

                var updatedFileContent = new List<string>();
                var updateMessages = new List<string>();

                using (var reader = new StreamReader(new FileStream(fileToUpdate.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    while (!reader.EndOfStream)
                    {
                        var dataLine = reader.ReadLine();
                        if (dataLine == null)
                            continue;

                        var lineUpdated = false;
                        foreach (var searchItem in searchReplaceList)
                        {
                            if (dataLine.Contains(searchItem.Key))
                            {
                                var updatedLine = dataLine.Replace(searchItem.Key, searchItem.Value);

                                // Create a two line debug message
                                updateMessages.Add(string.Format("{0,-10}{1}\n{2,-12}{3}", "Changing", dataLine, "  To", updatedLine));

                                updatedFileContent.Add(updatedLine);

                                lineUpdated = true;
                                break;
                            }
                        }

                        if (lineUpdated)
                        {
                            fileChanged = true;
                            continue;
                        }

                        updatedFileContent.Add(dataLine);
                    }
                }


                if (!fileChanged)
                    return true;

                Console.WriteLine();

                if (Options.Preview)
                {
                    OnStatusEvent("Previewing updates to " + fileToUpdate.FullName);
                    foreach (var message in updateMessages)
                    {
                        OnDebugEvent(message);
                    }

                    return true;
                }

                OnStatusEvent("Updating " + fileToUpdate.FullName);
                foreach (var message in updateMessages)
                {
                    OnDebugEvent(message);
                }

                var originalFilePath = fileToUpdate.FullName;

                var backupFilePath = fileToUpdate.FullName + ".old";
                if (!File.Exists(backupFilePath))
                {
                    File.Move(fileToUpdate.FullName, backupFilePath);
                }

                using (var writer = new StreamWriter(new FileStream(originalFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    foreach (var lineToWrite in updatedFileContent)
                    {
                        writer.WriteLine(lineToWrite);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in ReplaceTextInFile", ex);
                return false;
            }
        }
    }
}
