using OpcLib.Common;

namespace OpcServiceClient.Interface
{
    //request to OPC server
    public interface IClientOpcServer
    {
        void Connect();
        //bool DisConnect();
        bool CloseConnection();
        object ReadValue();
        //void WriteValue(object value);
        
        //more func
        bool WriteTag(string serverName, string groupName, string tagName, object value);
        
        bool RemoveTag(TagRequestModel tag);
        bool AddTag(TagRequestModel tag);
        bool AddGroup(GroupRequestModel model);
        bool RemoveGroup(GroupRequestModel model);
        bool AddServer(ServerRequestModel model);
        bool RemoveServer(ServerRequestModel model);

        bool GetListServer();
        bool ReadGroupTags(GroupRequestModel group);
    }
}
