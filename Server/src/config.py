from landmarkers.faceLandmarker import FaceLandmarker
from landmarkers.poseLandmarker import PoseLandmarker

SERVER_UDP_PORT = 5000

WINDOW_NAME = 'Live'
WINDOW_TOPMOST = True
WINDOW_ENABLE = True

VIDEO_CAPTURE_ARGS = (0,)
# VIDEO_CAPTURE_ARGS = (r'./Server/test/ikun1.mp4',)

LANDMARKER_TYPES = [
    FaceLandmarker,
    PoseLandmarker
]

def importServerPacketHandlers():
    import handlers.handleDisconnect
    import handlers.handleHeartBeat
