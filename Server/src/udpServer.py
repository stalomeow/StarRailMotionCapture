import importlib
import socket
import threading
import typing

from datetime import datetime
from packet import Packet
from protos.packetCode_pb2 import PacketCode

class UDPServer(object):
    _clientPacketHandlers = {}

    def __init__(self, port: int, *, kickSeconds=10.0, recvBufferSize=2048):
        hostName = socket.gethostname()
        addr = socket.gethostbyname(hostName)
        self._sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self._sock.bind((addr, port))

        self._kickSeconds = kickSeconds
        self._recvBufferSize = recvBufferSize

        self._clients = set()
        self._clientLastHeartBeatTimes = {}
        self._clientLock = threading.Lock()

        self._recvThreadAlive = True
        self._recvThread = threading.Thread(target=self._recv, daemon=True)

    def __enter__(self):
        self._recvThread.start()
        print(f'Start UDP Server {self._sock.getsockname()}...')
        return self

    def __exit__(self, *args, **kwargs):
        self.send(Packet(PacketCode.DISCONNECT))

        self._sock.close()
        self._recvThreadAlive = False

    def _recv(self):
        while self._recvThreadAlive:
            try:
                data, addr = self._sock.recvfrom(self._recvBufferSize)
            except:
                continue

            packet = Packet.decode(data)

            if packet is None:
                print(f'A bad packet was received from {addr}!')
                continue

            handler = type(self)._clientPacketHandlers.get(packet.packetCode, None)

            if handler is None:
                print(f'Packet \'{packet.packetCode}\' has no handler!')
                continue

            # handler 可能会调用 send，所以在调用 handler 前加上 client
            self._clientLock.acquire()
            try:
                if addr not in self._clients:
                    self._clients.add(addr)
                    print(f'New client {addr} was added')
            finally:
                self._clientLock.release()

            handler(self, addr, packet)

    def send(self, packet: Packet, *, clientAddr=...):
        data = packet.encode()

        if clientAddr is ...:
            self._clientLock.acquire()
            try:
                for client in self._clients:
                    self._sock.sendto(data, client)
            finally:
                self._clientLock.release()
        else:
            self._sock.sendto(data, clientAddr)

    def kickOfflineClients(self):
        now = datetime.now()

        self._clientLock.acquire()
        try:
            for client in self._clients:
                lastHeartBeatTime = self._clientLastHeartBeatTimes.get(client, datetime.min)
                if (now - lastHeartBeatTime).seconds >= self._kickSeconds:
                    self._clientLastHeartBeatTimes.pop(client, None)
                    print(f'Client {client} is offline')

            self._clients.clear()
            self._clients.update(self._clientLastHeartBeatTimes.keys())
        finally:
            self._clientLock.release()

    def heartBeatClient(self, clientAddr):
        self._clientLock.acquire()
        try:
            self._clientLastHeartBeatTimes[clientAddr] = datetime.now()
        finally:
            self._clientLock.release()

    def removeClient(self, clientAddr):
        self._clientLock.acquire()
        try:
            self._clients.discard(clientAddr)
            self._clientLastHeartBeatTimes.pop(clientAddr, None)
        finally:
            self._clientLock.release()

    @property
    def clientCount(self) -> int:
        self._clientLock.acquire()
        try:
            return len(self._clients)
        finally:
            self._clientLock.release()

    @classmethod
    def clientPacketHandler(cls, packetCode: PacketCode):
        def handlerDecorator(handler):
            if packetCode in cls._clientPacketHandlers:
                print(f'Duplicated packet handler \'{handler}\' for \'{packetCode}\'')
            else:
                cls._clientPacketHandlers[packetCode] = handler
                print(f'Register packet handler \'{handler}\' for \'{packetCode}\'')
            return handler
        return handlerDecorator

    @staticmethod
    def importClientPacketHandlers(handlerNames: typing.Iterable[str]):
        for handler in handlerNames:
            importlib.import_module('.' + handler, 'handlers')
