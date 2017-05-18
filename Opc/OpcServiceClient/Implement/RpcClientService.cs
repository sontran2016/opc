using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpcServiceClient.Interface;
using RpcPackage;
using Grpc.Core;
using System.Threading;
using OpcLib.Common;
using Google.Protobuf.Collections;
using System.ComponentModel;

namespace OpcServiceClient.Implement
{
    class RpcClientService : IRpcClient
    {
        private const string IpRpcServer = "192.168.1.79:50051";
        private Channel _channel;
        private RpcService.RpcServiceClient _rpcClient;
        private bool _connected;
        private const int TimeSleep = 5000;

        public bool IsConnected
        {
            get { return _connected; }
        }

        public RpcClientService()
        {
            _connected = false;
        }
        public void Connect()
        {
            try
            {
                if (_connected) return;
                _channel = new Channel(IpRpcServer, ChannelCredentials.Insecure);
                _rpcClient = new RpcService.RpcServiceClient(_channel);
                do
                {
                    try
                    {
                        var res = _rpcClient.TestConnection(new TestRequest());
                        _connected = true;
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("StatusCode=Unavailable"))
                            Thread.Sleep(TimeSleep); //wait for new connection
                        else
                            throw new Exception(ex.Message);
                    }
                } while (!_connected);
            }
            catch (Exception ex)
            {
            }
        }
        public bool ReceiveGroupTags(GroupResponseModel info)
        {
            try
            {
                var model = new GroupResponseModelRpc_Request();
                model.Server = new ServerModelRpc { Name = info.Server.Name, IsConnected = info.Server.IsConnected };
                model.Group = new GroupModelRpc() { Name = info.Group.Name, Active = info.Group.Active };
                model.Tags = new RepeatedField<TagModelRpc>();
                var tags = new BindingList<TagModelRpc>();
                foreach (var tag in info.Tags)
                {
                    tags.Add(new TagModelRpc()
                    {
                        Name = tag.Name,
                        Value = int.Parse(tag.Value.ToString()),
                        Quality = tag.Quality
                    });
                }
                model.Tags.Add(tags);
                var res = _rpcClient.ReceiveGroupTags(model);
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("StatusCode=Unavailable"))
                    _connected = false;
            }
            return false;
        }

    }
}
