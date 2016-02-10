using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FileUpdater
{
    class DirectoryComparer : IDisposable
    {
        public event EventHandler<string> OnLogMessage;
        public event EventHandler<double> OnProgressUpdated;
        public event EventHandler<FileOperationRequestBase> OnFileOperation;

        private int numFilesCompared;
        private int numFilesToCompare;
        private float progress;

        private CancellationToken cancelToken;
        protected bool handleFileOperationsAutomatically { get; private set; }

#if DEBUG
        private DirectoryInfo tmpDir;
#endif

        public DirectoryComparer()
        {
            handleFileOperationsAutomatically = false;
        }
        public virtual void HandleFileOperationRequestsAutomatically()
        {
            handleFileOperationsAutomatically = true;
        }

        public Task CompareAsync(DirectoryInfo master, DirectoryInfo other)
        {
            return Task.Run(() => Compare(master, other));
        }
        public Task CompareAsync(DirectoryInfo master, DirectoryInfo other, CancellationToken cancelationToken)
        {
            cancelToken = cancelationToken;
            return Task.Run(() => Compare(master, other), cancelationToken);
        }

        public void Compare(DirectoryInfo master, DirectoryInfo other)
        {
            //LogMessage("Calculating files...");
            //numFilesCompared = 0;
            //numFilesToCompare = GetNumFiles(master);
            //progress = 0;

#if DEBUG
            if (tmpDir != null)
                RestoreDirectory(tmpDir);
            tmpDir = CreateDirectoryCopy(other);
#endif

            LogMessage("Comparing " + numFilesToCompare + " files");
            CompareDirectories(master, other);
        }
        private void CompareDirectories(DirectoryInfo master, DirectoryInfo other)
        {
            if (cancelToken != null && cancelToken.IsCancellationRequested)
            {
#if DEBUG
                RestoreDirectory(tmpDir);
                tmpDir = null;
#endif
                throw new TaskCanceledException();
            }

            try
            {
                DirectoryInfo[] masterSubDirs = master.GetDirectories();
                DirectoryInfo[] otherSubDirs = other.GetDirectories();

                if (masterSubDirs.Length == 0 || otherSubDirs.Length == 0)
                {
                    if (masterSubDirs.Length > 0)
                    {
                        foreach (var dir in masterSubDirs)
                        {
                            LogMessage(string.Format("Copying directory {0} from {1} to {2}", dir.Name, master.FullName, other.FullName));
                            SendOperationRequest(new ReplaceDirectoryOperationRequest(dir.FullName, other.FullName + "/" + dir.Name));
                            UpdateProgress();
                        }
                    }
                    else if (otherSubDirs.Length > 0)
                    {
                        foreach (var dir in otherSubDirs)
                        {
                            LogMessage(string.Format("Deleting directory {0} from {1}", dir.Name, other.FullName));
                            SendOperationRequest(new DeleteDirectoryOperationRequest(dir.FullName));
                        }
                    }
                }
                else
                {
                    var joinedDirs = masterSubDirs.FullOuterGroupJoin(otherSubDirs, a => a.Name, b => b.Name,
                        (m, o, path) => new { MasterDirDir = m.FirstOrDefault(), OtherDirDir = o.FirstOrDefault(), Path = path });

                    Parallel.ForEach(joinedDirs, (dirPair) =>
                        {
                            if (dirPair.MasterDirDir == null)
                            {
                                LogMessage(string.Format("Deleting directory {0} from {1}", dirPair.Path, other.FullName));
                                SendOperationRequest(new DeleteDirectoryOperationRequest(dirPair.OtherDirDir.FullName));
                            }
                            else if (dirPair.OtherDirDir == null)
                            {
                                LogMessage(string.Format("Copying directory {0} from {1} to {2}", dirPair.Path, master.FullName, other.FullName));
                                SendOperationRequest(new ReplaceDirectoryOperationRequest(dirPair.MasterDirDir.FullName, other.FullName + "/" + dirPair.MasterDirDir.Name));
                                UpdateProgress();
                            }
                            else
                            {
                                LogMessage(string.Format("Comparing directory {0} to {1}", dirPair.MasterDirDir, dirPair.OtherDirDir));
                                CompareDirectories(dirPair.MasterDirDir, dirPair.OtherDirDir);
                                UpdateProgress();
                            }
                        });
                    //foreach (var dirPair in joinedDirs)
                    //{
                    //    if (dirPair.MasterDirDir == null)
                    //    {
                    //        LogMessage(string.Format("Deleting directory {0} from {1}", dirPair.Path, other.FullName));
                    //        SendOperationRequest(new DeleteDirectoryOperationRequest(dirPair.OtherDirDir.FullName));
                    //    }
                    //    else if (dirPair.OtherDirDir == null)
                    //    {
                    //        LogMessage(string.Format("Copying directory {0} from {1} to {2}", dirPair.Path, master.FullName, other.FullName));
                    //        SendOperationRequest(new ReplaceDirectoryOperationRequest(dirPair.MasterDirDir.FullName, other.FullName + "/" + dirPair.MasterDirDir.Name));
                    //        UpdateProgress();
                    //    }
                    //    else
                    //    {
                    //        LogMessage(string.Format("Comparing directory {0} to {1}", dirPair.MasterDirDir, dirPair.OtherDirDir));
                    //        CompareDirectories(dirPair.MasterDirDir, dirPair.OtherDirDir);
                    //        UpdateProgress();
                    //    }
                    //}
                }

                FileInfo[] masterFiles = master.GetFiles();
                FileInfo[] otherFiles = other.GetFiles();

                if (masterFiles.Length == 0 || otherFiles.Length == 0)
                {
                    if (masterFiles.Length > 0)
                    {
                        foreach (var file in masterFiles)
                        {
                            LogMessage(string.Format("Copying file {0} from {1} to {2}", file.Name, master.FullName, other.FullName));
                            SendOperationRequest(new ReplaceFileOperationRequest(file.FullName, other.FullName + "/" + file.Name));
                            UpdateProgress();
                        }
                    }
                    else if (otherFiles.Length > 0)
                    {
                        foreach (var file in otherFiles)
                        {
                            LogMessage(string.Format("Deleting file {0} from {1}", file.Name, other.FullName));
                            SendOperationRequest(new DeleteFileOperationRequest(file.FullName));
                        }
                    }
                }
                else
                {
                    var joinedFiles = master.GetFiles().FullOuterGroupJoin(other.GetFiles(), a => a.Name, b => b.Name,
                        (m, o, path) => new { MasterDirFile = m.FirstOrDefault(), OtherDirFile = o.FirstOrDefault(), Path = path });

                    Parallel.ForEach(joinedFiles, (filePair) =>
                        {
                            CompareFiles(filePair.MasterDirFile, filePair.OtherDirFile, other);
                            if (filePair.MasterDirFile != null)
                                UpdateProgress();
                        });
                    //foreach (var filePair in joinedFiles)
                    //{
                    //    CompareFiles(filePair.MasterDirFile, filePair.OtherDirFile, other);
                    //    if (filePair.MasterDirFile != null)
                    //        UpdateProgress();
                    //}
                }
            }
            catch (SecurityException e)
            {
                LogMessage(e.Message);
                UpdateProgress();
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                LogMessage(e.Message);
                UpdateProgress();
                return;
            }
        }

        private void CompareFiles(FileInfo masterFile, FileInfo otherFile, DirectoryInfo otherParentDir)
        {
            if (cancelToken != null && cancelToken.IsCancellationRequested)
            {
#if DEBUG
                RestoreDirectory(tmpDir);
                tmpDir = null;
#endif
                throw new TaskCanceledException();
            }

            LogMessage(string.Format("Comparing {0} to {1}", 
                masterFile == null ? "null" : masterFile.FullName, otherFile == null ? "null" : otherFile.FullName));
            if (masterFile == null || !masterFile.Exists)
            {
                LogMessage("File does not exist");
                SendOperationRequest(new DeleteFileOperationRequest(otherFile.FullName));
                return;
            }
            if (otherFile == null || !otherFile.Exists)
            {
                LogMessage("File does not exist");
                SendOperationRequest(new ReplaceFileOperationRequest(masterFile.FullName, otherParentDir.FullName + "/" + masterFile.Name));
                return;
            }

            if (masterFile.Length != otherFile.Length)
            {
                LogMessage("Files are different (lenghts differ)");
                SendOperationRequest(new ReplaceFileOperationRequest(masterFile.FullName, otherParentDir.FullName + "/" + masterFile.Name));
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            string aHash = Utility.FileToHashString(masterFile.FullName);
            string bHash = Utility.FileToHashString(otherFile.FullName);
            sw.Stop();
            if (string.Compare(aHash, bHash) == 0)
            {
                LogMessage(string.Format("Files are equal (from hash, in {0}ms)", sw.ElapsedMilliseconds));
            }
            else
            {
                LogMessage(string.Format("Files are different (from hash, in {0}ms)", sw.ElapsedMilliseconds));
                SendOperationRequest(new ReplaceFileOperationRequest(masterFile.FullName, otherParentDir.FullName + "/" + masterFile.Name));
            }
        }

#if DEBUG
        private DirectoryInfo CreateDirectoryCopy(DirectoryInfo dir)
        {
            int slashIdx = dir.FullName.LastIndexOf('\\');
            string newDirName = dir.FullName.Substring(0, slashIdx + 1) + "tmp_" + dir.Name;
            ReplaceDirectoryOperationRequest.DirectoryCopy(dir.FullName, newDirName, true);

            return new DirectoryInfo(newDirName);
        }
        private void RestoreDirectory(DirectoryInfo dir)
        {
            int slashIdx = dir.FullName.LastIndexOf('\\');
            string newDirName = dir.FullName.Substring(0, slashIdx + 1) + dir.Name.Substring(4);

            (new DirectoryInfo(newDirName)).Delete(true);
            dir.MoveTo(newDirName);
        }
#endif

        private int GetNumFiles(DirectoryInfo dir)
        {
            int num = 0;
            try
            {
                num = dir.GetFiles().Length;
            }
            catch (SecurityException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }

            foreach (var childDir in dir.EnumerateDirectories())
            {
                num += 1 + GetNumFiles(childDir);
            }
            return num;
        }
        private void LogMessage(string m)
        {
            if (OnLogMessage != null)
                Application.Current.Dispatcher.Invoke(() => OnLogMessage(this, m));
        }
        private void UpdateProgress()
        {
            return;
            numFilesCompared++;
            progress = (float)numFilesCompared / numFilesToCompare;

            if (OnProgressUpdated != null)
            {
                Application.Current.Dispatcher.Invoke(() => OnProgressUpdated(this, progress));
            }
        }
        protected virtual void SendOperationRequest(FileOperationRequestBase operation)
        {
            if(handleFileOperationsAutomatically)
            {
                operation.PerformOperation();
                return;
            }
            CallOnFileOperation(operation);
        }
        protected void CallOnFileOperation(FileOperationRequestBase operation)
        {
            if (OnFileOperation != null)
                OnFileOperation(this, operation);
        }

        public virtual void Dispose()
        {
#if DEBUG
            if (tmpDir != null)
                RestoreDirectory(tmpDir);
#endif
            OnLogMessage = null;
            OnProgressUpdated = null;
            OnFileOperation = null;
        }
    }
}
