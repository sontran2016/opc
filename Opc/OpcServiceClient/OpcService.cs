using Newtonsoft.Json;
using OpcLib.Common;
using OpcServiceClient.Implement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace OpcServiceClient
{
    public partial class OpcService : ServiceBase
    {
        private ClientOpcServerService _opcService;
        private ClientWinsService _clientService;
        private RpcClientService _rpcClient;
        private bool _isStopWService;
        private const int TimeSleep=5000;
        public OpcService()
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
                _isStopWService = false;
                _opcService = new ClientOpcServerService();
                _clientService=new ClientWinsService();
                _rpcClient=new RpcClientService();

                var opcTask=new Thread(new ThreadStart(DoWorkOpc));
                opcTask.Start();
                var clientTask = new Thread(new ThreadStart(DoWorkClient));
                clientTask.Start();
                Connect_RpcServer();
            }
            catch (Exception ex)
            {
            }
        }

        protected override void OnStop()
        {
            try
            {
                _isStopWService = true;
                _opcService.CloseConnection();
                _clientService.CloseConnection();
            }
            catch (Exception ex)
            {
            }
        }
        private void DoWorkOpc()//get result from opc server
        {
            try
            {
                while (!_isStopWService)
                {
                    _opcService.Connect();
                    while (_opcService.IsConnected)
                    {
                        var st = _opcService.ReadValue();   //json string
                        if (st != null)
                        {
                            var txt = st.ToString();
                            if (txt == "") continue;
                            var objJson = JsonConvert.DeserializeObject(txt);
                            var requestType = int.Parse(((Newtonsoft.Json.Linq.JValue)(((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)objJson).First).Value)).Value.ToString());
                            switch (requestType)
                            {
                                case (int)RequestType.ReadTag:
                                    var tag = JsonConvert.DeserializeObject<GroupResponseModel>(txt);
                                    WorkOpc_Result(tag);
                                    break;
                                case (int)RequestType.GetListServer:
                                    var retServers = JsonConvert.DeserializeObject<ServerListResponseModel>(txt);
                                    WorkOpc_Result(retServers.Servers);
                                    break;
                                case (int)RequestType.ReadGroupTags:
                                    var tags = JsonConvert.DeserializeObject<GroupResponseModel>(txt);
                                    WorkOpc_Result(tags);
                                    break;
                                case (int)RequestType.RemoveTag:
                                    var tagRemove = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                    WorkOpc_Result(tagRemove);
                                    break;
                                case (int)RequestType.AddTag:
                                    var tagAdd = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                    WorkOpc_Result(tagAdd);
                                    break;
                                case (int)RequestType.AddGroup:
                                    var groupAdd = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                    WorkOpc_Result(groupAdd);
                                    break;
                                case (int)RequestType.RemoveGroup:
                                    var groupRemove = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                    WorkOpc_Result(groupRemove);
                                    break;
                                case (int)RequestType.AddServer:
                                    var serverAdd = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                    WorkOpc_Result(serverAdd);
                                    break;
                                case (int)RequestType.RemoveServer:
                                    var serverRemove = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                    WorkOpc_Result(serverRemove);
                                    break;
                                case (int)RequestType.Error:
                                    var error = JsonConvert.DeserializeObject<ErrorResponseModel>(txt);
                                    WorkOpc_Result(error);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            _opcService.CloseConnection();
                        }
                    }
                    Thread.Sleep(TimeSleep);
                }                
            }
            catch (Exception ex)
            {
            }
        }
        private void DoWorkClient() //get request from from client
        {
            try
            {
                while (!_isStopWService)
                {
                    _clientService.Connect();
                    while (_clientService.IsConnected)
                    {
                        var st = _clientService.ReadValue();   //json string
                        if (st != null)
                        {
                            var txt = st.ToString();
                            if (txt == "") continue;
                            var objJson = JsonConvert.DeserializeObject(txt);
                            var requestType = int.Parse(((Newtonsoft.Json.Linq.JValue)(((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)objJson).First).Value)).Value.ToString());
                            switch (requestType)
                            {
                                case (int)RequestType.ReadTag:
                                    var tag = JsonConvert.DeserializeObject<GroupResponseModel>(txt);
                                    WorkClient_Request(tag);
                                    break;
                                case (int)RequestType.GetListServer:
                                    _opcService.GetListServer();
                                    //var retServers = JsonConvert.DeserializeObject<ServerListResponseModel>(txt);
                                    //WorkClient_Request(retServers.Servers);
                                    break;
                                case (int)RequestType.ReadGroupTags:
                                    var tags = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                    WorkClient_Request(tags);
                                    break;
                                case (int)RequestType.RemoveTag:
                                    var tagRemove = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                    WorkClient_Request(tagRemove);
                                    break;
                                case (int)RequestType.AddTag:
                                    var tagAdd = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                    WorkClient_Request(tagAdd);
                                    break;
                                case (int)RequestType.AddGroup:
                                    var groupAdd = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                    WorkClient_Request(groupAdd);
                                    break;
                                case (int)RequestType.RemoveGroup:
                                    var groupRemove = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                    WorkClient_Request(groupRemove);
                                    break;
                                case (int)RequestType.AddServer:
                                    var serverAdd = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                    WorkClient_Request(serverAdd);
                                    break;
                                case (int)RequestType.RemoveServer:
                                    var serverRemove = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                    WorkClient_Request(serverRemove);
                                    break;
                                case (int)RequestType.Error:
                                    var error = JsonConvert.DeserializeObject<ErrorResponseModel>(txt);
                                    WorkClient_Request(error);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            _clientService.CloseConnection();
                        }
                    }
                    Thread.Sleep(TimeSleep);
                }
            }
            catch (Exception ex)
            {
            }
        }
        private void Connect_RpcServer()
        {
            if (!_rpcClient.IsConnected)
            {
                var rpcTask = new Thread(()=>
                {
                    _rpcClient.Connect();
                });
                rpcTask.Start();
            }
        }
        private void WorkOpc_Result(object result)//reply to client
        {
            if(result==null) return;
            if (result.GetType() == typeof(TagRequestModel))
            {
                var tag = result as TagRequestModel;
                if (_clientService.IsConnected)
                {
                    if (tag.RequestType == RequestType.RemoveTag)
                        _clientService.ResponseRemoveTag(tag);
                    else if (tag.RequestType == RequestType.AddTag)
                        _clientService.ResponseAddTag(tag);
                }
            }
            else if (result.GetType() == typeof(List<ServerInfoModel>))
            {
                if (_clientService.IsConnected)
                    _clientService.ResponseListServer(result as List<ServerInfoModel>);
            }
            else if (result.GetType() == typeof(GroupResponseModel))
            {
                var info = result as GroupResponseModel;
                if (_clientService.IsConnected)
                {
                    if (info.RequestType == RequestType.ReadTag)
                        _clientService.ResponseTag(info);
                    else if (info.RequestType == RequestType.ReadGroupTags)
                        _clientService.ResponseGroupTags(info);
                }
                if (_rpcClient.IsConnected)
                {
                    if (info.RequestType == RequestType.ReadTag || info.RequestType == RequestType.ReadGroupTags)
                    {
                        var success = _rpcClient.ReceiveGroupTags(info);
                        if (!success) Connect_RpcServer();
                    }
                }
            }
            else if (result.GetType() == typeof(ErrorResponseModel))
            {
                if (_clientService.IsConnected)
                    _clientService.ResponseError(result as ErrorResponseModel);
            }
            else if (result.GetType() == typeof(GroupRequestModel))
            {
                var info = result as GroupRequestModel;
                if (_clientService.IsConnected)
                {
                    if (info.RequestType == RequestType.AddGroup)
                        _clientService.ResponseAddGroup(info);
                    else if (info.RequestType == RequestType.RemoveGroup)
                        _clientService.ResponseRemoveGroup(info);
                }
            }
            else if (result.GetType() == typeof(ServerRequestModel))
            {
                var info = result as ServerRequestModel;
                if (_clientService.IsConnected)
                {
                    if (info.RequestType == RequestType.AddServer)
                        _clientService.ResponseAddServer(info);
                    else if (info.RequestType == RequestType.RemoveServer)
                        _clientService.ResponseRemoveServer(info);
                }
            }
        }
        private void WorkClient_Request(object model)//request to opc server
        {
            if (model == null || _opcService.IsConnected==false) return;
            if (model.GetType() == typeof(TagRequestModel))
            {
                var tag = model as TagRequestModel;
                if (tag.RequestType == RequestType.RemoveTag)
                    _opcService.RemoveTag(tag);
                else if (tag.RequestType == RequestType.AddTag)
                    _opcService.AddTag(tag);
            }
            else if (model.GetType() == typeof(GroupRequestModel))
            {
                var info = model as GroupRequestModel;
                if (info.RequestType == RequestType.ReadTag)
                {
                }
                else if (info.RequestType == RequestType.ReadGroupTags)
                {
                    _opcService.ReadGroupTags(info);
                }
            }
            else if (model.GetType() == typeof(GroupRequestModel))
            {
                var info = model as GroupRequestModel;
                if (info.RequestType == RequestType.AddGroup)
                    _opcService.AddGroup(info);
                else if (info.RequestType == RequestType.RemoveGroup)
                    _opcService.RemoveGroup(info);
            }
            else if (model.GetType() == typeof(ServerRequestModel))
            {
                var info = model as ServerRequestModel;
                if (info.RequestType == RequestType.AddServer)
                    _opcService.AddServer(info);
                else if (info.RequestType == RequestType.RemoveServer)
                    _opcService.RemoveServer(info);
            }
        }    
    }
}
