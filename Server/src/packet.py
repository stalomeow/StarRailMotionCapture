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
        # sizeof(CONST_HEAD + PacketCode + PayloadLength + CONST_TAIL) == 8
        if len(data) < 8:
            return None

        # Check Head Const
        if _readUInt16(data, 0) != PACKET_CONST_HEAD:
            return None

        payloadLength = _readUInt16(data, 4)

        # Check Packet Size
        if len(data) != 8 + payloadLength:
            return None

        # Check Tail Const
        if _readUInt16(data, 6 + payloadLength) != PACKET_CONST_TAIL:
            return None

        packetCode = _readUInt16(data, 2)
        payloadBytes = data[6:6+payloadLength]
        return cls(packetCode, payloadBytes)
