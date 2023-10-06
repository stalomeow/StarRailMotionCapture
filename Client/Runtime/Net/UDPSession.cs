using HSR.MotionCapture.Net.Protos;
using JetBrains.Annotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace HSR.MotionCapture.Net
{
    public class UDPSession : MonoBehaviour
    {
        private const int BufferSize = 2048;

        [Header("Server")]

        [SerializeField] private string m_ServerIPAddress = "127.0.0.1";
        [SerializeField] private int m_ServerPort = 5000;

        [Header("Client")]

        [SerializeField] private float m_HeartBeatIntervalSeconds = 2.0f;
        [SerializeField] private float m_HeartBeatResponseTimeout = 10.0f;
        [SerializeField] private List<MotionActorGameModel> m_Actors;

        public Transform Points;

        public IReadOnlyList<MotionActorGameModel> Actors => m_Actors;

        private EndPoint m_ServerEP;
        private Socket m_Socket;

        private ConcurrentQueue<(PacketCode code, object payload, IPacketHandler handler)> m_ReceivedPackets;
        private byte[] m_ReceiveBuffer;
        private bool m_IsReceiveThreadAlive;
        private Thread m_ReceiveThread;

        private byte[] m_SendBuffer;
        private float m_LastTimeHeartBeat;

        public float? LastHeartBeatResponseTime { get; set; }

        private void Start()
        {
            InitServerEP();
            InitSocket();
            InitReceive();
            InitSend();
        }

        private void InitServerEP()
        {
            IPAddress ipAddress = IPAddress.Parse(m_ServerIPAddress);
            m_ServerEP = new IPEndPoint(ipAddress, m_ServerPort);
        }

        private void InitSocket()
        {
            string hostName = Dns.GetHostName();

            foreach (IPAddress ipAddress in Dns.GetHostAddresses(hostName))
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 0); // With any port available
                    m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    m_Socket.Bind(ipEndPoint);
                    m_Socket.Connect(m_ServerEP);
                    break;
                }
            }
        }

        private void InitReceive()
        {
            m_ReceivedPackets = new ConcurrentQueue<(PacketCode code, object payload, IPacketHandler handler)>();
            m_ReceiveBuffer = new byte[BufferSize];
            m_IsReceiveThreadAlive = true;
            m_ReceiveThread = new Thread(Receive) { IsBackground = true };
            m_ReceiveThread.Start();
        }

        private void InitSend()
        {
            m_SendBuffer = new byte[BufferSize];
            m_LastTimeHeartBeat = 0;
        }

        private void Receive()
        {
            while (m_IsReceiveThreadAlive)
            {
                try
                {
                    m_Socket.ReceiveFrom(m_ReceiveBuffer, ref m_ServerEP);
                }
                catch
                {
                    continue;
                }

                if (!Packet.TryRead(m_ReceiveBuffer, out PacketCode code, out ReadOnlySpan<byte> payloadBytes))
                {
                    Debug.LogWarning("A bad packet was received!");
                    continue;
                }

                if (!TryGetPacketHandler(code, out IPacketHandler handler))
                {
                    Debug.LogWarning($"An unhandled packet '{code}' was received!");
                    continue;
                }

                object payload = handler.ParsePayload(code, payloadBytes);
                m_ReceivedPackets.Enqueue((code, payload, handler));
            }
        }

        public void Send(PacketCode packetCode)
        {
            Send<object>(packetCode, null, null);
        }

        public void Send<T>(PacketCode packetCode, T payload, [CanBeNull] Packet.WritePayloadAction<T> writePayloadBytes)
        {
            int len = Packet.Write(m_SendBuffer, packetCode, payload, writePayloadBytes);
            m_Socket.Send(m_SendBuffer, 0, len, SocketFlags.None);
        }

        private void Update()
        {
            HandlePendingPackets();
            CheckConnection();
            HeartBeat();
        }

        private void HandlePendingPackets()
        {
            while (m_ReceivedPackets.TryDequeue(out (PacketCode code, object payload, IPacketHandler handler) packet))
            {
                packet.handler.Handle(this, packet.code, packet.payload);
            }
        }

        private void CheckConnection()
        {
            if (LastHeartBeatResponseTime == null)
            {
                return;
            }

            if (m_LastTimeHeartBeat - LastHeartBeatResponseTime < m_HeartBeatResponseTimeout)
            {
                return;
            }

            Debug.LogWarning("Server is not available!");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            UnityEngine.Application.Quit();
#endif
        }

        private void HeartBeat()
        {
            float time = Time.realtimeSinceStartup;

            if (time - m_LastTimeHeartBeat < m_HeartBeatIntervalSeconds)
            {
                return;
            }

            Send(PacketCode.ClientHeartBeat);
            m_LastTimeHeartBeat = time;
            LastHeartBeatResponseTime ??= time; // 第一次发包
        }

        private void OnDestroy()
        {
            Send(PacketCode.Disconnect);

            m_Socket.Dispose();
            m_IsReceiveThreadAlive = false;
            m_ReceiveThread.Join();

            Debug.Log($"[{GetType().Name}] Exit");
        }

        #region Packet Handlers

        private static readonly Dictionary<PacketCode, IPacketHandler> s_PacketHandlers = new();

        static UDPSession() => CollectAllPacketHandlers();

        private static void CollectAllPacketHandlers()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    var attr = type.GetCustomAttribute<CustomPacketHandlerAttribute>();

                    if (attr == null)
                    {
                        continue;
                    }

                    if (!typeof(IPacketHandler).IsAssignableFrom(type))
                    {
                        Debug.LogError($"Type '{type}' does not implement {nameof(IPacketHandler)}");
                        continue;
                    }

                    if (s_PacketHandlers.ContainsKey(attr.HandlePacketCode))
                    {
                        Debug.LogError($"Duplicated handler '{type}' for {attr.HandlePacketCode}");
                        continue;
                    }

                    IPacketHandler handler = (IPacketHandler)Activator.CreateInstance(type, false);
                    s_PacketHandlers[attr.HandlePacketCode] = handler;
                }
            }
        }

        private static bool TryGetPacketHandler(PacketCode code, out IPacketHandler handler)
        {
            return s_PacketHandlers.TryGetValue(code, out handler);
        }

        #endregion
    }
}
