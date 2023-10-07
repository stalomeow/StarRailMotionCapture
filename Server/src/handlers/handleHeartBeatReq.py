from server import UDPServer
from packet import Packet
from protos.packetCode_pb2 import PacketCode

@UDPServer.clientPacketHandler(PacketCode.HEART_BEAT_REQ)
def handleHeartBeatReq(server: UDPServer, senderAddr, packet: Packet):
    print(f'{senderAddr}: Heart beat.')
    server.refreshClientLastHeartBeatTime(senderAddr)
    server.send(Packet(PacketCode.HEART_BEAT_RSP), clientAddr=senderAddr)
