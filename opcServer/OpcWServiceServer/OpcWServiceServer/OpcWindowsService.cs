//using OPCLayer.OPC;
//using OPCUtility;
using System;
using System.ServiceProcess;
using System.Threading;
using OpcWServiceServer.Implement;
using OpcWServiceServer.Interface;
using OpcWServiceServer.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace OpcWServiceServer
{    
    public partial class OpcWindowsService : ServiceBase
    {
        private TagService _tagService;
        private AsyncSocketServer _sServer;
        //private IServerClientService _OpcWServiceServer;

        //private bool _disConnect = true;    //to client
        private bool _stop = true;    //to server
        string _pathLog = null;

        public OpcWindowsService()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _pathLog = AppDomain.CurrentDomain.BaseDirectory + "MyLog.txt";
                _tagService = new TagService();
                _tagService.OnDataChanged += new DataChanged(_tagService_OnDataChanged);
                _stop=!_tagService.Connect();
                _sServer=new AsyncSocketServer();
                _sServer.OnReceiveData += new ReceiveDataDelegate(ProccessData);
                //_OpcWServiceServer=new ServerClientService();

                var tStart = new Thread(new ThreadStart(() =>
                {
                    if(!_stop)   
                        _sServer.StartListening();
                    //while (!_stop)
                    //{
                    //    if(_disConnect)
                    //        _disConnect = !_OpcWServiceServer.Connect();
                    //    Thread.Sleep(5000); //for new Connection
                    //}                    
                }));
                tStart.Start();

                //Execute client request
                //var tRead = new Thread(ServerThreadRead);
                //tRead.Start();
            }
            catch (Exception ex)
            {
                WriteFile(ex.ToString());
                //Console.WriteLine(ex.Message);
                //Environment.Exit(-1);
            }
        }

        void _tagService_OnDataChanged(GroupResponseModel model)
        {
            try
            {
                _sServer.Send(model);
                //if (!_disConnect)
                //{
                //    _OpcWServiceServer.ResponseTag(model);
                //}
            }
            catch (Exception ex)
            {
                //_disConnect = true;
                //_OpcWServiceServer.DisConnect();
            }
        }

        protected override void OnStop()
        {
            //_disConnect = true;
            _stop = true;
            _tagService.DisConnect();
            _sServer.Stop();
            //_OpcWServiceServer.DisConnect();
        }

        #region myfunc

        private void ProccessData(RequestType type, object data)
        {
            try
            {
                string msg;
                bool success;
                //while (!_stop)
                //{
                    //if (_disConnect) continue;
                    //var st = _OpcWServiceServer.ReadValue();
                    //if (st != null)
                    //{
                        //var txt = st.ToString();
                        //if (txt == "") continue;
                        //var objJson = JsonConvert.DeserializeObject(txt);
                        //var requestType = int.Parse(((Newtonsoft.Json.Linq.JValue)(((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)objJson).First).Value)).Value.ToString());
                        switch (type)
                        {
                            case RequestType.WriteTag:
                                var tag = data as TagRequestModel;
                                //var tag = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                _tagService.SetValue(tag.Server.Name, tag.Group.Name, tag.Tag.Name, tag.Tag.Value);
                                break;
                            case RequestType.GetListServer:
                                var servers = _tagService.GetListServer();
                                //_OpcWServiceServer.ResponseListServer(servers);
                                _sServer.Send(servers);
                                break;
                            case RequestType.ReadGroupTags:
                                var group = data as GroupRequestModel;
                                //var group = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                var tags = _tagService.ReadGroupTags(group, out msg);
                                if (tags != null)
                                    _sServer.Send(tags);
                                    //_OpcWServiceServer.ResponseGroupTags(tags);
                                else
                                    _sServer.Send(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                    //_OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                break;
                            case RequestType.RemoveTag:
                                var tagRemove = data as TagRequestModel;
                                //var tagRemove = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                success = _tagService.RemoveTag(tagRemove,out msg);
                                if (success)
                                    _sServer.Send(tagRemove);
                                    //_OpcWServiceServer.ResponseRemoveTag(tagRemove);
                                else
                                    _sServer.Send(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                break;
                            case RequestType.AddTag:
                                var tagAdd = data as TagRequestModel;
                                //var tagAdd = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                success = _tagService.AddTag(tagAdd, out msg);
                                if (success)
                                    _sServer.Send(tagAdd);
                                    //_OpcWServiceServer.ResponseAddTag(tagAdd);
                                else
                                    _sServer.Send(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                    //_OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                break;
                            case RequestType.AddGroup:
                                var groupAdd = data as GroupRequestModel;
                                //var groupAdd = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                success = _tagService.AddGroup(groupAdd, out msg);
                                if (success)
                                    _sServer.Send(groupAdd);
                                    //_OpcWServiceServer.ResponseAddGroup(groupAdd);
                                else
                                    _sServer.Send(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                    //_OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                break;
                            case RequestType.RemoveGroup:
                                var groupRemove = data as GroupRequestModel;
                                //var groupRemove = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                success = _tagService.RemoveGroup(groupRemove, out msg);
                                if (success)
                                    _sServer.Send(groupRemove);
                                    //_OpcWServiceServer.ResponseRemoveGroup(groupRemove);
                                else
                                    _sServer.Send(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                    //_OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                break;
                            case RequestType.AddServer:
                                var serverAdd = data as ServerRequestModel;
                                //var serverAdd = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                success = _tagService.AddServer(serverAdd.ServerName, out msg);
                                if (success)
                                    _sServer.Send(serverAdd);
                                    //_OpcWServiceServer.ResponseAddServer(serverAdd);
                                else
                                    _sServer.Send(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                    //_OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                break;
                            case RequestType.RemoveServer:
                                var serverRemove = data as ServerRequestModel;
                                //var serverRemove = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                success = _tagService.RemoveServer(serverRemove.ServerName, out msg);
                                if (success)
                                    _sServer.Send(serverRemove);
                                    //_OpcWServiceServer.ResponseRemoveServer(serverRemove);
                                else
                                    _sServer.Send(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                    //_OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
                                break;
                            default:
                                break;
                        }
                    //}
                    //else
                    //{
                    //    _disConnect = true;
                    //    _OpcWServiceServer.DisConnect();
                    //}
                //}
            }
            catch (Exception e)
            {
                WriteFile(e.ToString());
                //Console.WriteLine("ERROR: {0}", e.Message);
            }
        }
        private void WriteFile(string st)
        {
            File.AppendAllText(_pathLog, "\r\n" + DateTime.Now.ToString() + ", " + st);
        }

        //private void ServerThreadRead()
        //{
        //    try
        //    {
        //        string msg;
        //        bool success;
        //        while (!_stop)
        //        {
        //            if (_disConnect) continue;
        //            var st = _OpcWServiceServer.ReadValue();
        //            if (st != null)
        //            {
        //                var txt = st.ToString();
        //                if (txt == "") continue;
        //                var objJson = JsonConvert.DeserializeObject(txt);
        //                var requestType = int.Parse(((Newtonsoft.Json.Linq.JValue)(((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)objJson).First).Value)).Value.ToString());
        //                switch (requestType)
        //                {
        //                    case (int)RequestType.WriteTag:
        //                        var tag = JsonConvert.DeserializeObject<TagRequestModel>(txt);
        //                        _tagService.SetValue(tag.Server.Name, tag.Group.Name, tag.Tag.Name, tag.Tag.Value);
        //                        break;
        //                    case (int)RequestType.GetListServer:
        //                        var servers = _tagService.GetListServer();
        //                        _OpcWServiceServer.ResponseListServer(servers);
        //                        break;
        //                    case (int)RequestType.ReadGroupTags:
        //                        var group = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
        //                        var tags = _tagService.ReadGroupTags(group, out msg);
        //                        if (tags != null)
        //                            _OpcWServiceServer.ResponseGroupTags(tags);
        //                        else
        //                            _OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
        //                        break;
        //                    case (int)RequestType.RemoveTag:
        //                        var tagRemove = JsonConvert.DeserializeObject<TagRequestModel>(txt);
        //                        success = _tagService.RemoveTag(tagRemove);
        //                        if (success)
        //                            _OpcWServiceServer.ResponseRemoveTag(tagRemove);
        //                        break;
        //                    case (int)RequestType.AddTag:
        //                        var tagAdd = JsonConvert.DeserializeObject<TagRequestModel>(txt);
        //                        success = _tagService.AddTag(tagAdd, out msg);
        //                        if (success)
        //                            _OpcWServiceServer.ResponseAddTag(tagAdd);
        //                        else
        //                            _OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
        //                        break;
        //                    case (int)RequestType.AddGroup:
        //                        var groupAdd = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
        //                        success = _tagService.AddGroup(groupAdd, out msg);
        //                        if (success)
        //                            _OpcWServiceServer.ResponseAddGroup(groupAdd);
        //                        else
        //                            _OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
        //                        break;
        //                    case (int)RequestType.RemoveGroup:
        //                        var groupRemove = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
        //                        success = _tagService.RemoveGroup(groupRemove, out msg);
        //                        if (success)
        //                            _OpcWServiceServer.ResponseRemoveGroup(groupRemove);
        //                        else
        //                            _OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
        //                        break;
        //                    case (int)RequestType.AddServer:
        //                        var serverAdd = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
        //                        success = _tagService.AddServer(serverAdd.ServerName, out msg);
        //                        if (success)
        //                            _OpcWServiceServer.ResponseAddServer(serverAdd);
        //                        else
        //                            _OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
        //                        break;
        //                    case (int)RequestType.RemoveServer:
        //                        var serverRemove = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
        //                        success = _tagService.RemoveServer(serverRemove.ServerName, out msg);
        //                        if (success)
        //                            _OpcWServiceServer.ResponseRemoveServer(serverRemove);
        //                        else
        //                            _OpcWServiceServer.ResponseError(new ErrorResponseModel() { RequestType = RequestType.Error, Message = msg });
        //                        break;
        //                    default:
        //                        break;
        //                }
        //            }
        //            else
        //            {
        //                _disConnect = true;
        //                _OpcWServiceServer.DisConnect();
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("ERROR: {0}", e.Message);
        //    }
        //}
        #endregion
    }
}
