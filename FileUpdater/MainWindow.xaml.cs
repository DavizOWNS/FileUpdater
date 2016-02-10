using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace FileUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DirectoryInfo masterDir;
        DirectoryInfo secondDir;

        public MainWindow()
        {
            InitializeComponent();

            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            System.Net.ServicePointManager.DefaultConnectionLimit = 100000;
        }

        private DirectoryInfo GetDirectory()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;
            dialog.ShowDialog();

            if (string.IsNullOrEmpty(dialog.SelectedPath))
                return null;
            return new DirectoryInfo(dialog.SelectedPath);
        }

        private void BTNSelMasterDir_Click(object sender, RoutedEventArgs e)
        {
            masterDir = GetDirectory();
            if (masterDir == null)
                return;

            LBLMasterDir.Content = masterDir.FullName;

            if(masterDir != null && secondDir != null)
            {
                BTNCompare.IsEnabled = true;
            }
        }

        private void BTNSelOtherDir_Click(object sender, RoutedEventArgs e)
        {
            secondDir = GetDirectory();
            if (secondDir == null)
                return;
            LBLOtherDir.Content = secondDir.FullName;

            if (masterDir != null && secondDir != null)
            {
                BTNCompare.IsEnabled = true;
            }
        }

        private async void BTNCompare_Click(object sender, RoutedEventArgs e)
        {
            using (DirectoryComparer comparer = new DirectoryComparer())
            {
                comparer.OnProgressUpdated += comparer_OnProgressUpdated;
                //comparer.OnLogMessage += comparer_OnLogMessage;
                LBLLog.Content = string.Empty;

                Thread timeSpentCounter = new Thread(UpdateTimeElapsed);
                timeSpentCounter.IsBackground = true;
                timeSpentCounter.Priority = ThreadPriority.Lowest;
                timeSpentCounter.Start();

                CompareProgressBar.Visibility = System.Windows.Visibility.Visible;
                BTNCompare.Visibility = System.Windows.Visibility.Hidden;
                BTNStopCompare.Visibility = System.Windows.Visibility.Visible;

                comparer_OnProgressUpdated(null, 0);
                LBLLog.Content = "";

                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                TaskbarItemInfo.ProgressValue = 0;

                CancellationTokenSource cts = new CancellationTokenSource();
                RoutedEventHandler cancelHandler = (obj, args) =>
                {
                    cts.Cancel();
                };
                BTNStopCompare.Click += cancelHandler;

                //comparer.HandleFileOperationRequestsAutomatically();

                Stopwatch sw = new Stopwatch();
                try
                {
                    sw.Start();
                    await comparer.CompareAsync(masterDir, secondDir, cts.Token);
                }
                catch(TaskCanceledException)
                {
                    CompareProgressBar.Value = 1;
                    comparer_OnLogMessage(null, "Comparing cancelled");
                }
                finally
                {
                    sw.Stop();

                    BTNStopCompare.Click -= cancelHandler;

                    BTNStopCompare.Visibility = System.Windows.Visibility.Hidden;
                    BTNCompare.Visibility = System.Windows.Visibility.Visible;

                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                }

                timeSpentCounter.Abort();

                BTNCompare.IsEnabled = true;
            }
        }

        void UpdateTimeElapsed()
        {
            DateTime startTime = DateTime.Now;
            while(true)
            {
                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            BTNStopCompare.Content = string.Format("Stop ({0})", (DateTime.Now - startTime).ToString("hh':'mm':'ss"));
                        });
                    Thread.Sleep(1000);
                }
                catch(ThreadAbortException)
                {
                    break;
                }
            }
        }

        void comparer_OnProgressUpdated(object sender, double e)
        {
            CompareProgressBar.Value = e;
            TaskbarItemInfo.ProgressValue = e;
        }

        void comparer_OnLogMessage(object sender, string e)
        {
            LBLLog.Content += e + "\n";
            LogsScrollViewer.ScrollToBottom();
        }
    }
}
