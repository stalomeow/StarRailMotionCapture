from server import UDPServer
from packet import Packet
from protos.packetCode_pb2 import PacketCode

@UDPServer.clientPacketHandler(PacketCode.QUIT_NOTIFY)
def handleQuitNotify(server: UDPServer, senderAddr, packet: Packet):
    print(f'{senderAddr}: Quit.')
    server.removeClient(senderAddr)
