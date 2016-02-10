using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FileUpdater
{
    class RemoteDirectoryComparer : DirectoryComparer
    {
        private WebClient downloadClient;
        private Task lastDownloadOperation;

        public RemoteDirectoryComparer() : base()
        {
            //downloadClient = new WebClient();
            lastDownloadOperation = null;
        }

        public override void HandleFileOperationRequestsAutomatically()
        {
            base.HandleFileOperationRequestsAutomatically();

            downloadClient = new WebClient();
            lastDownloadOperation = null;
        }

        protected async override void SendOperationRequest(FileOperationRequestBase operation)
        {
            if (handleFileOperationsAutomatically)
            {
                if (lastDownloadOperation != null)
                    await lastDownloadOperation;
                lastDownloadOperation = operation.PerformOperationAsyncTask(downloadClient);
                return;
            }

            CallOnFileOperation(operation);
        }

        public override void Dispose()
        {
            base.Dispose();

            downloadClient.Dispose();
        }
    }
}
