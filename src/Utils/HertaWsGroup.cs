using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Herta.Utils.HertaWebsocketUtil;
using Herta.Models.Enums.WebsocketEnum;
using Herta.Utils.Logger;
using NLog;

namespace Herta.Utils.WebsocketGroup
{
    public class HertaWsGroup
    {
        private readonly ConcurrentDictionary<int, List<HertaWebsocket>> _groups = new ConcurrentDictionary<int, List<HertaWebsocket>>();
        private readonly Timer _heartbeatTimer;
        private static readonly NLog.ILogger _logger = LoggerManager.GetLogger(typeof(HertaWsGroup));

        public HertaWsGroup()
        {
            // 启动心跳机制，每30秒检查一次连接状态
            _heartbeatTimer = new Timer(Heartbeat, null, 0, 30000);
        }

        /// <summary>
        /// 将 WebSocket 添加到指定群组
        /// </summary>
        /// <param name="groupId">群组 ID</param>
        /// <param name="ws">WebSocket 实例</param>
        public void AddToGroup(int groupId, HertaWebsocket ws)
        {
            if (!_groups.ContainsKey(groupId))
            {
                _groups[groupId] = new List<HertaWebsocket>();
            }

            _groups[groupId].Add(ws);
            _logger.Trace($"WebSocket added to group {groupId}. Total connections: {_groups[groupId].Count}");
        }

        /// <summary>
        /// 从群组中移除 WebSocket
        /// </summary>
        /// <param name="groupId">群组 ID</param>
        /// <param name="ws">WebSocket 实例</param>
        public void RemoveFromGroup(int groupId, HertaWebsocket ws)
        {
            if (_groups.ContainsKey(groupId))
            {
                _groups[groupId].Remove(ws);
                _logger.Trace($"WebSocket removed from group {groupId}. Total connections: {_groups[groupId].Count}");
            }
        }

        /// <summary>
        /// 获取指定群组的连接数量
        /// </summary>
        /// <param name="groupId">群组 ID</param>
        /// <returns>连接数量</returns>
        public int GetGroupConnectionCount(int groupId)
        {
            return _groups.ContainsKey(groupId) ? _groups[groupId].Count : 0;
        }

        /// <summary>
        /// 获取群组中的所有 WebSocket 连接
        /// </summary>
        /// <param name="groupId">群组 ID</param>
        /// <returns>WebSocket 列表</returns>
        public List<HertaWebsocket> GetGroupConnections(int groupId)
        {
            return _groups.ContainsKey(groupId) ? _groups[groupId] : new List<HertaWebsocket>();
        }

        /// <summary>
        /// 向指定群组广播文本消息
        /// </summary>
        /// <param name="groupId">群组 ID</param>
        /// <param name="message">消息内容</param>
        public async Task BroadcastTextAsync(int groupId, string message)
        {
            if (_groups.ContainsKey(groupId))
            {
                foreach (var ws in _groups[groupId])
                {
                    if (ws.IsConnected())
                    {
                        await ws.SendTextAsync(message);
                    }
                }
            }
        }

        /// <summary>
        /// 向指定群组广播 JSON 消息
        /// </summary>
        /// <param name="groupId">群组 ID</param>
        /// <param name="data">消息数据</param>
        public async Task BroadcastJsonAsync<T>(int groupId, T data)
        {
            if (_groups.ContainsKey(groupId))
            {
                foreach (var ws in _groups[groupId])
                {
                    if (ws.IsConnected())
                    {
                        await ws.SendJsonAsync(data);
                    }
                }
            }
        }

        /// <summary>
        /// 心跳机制，定期检查连接状态
        /// </summary>
        private void Heartbeat(object? state)
        {
            foreach (var group in _groups.Values)
            {
                foreach (var ws in group.ToList())
                {
                    if (!ws.IsConnected())
                    {
                        group.Remove(ws);
                    }
                    _logger.Warn($"Removed inactive WebSocket connection. Total connections: {group.Count}");
                }
            }
        }

        /// <summary>
        /// 清理所有连接（用于应用关闭时）
        /// </summary>
        public void Cleanup()
        {
            foreach (var group in _groups.Values)
            {
                foreach (var ws in group)
                {
                    ws.Dispose();
                }
            }
            _groups.Clear();
            _logger.Info("All WebSocket connections cleaned up.");
        }
    }
}