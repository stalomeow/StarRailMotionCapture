import mediapipe as mp
import numpy as np

from landmarkerWrapper import LandmarkerWrapper
from udpServer import UDPServer
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

class FaceLandmarker(LandmarkerWrapper):
    CONFIG = {
        # base options
        'model_asset_path': r'./Server/models/face_landmarker.task',

        # landmarker options
        'landmarker_type': mp.tasks.vision.FaceLandmarker,
        'landmarker_options_type': mp.tasks.vision.FaceLandmarkerOptions,

        # draw landmark options
        'landmark_drawing_spec': None,
        'landmark_drawing_connections': mp.solutions.face_mesh.FACEMESH_CONTOURS,
        'landmark_drawing_connection_spec': mp.solutions.drawing_styles.get_default_face_mesh_contours_style(),

        # face landmarker options
        'min_face_detection_confidence': 0.8,
        'output_face_blendshapes': True,
        'output_facial_transformation_matrixes': True,
    }

    def __init__(self, server: UDPServer, **kwargs):
        super().__init__(server, **kwargs)

    def _checkIsResultValid(self, result: FaceLandmarkerResult) -> bool:
        return all([
            len(result.face_blendshapes) > 0,
            len(result.facial_transformation_matrixes) > 0,
            len(result.face_landmarks) > 0,
        ])

    def _createPacket(self, result: FaceLandmarkerResult, outputImage: mp.Image, timestampMS: int) -> Packet:
        faceData = FaceData()

        # head rotation
        _extractRotation(faceData.headRotation, result.facial_transformation_matrixes[0])

        # blend shape value
        for blendShapeData in result.face_blendshapes[0]:
            item = faceData.blendShapes.add()
            item.name = blendShapeData.category_name
            item.value = round(blendShapeData.score, 4)

        return Packet(PacketCode.FACE_DATA, faceData.SerializeToString())

    def _getDrawingLandmarks(self, result: FaceLandmarkerResult):
        return result.face_landmarks[0]
