from packet import Packet
from protos.packetCode_pb2 import PacketCode
from udpServer import UDPServer

@UDPServer.clientPacketHandler(PacketCode.CLIENT_HEART_BEAT)
def handleHeartBeat(server: UDPServer, senderAddr, packet: Packet):
    print(f'Heart beat from {senderAddr}')
    server.heartBeatClient(senderAddr)
    server.send(Packet(PacketCode.CLIENT_HEART_BEAT_SERVER_RESPONSE), clientAddr=senderAddr)
