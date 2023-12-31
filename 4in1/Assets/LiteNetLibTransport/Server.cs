using System;
using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using Mirror;
using UnityEngine;

namespace LiteNetLibMirror
{
    public delegate void OnConnected(int clientId);
    public delegate void OnServerData(int clientId, ArraySegment<byte> data, DeliveryMethod deliveryMethod);
    public delegate void OnDisconnected(int clientId);

    public class Server
    {
        private const string Scheme = "litenet";
        private const int ConnectionCapacity = 1000;

        // configuration
        readonly ushort port;
        readonly int updateTime;
        readonly int disconnectTimeout;
        readonly string acceptConnectKey;

        // LiteNetLib state
        NetManager server;
        Dictionary<int, NetPeer> connections = new Dictionary<int, NetPeer>(ConnectionCapacity);

        public event OnConnected onConnected;
        public event OnServerData onData;
        public event OnDisconnected onDisconnected;

        public Server(ushort port, int updateTime, int disconnectTimeout, string acceptConnectKey)
        {
            this.port = port;
            this.updateTime = updateTime;
            this.disconnectTimeout = disconnectTimeout;
            this.acceptConnectKey = acceptConnectKey;
        }

        /// <summary>
        /// Mirror connection Ids are 1 indexed but LiteNetLib is 0 indexed so we have to add 1 to the peer Id
        /// </summary>
        /// <param name="peerId">0 indexed id used by LiteNetLib</param>
        /// <returns>1 indexed id used by mirror</returns>
        private static int ToMirrorId(int peerId)
        {
            return peerId + 1;
        }

        /// <summary>
        /// Mirror connection Ids are 1 indexed but LiteNetLib is 0 indexed so we have to add 1 to the peer Id
        /// </summary>
        /// <param name="mirrorId">1 indexed id used by mirror</param>
        /// <returns>0 indexed id used by LiteNetLib</returns>
        private static int ToPeerId(int mirrorId)
        {
            return mirrorId - 1;
        }

        public void Start()
        {
            // not if already started
            if (server != null)
            {
                Debug.LogWarning("LiteNetLib: server already started.");
                return;
            }

            Debug.Log("LiteNet SV: starting...");

            // create server
            EventBasedNetListener listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.UpdateTime = updateTime;
            server.DisconnectTimeout = disconnectTimeout;

            // set up events
            listener.ConnectionRequestEvent += Listener_ConnectionRequestEvent;
            listener.PeerConnectedEvent += Listener_PeerConnectedEvent;
            listener.NetworkReceiveEvent += Listener_NetworkReceiveEvent;
            listener.PeerDisconnectedEvent += Listener_PeerDisconnectedEvent;
            listener.NetworkErrorEvent += Listener_NetworkErrorEvent;


            // start listening
            server.Start(port);
        }

        private void Listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            Debug.Log("LiteNet SV connection request");
            request.AcceptIfKey(acceptConnectKey);
        }

        private void Listener_PeerConnectedEvent(NetPeer peer)
        {
            int id = ToMirrorId(peer.Id);
            connections[id] = peer;
            onConnected?.Invoke(id);
        }

        private void Listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            int id = ToMirrorId(peer.Id);

            onData?.Invoke(id, reader.GetRemainingBytesSegment(), deliveryMethod);
            reader.Recycle();
        }

        private void Listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            int id = ToMirrorId(peer.Id);
            // this is called both when a client disconnects, and when we
            // disconnect a client.
            onDisconnected?.Invoke(id);
            connections.Remove(id);
        }

        private void Listener_NetworkErrorEvent(IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
        {
            // TODO should we disconnect or is it called automatically?
        }

        public void Stop()
        {
            if (server != null)
            {
                server.Stop();
                server = null;
            }
        }


        public bool Send(List<int> connectionIds, DeliveryMethod deliveryMethod, ArraySegment<byte> segment)
        {
            if (server == null)
            {
                Debug.LogWarning("LiteNet SV: can't send because not started yet.");
                return false;
            }

            foreach (int connectionId in connectionIds)
            {
                SendOne(connectionId, deliveryMethod, segment);
            }
            return true;
        }

        public void SendOne(int connectionId, DeliveryMethod deliveryMethod, ArraySegment<byte> segment)
        {
            if (server == null)
            {
                Debug.LogWarning("LiteNet SV: can't send because not started yet.");
                return;
            }

            if (connections.TryGetValue(connectionId, out NetPeer peer))
            {
                try
                {
                    peer.Send(segment.Array, segment.Offset, segment.Count, deliveryMethod);
                }
                catch (TooBigPacketException exception)
                {
                    Debug.LogWarning($"LiteNet SV: send failed for connectionId={connectionId} reason={exception}");
                }
            }
            else
            {
                Debug.LogWarning($"LiteNet SV: invalid connectionId={connectionId}");
            }
        }

        /// <summary>
        /// Kicks player
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public bool Disconnect(int connectionId)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    // disconnect the client.
                    // PeerDisconnectedEvent will call OnDisconnect.
                    peer.Disconnect();
                    return true;
                }
                Debug.LogWarning($"LiteNet SV: invalid connectionId={connectionId}");
                return false;
            }
            return false;
        }

        public Uri GetUri()
        {
            UriBuilder builder = new UriBuilder();
            builder.Scheme = Scheme;
            builder.Host = Dns.GetHostName();
            builder.Port = port;
            return builder.Uri;
        }

        public string GetClientAddress(int connectionId)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    return peer.EndPoint.ToString();
                }
            }
            return string.Empty;
        }

        public IPEndPoint GetClientIPEndPoint(int connectionId)
        {
            if (server != null)
            {
                if (connections.TryGetValue(connectionId, out NetPeer peer))
                {
                    return peer.EndPoint;
                }
            }
            return null;
        }

        public void OnUpdate()
        {
            if (server != null)
            {
                server.PollEvents();
            }
        }
    }
}
