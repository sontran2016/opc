using Newtonsoft.Json;
using OpcLib.Common;
using OpcServiceClient.Interface;
using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using OpcLib.Implement;
using OpcLib.Interface;
using System.IO;

namespace OpcServiceClient.Implement
{
    //public delegate void ConnectDelegate(bool isConnected);
    public class ClientOpcServerService : IClientOpcServer
    {
        private const string OpcServerComputer = "hoang-virmach";
        private NamedPipeClientStream _pipeWClientRead, _pipeWClientWrite;
        private IStreamString _ssRead, _ssWrite;
        private bool _connected;
        private const int TimeSleep = 5000;
        private string _pathLog;
        public bool IsConnected {
            get { return _connected; }
        }

        //private BackgroundWorker _worker;
        //public event ConnectDelegate OnConnect;
        public ClientOpcServerService()
        {
            _connected = false;
            _pathLog = AppDomain.CurrentDomain.BaseDirectory + "MyLog.txt";

            //_worker = new BackgroundWorker();
            //_worker.DoWork += Woker_DoWork;
            //_worker.RunWorkerCompleted += Woker_RunWorkerCompleted;
            //_worker.ProgressChanged += Woker_ProgressChanged;
            //_worker.WorkerSupportsCancellation = true;
            //_worker.WorkerReportsProgress = true;
        }

        public void Connect()
        {
            try
            {
                if (_connected) return;
                do
                {
                    //try
                    //{
                        _pipeWClientRead = new NamedPipeClientStream(OpcServerComputer, "opcPipeRead", PipeDirection.InOut);
                        _pipeWClientWrite = new NamedPipeClientStream(OpcServerComputer, "opcPipeWrite",PipeDirection.InOut);

                        _pipeWClientRead.Connect();
                        _pipeWClientWrite.Connect();
                        _ssRead = new StreamString(_pipeWClientRead);
                        _ssWrite = new StreamString(_pipeWClientWrite);

                        _connected = true;
                        return;
                    //}
                    //catch (Exception ex)
                    //{
                    //    if (ex.Message.Contains("The network path was not found"))
                    //        Thread.Sleep(TimeSleep); //wait for new connection
                    //    else
                    //        throw new Exception(ex.Message);
                    //}
                } while (!_connected);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_pathLog, "\r\n" + DateTime.Now.ToString() + ", ClientOpcServerService: " + ex.ToString());
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
                _connected = false;
                _pipeWClientRead.Close();
                _pipeWClientWrite.Close();
            }
            return true;
        }

        //private void Woker_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    bool serverStop=false;
        //    var worker = sender as BackgroundWorker;
        //    do
        //    {
        //        try
        //        {
        //            serverStop = false;
        //            if (!_connected)
        //            {
        //                _pipeWClientRead = new NamedPipeClientStream(OpcServerComputer, "opcPipeRead", PipeDirection.InOut,
        //                    PipeOptions.None, TokenImpersonationLevel.Impersonation);
        //                _pipeWClientWrite = new NamedPipeClientStream(OpcServerComputer, "opcPipeWrite",
        //                    PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);

        //                _pipeWClientRead.Connect();
        //                _pipeWClientWrite.Connect();
        //                _ssRead = new StreamString(_pipeWClientRead);
        //                _ssWrite = new StreamString(_pipeWClientWrite);
        //            }
        //            e.Result = true;
        //            return;
        //        }
        //        catch (Exception ex)
        //        {
        //            if (ex.Message.Contains("The network path was not found"))
        //            {
        //                serverStop = true;
        //                Thread.Sleep(5000); //wait for new connection
        //            }
        //            else
        //                e.Result = false;
        //            //e.Result = false;
        //            //e.Cancel = true;
        //        }
        //    } while (serverStop);             
        //}

        //private void Woker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //}

        //private void Woker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    try
        //    {
        //        _connected = (bool)e.Result;
        //        if ((bool)e.Result)
        //            OnConnect(_connected);
        //        else if (e.Cancelled)
        //            OnConnect(false);
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}

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
        public bool RemoveTag(TagRequestModel tag)
        {
            try
            {
                var jsontext = JsonConvert.SerializeObject(tag);
                WriteValue(jsontext);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool AddTag(TagRequestModel tag)
        {
            try
            {
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
