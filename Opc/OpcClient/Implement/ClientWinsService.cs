using Newtonsoft.Json;
using OpcLib.Common;
using OpcClient.Interface;
using System;
using System.ComponentModel;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using OpcLib.Implement;
using OpcLib.Interface;

namespace OpcClient.Implement
{
    public delegate void ConnectDelegate(bool isConnected);
    public class ClientWinsService : IClientWins
    {
        private const string WinsServerComputer = "localhost";
        private const int TimeSleep = 5000;

        private NamedPipeClientStream _pipeClientRead, _pipeClientWrite;
        private IStreamString _ssRead, _ssWrite;
        private bool _connected;

        private BackgroundWorker _worker;
        public event ConnectDelegate OnConnect;
        public ClientWinsService()
        {
            _connected = false;
            _worker = new BackgroundWorker();
            _worker.DoWork += Woker_DoWork;
            _worker.RunWorkerCompleted += Woker_RunWorkerCompleted;
            _worker.ProgressChanged += Woker_ProgressChanged;
            _worker.WorkerSupportsCancellation = true;
            _worker.WorkerReportsProgress = true;
        }

        public void Connect()
        {
            try
            {
                if (!_connected && !_worker.IsBusy)
                    _worker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
            }
        }

        //public bool DisConnect()
        //{
        //    _connected = false;
        //    return true;
        //}

        public bool CloseConnection()
        {
            if (_connected)
            {
                _worker.CancelAsync();
                _connected = false;
                _pipeClientRead.Close();
                _pipeClientWrite.Close();
            }
            return true;
        }

        private void Woker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool serverStop=false;
            var worker = sender as BackgroundWorker;
            do
            {
                try
                {
                    serverStop = false;
                    if (!_connected)
                    {
                        _pipeClientRead = new NamedPipeClientStream(WinsServerComputer, "clientPipeRead", PipeDirection.InOut,
                            PipeOptions.None, TokenImpersonationLevel.Impersonation);
                        _pipeClientWrite = new NamedPipeClientStream(WinsServerComputer, "clientPipeWrite",
                            PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);

                        _pipeClientRead.Connect();
                        _pipeClientWrite.Connect();
                        _ssRead = new StreamString(_pipeClientRead);
                        _ssWrite = new StreamString(_pipeClientWrite);
                    }
                    e.Result = true;
                    return;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("The network path was not found"))
                    {
                        serverStop = true;
                        Thread.Sleep(TimeSleep); //wait for new connection
                    }
                    else
                        e.Result = false;
                    //e.Result = false;
                    //e.Cancel = true;
                }
            } while (serverStop);             
        }

        private void Woker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void Woker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                _connected = (bool)e.Result;
                if ((bool)e.Result)
                    OnConnect(_connected);
                else if (e.Cancelled)
                    OnConnect(false);
            }
            catch (Exception ex)
            {
            }
        }

        public object ReadValue()
        {
            if (!_connected) return null;
            var value = _ssRead.ReadString();
            return value;
        }

        private void WriteValue(object value)
        {
            if (!_connected) return;
            _ssWrite.WriteString(value.ToString());
        }
        //more func
        public bool WriteTag(string serverName, string groupName, string tagName, object value)
        {
            try
            {
                var tag = new TagRequestModel()
                {
                    RequestType = RequestType.WriteTag,
                    Server = new ServerModel() { Name=serverName},
                    Group = new GroupModel() {  Name=groupName},
                    Tag = new TagModel() { Name=tagName , Value = value }
                };
                var jsontext = JsonConvert.SerializeObject(tag);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool RemoveTag(ListTagModel model)
        {
            try
            {
                var tag = new TagRequestModel()
                {
                    RequestType = RequestType.RemoveTag,
                    Server = model.Server,
                    Group = model.Group,
                    Tag = new TagModel() { Name = model.Name }
                };
                var jsontext = JsonConvert.SerializeObject(tag);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool AddTag(TagInfoModel model)
        {
            try
            {
                var tag = new TagRequestModel()
                {
                    RequestType = RequestType.AddTag,
                    Server = model.Server,
                    Group = model.Group,
                    Tag = model.Tag
                };
                var jsontext = JsonConvert.SerializeObject(tag);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool GetListServer()
        {
            try
            {
                if (!_connected) return false;
                var data = new {
                    RequestType = RequestType.GetListServer
                };
                var jsontext = JsonConvert.SerializeObject(data);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ReadGroupTags(GroupRequestModel group)
        {
            try
            {
                group.RequestType= RequestType.ReadGroupTags;
                var jsontext = JsonConvert.SerializeObject(group);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool AddGroup(GroupRequestModel model)
        {
            try
            {
                var jsontext = JsonConvert.SerializeObject(model);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RemoveGroup(GroupRequestModel model)
        {
            try
            {
                var jsontext = JsonConvert.SerializeObject(model);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool AddServer(ServerRequestModel model)
        {
            try
            {
                var jsontext = JsonConvert.SerializeObject(model);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool RemoveServer(ServerRequestModel model)
        {
            try
            {
                var jsontext = JsonConvert.SerializeObject(model);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        
    }
}
