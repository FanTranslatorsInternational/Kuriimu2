using System;

namespace Kontract.Models.Intermediate
{
    /// <summary>
    /// The event arguments for requesting key material
    /// </summary>
    public class RequestDataEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <param name="dataLength"></param>
        /// <param name="isRequestFile"></param>
        public RequestDataEventArgs(string requestMessage, int dataLength, bool isRequestFile, string requestId)
        {
            RequestId = requestId;
            RequestMessage = requestMessage;
            DataSize = dataLength;
            IsRequestFile = isRequestFile;
        }

        /// <summary>
        /// The RequestId to distinguish process operations on different files.
        /// </summary>
        public string RequestId { get; }

        /// <summary>
        /// The message displayed in an input field.
        /// </summary>
        public string RequestMessage { get; }

        /// <summary>
        /// The length of the data.
        /// </summary>
        public int DataSize { get; }

        /// <summary>
        /// Defines if event should request a file.
        /// </summary>
        public bool IsRequestFile { get; }

        /// <summary>
        /// The data returned by the event.
        /// </summary>
        public string Data { get; set; }
    }
}
