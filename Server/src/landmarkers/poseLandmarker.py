import mediapipe as mp

from landmarkers.landmarker import Landmarker
from server import UDPServer
from packet import Packet
from protos.poseData_pb2 import PoseData
from protos.packetCode_pb2 import PacketCode

PoseLandmarkerResult = mp.tasks.vision.PoseLandmarkerResult

class PoseLandmarker(Landmarker):
    CONFIG = {
        # landmarker options
        'landmarker_type': mp.tasks.vision.PoseLandmarker,
        'landmarker_options_type': mp.tasks.vision.PoseLandmarkerOptions,

        # draw landmark options
        'landmark_drawing_spec': mp.solutions.drawing_styles.get_default_pose_landmarks_style(),
        'landmark_drawing_connections': mp.solutions.pose.POSE_CONNECTIONS,

        # pose landmarker options
        'min_pose_detection_confidence': 0.8,
        'min_pose_presence_confidence': 0.8,
        'min_tracking_confidence': 0.8,
    }

    def __init__(self, server: UDPServer, **kwargs):
        super().__init__(server, **kwargs)

    def _checkIsResultValid(self, result: PoseLandmarkerResult) -> bool:
        return len(result.pose_landmarks) > 0

    def _createPacket(self, result: PoseLandmarkerResult, outputImage: mp.Image, timestampMS: int) -> Packet:
        poseData = PoseData()
        return Packet(PacketCode.POSE_DATA, poseData.SerializeToString())

    def _getDrawingLandmarks(self, result: PoseLandmarkerResult):
        return result.pose_landmarks[0]
