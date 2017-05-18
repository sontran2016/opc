using Newtonsoft.Json;
using OpcLib.Common;
using OpcServiceClient.Interface;
using System;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using OpcLib.Implement;
using OpcLib.Interface;
using System.Collections.Generic;
using System.IO;

namespace OpcServiceClient.Implement
{
    public delegate void ConnectDelegate(bool isConnected);
    public class ClientWinsService : IClientWins
    {
        private const string WinsServerComputer = "localhost";
        private NamedPipeServerStream _pipeClientRead, _pipeClientWrite;
        private IStreamString _ssRead, _ssWrite;
        private bool _connected;
        private const int TimeSleep = 5000;
        private string _pathLog;
        public bool IsConnected
        {
            get { return _connected; }
        }

        //private BackgroundWorker _worker;
        //public event ConnectDelegate OnConnect;
        public ClientWinsService()
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
                    try
                    {
                        //at server clientPipeRead->_pipeClientWrite
                        var ps = new PipeSecurity();
                        ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));

                        if (_pipeClientWrite != null)
                            _pipeClientWrite.Close();
                            //_pipeClientWrite.Dispose();
                        _pipeClientWrite = new NamedPipeServerStream("clientPipeRead", PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);
                        //if (_pipeServerWrite == null)
                        //    _pipeServerWrite = new NamedPipeServerStream("opcPipeRead", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

                        _pipeClientWrite.WaitForConnection();
                        _ssWrite = new StreamString(_pipeClientWrite);

                        if (_pipeClientRead != null)
                            _pipeClientRead.Close();
                            //_pipeClientRead.Dispose();
                        _pipeClientRead = new NamedPipeServerStream("clientPipeWrite", PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);
                        _pipeClientRead.WaitForConnection();
                        _ssRead = new StreamString(_pipeClientRead);

                        _connected = true;
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("The network path was not found"))
                            Thread.Sleep(TimeSleep); //wait for new connection
                        else
                            throw new Exception(ex.Message);
                    }
                } while (!_connected);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_pathLog, "\r\n"+DateTime.Now.ToString()+", ClientWinsService: "+ex.ToString());
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
                _pipeClientRead.Close();
                _pipeClientWrite.Close();
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
        //                _pipeClientRead = new NamedPipeClientStream(OpcServerComputer, "opcPipeRead", PipeDirection.InOut,
        //                    PipeOptions.None, TokenImpersonationLevel.Impersonation);
        //                _pipeClientWrite = new NamedPipeClientStream(OpcServerComputer, "opcPipeWrite",
        //                    PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);

        //                _pipeClientRead.Connect();
        //                _pipeClientWrite.Connect();
        //                _ssRead = new StreamString(_pipeClientRead);
        //                _ssWrite = new StreamString(_pipeClientWrite);
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
                    Server = new ServerModel() { Name = serverName },
                    Group = new GroupModel() { Name = groupName },
                    Tag = new TagModel() { Name = tagName, Value = value }
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
                var data = new
                {
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
                group.RequestType = RequestType.ReadGroupTags;
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
        //
        public bool ResponseTag(GroupResponseModel model)
        {
            try
            {
                model.RequestType = RequestType.ReadTag;
                var jsonText = JsonConvert.SerializeObject(model);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool ResponseTag(string serverName, string groupName, string tagName, object value)
        {
            try
            {
                var tag = new TagRequestModel()
                {
                    RequestType = RequestType.ReadTag,
                    Server = new ServerModel() { Name = serverName },
                    Group = new GroupModel() { Name = groupName },
                    Tag = new TagModel() { Name = tagName, Value = value }
                };
                var jsonText = JsonConvert.SerializeObject(tag);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ResponseListServer(List<ServerInfoModel> servers)
        {
            try
            {
                var data = new ServerListResponseModel();
                data.RequestType = RequestType.GetListServer;
                data.Servers = servers;
                var jsonText = JsonConvert.SerializeObject(data);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ResponseGroupTags(GroupResponseModel tags)
        {
            try
            {
                tags.RequestType = RequestType.ReadGroupTags;
                var jsonText = JsonConvert.SerializeObject(tags);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ResponseRemoveTag(TagRequestModel tag)
        {
            try
            {
                tag.RequestType = RequestType.RemoveTag;
                var jsonText = JsonConvert.SerializeObject(tag);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool ResponseAddTag(TagRequestModel tag)
        {
            try
            {
                tag.RequestType = RequestType.AddTag;
                var jsonText = JsonConvert.SerializeObject(tag);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool ResponseAddGroup(GroupRequestModel model)
        {
            try
            {
                model.RequestType = RequestType.AddGroup;
                var jsonText = JsonConvert.SerializeObject(model);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool ResponseRemoveGroup(GroupRequestModel model)
        {
            try
            {
                model.RequestType = RequestType.RemoveGroup;
                var jsonText = JsonConvert.SerializeObject(model);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool ResponseAddServer(ServerRequestModel model)
        {
            try
            {
                model.RequestType = RequestType.AddServer;
                var jsonText = JsonConvert.SerializeObject(model);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool ResponseRemoveServer(ServerRequestModel model)
        {
            try
            {
                model.RequestType = RequestType.RemoveServer;
                var jsonText = JsonConvert.SerializeObject(model);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool ResponseError(ErrorResponseModel error)
        {
            try
            {
                error.RequestType = RequestType.Error;
                var jsonText = JsonConvert.SerializeObject(error);
                WriteValue(jsonText);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
