using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FileUpdater
{
    abstract class FileOperationRequestBase
    {
        public enum FIleOperationRequestType
        {
            Delete,
            Replace
        }

        public FIleOperationRequestType RequestType { get; protected set; }

        public abstract void PerformOperation();
        public abstract Task PerformOperationAsyncTask(WebClient webClient);
    }
}
