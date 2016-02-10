using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUpdater
{
    class ReplaceDirectoryOperationRequest : FileOperationRequestBase
    {
        public string SourceDirPath { get; private set; }
        public string DestinationDirPath { get; private set; }

        public ReplaceDirectoryOperationRequest(string sourcePath, string destPath)
        {
            SourceDirPath = sourcePath;
            DestinationDirPath = destPath;
        }

        public override void PerformOperation()
        {
            DirectoryInfo sourceDir = new DirectoryInfo(SourceDirPath);
            try
            {
                DirectoryCopy(SourceDirPath, DestinationDirPath, true);
            }
            catch(Exception)
            {

            }
        }
        public override Task PerformOperationAsyncTask(System.Net.WebClient webClient)
        {
            throw new NotImplementedException();
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
