using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpcWServiceServer.Interface;
using OpcWServiceServer.Common;
using System.IO.Pipes;
using System.Security.AccessControl;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;

namespace OpcWServiceServer.Implement
{
    //Socket between windows service: Opc Server and Opc Client 
    public class StateObject
    {
        // Client  socket.
        public Socket WorkSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] Buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder Sb = new StringBuilder();
    }

    public delegate void ReceiveDataDelegate(RequestType type, object data);
    class AsyncSocketServer : ISocketServer
    {
        public ReceiveDataDelegate OnReceiveData;
        // Thread signal.
        public ManualResetEvent AllDone = new ManualResetEvent(false);
        private Socket _listener = null;
        private Socket _client;
        //public bool _isConnected = false;
        private bool _isStop = false;
        string _pathLog = null;

        //public AsyncSocketServer() { }

        public void StartListening()
        {
            _pathLog = AppDomain.CurrentDomain.BaseDirectory + "MyLog.txt";
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList.SingleOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);          
            IPAddress ipAddress = IPAddress.Parse("192.168.1.182");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);//11000

            // Create a TCP/IP socket.
            //Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                _listener.Bind(localEndPoint);
                _listener.Listen(100);

                while (!_isStop)
                {
                    // Set the event to nonsignaled state.
                    AllDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    //Console.WriteLine("Waiting for a connection...");
                    WriteFile("Waiting for a connection...");
                    _listener.BeginAccept(new AsyncCallback(AcceptCallback), _listener);

                    // Wait until a connection is made before continuing.
                    AllDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
                WriteFile(e.ToString());
            }
            //Console.WriteLine("\nPress ENTER to continue...");
            //Console.Read();
        }
        
        public void Stop()
        {
            try
            {
                _isStop = true;
                //Listener.Shutdown(SocketShutdown.Both);
                _listener.Close();
            }
            catch (Exception ex)
            {
                WriteFile(ex.ToString());
            }
        }
        
        private void WriteFile(string st)
        {
            File.AppendAllText(_pathLog, "\r\n" + DateTime.Now.ToString() + ", " + st);
        }
        
        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            AllDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            _client = handler;

            // Create the state object.
            StateObject state = new StateObject();
            state.WorkSocket = handler;
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            //_isConnected = true;
        }

        private void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.Sb.Append(Encoding.Unicode.GetString(state.Buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read more data.
                content = state.Sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the client. Display it on the console.
                    //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                    WriteFile(string.Format("Read {0} bytes from socket. \n Data : {1}", content.Length, content));
                    // Echo the data back to the client.
                    //Send(handler, content);

                    //raise event data
                    DoTaskFor_DataReceive(content);
                    //continue receive other data
                    state.Sb.Clear();
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }

        public void Send(object objData)//Socket handler, String data
        {
            if(_client==null || !_client.Connected) return;
            Socket handler = _client;
            var jsonText = JsonConvert.SerializeObject(objData)+"<EOF>";
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.Unicode.GetBytes(jsonText);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                WriteFile(string.Format("Sent {0} bytes to client.", bytesSent));

                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
                WriteFile(e.ToString());
            }
        }

        private void DoTaskFor_DataReceive(string st)
        {
            try
            {
                string txtJson = st.Replace("<EOF>", "");
                var objJson = JsonConvert.DeserializeObject(txtJson);
                var requestType = int.Parse(((Newtonsoft.Json.Linq.JValue)(((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)objJson).First).Value)).Value.ToString());
                switch (requestType)
                {
                    case (int)RequestType.WriteTag:
                        var data = JsonConvert.DeserializeObject<TagRequestModel>(txtJson);
                        OnReceiveData(RequestType.WriteTag, data);
                        break;
                    case (int)RequestType.GetListServer:
                        OnReceiveData(RequestType.GetListServer, null);
                        break;
                    case (int)RequestType.ReadGroupTags:
                        var dataReadGroupTags = JsonConvert.DeserializeObject<GroupRequestModel>(txtJson);
                        OnReceiveData(RequestType.ReadGroupTags, dataReadGroupTags);
                        break;
                    case (int)RequestType.RemoveTag:
                        var dataRemoveTag = JsonConvert.DeserializeObject<TagRequestModel>(txtJson);
                        OnReceiveData(RequestType.RemoveTag, dataRemoveTag);
                        break;
                    case (int)RequestType.AddTag:
                        var dataAddTag = JsonConvert.DeserializeObject<TagRequestModel>(txtJson);
                        OnReceiveData(RequestType.AddTag, dataAddTag);
                        break;
                    case (int)RequestType.AddGroup:
                        var dataAddGroup = JsonConvert.DeserializeObject<GroupRequestModel>(txtJson);
                        OnReceiveData(RequestType.AddGroup, dataAddGroup);
                        break;
                    case (int)RequestType.RemoveGroup:
                        var dataRemoveGroup = JsonConvert.DeserializeObject<GroupRequestModel>(txtJson);
                        OnReceiveData(RequestType.RemoveGroup, dataRemoveGroup);
                        break;
                    case (int)RequestType.AddServer:
                        var dataAddServer = JsonConvert.DeserializeObject<ServerRequestModel>(txtJson);
                        OnReceiveData(RequestType.AddServer, dataAddServer);
                        break;
                    case (int)RequestType.RemoveServer:
                        var dataRemoveServer = JsonConvert.DeserializeObject<ServerRequestModel>(txtJson);
                        OnReceiveData(RequestType.RemoveServer, dataRemoveServer);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                WriteFile(e.ToString());
            }
        }
    //
        //public bool ResponseTag(GroupResponseModel model)
        //{
        //    try
        //    {
        //        model.RequestType = RequestType.ReadTag;
        //        var jsonText = JsonConvert.SerializeObject(model);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseTag(string serverName, string groupName, string tagName, object value)
        //{
        //    try
        //    {
        //        var tag = new TagRequestModel()
        //        {
        //            RequestType = RequestType.ReadTag,
        //            Server = new ServerModel() { Name = serverName },
        //            Group = new GroupModel() { Name = groupName },
        //            Tag = new TagModel() { Name = tagName, Value = value }
        //        };
        //        var jsonText = JsonConvert.SerializeObject(tag);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseListServer(List<ServerInfoModel> servers)
        //{
        //    try
        //    {
        //        var data = new ServerListResponseModel();
        //        data.RequestType = RequestType.GetListServer;
        //        data.Servers = servers;
        //        var jsonText = JsonConvert.SerializeObject(data);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseGroupTags(GroupResponseModel tags)
        //{
        //    try
        //    {
        //        tags.RequestType = RequestType.ReadGroupTags;
        //        var jsonText = JsonConvert.SerializeObject(tags);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseRemoveTag(TagRequestModel tag)
        //{
        //    try
        //    {
        //        tag.RequestType = RequestType.RemoveTag;
        //        var jsonText = JsonConvert.SerializeObject(tag);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseAddTag(TagRequestModel tag)
        //{
        //    try
        //    {
        //        tag.RequestType = RequestType.AddTag;
        //        var jsonText = JsonConvert.SerializeObject(tag);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseAddGroup(GroupRequestModel model)
        //{
        //    try
        //    {
        //        model.RequestType = RequestType.AddGroup;
        //        var jsonText = JsonConvert.SerializeObject(model);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseRemoveGroup(GroupRequestModel model)
        //{
        //    try
        //    {
        //        model.RequestType = RequestType.RemoveGroup;
        //        var jsonText = JsonConvert.SerializeObject(model);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseAddServer(ServerRequestModel model)
        //{
        //    try
        //    {
        //        model.RequestType = RequestType.AddServer;
        //        var jsonText = JsonConvert.SerializeObject(model);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseRemoveServer(ServerRequestModel model)
        //{
        //    try
        //    {
        //        model.RequestType = RequestType.RemoveServer;
        //        var jsonText = JsonConvert.SerializeObject(model);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public bool ResponseError(ErrorResponseModel error)
        //{
        //    try
        //    {
        //        error.RequestType = RequestType.Error;
        //        var jsonText = JsonConvert.SerializeObject(error);
        //        WriteValue(jsonText);
        //        return true;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}
    }
    //
    //public class SocketServerService
    //{
    //    private NamedPipeServerStream _pipeServerRead, _pipeServerWrite;
    //    private StreamString _ssRead, _ssWrite;
    //    private bool _connected = false;

    //    public bool Connect()
    //    {
    //        try
    //        {
    //            if (!_connected)
    //            {
    //                var ps = new PipeSecurity();
    //                ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.FullControl, AccessControlType.Allow));

    //                //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinNetworkConfigurationOperatorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
    //                //ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));                    
    //                //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid,null), PipeAccessRights.FullControl, AccessControlType.Allow));

    //                //ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
    //                //ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
    //                //ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));

    //                //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid,null),
    //                //PipeAccessRights.ReadWrite,AccessControlType.Allow));
    //                //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null),
    //                //  PipeAccessRights.FullControl, AccessControlType.Allow));
    //                //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
    //                //  PipeAccessRights.FullControl, AccessControlType.Allow));
    //                //
    //                if (_pipeServerWrite != null)
    //                    _pipeServerWrite.Close(); //_pipeServerWrite.Dispose();
    //                _pipeServerWrite = new NamedPipeServerStream("opcPipeRead", PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

    //                _pipeServerWrite.WaitForConnection();
    //                _ssWrite = new StreamString(_pipeServerWrite);

    //                if (_pipeServerRead != null)
    //                    _pipeServerRead.Close(); //_pipeServerRead.Dispose();
    //                _pipeServerRead = new NamedPipeServerStream("opcPipeWrite", PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

    //                _pipeServerRead.WaitForConnection();
    //                _ssRead = new StreamString(_pipeServerRead);
    //                _connected = true;
    //            }
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool DisConnect()
    //    {
    //        _connected = false;
    //        if (_pipeServerRead != null && _pipeServerRead.IsConnected)
    //        {
    //            _pipeServerRead.Disconnect();
    //            _pipeServerRead.Close();
    //        }
    //        if (_pipeServerWrite != null && _pipeServerWrite.IsConnected)
    //        {
    //            _pipeServerWrite.Disconnect();
    //            _pipeServerWrite.Close();
    //        }
    //        return true;
    //    }

    //    public object ReadValue()
    //    {
    //        if (!_connected) return null;
    //        var value = _ssRead.ReadString();
    //        return value;
    //    }

    //    private void WriteValue(object value)
    //    {
    //        if (!_connected) return;
    //        _ssWrite.WriteString(value.ToString());
    //    }
        
    //    //
    //    public bool ResponseTag(GroupResponseModel model)
    //    {
    //        try
    //        {
    //            model.RequestType = RequestType.ReadTag;
    //            var jsonText = JsonConvert.SerializeObject(model);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }
        
    //    public bool ResponseTag(string serverName, string groupName, string tagName, object value)
    //    {
    //        try
    //        {
    //            var tag = new TagRequestModel()
    //            {
    //                RequestType = RequestType.ReadTag,
    //                Server = new ServerModel() { Name = serverName },
    //                Group = new GroupModel() { Name = groupName },
    //                Tag = new TagModel() { Name = tagName, Value = value }
    //            };
    //            var jsonText = JsonConvert.SerializeObject(tag);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool ResponseListServer(List<ServerInfoModel> servers)
    //    {
    //        try
    //        {
    //            var data = new ServerListResponseModel();
    //            data.RequestType = RequestType.GetListServer;
    //            data.Servers = servers;
    //            var jsonText = JsonConvert.SerializeObject(data);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool ResponseGroupTags(GroupResponseModel tags)
    //    {
    //        try
    //        {
    //            tags.RequestType = RequestType.ReadGroupTags;
    //            var jsonText = JsonConvert.SerializeObject(tags);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool ResponseRemoveTag(TagRequestModel tag)
    //    {
    //        try
    //        {
    //            tag.RequestType = RequestType.RemoveTag;
    //            var jsonText = JsonConvert.SerializeObject(tag);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }
        
    //    public bool ResponseAddTag(TagRequestModel tag)
    //    {
    //        try
    //        {
    //            tag.RequestType = RequestType.AddTag;
    //            var jsonText = JsonConvert.SerializeObject(tag);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            return false;
    //        }
    //    }
        
    //    public bool ResponseAddGroup(GroupRequestModel model)
    //    {
    //        try
    //        {
    //            model.RequestType = RequestType.AddGroup;
    //            var jsonText = JsonConvert.SerializeObject(model);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool ResponseRemoveGroup(GroupRequestModel model)
    //    {
    //        try
    //        {
    //            model.RequestType = RequestType.RemoveGroup;
    //            var jsonText = JsonConvert.SerializeObject(model);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool ResponseAddServer(ServerRequestModel model)
    //    {
    //        try
    //        {
    //            model.RequestType = RequestType.AddServer;
    //            var jsonText = JsonConvert.SerializeObject(model);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool ResponseRemoveServer(ServerRequestModel model)
    //    {
    //        try
    //        {
    //            model.RequestType = RequestType.RemoveServer;
    //            var jsonText = JsonConvert.SerializeObject(model);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            return false;
    //        }
    //    }

    //    public bool ResponseError(ErrorResponseModel error)
    //    {
    //        try
    //        {
    //            error.RequestType = RequestType.Error;
    //            var jsonText = JsonConvert.SerializeObject(error);
    //            WriteValue(jsonText);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            return false;
    //        }
    //    }
    //}
}
