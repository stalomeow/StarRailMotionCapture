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
        [SerializeField] private float m_HeartBeatTimeoutSeconds = 10.0f;
        [SerializeField] private List<MotionActorGameModel> m_Actors;

        private Socket m_Socket;

        private byte[] m_SendBuffer;
        private float m_LastTimeHeartBeat;
        private float? m_LastHeartBeatResponseTime;

        private byte[] m_RecvBuffer;
        private Thread m_RecvThread;
        private ManualResetEventSlim m_RecvThreadStopEvent;
        private ConcurrentQueue<HandlePacketTask> m_HandlePacketTaskQueue;

        public List<MotionActorGameModel> Actors => m_Actors;

        private void Start()
        {
            var serverEP = new IPEndPoint(IPAddress.Parse(m_ServerIPAddress), m_ServerPort);

            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_Socket.ReceiveTimeout = 100; // milliseconds. 0 or -1 means infinity.
            m_Socket.Connect(serverEP);

            m_SendBuffer = new byte[m_BufferSize];
            m_LastTimeHeartBeat = 0;
            m_LastHeartBeatResponseTime = null;

            m_RecvBuffer = new byte[m_BufferSize];
            m_RecvThread = new Thread(Receive) { Name = "Receive UDP Packets", IsBackground = false };
            m_RecvThreadStopEvent = new ManualResetEventSlim(false);
            m_HandlePacketTaskQueue = new ConcurrentQueue<HandlePacketTask>();

            m_RecvThread.Start();
        }

        private void Receive()
        {
            while (!m_RecvThreadStopEvent.IsSet)
            {
                int len;

                try
                {
                    len = m_Socket.Receive(m_RecvBuffer);
                }
                catch
                {
                    continue;
                }

                Span<byte> bytes = m_RecvBuffer.AsSpan(0, len);

                if (!Packet.TryRead(bytes, out PacketCode code, out ReadOnlySpan<byte> payloadBytes))
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
                m_HandlePacketTaskQueue.Enqueue(new HandlePacketTask(code, payload, handler));
            }
        }

        public void Send(PacketCode packetCode)
        {
            Send<object>(packetCode, null, null);
        }

        public void Send<T>(PacketCode packetCode, T payload, [CanBeNull] Packet.WritePayloadAction<T> writePayloadBytes)
        {
            Span<byte> buffer = m_SendBuffer.AsSpan();
            int len = Packet.Write(buffer, packetCode, payload, writePayloadBytes);
            m_Socket.Send(buffer[..len]);
        }

        private void Update()
        {
            HandlePackets();
            CheckServerConnection();
            SendHeartBeat();
        }

        private void HandlePackets()
        {
            while (m_HandlePacketTaskQueue.TryDequeue(out HandlePacketTask task))
            {
                task.Handle(this);
            }
        }

        private void CheckServerConnection()
        {
            if (m_LastHeartBeatResponseTime == null)
            {
                return;
            }

            if (m_LastTimeHeartBeat - m_LastHeartBeatResponseTime < m_HeartBeatTimeoutSeconds)
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

        private void SendHeartBeat()
        {
            float time = Time.realtimeSinceStartup;

            if (time - m_LastTimeHeartBeat < m_HeartBeatIntervalSeconds)
            {
                return;
            }

            Send(PacketCode.HeartBeatReq);
            m_LastTimeHeartBeat = time;

            // 第一次发心跳包的时候更新
            m_LastHeartBeatResponseTime ??= time;
        }

        public void RefreshLastHeartBeatResponseTime()
        {
            m_LastHeartBeatResponseTime = Time.realtimeSinceStartup;
        }

        private void OnDestroy()
        {
            m_RecvThreadStopEvent.Set();
            m_RecvThread.Join();

            Send(PacketCode.QuitNotify);
            m_Socket.Dispose();
        }

        private readonly struct HandlePacketTask
        {
            private readonly PacketCode m_Code;
            private readonly object m_Payload;
            private readonly IPacketHandler m_Handler;

            public HandlePacketTask(PacketCode code, object payload, IPacketHandler handler)
            {
                m_Code = code;
                m_Payload = payload;
                m_Handler = handler;
            }

            public void Handle(UDPSession session)
            {
                m_Handler.HandlePacketAndReleasePayload(session, m_Code, m_Payload);
            }
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
