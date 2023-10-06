import mediapipe as mp

from datetime import datetime
from packet import Packet
from udpServer import UDPServer
from mediapipe.framework.formats import landmark_pb2

BaseOptions = mp.tasks.BaseOptions
VisionRunningMode = mp.tasks.vision.RunningMode

def _mergeConfig(obj, **kwargs) -> dict:
    clazz = type(obj)
    configList = [kwargs]

    while clazz is not None:
        config = getattr(clazz, 'CONFIG', None)
        if isinstance(config, dict):
            configList.append(config)
        clazz = clazz.__base__

    results = {}
    for config in reversed(configList):
        results.update(config)
    return results

class LandmarkerWrapper(object):
    CONFIG = {
        # base options
        'model_asset_path': None,
        'model_asset_buffer': None,
        'delegate': BaseOptions.Delegate.CPU,

        # landmarker options
        'landmarker_type': None,
        'landmarker_options_type': None,
        'running_mode': VisionRunningMode.LIVE_STREAM,

        # draw landmark options
        'landmark_drawing_spec': mp.solutions.drawing_utils.DrawingSpec(color=mp.solutions.drawing_utils.RED_COLOR),
        'landmark_drawing_connections': None,
        'landmark_drawing_connection_spec': mp.solutions.drawing_utils.DrawingSpec(),
    }

    def __init__(self, server: UDPServer, **kwargs):
        config = _mergeConfig(self, **kwargs)
        landmarkerOptionsType = config.pop('landmarker_options_type')

        self._server = server
        self._startTime = datetime.now()
        self._landmarker = None
        self._landmarkDrawingList = None

        self._landmarkDrawingSpec = config.pop('landmark_drawing_spec')
        self._landmarkDrawingConnections = config.pop('landmark_drawing_connections')
        self._landmarkDrawingConnectionSpec = config.pop('landmark_drawing_connection_spec')

        self._landmarkerType = config.pop('landmarker_type')
        self._landmarkerOptions = landmarkerOptionsType(
            base_options=BaseOptions(
                model_asset_path=config.pop('model_asset_path'),
                model_asset_buffer=config.pop('model_asset_buffer'),
                delegate=config.pop('delegate')
            ),
            **config,
        )

        if self._landmarkerOptions.running_mode == VisionRunningMode.LIVE_STREAM:
            self._landmarkerOptions.result_callback = self._resultCallback

    @property
    def runningMode(self) -> VisionRunningMode:
        return self._landmarkerOptions.running_mode

    def stop(self):
        if self._landmarker is None:
            return

        self._landmarker.close()
        self._landmarker = None

    def detect(self, mpImage: mp.Image):
        landmarker = self._landmarker

        if landmarker is None:
            landmarker = self._landmarkerType.create_from_options(self._landmarkerOptions)
            self._landmarker = landmarker
            self._startTime = datetime.now()

        timestampMS = int((datetime.now() - self._startTime).total_seconds() * 1000)

        match self.runningMode:
            case VisionRunningMode.LIVE_STREAM:
                landmarker.detect_async(mpImage, timestampMS)

            case VisionRunningMode.VIDEO:
                result = landmarker.detect_for_video(mpImage, timestampMS)
                self._resultCallback(result, mpImage, timestampMS)

            case VisionRunningMode.IMAGE:
                result = landmarker.detect(mpImage)
                self._resultCallback(result, mpImage, timestampMS)

            case _:
                raise NotImplementedError(self.runningMode)

    def drawLandmarks(self, img):
        if self._landmarkDrawingList is None:
            return

        mp.solutions.drawing_utils.draw_landmarks(
            image=img,
            landmark_list=self._landmarkDrawingList,
            landmark_drawing_spec=self._landmarkDrawingSpec,
            connections=self._landmarkDrawingConnections,
            connection_drawing_spec=self._landmarkDrawingConnectionSpec)

    def _resultCallback(self, result, outputImage: mp.Image, timestampMS: int):
        #! 在 Live Stream 模式下，这段代码在子线程执行！

        # clear cache
        self._landmarkDrawingList = None

        try:
            if not self._checkIsResultValid(result):
                return

            # send packet
            packet = self._createPacket(result, outputImage, timestampMS)
            self._server.send(packet)

            # update drawing landmarks
            drawingList = landmark_pb2.NormalizedLandmarkList()
            drawingList.landmark.extend(
                landmark_pb2.NormalizedLandmark(x=landmark.x, y=landmark.y, z=landmark.z)
                for landmark in self._getDrawingLandmarks(result)
            )
            self._landmarkDrawingList = drawingList #! 赋值放在最后，保证线程安全
        except Exception as e:
            self._landmarkDrawingList = None
            print(e)

    def _checkIsResultValid(self, result) -> bool:
        return False

    def _createPacket(self, result, outputImage: mp.Image, timestampMS: int) -> Packet:
        pass

    def _getDrawingLandmarks(self, result):
        pass

class LandmarkerWrapperGroup(object):
    def __init__(self, *landmarkers: LandmarkerWrapper):
        self._landmarkers = landmarkers

    def stop(self):
        for l in self._landmarkers:
            l.stop()

    def detect(self, mpImage: mp.Image):
        for l in self._landmarkers:
            l.detect(mpImage)

    def drawLandmarks(self, img):
        for l in self._landmarkers:
            l.drawLandmarks(img)
