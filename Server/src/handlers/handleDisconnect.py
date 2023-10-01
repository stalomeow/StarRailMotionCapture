from packet import Packet
from protos.packetCode_pb2 import PacketCode
from udpServer import UDPServer

@UDPServer.clientPacketHandler(PacketCode.DISCONNECT)
def handleDisconnect(server: UDPServer, senderAddr, packet: Packet):
    print(f'Client {senderAddr} is disconnected')
    server.removeClient(senderAddr)
