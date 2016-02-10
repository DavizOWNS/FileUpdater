using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FileUpdater
{
    class ReplaceFileOperationRequest : FileOperationRequestBase
    {
        public string SourceFilePath { get; private set; }
        public string DestinationFilePath { get; private set; }

        public ReplaceFileOperationRequest(string source, string dest)
        {
            RequestType = FIleOperationRequestType.Replace;

            SourceFilePath = source;
            DestinationFilePath = dest;
        }

        public override void PerformOperation()
        {
            FileInfo sourceFile = new FileInfo(SourceFilePath);

            try
            {
                sourceFile.CopyTo(DestinationFilePath, true);
            }
            catch(Exception)
            {
                //TODO
            }
        }
        public override Task PerformOperationAsyncTask(WebClient webClient)
        {
            return webClient.DownloadFileTaskAsync(SourceFilePath, DestinationFilePath);
        }
    }
}
