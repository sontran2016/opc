using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpcWServiceServer.Common;

namespace OpcWServiceServer.Interface
{
    public interface IServerClientService
    {
        bool Connect();
        bool DisConnect();
        object ReadValue();
        //void WriteValue(object value);
        //bool ResponseTag(int serverId, int groupId, int tagId, object value);
        bool ResponseTag(string serverName, string groupName, string tagName, object value);
        bool ResponseTag(GroupResponseModel model);
        
        bool ResponseListServer(List<ServerInfoModel> servers);
        bool ResponseGroupTags(GroupResponseModel model);
        bool ResponseError(ErrorResponseModel error);

        bool ResponseAddTag(TagRequestModel model);
        bool ResponseRemoveTag(TagRequestModel model);
        bool ResponseAddGroup(GroupRequestModel model);
        bool ResponseRemoveGroup(GroupRequestModel model);

        bool ResponseAddServer(ServerRequestModel model);
        bool ResponseRemoveServer(ServerRequestModel model);

        //bool UpdatedsConfigFile_AddTag(TagRequestModel tag, IList<string> fileNames);
        //bool UpdatedsConfigFile_RemoveTag(TagRequestModel tag,IList<string> fileNames);
        //bool UpdatedsConfigFile_AddGroup(GroupTagsRequestModel tag);
        //bool UpdatedsConfigFile_RemoveGroup(GroupTagsRequestModel tag);

    }
}
