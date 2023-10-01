import mediapipe as mp
import numpy as np

from packet import Packet
from protos.faceData_pb2 import FaceData
from protos.packetCode_pb2 import PacketCode

FaceLandmarkerResult = mp.tasks.vision.FaceLandmarkerResult

def _extractRotation(outQuaternion, matrix):
    try:
        matrix = matrix[:3, :3]
        matrix /= np.linalg.norm(matrix, axis=0)

        w = np.sqrt(np.maximum(0, 1 + matrix[0, 0] + matrix[1, 1] + matrix[2, 2])) * 0.5
        x = np.sqrt(np.maximum(0, 1 + matrix[0, 0] - matrix[1, 1] - matrix[2, 2])) * 0.5
        y = np.sqrt(np.maximum(0, 1 - matrix[0, 0] + matrix[1, 1] - matrix[2, 2])) * 0.5
        z = np.sqrt(np.maximum(0, 1 - matrix[0, 0] - matrix[1, 1] + matrix[2, 2])) * 0.5

        x = np.copysign(x, matrix[2, 1] - matrix[1, 2])
        y = np.copysign(y, matrix[0, 2] - matrix[2, 0])
        z = np.copysign(z, matrix[1, 0] - matrix[0, 1])

        # Unity 是左手系。Y 轴向上，Z 轴向前，X 轴向右
        # mediapipe 是右手系。Y 轴向上，Z 轴向前，X 轴向左
        # rotation.w == cos(theta/2) 偶函数，不用管
        outQuaternion.w = w
        outQuaternion.x = x
        outQuaternion.y = -y
        outQuaternion.z = -z
    except Exception as e:
        print(e)

class FaceDataPacket(Packet):
    def __init__(self, result: FaceLandmarkerResult) -> None:
        faceData = FaceData()

        # head rotation
        _extractRotation(faceData.headRotation, result.facial_transformation_matrixes[0])

        # blend shape value
        for blendShapeData in result.face_blendshapes[0]:
            item = faceData.blendShapes.add()
            item.name = blendShapeData.category_name
            item.value = round(blendShapeData.score, 4)

        super().__init__(PacketCode.FACE_DATA, faceData.SerializeToString())