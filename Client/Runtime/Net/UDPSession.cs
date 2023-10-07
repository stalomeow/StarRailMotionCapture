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
        [Header("Server")]

        [SerializeField] private string m_ServerIPAddress = "127.0.0.1";
        [SerializeField] private int m_ServerPort = 5000;

        [Header("Client")]
        [SerializeField, Range(1024, 4096)] private int m_BufferSize = 2048;
        [SerializeField] private float m_HeartBeatIntervalSeconds = 2.0f;
        [SerializeField] private float m_HeartBeatResponseTimeout = 10.0f;
        [SerializeField] private List<MotionActorGameModel> m_Actors;

        public List<MotionActorGameModel> Actors => m_Actors;

        private EndPoint m_ServerEP;
        private Socket m_Socket;

        private ConcurrentQueue<(PacketCode code, object payload, IPacketHandler handler)> m_RecvPackets;
        private byte[] m_RecvBuffer;
        private ManualResetEventSlim m_RecvThreadStopEvent;
        private Thread m_RecvThread;

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
                    m_Socket.ReceiveTimeout = 1; // milliseconds. 0 or -1 means infinity.
                    m_Socket.Bind(ipEndPoint);
                    m_Socket.Connect(m_ServerEP);
                    break;
                }
            }
        }

        private void InitReceive()
        {
            m_RecvPackets = new ConcurrentQueue<(PacketCode code, object payload, IPacketHandler handler)>();
            m_RecvBuffer = new byte[m_BufferSize];
            m_RecvThreadStopEvent = new ManualResetEventSlim(false);
            m_RecvThread = new Thread(Receive) { IsBackground = false };
            m_RecvThread.Start();
        }

        private void InitSend()
        {
            m_SendBuffer = new byte[m_BufferSize];
            m_LastTimeHeartBeat = 0;
        }

        private void Receive()
        {
            while (!m_RecvThreadStopEvent.IsSet)
            {
                try
                {
                    m_Socket.ReceiveFrom(m_RecvBuffer, ref m_ServerEP);
                }
                catch
                {
                    continue;
                }

                if (!Packet.TryRead(m_RecvBuffer, out PacketCode code, out ReadOnlySpan<byte> payloadBytes))
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
                m_RecvPackets.Enqueue((code, payload, handler));
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
            while (m_RecvPackets.TryDequeue(out (PacketCode code, object payload, IPacketHandler handler) packet))
            {
                packet.handler.HandlePacketAndReleasePayload(this, packet.code, packet.payload);
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

            Debug.LogWarning("Server lost connection!");

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

            Send(PacketCode.HeartBeatReq);
            m_LastTimeHeartBeat = time;
            LastHeartBeatResponseTime ??= time; // 第一次发包
        }

        private void OnDestroy()
        {
            m_RecvThreadStopEvent.Set();
            m_RecvThread.Join();

            Send(PacketCode.QuitNotify);
            m_Socket.Dispose();
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
