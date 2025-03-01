
using System.Collections.Generic;
using System.Threading.Tasks;
using Herta.Utils.HertaWebsocketUtil;

namespace Herta.Interfaces.IHertaWsGroup;

public interface IHertaWsGroup
{
    void AddToGroup(int groupId, HertaWebsocket ws);
    void RemoveFromGroup(int groupId, HertaWebsocket ws);
    int GetGroupConnectionCount(int groupId);
    List<HertaWebsocket> GetGroupConnections(int groupId);
    Task BroadcastTextAsync(int groupId, string message);
    Task BroadcastJsonAsync<T>(int groupId, T data);
    void Cleanup();
}