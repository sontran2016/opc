using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Threading;

namespace OpcWServiceServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //var p = new OpcWindowsService();
            //p.OnDebug();
            //Thread.Sleep(System.Threading.Timeout.Infinite);
#if DEBUG
            var p = new OpcWindowsService();
            p.OnDebug();
            Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            var serviceToRun = new ServiceBase[] { new OpcWindowsService() };
            ServiceBase.Run(serviceToRun);
#endif
            //var serviceToRun = new ServiceBase[] { new OpcWindowsService() };
            //ServiceBase.Run(serviceToRun);
        }
    }
}
