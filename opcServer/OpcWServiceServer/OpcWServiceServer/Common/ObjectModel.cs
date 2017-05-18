using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcWServiceServer.Common
{    
    public class TagModel
    {
        //public int Id { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public string Quality { get; set; }
    }
    public class GroupModel
    {
        //public int Id { get; set; }
        public string Name { get; set; }
        //public int UpdateRate { get; set; }
        //public string Description { get; set; }
        public bool Active { get; set; }
    }
    public class ServerModel
    {
        //public int Id { get; set; }
        public string Name { get; set; }
        public bool IsConnected { get; set; }
    }
    public class ServerInfoModel
    {
        public ServerModel Server { get; set; }
        public List<GroupModel> Groups { get; set; }
    }
    public class GroupInfoModel
    {
        public ServerModel Server { get; set; }
        public GroupModel Group { get; set; }
        public List<TagModel> Tags { get; set; }
    }
    public class TagInfoModel
    {
        public ServerModel Server { get; set; }
        public GroupModel Group { get; set; }
        public TagModel Tag { get; set; }
    }

    //
    public class ServerRequestModel
    {
        public RequestType RequestType { get; set; }
        public string ServerName { get; set; }
    }
    public class ServerListRequestModel
    {
        public RequestType RequestType { get; set; }
    }

    public class ServerListResponseModel
    {
        public RequestType RequestType { get; set; }
        public List<ServerInfoModel> Servers { get; set; }
    }
    public class GroupRequestModel
    {
        public RequestType RequestType { get; set; }
        public string ServerName { get; set; }
        public string GroupName { get; set; }
    }
    public class GroupResponseModel
    {
        public RequestType RequestType { get; set; }
        public ServerModel Server { get; set; }
        public GroupModel Group { get; set; }
        public List<TagModel> Tags { get; set; }
    }
    public class TagRequestModel
    {
        public RequestType RequestType { get; set; }
        public ServerModel Server { get; set; }
        public GroupModel Group { get; set; }
        public TagModel Tag { get; set; }
    }
    public class ErrorResponseModel
    {
        public RequestType RequestType { get; set; }
        public string Message { get; set; }
    }  
}
