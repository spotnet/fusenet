using Fusenet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace Fusenet
{
    internal enum ArticleEncoding : int
    {
        yEnc,
        None,
        Base64,
        UuEncode,
        Quoted,
        gZip,
        AutoDetect
    }

    internal enum ArticleCompression : int
    {
        Zip,
        Rar,
        None
    }

    internal enum NNTPStatus : int
    {
        Connecting,
        Authenticating,
        Singleline,
        Multiline,
        Closed
    }

    internal enum WorkStatus : int
    {
        Queued,
        Downloading,
        Completed,
        Decoded,
        Decompressed,
        Missing,
        Failed
    }

    internal enum SchedulerMode : int
    {
        Sequential,
        Synchronous
    }

    internal enum SlotStatus
    {
        Queued,
        Downloading,
        Decoding,
        Extracting,
        Verifying,
        Repairing,
        Paused,
        Completed,
        Failed,
    }

    internal enum ConnectionStatus
    {
        Enabled,
        Disabled,
    }

    public enum ServerPriority
    {
        High,
        Default,
        Low,
    }

    public enum NNTPCodes
    {
        DateFollows = 111,
        PostingAllowed = 200,
        PostingDisallowed = 201,
        StreamingOK = 203,
        GoodBye = 205,
        GroupSelected = 211,
        GroupsFollow = 215,
        XINDEXFollows = 218,
        ArticleFollows = 220,
        HeadFollows = 221,
        BodyFollows = 222,
        StatFollows = 223,
        XOVERFollows = 224,
        NewNewsFollows = 230,
        Transferred = 235,
        PleaseSend = 238,
        TransferredOK = 239,
        PostedOK = 240,
        Authenticated1 = 250,
        Authenticated2 = 281,
        XGTITLEFollows = 282,
        XTHREADFollows = 288,
        SendArticle = 335,
        SendPost = 340,
        ContinueAuth = 350,
        MoreAuthentication = 381,
        TooManyConnections = 400,
        GroupNotFound = 411,
        NoGroupSelected = 412,
        NoArticleSelected = 420,
        NoNext = 421,
        NoPrevious = 422,
        NumberNotFound = 423,
        IDNotFound = 430,
        TryLater = 431,
        NotWanted = 435,
        TryAgain = 436,
        DoNotTryAgain = 437,
        AlreadyHave = 438,
        TransferFailed = 438,
        PostingNotAllowed = 440,
        PostingFailed = 441,
        AuthRequired = 450,
        AuthRejected = 452,
        TransferDenied = 480,
        AuthFailed1 = 481,
        AuthFailed2 = 482,
        BadCommand = 500,
        BadSyntax = 501,
        PermissionDenied = 502,
        FatalError = 503,
        SocketSuccess = 900,
        SocketUnknown = 901,
        SocketInterrupted = 902,
        SocketAccessDenied = 903,
        SocketFault = 904,
        SocketInvalidArgument = 905,
        SocketTooManyOpenSockets = 906,
        SocketWouldBlock = 907,
        SocketInProgress = 908,
        SocketAlreadyInProgress = 909,
        SocketNotSocket = 910,
        SocketDestinationAddressRequired = 911,
        SocketMessageSize = 912,
        SocketProtocolType = 913,
        SocketProtocolOption = 914,
        SocketProtocolNotSupported = 915,
        SocketSocketNotSupported = 916,
        SocketOperationNotSupported = 917,
        SocketProtocolFamilyNotSupported = 918,
        SocketAddressFamilyNotSupported = 919,
        SocketAddressAlreadyInUse = 920,
        SocketAddressNotAvailable = 921,
        SocketNetworkDown = 922,
        SocketNetworkUnreachable = 923,
        SocketNetworkReset = 924,
        SocketConnectionAborted = 925,
        SocketConnectionReset = 926,
        SocketNoBufferSpaceAvailable = 927,
        SocketIsConnected = 928,
        SocketNotConnected = 929,
        SocketShutdown = 930,
        SocketTimedOut = 931,
        SocketConnectionRefused = 932,
        SocketHostDown = 933,
        SocketHostUnreachable = 934,
        SocketProcessLimit = 935,
        SocketSystemNotReady = 936,
        SocketVersionNotSupported = 937,
        SocketNotInitialized = 938,
        SocketDisconnecting = 939,
        SocketTypeNotFound = 940,
        SocketHostNotFound = 941,
        SocketTryAgain = 942,
        SocketNoRecovery = 943,
        SocketNoData = 944,
        SocketIOPending = 945,
        SocketOperationAborted = 946,
    }

    internal interface VirtualItem
    {
        int Count { get; }
        NNTPInfo Info { get; }
        bool WriteXML(XmlWriter xR);
        List<VirtualItem> VirtualList { get; }
    }

    internal interface IndexedObject
    {
        int ID { get; set; }
        int Index { get; set; }

        int CompareTo(object obj);
        int CompareTo(IndexedObject obj);
    }

    internal class NNTPInput
    {
        private string zName;
        public List<NNTPSegment> Segments = new List<NNTPSegment>();
        internal NNTPInput(string sName) { zName = sName; }
        public string Name { get { return zName; } }
    }

    internal class NNTPSegment : IComparable<NNTPSegment>, IComparable
    {
        public int Index = 0;
        public int ExpectedSize = 0;

        public string Command = "";
        public List<string> Commands = null;

        internal NNTPSegment() { }

        internal NNTPSegment(int Number, int Bytes, string MessageID)
        {
            Index = Number;
            ExpectedSize = Bytes;
            this.Command = "BODY <" + MessageID + ">";
        }

        public int CompareTo(object obj) { return CompareTo(obj as NNTPSegment); }
        public int CompareTo(NNTPSegment obj) { return this.Index.CompareTo(obj.Index); }
    }

    internal class NNTPError
    {
        public int Code = 0;
        public int Tries = 0;
        public string Log = "";
        public string Message = "";
    }

    internal class WorkArgs : EventArgs
    {
        private int iCode;
        private Stream bData;
        private string sMessage;

        public WorkArgs(Stream bDat) { bData = bDat; }

        public WorkArgs(int Code, string sMsg)
        {
            iCode = Code;
            sMessage = sMsg;
        }

        public int Code { get { return iCode; } }
        public Stream Data { get { return bData; } }
        public string Message { get { return sMessage; } }

    } // <BZ5P64z4ag>

    internal class Stats
    {
        private long zLastTime = 0;
        private long zLastReset = 0;
        private long zTotalTime = 0;

        private long zLastBytes = 0;
        private long zFakeBytes = 0;
        private long zTotalBytes = 0;

        internal long TotalTime
        {
            set { Common.Safe32(ref zTotalTime, value); }
            get { return Interlocked.Read(ref zTotalTime); }
        }

        internal long TotalBytes
        {
            set { Common.Safe32(ref zTotalBytes, value); }
            get { return Interlocked.Read(ref zTotalBytes); }
        }

        internal long FakeBytes
        {
            set { Common.Safe32(ref zFakeBytes, value); }
            get { return Interlocked.Read(ref zFakeBytes); }
        }

        internal long LastTime
        {
            set { Common.Safe32(ref zLastTime, value); }
            get { return Interlocked.Read(ref zLastTime); }
        }

        private long LastReset
        {
            set { Common.Safe32(ref zLastReset, value); }
            get { return Interlocked.Read(ref zLastReset); }
        }

        internal long LastBytes
        {
            set { Common.Safe32(ref zLastBytes, value); }
            get { return Interlocked.Read(ref zLastBytes); }
        }

        internal void ValidateCache()
        {
            if (DateTime.UtcNow.Subtract(new DateTime(LastReset)).TotalSeconds >= 5)
            {
                LastTime = 0;
                LastBytes = 0;
                LastReset = DateTime.UtcNow.Ticks;
            }
        }

        internal void Progress(long AddedBytes)
        {
            Common.Add32(ref zFakeBytes, AddedBytes);
        }

        internal void Statistics(long AddedBytes, long AddedTime)
        {
            ValidateCache();

            Common.Add32(ref zLastTime, AddedTime);
            Common.Add32(ref zTotalTime, AddedTime);

            Common.Add32(ref zLastBytes, AddedBytes);
            Common.Add32(ref zTotalBytes, AddedBytes);
        }
    }  // <weSceqYmadY>
} 
