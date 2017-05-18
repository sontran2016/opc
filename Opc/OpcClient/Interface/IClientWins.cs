using OpcLib.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcClient.Interface
{
    //request to windows service
    public interface IClientWins
    {
        void Connect();
        //bool DisConnect();
        bool CloseConnection();
        object ReadValue();
        //void WriteValue(object value);
        
        //more func
        bool WriteTag(string serverName, string groupName, string tagName, object value);
        
        bool RemoveTag(ListTagModel tag);
        bool AddTag(TagInfoModel tag);
        bool AddGroup(GroupRequestModel model);
        bool RemoveGroup(GroupRequestModel model);
        bool AddServer(ServerRequestModel model);
        bool RemoveServer(ServerRequestModel model);

        bool GetListServer();
        bool ReadGroupTags(GroupRequestModel group);
    }
}
