// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: packetCode.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace HSR.MotionCapture.Net.Protos {

  /// <summary>Holder for reflection information generated from packetCode.proto</summary>
  public static partial class PacketCodeReflection {

    #region Descriptor
    /// <summary>File descriptor for packetCode.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static PacketCodeReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChBwYWNrZXRDb2RlLnByb3RvKs0BCgpQYWNrZXRDb2RlEggKBE5PTkUQABIO",
            "CgpESVNDT05ORUNUEAESFQoRQ0xJRU5UX0hFQVJUX0JFQVQQBBIlCiFDTElF",
            "TlRfSEVBUlRfQkVBVF9TRVJWRVJfUkVTUE9OU0UQBRINCglGQUNFX0RBVEEQ",
            "BhINCglQT1NFX0RBVEEQBxINCglIQU5EX0RBVEEQCCIECAIQAiIECAMQAyoO",
            "Q0xJRU5UX0NPTk5FQ1QqHkNMSUVOVF9DT05ORUNUX1NFUlZFUl9SRVNQT05T",
            "RUIfqgIcSFNSLk1vdGlvbkNhcHR1cmUuTmV0LlByb3Rvc2IGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::HSR.MotionCapture.Net.Protos.PacketCode), }, null, null));
    }
    #endregion

  }
  #region Enums
  public enum PacketCode {
    [pbr::OriginalName("NONE")] None = 0,
    [pbr::OriginalName("DISCONNECT")] Disconnect = 1,
    [pbr::OriginalName("CLIENT_HEART_BEAT")] ClientHeartBeat = 4,
    [pbr::OriginalName("CLIENT_HEART_BEAT_SERVER_RESPONSE")] ClientHeartBeatServerResponse = 5,
    [pbr::OriginalName("FACE_DATA")] FaceData = 6,
    [pbr::OriginalName("POSE_DATA")] PoseData = 7,
    [pbr::OriginalName("HAND_DATA")] HandData = 8,
  }

  #endregion

}

#endregion Designer generated code