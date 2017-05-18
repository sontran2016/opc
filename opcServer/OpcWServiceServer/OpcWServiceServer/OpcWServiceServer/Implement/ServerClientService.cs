using System;
using OpcWServiceServer.Interface;
using System.IO.Pipes;
using OpcWServiceServer.Common;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.IO;
using System.Threading;
using System.Security.Principal;
using System.Security.AccessControl;

namespace OpcWServiceServer.Implement
{
    public class ServerClientService : IServerClientService
    {
        private NamedPipeServerStream _pipeServerRead, _pipeServerWrite;
        private StreamString _ssRead, _ssWrite;
        private bool _connected = false;

        public bool Connect()
        {
            try
            {
                if (!_connected)
                {
                    var ps = new PipeSecurity();
                    ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.FullControl, AccessControlType.Allow));
                    
                    //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinNetworkConfigurationOperatorsSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
                    //ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));                    
                    //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid,null), PipeAccessRights.FullControl, AccessControlType.Allow));

                    //ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
                    //ps.AddAccessRule(new PipeAccessRule("CREATOR OWNER", PipeAccessRights.FullControl, AccessControlType.Allow));
                    //ps.AddAccessRule(new PipeAccessRule("SYSTEM", PipeAccessRights.FullControl, AccessControlType.Allow));

                    //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid,null),
                        //PipeAccessRights.ReadWrite,AccessControlType.Allow));
                    //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null),
                    //  PipeAccessRights.FullControl, AccessControlType.Allow));
                    //ps.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    //  PipeAccessRights.FullControl, AccessControlType.Allow));
                    //
                    if (_pipeServerWrite != null)
                        _pipeServerWrite.Close(); //_pipeServerWrite.Dispose();
                    _pipeServerWrite = new NamedPipeServerStream("opcPipeRead", PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

                    _pipeServerWrite.WaitForConnection();
                    _ssWrite = new StreamString(_pipeServerWrite);

                    if (_pipeServerRead != null)
                        _pipeServerRead.Close(); //_pipeServerRead.Dispose();
                    _pipeServerRead = new NamedPipeServerStream("opcPipeWrite", PipeDirection.InOut, 10, PipeTransmissionMode.Message, PipeOptions.WriteThrough, 1024, 1024, ps);

                    _pipeServerRead.WaitForConnection();
                    _ssRead = new StreamString(_pipeServerRead);
                    _connected = true;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DisConnect()
        {
            _connected = false;
            if (_pipeServerRead != null && _pipeServerRead.IsConnected)
            {
                _pipeServerRead.Disconnect();
                _pipeServerRead.Close();
            }
            if (_pipeServerWrite != null && _pipeServerWrite.IsConnected)
            {
                _pipeServerWrite.Disconnect();
                _pipeServerWrite.Close();
            }
            return true;
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
                    Server = new ServerModel() {  Name=serverName },
                    Group = new GroupModel() {  Name=groupName },
                    Tag = new TagModel() { Name=tagName, Value = value }
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
                var data=new ServerListResponseModel();
                data.RequestType= RequestType.GetListServer;
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

        //public bool UpdatedsConfigFile_RemoveTag(TagRequestModel tag, IList<string> fileNames)
        //{
        //    try
        //    {
        //        //foreach (var fName in fileNames)
        //        //{
        //        //    XmlDocument doc = new XmlDocument();
        //        //    doc.Load(fName);

        //        //    var node = GetNodeTag(doc, "name", tag.Server.Name, "name", tag.Group.Name, "name", tag.Tag.Name);
        //        //    if (node != null)
        //        //    {
        //        //        node.ParentNode.RemoveChild(node);
        //        //        doc.Save(fName);
        //        //        return true;
        //        //    }
        //        //}
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
        //public bool UpdatedsConfigFile_AddTag(TagRequestModel tag, IList<string> fileNames)
        //{
        //    return false;
        //    //try
        //    //{
        //    //    foreach (var fName in fileNames)
        //    //    {
        //    //        XmlDocument doc = new XmlDocument();
        //    //        doc.Load(fName);

        //    //        var node = GetNodeGroup(doc, "name", tag.Server.Name, "name", tag.Group.Name);
        //    //        if (node != null)
        //    //        {
        //    //            var nodeTag = doc.CreateNode(XmlNodeType.Element, "opc-tag", null);
        //    //            var attId = doc.CreateAttribute("id");
        //    //            attId.Value = tag.Tag.Id.ToString();
        //    //            var attName = doc.CreateAttribute("name");
        //    //            attName.Value = tag.Tag.Name;
        //    //            var attAddress = doc.CreateAttribute("address");
        //    //            attAddress.Value = tag.Tag.Address;

        //    //            nodeTag.Attributes.Append(attId);
        //    //            nodeTag.Attributes.Append(attName);
        //    //            nodeTag.Attributes.Append(attAddress);
        //    //            node.AppendChild(nodeTag);
        //    //            doc.Save(fName);
        //    //            return true;
        //    //        }
        //    //    }
        //    //    return false;
        //    //}
        //    //catch (Exception ex)
        //    //{
        //    //    return false;
        //    //}
        //}
        //#region my private

        //private XmlNode GetNodeServer(XmlDocument doc, string attName, object value)
        //{
        //    XmlNodeList nodeServers = doc.GetElementsByTagName("opc-server");
        //    var node= GetNode(nodeServers, attName, value);
        //    return node;
        //}
        //private XmlNode GetNodeGroup(XmlDocument doc, string attServerName, object serverValue, string attGroupName, object groupValue)
        //{
        //    var nodeServer = GetNodeServer(doc, attServerName, serverValue);
        //    if (nodeServer == null) return null;
        //    var nodeGroups = nodeServer.ChildNodes;
        //    var nodeGroup = GetNode(nodeGroups, attGroupName, groupValue);
        //    return nodeGroup;
        //}
        //private XmlNode GetNodeTag(XmlDocument doc, string attServerName, object serverValue, string attGroupName, object groupValue, string attTagName, object tagValue)
        //{
        //    var nodeServer = GetNodeServer(doc, attServerName, serverValue);
        //    if (nodeServer == null) return null;
        //    var nodeGroups = nodeServer.ChildNodes;
        //    var nodeGroup = GetNode(nodeGroups, attGroupName, groupValue);
        //    if (nodeGroup == null) return null;
        //    var nodeTags = nodeGroup.ChildNodes;
        //    var nodeTag = GetNode(nodeTags, attTagName, tagValue);
        //    return nodeTag;
        //}
        //private XmlNode GetNode(XmlNodeList nodes,string attName, object value)
        //{
        //    if (nodes == null) return null;
        //    for (int i = 0; i < nodes.Count; i++)
        //    {
        //        var node = nodes[i];
        //        if(node.NodeType!= XmlNodeType.Element) continue;
        //        for (int j = 0; j < node.Attributes.Count;j++)
        //        {
        //            if (node.Attributes[j].Name == attName && node.Attributes[j].Value == value.ToString())
        //            {
        //                return node;
        //            }
        //        }
        //    }
        //    return null;
        //}

        //#endregion
    }
}
