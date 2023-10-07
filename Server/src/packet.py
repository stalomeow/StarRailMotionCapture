import typing

from protos.packetCode_pb2 import PacketCode

PACKET_CONST_HEAD = 0x2B3C
PACKET_CONST_TAIL = 0x4D5F

def _readUInt16(data: bytes, startIndex: int) -> int:
    b1 = data[startIndex]
    b2 = data[startIndex + 1]
    return (b1 << 8) | b2

def _writeUInt16(buffer: bytearray, value: int) -> None:
    buffer.append((value >> 8) & 0xFF)
    buffer.append(value & 0xFF)

class Packet(object):
    def __init__(self, packetCode: PacketCode, payloadBytes: bytes | bytearray | None = None) -> None:
        self.packetCode = packetCode
        self.payloadBytes = payloadBytes

    def encode(self) -> bytearray:
        buffer = bytearray()
        _writeUInt16(buffer, PACKET_CONST_HEAD)
        _writeUInt16(buffer, self.packetCode)

        if self.payloadBytes is None:
            _writeUInt16(buffer, 0)
        else:
            _writeUInt16(buffer, len(self.payloadBytes))
            buffer.extend(self.payloadBytes)

        _writeUInt16(buffer, PACKET_CONST_TAIL)
        return buffer

    @classmethod
    def decode(cls, data: bytes) -> typing.Union["Packet", None]:
        head = _readUInt16(data, 0)
        if head != PACKET_CONST_HEAD:
            return None

        packetCode = _readUInt16(data, 2)
        payloadLength = _readUInt16(data, 4)
        payloadBytes = data[6:6+payloadLength]

        tail = _readUInt16(data, 6 + payloadLength)
        if tail != PACKET_CONST_TAIL:
            return None

        return cls(packetCode, payloadBytes)
