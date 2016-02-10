using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUpdater
{
    class DeleteFileOperationRequest : FileOperationRequestBase
    {
        public string FilePath { get; private set; }

        public DeleteFileOperationRequest(string path)
        {
            RequestType = FileOperationRequestBase.FIleOperationRequestType.Delete;

            FilePath = path;
        }

        public override void PerformOperation()
        {
            FileInfo fileInfo = new FileInfo(FilePath);
            try
            {
                fileInfo.Delete();
            }
            catch(Exception)
            {
                //TODO
            }
        }
        public override Task PerformOperationAsyncTask(System.Net.WebClient webClient)
        {
            return Task.Factory.StartNew(() => PerformOperation());
        }
    }
}
