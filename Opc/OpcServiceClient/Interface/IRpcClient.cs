using OpcLib.Common;
using RpcPackage;

namespace OpcServiceClient.Interface
{
    interface IRpcClient
    {
        bool ReceiveGroupTags(GroupResponseModel info);
    }
}
