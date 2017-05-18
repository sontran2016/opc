using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpcWServiceServer.Common;

namespace OpcWServiceServer.Interface
{
    interface ISocketServer
    {
        void StartListening();
        void Stop();
        void Send(object objData);

        //bool ResponseTag(string serverName, string groupName, string tagName, object value);
        //bool ResponseTag(GroupResponseModel model);

        //bool ResponseListServer(List<ServerInfoModel> servers);
        //bool ResponseGroupTags(GroupResponseModel model);
        //bool ResponseError(ErrorResponseModel error);

        //bool ResponseAddTag(TagRequestModel model);
        //bool ResponseRemoveTag(TagRequestModel model);
        //bool ResponseAddGroup(GroupRequestModel model);
        //bool ResponseRemoveGroup(GroupRequestModel model);

        //bool ResponseAddServer(ServerRequestModel model);
        //bool ResponseRemoveServer(ServerRequestModel model);
    }
}
