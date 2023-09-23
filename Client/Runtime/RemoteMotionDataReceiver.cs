using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HSR.MotionCapture
{
    [DisallowMultipleComponent]
    public abstract class RemoteMotionDataReceiver : MonoBehaviour
    {
        [Header("Network Settings")] [SerializeField]
        private string m_IPAddress = "127.0.0.1";

        [SerializeField, Range(IPEndPoint.MinPort, IPEndPoint.MaxPort)]
        private int m_Port = 5000;

        private readonly byte[] m_SizeBuffer = new byte[4];
        private readonly byte[] m_DataBuffer = new byte[2048];

        private Socket m_Socket;
        private Thread m_Thread;

        private int m_LocalFaceDataVersion = 0;
        private volatile int m_RemoteFaceDataVersion = 0;
        private volatile FaceData m_RemoteFaceData = null;

        protected virtual void Start()
        {
            IPAddress ipAddress = IPAddress.Parse(m_IPAddress);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, m_Port);
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_Socket.Connect(ipEndPoint);

            m_Thread = new Thread(Receive) { IsBackground = true };
            m_Thread.Start();
        }

        private void Receive()
        {
            try
            {
                while (true)
                {
                    m_Socket.Receive(m_SizeBuffer);

                    if (!BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(m_SizeBuffer);
                    }

                    int totalSize = BitConverter.ToInt32(m_SizeBuffer);
                    int receivedLen = 0;

                    while (receivedLen < totalSize)
                    {
                        // 必须把所有字节读到内存
                        receivedLen += m_Socket.Receive(m_DataBuffer, receivedLen, totalSize - receivedLen,
                            SocketFlags.None);
                    }

                    FaceData remoteData = FaceData.Parser.ParseFrom(m_DataBuffer, 0, totalSize);
                    Interlocked.Exchange(ref m_RemoteFaceData, remoteData);
                    Interlocked.Increment(ref m_RemoteFaceDataVersion);
                }
            }
            catch (Exception e) when (e is ObjectDisposedException or SocketException)
            {
                Debug.Log($"[{GetType().Name}] Thread Abort\n" + e);
            }
        }

        protected virtual void Update()
        {
            if (!m_Thread.IsAlive)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#endif
            }

            int remoteDataVersion = m_RemoteFaceDataVersion;

            if (m_LocalFaceDataVersion != remoteDataVersion)
            {
                m_LocalFaceDataVersion = remoteDataVersion;
                UpdateFaceMotion(m_RemoteFaceData);
            }
        }

        protected virtual void OnDestroy()
        {
            m_Socket.Dispose();
            m_Thread.Join();

            Debug.Log($"[{GetType().Name}] Exit");
        }

        protected abstract void UpdateFaceMotion(FaceData data);
    }
}
