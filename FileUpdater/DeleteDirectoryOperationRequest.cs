using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileUpdater
{
    class DeleteDirectoryOperationRequest : FileOperationRequestBase
    {
        public string FilePath { get; private set; }

        public DeleteDirectoryOperationRequest(string dirPath)
        {
            RequestType = FIleOperationRequestType.Delete;

            FilePath = dirPath;
        }

        public override void PerformOperation()
        {
            DirectoryInfo dir = new DirectoryInfo(FilePath);
            try
            {
                dir.Delete(true);
            }
            catch(Exception)
            {

            }
        }
        public override Task PerformOperationAsyncTask(System.Net.WebClient webClient)
        {
            return Task.Factory.StartNew(() => PerformOperation());
        }
    }
}
