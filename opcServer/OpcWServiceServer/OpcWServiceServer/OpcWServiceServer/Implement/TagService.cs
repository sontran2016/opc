using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using OPCUtility.Interface;
//using OPCUtility;
using OpcWServiceServer.Common;
using OpcWServiceServer.Interface;
using System.Data;
//using OPCUtility.Objects;
using System.IO;
using System.Configuration;
using System.Threading;

namespace OpcWServiceServer.Implement
{
    public delegate void DataChanged(GroupResponseModel model);

    public class TagService:ITagService
    {
        public event DataChanged OnDataChanged;
        private static List<Opc.Da.Server> _servers;
        private string _pathConfig;
        private string _pathLog;

        public TagService()
        {
            _pathConfig = AppDomain.CurrentDomain.BaseDirectory + "OpcConfig.xml";
            _pathLog = AppDomain.CurrentDomain.BaseDirectory + "MyLog.txt";
        }

        public bool Connect()
        {
            try
            {
                //read config file
                //string filePath = ConfigurationManager.AppSettings["PathFileConfig"];
                //string filePath = AppDomain.CurrentDomain.BaseDirectory;
                //filePath = filePath.Replace(@"bin\Debug", "Config");
                //filePath += "/OpcConfig.xml";
                var ds = new dsConfig();
                if (File.Exists(_pathConfig))
                {
                    ds.ReadXml(_pathConfig);
                }
                else
                {
                    //ds.tblServer.Rows.Add(1, "192.168.1.182/RSLinx OPC Server");//demo
                    //ds.tblGroup.Rows.Add(1, "RSLinx", 1);
                    //ds.tblTag.Rows.Add(1, "[RSLinx]Input_Int", 1);
                    //ds.tblTag.Rows.Add(2, "[RSLinx]Input_From_Client", 1);
                    //ds.AcceptChanges();
                    ds.WriteXml(_pathConfig);
                }
                //con net opc server
                _servers = new List<Opc.Da.Server>();
                foreach (DataRow r in ds.tblServer.Rows)
                {                    
                    var url = new Opc.URL("opcda://"+r["Name"]);
                    var fact = new OpcCom.Factory();
                    var server = new Opc.Da.Server(fact, null);
                    server.Connect(url, new Opc.ConnectData(new System.Net.NetworkCredential()));
                    //connect Group
                    var groups = ds.tblGroup.Where(x => x.ServerId.ToString() == r["Id"].ToString()).ToList();
                    foreach(var group in groups)
                    {
                        var groupState = new Opc.Da.SubscriptionState();
                        groupState.Name = group.Name;
                        groupState.UpdateRate = 100;
                        groupState.Active = true;
                        var groupRead = (Opc.Da.Subscription)server.CreateSubscription(groupState);
                        groupRead.DataChanged += new Opc.Da.DataChangedEventHandler(group_DataChanged);
                        //connect Tag
                        var tags = ds.tblTag.Where(x => x.GroupId == group.Id).ToList();
                        foreach (var tag in tags)
                        {
                            Opc.Da.Item[] items = new Opc.Da.Item[1];
                            items[0] = new Opc.Da.Item();
                            items[0].ItemName = tag.Name;
                            //var result=groupRead.AddItems(items);
                            //if (!result[0].ResultID.Succeeded())
                            //    throw new Exception(string.Format("{0}: {1}", result[0].ResultID.Name.Name, tag.Name));
                            bool success;                            
                            do{
                                var result=groupRead.AddItems(items);
                                success=result[0].ResultID.Succeeded();
                                if(!success)
                                    Thread.Sleep(5000); //wait for RSLinx server run item
                            } while (!success);
                        }
                    }
                    _servers.Add(server);
                }
                return true;
            }
            catch (Exception ex)
            {
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }
        void GetGroupInfo(Opc.Da.ItemValue itemValue, out Opc.Da.Server Server, out Opc.Da.Subscription Group)
        {
            Server = null;
            Group = null;
            foreach (var s in _servers.ToList())
            {
                if (s.Subscriptions == null) return;
                for (int i = 0; i < s.Subscriptions.Count; i++)
                {
                    if (s.Subscriptions[i].Items.Any(x => x.ItemName == itemValue.ItemName))
                    {
                        Server = s;
                        Group = s.Subscriptions[i];
                        return;
                    }
                }
            }        
        }
        void group_DataChanged(object subscriptionHandle, object requestHandle, Opc.Da.ItemValueResult[] values)
        {
            var result = new GroupResponseModel();
            Opc.Da.Server server;
            Opc.Da.Subscription group;
            GetGroupInfo(values[0], out server, out group);
            if (server == null || group == null) return;

            result.Server = new ServerModel() { Name = string.Format("{0}/{1}", server.Url.HostName, server.Url.Path) };
            result.Group = new GroupModel() { Name = group.Name };

            result.Tags = new List<TagModel>();
            foreach (var item in values)
            {
                result.Tags.Add(new TagModel() { Name = item.ItemName, Value = item.Value, Quality=item.Quality.ToString() });
            }
            OnDataChanged(result);
        }
        public bool DisConnect()
        {
            try
            {
                if (_servers == null) return true;
                foreach (var server in _servers)
                    if (server.IsConnected) server.Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }
        public object GetValue(string serverName, string groupName, string tagName)
        {
            bool errorConnect;
            do{
                errorConnect = false;
                try
                {
                    object value = null;
                    var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == serverName);
                    foreach (Opc.Da.Subscription group in server.Subscriptions)
                    {
                        if (group.Name == groupName)
                        {
                            var item = group.Items.Single(x => x.ItemName == tagName);
                            Opc.Da.Item[] itemToRead = new Opc.Da.Item[1] { item };
                            var result = group.Read(itemToRead);
                            value = result.Single(x => x.ItemName == tagName);
                            break;
                        }
                    }
                    return value;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("IOPCSyncIO.Read"))
                    {
                        errorConnect = true;
                        var b = ReconnectServer(serverName);
                        if (!b) return null;
                        //var objValue = GetValue(serverName, groupName, tagName);
                        //return objValue;
                    }
                    else
                    {
                        Utils.WriteFile(_pathLog, ex.ToString());
                        //Console.WriteLine(ex.Message);
                        return null;
                    }
                }
                Thread.Sleep(5000); //wait for new connection
            }
            while (errorConnect);
            return null;            
        }
        private bool ReconnectServer(string serverName)
        {
            try {
                var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == serverName);                
                var gInfo = new List<GroupInfoModel>();
                while (server.Subscriptions.Count>0)
                {
                    var group = server.Subscriptions[0];
                    var info = new GroupInfoModel(){ Group = new GroupModel() { Name = group.Name }, Tags=new List<TagModel>()};
                    foreach (var t in group.Items)
                    {
                        info.Tags.Add(new TagModel() { Name=t.ItemName });
                    }
                    gInfo.Add(info);
                    server.CancelSubscription(group);
                    server.Subscriptions.Remove(group);
                }
                server.Disconnect();
                var url = new Opc.URL("opcda://" + serverName);
                var fact = new OpcCom.Factory();
                server = new Opc.Da.Server(fact, null);
                server.Connect(url, new Opc.ConnectData(new System.Net.NetworkCredential()));
                foreach (var g in gInfo)
                {
                    var groupState = new Opc.Da.SubscriptionState();
                    groupState.Name = g.Group.Name;
                    groupState.UpdateRate = 100;
                    groupState.Active = true;
                    var groupRead = (Opc.Da.Subscription)server.CreateSubscription(groupState);
                    groupRead.DataChanged += new Opc.Da.DataChangedEventHandler(group_DataChanged);

                    foreach (var tag in g.Tags)
                    {
                        Opc.Da.Item[] items = new Opc.Da.Item[1];
                        items[0] = new Opc.Da.Item();
                        items[0].ItemName = tag.Name;
                        bool success;
                        do
                        {
                            var result = groupRead.AddItems(items);
                            success = result[0].ResultID.Succeeded();
                            if (!success)
                                Thread.Sleep(5000); //wait for RSLinx server run item
                        } while (!success);
                        
                        //var result = groupRead.AddItems(items);
                        //if (!result[0].ResultID.Succeeded())
                            //throw new Exception(string.Format("{0}: {1}", result[0].ResultID.Name.Name, tag.Name));
                    }
                }
                var temp = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == serverName);
                _servers.Remove(temp);
                _servers.Add(server);
                return true;
            }
            catch (Exception ex)
            {
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }  
        }
        public bool SetValue(string serverName, string groupName, string tagName, object value)
        {
            try
            {
                var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == serverName);
                foreach (Opc.Da.Subscription group in server.Subscriptions)
                {
                    if (group.Name == groupName)
                    {
                        var item = group.Items.Single(x => x.ItemName == tagName);
                        //Opc.Da.Item[] itemToRead = new Opc.Da.Item[1] { item };
                        Opc.Da.ItemValue[] writeValues = new Opc.Da.ItemValue[1];
                        writeValues[0] = new Opc.Da.ItemValue(tagName);
                        writeValues[0].Value = value;
                        writeValues[0].ServerHandle = item.ServerHandle;
                        var result = group.Write(writeValues);                        
                        break;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }            
        }
        
        public List<ServerInfoModel> GetListServer()
        {
            try
            {
                var servers = new List<ServerInfoModel>();
                foreach (var s in _servers)
                {
                    var info = new ServerInfoModel();
                    info.Server=new ServerModel() {Name = string.Format("{0}/{1}",s.Url.HostName,s.Url.Path), IsConnected=s.IsConnected};
                    info.Groups = new List<GroupModel>();
                    foreach (Opc.Da.Subscription g in s.Subscriptions)
                    {
                        info.Groups.Add(new GroupModel()
                        {
                            Name = g.Name,
                            Active = g.Active
                        });
                    }
                    servers.Add(info);
                }
                return servers;
            }
            catch (Exception ex)
            {
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return null;
            }
        }

        public GroupResponseModel ReadGroupTags(GroupRequestModel model, out string errorMessage)
        {
            bool errorConnect;
            errorMessage = "";
            do
            {
                errorConnect = false;
                try
                {
                    Opc.Da.ItemValue[] values = null; ;
                    var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == model.ServerName);
                    var groupTags = new GroupResponseModel()
                    {
                        RequestType = RequestType.ReadGroupTags,
                        Server = new ServerModel() { Name = string.Format("{0}/{1}", server.Url.HostName, server.Url.Path), IsConnected = server.IsConnected },
                        Group = new GroupModel() { Name = model.GroupName }
                    };
                    if (server.Subscriptions == null) return groupTags;
                    foreach (Opc.Da.Subscription group in server.Subscriptions)
                    {
                        if (group.Name == model.GroupName)
                        {
                            groupTags.Group.Active = group.Active;
                            values = group.Read(group.Items);
                            break;
                        }
                    }
                    if (values == null) return groupTags;
                    groupTags.Tags = new List<TagModel>();
                    foreach (var t in values)
                    {
                        groupTags.Tags.Add(new TagModel() { Name = t.ItemName, Value = t.Value, Quality = t.Quality.ToString() });
                    }
                    return groupTags;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("IOPCSyncIO.Read") || ex.Message.Contains("E_UNKNOWN_ITEM_NAME"))
                    {
                        errorConnect = true;
                        var b = ReconnectServer(model.ServerName);
                        if (!b) return null;
                        //var res = ReadGroupTags(model);
                        //return res;
                    }
                    else
                    {
                        errorMessage = ex.Message;
                        Utils.WriteFile(_pathLog, ex.ToString());
                        //Console.WriteLine(ex.Message);
                        return null;
                    }
                }
                Thread.Sleep(5000); //wait for new connection
            }
            while (errorConnect);
            return null;
        }
        public bool AddTag(TagRequestModel tag,out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == tag.Server.Name);
                foreach (Opc.Da.Subscription group in server.Subscriptions)
                {
                    if (group.Name == tag.Group.Name)
                    {
                        Opc.Da.Item[] items = new Opc.Da.Item[1];
                        items[0] = new Opc.Da.Item();
                        items[0].ItemName = tag.Tag.Name;
                        var result = group.AddItems(items);
                        if (!result[0].ResultID.Succeeded())
                            throw new Exception(string.Format("{0}: {1}", result[0].ResultID.Name.Name,tag.Tag.Name));

                        Opc.Da.ItemValueResult res = GetValue(tag.Server.Name, tag.Group.Name, tag.Tag.Name) as Opc.Da.ItemValueResult;
                        tag.Tag.Value = res.Value;
                        tag.Tag.Quality = res.Quality.ToString();
                        var isSuccess= AddTag_Config(tag.Server.Name, tag.Group.Name, tag.Tag.Name);
                        return isSuccess;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }
        public bool RemoveTag(TagRequestModel tag, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == tag.Server.Name);
                foreach (Opc.Da.Subscription group in server.Subscriptions)
                {
                    if (group.Name == tag.Group.Name)
                    {
                        var item = group.Items.Single(x => x.ItemName == tag.Tag.Name);
                        Opc.Da.Item[] items = new Opc.Da.Item[1] { item };
                        group.RemoveItems(items);
                        var isSuccess = RemoveTag_Config(tag.Server.Name, tag.Group.Name, tag.Tag.Name);
                        return isSuccess;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }
        public bool AddGroup(GroupRequestModel group, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == group.ServerName);
                foreach (Opc.Da.Subscription g in server.Subscriptions)
                {
                    if (g.Name == group.GroupName)
                        throw new Exception(group.GroupName +" is existing");
                }
                var groupState = new Opc.Da.SubscriptionState();
                groupState.Name = group.GroupName;
                groupState.UpdateRate = 100;
                groupState.Active = true;
                var groupRead = (Opc.Da.Subscription)server.CreateSubscription(groupState);
                groupRead.DataChanged += new Opc.Da.DataChangedEventHandler(group_DataChanged);
                var isSuccess = AddGroup_Config(group.ServerName, group.GroupName);
                return isSuccess;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }
        public bool RemoveGroup(GroupRequestModel group, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var server = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == group.ServerName);
                foreach (Opc.Da.Subscription g in server.Subscriptions)
                {
                    if (g.Name == group.GroupName)
                    {
                        server.CancelSubscription(g);
                        server.Subscriptions.Remove(g);
                        var isSuccess = RemoveGroup_Config(group.ServerName, group.GroupName);
                        return isSuccess;
                    }
                }
                throw new Exception(group.GroupName + " is not existing");
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }
        public bool AddServer(string server, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var bExist = _servers.Any(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == server);
                if(bExist)
                    throw new Exception(server + " is existing");
                var url = new Opc.URL("opcda://" + server);
                var fact = new OpcCom.Factory();
                var opcServer = new Opc.Da.Server(fact, null);
                opcServer.Connect(url, new Opc.ConnectData(new System.Net.NetworkCredential()));
                _servers.Add(opcServer);

                var isSuccess = AddServer_Config(server);
                return isSuccess;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool RemoveServer(string server, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                var bExist = _servers.Any(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == server);
                if (!bExist)
                    throw new Exception(server + " is not existing");
                
                var opcServer = _servers.Single(x => string.Format("{0}/{1}", x.Url.HostName, x.Url.Path) == server);
                if(opcServer.IsConnected)
                    opcServer.Disconnect();
                _servers.Remove(opcServer);

                var isSuccess = RemoveServer_Config(server);
                return isSuccess;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                Utils.WriteFile(_pathLog, ex.ToString());
                //Console.WriteLine(ex.Message);
                return false;
            }
        }

        #region file xml
        private bool AddTag_Config(string serverName, string groupName, string tagName)
        {
            //string filePath =ConfigurationManager.AppSettings["PathFileConfig"];
            //filePath += "/OpcConfig.xml";
            var ds = new dsConfig();
            if (!File.Exists(_pathConfig)) return false;
            ds.ReadXml(_pathConfig);
            var index = serverName;
            if (!ds.tblServer.Any(x => x.Name == serverName)) return false;
            if (!ds.tblGroup.Any(x => x.Name == groupName)) return false;
            var group = ds.tblGroup.First(x => x.Name == groupName);
            int max;
            if (!ds.tblTag.Any(x => x.GroupId == group.Id))
                max = 0;
            else
            {
                var rTags = ds.tblTag.Where(x => x.GroupId == group.Id);
                max = rTags.Select(x => x.Id).Max();
            }
            var r = ds.tblTag.NewRow();
            r["Id"] = max+1;
            r["Name"] = tagName;
            r["GroupId"] = group.Id;
            ds.tblTag.Rows.Add(r);
            ds.WriteXml(_pathConfig);
            return true;
        }
        private bool RemoveTag_Config(string serverName, string groupName, string tagName)
        {
            //string filePath = ConfigurationManager.AppSettings["PathFileConfig"];
            //filePath += "/OpcConfig.xml";
            var ds = new dsConfig();
            if (!File.Exists(_pathConfig)) return false;
            ds.ReadXml(_pathConfig);
            if (!ds.tblServer.Any(x => x.Name == serverName)) return false;
            if (!ds.tblGroup.Any(x => x.Name == groupName)) return false;
            var group = ds.tblGroup.First(x => x.Name == groupName);
            if (!ds.tblTag.Any(x => x.GroupId == group.Id))
                return true;
            else
            {
                var rTags = ds.tblTag.Where(x => x.GroupId == group.Id);
                var tag= rTags.Single(x=>x.Name==tagName);
                ds.tblTag.Rows.Remove(tag);
                ds.WriteXml(_pathConfig);
            }
            return true;
        }
        private bool AddGroup_Config(string serverName, string groupName)
        {
            //string filePath = ConfigurationManager.AppSettings["PathFileConfig"];
            //filePath += "/OpcConfig.xml";
            var ds = new dsConfig();
            if (!File.Exists(_pathConfig)) return false;
            ds.ReadXml(_pathConfig);
            if (!ds.tblServer.Any(x => x.Name == serverName)) return false;
            if (ds.tblGroup.Any(x => x.Name == groupName)) return true;
            var rServer = ds.tblServer.Single(x => x.Name == serverName);
            var rGroup = ds.tblGroup.Where(x => x.ServerId == rServer.Id);
            int max;
            if (!ds.tblGroup.Any(x => x.ServerId == rServer.Id))
                max = 0;
            else
            {
                var group = ds.tblGroup.Where(x => x.ServerId == rServer.Id);
                max = group.Select(x => x.Id).Max();
            }
            var r = ds.tblGroup.NewRow();
            r["Id"] = max + 1;
            r["Name"] = groupName;
            r["ServerId"] = rServer.Id;
            ds.tblGroup.Rows.Add(r);
            ds.WriteXml(_pathConfig);
            return true;
        }
        private bool RemoveGroup_Config(string serverName, string groupName)
        {
            //string filePath = ConfigurationManager.AppSettings["PathFileConfig"];
            //filePath += "/OpcConfig.xml";
            var ds = new dsConfig();
            if (!File.Exists(_pathConfig)) return false;
            ds.ReadXml(_pathConfig);
            if (!ds.tblServer.Any(x => x.Name == serverName)) return false;
            if (!ds.tblGroup.Any(x => x.Name == groupName)) return true;

            var rServer = ds.tblServer.Single(x => x.Name == serverName);
            var rGroup = ds.tblGroup.Where(x => x.ServerId == rServer.Id);
            var r = rGroup.Single(x => x.Name == groupName);
            ds.tblGroup.Rows.Remove(r);
            ds.WriteXml(_pathConfig);
            return true;
        }
        private bool AddServer_Config(string serverName)
        {
            //string filePath = ConfigurationManager.AppSettings["PathFileConfig"];
            //filePath += "/OpcConfig.xml";
            var ds = new dsConfig();
            if (!File.Exists(_pathConfig)) return false;
            ds.ReadXml(_pathConfig);
            if (ds.tblServer.Any(x => x.Name == serverName)) return true;
            int max;
            if (ds.tblServer.Count==0)
                max = 0;
            else
            {
                max = ds.tblServer.Select(x => x.Id).Max();
            }
            ds.tblServer.Rows.Add(max + 1, serverName);
            ds.WriteXml(_pathConfig);
            return true;
        }
        private bool RemoveServer_Config(string serverName)
        {
            //string filePath = ConfigurationManager.AppSettings["PathFileConfig"];
            //filePath += "/OpcConfig.xml";
            var ds = new dsConfig();
            if (!File.Exists(_pathConfig)) return false;
            ds.ReadXml(_pathConfig);
            if (!ds.tblServer.Any(x => x.Name == serverName)) return true;
            var r = ds.tblServer.Single(x=>x.Name==serverName);
            ds.tblServer.Rows.Remove(r);
            ds.WriteXml(_pathConfig);
            return true;
        }
        #endregion
    }
}
