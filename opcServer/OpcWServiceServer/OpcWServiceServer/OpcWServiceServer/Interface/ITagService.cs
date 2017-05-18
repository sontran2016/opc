using OpcWServiceServer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpcWServiceServer.Interface
{
    public interface ITagService
    {
        bool Connect();
        bool DisConnect();
        object GetValue(string serverName, string groupName, string tagName);
        bool SetValue(string serverName, string groupName, string tagName, object value);
        List<ServerInfoModel> GetListServer();
        GroupResponseModel ReadGroupTags(GroupRequestModel group,out string errorMessage);

        bool RemoveTag(TagRequestModel tag, out string errorMessage);
        bool AddTag(TagRequestModel tag,out string errorMessage);
        bool AddGroup(GroupRequestModel group, out string errorMessage);
        bool RemoveGroup(GroupRequestModel group, out string errorMessage);
        bool AddServer(string server, out string errorMessage);
        bool RemoveServer(string server, out string errorMessage);
    }
}
