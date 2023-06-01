using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concerto.Shared.Constants
{
    public static class DawHubMethods
    {
        public static class Server
        {
            public const string JoinDawProject = nameof(JoinDawProject);
            public const string RequestStopSharingVideo = nameof(RequestStopSharingVideo);
        }
        public static class Client
        {
            public const string OnProjectChanged = nameof(OnProjectChanged);
            public const string OnRequestStopSharingVideo = nameof(OnRequestStopSharingVideo);
        }
    }
}
