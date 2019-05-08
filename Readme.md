# Overview

The File Content Updater searches for files that match the specified name (or names),
then updates the files to find and replace text in the files.

Before using this program, create a tab-delimited text file with two columns.
For each row in the file, the first column has text to find and the second column has the replacement text.
Optionally include a header line with header names `TextToFind` and `ReplacementText`

See example file `SearchReplaceData.txt` in the `Docs` directory.

Use /Preview to preview the changes that would be made.
When preview is not used, for any file that is updated, 
a backup copy is made with a filename ending in `.old`.

## Console Switches

FileContentUpdater is a command line application.  Syntax:

```
FileContentUpdater.exe
 /I:InputDirectoryPath /F:FilenameToProcess /T:SearchReplaceFile.txt
 [/S] [/Preview]
```

Use /I to specify the directory to scan

Use /F to specify the filename to match; the path can contain the wildcard character *\
Multiple filenames can be specified by separating them with a semicolon, e.g. `RunStart.txt;TIC_Front.csv`

Use /T to specify the tab-delimited text file with text to find and replacement text.
The first line of this can have column headers `TextToFind` and `ReplacementText`.

Use /S to process all matching files in the input directory and its subdirectories.

Use /Preview to see the files that would be updated.

## Contacts

Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA) \
E-mail: matthew.monroe@pnnl.gov or proteomics@pnnl.gov \
Website: https://omics.pnl.gov/ or https://panomics.pnnl.gov/

## License

File Content Updater is licensed under the 2-Clause BSD License; you may not use this file 
except in compliance with the License. You may obtain a copy of the License at 
https://opensource.org/licenses/BSD-2-Clause

Copyright 2019 Battelle Memorial Institute
