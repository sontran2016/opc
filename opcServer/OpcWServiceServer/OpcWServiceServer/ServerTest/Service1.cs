using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace ServerTest
{
    public partial class Service1 : ServiceBase
    {
        SocketServer sServer;
        public Service1()
        {
            InitializeComponent();            
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            sServer = new SocketServer();
            var t = new Thread(new ThreadStart(() => {
                sServer.StartListening();
            }));
            t.Start();
        }

        protected override void OnStop()
        {
            sServer.Stop();
        }
    }
}
