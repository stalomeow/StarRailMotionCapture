import sys
sys.path.append("./Server/src/protos")

import cv2
import mediapipe as mp
import time

from datetime import datetime
from udpServer import UDPServer
from mediapipe.framework.formats import landmark_pb2
from protos.faceData_pb2 import FaceData
from protos.packetCode_pb2 import PacketCode
from packets.faceDataPacket import FaceDataPacket

BaseOptions = mp.tasks.BaseOptions
FaceLandmarker = mp.tasks.vision.FaceLandmarker
FaceLandmarkerOptions = mp.tasks.vision.FaceLandmarkerOptions
FaceLandmarkerResult = mp.tasks.vision.FaceLandmarkerResult
PoseLandmarker = mp.tasks.vision.PoseLandmarker
PoseLandmarkerOptions = mp.tasks.vision.PoseLandmarkerOptions
PoseLandmarkerResult = mp.tasks.vision.PoseLandmarkerResult
VisionRunningMode = mp.tasks.vision.RunningMode

WINDOW_NAME = 'Live'

if __name__ == '__main__':
    UDPServer.importClientPacketHandlers([
        "handleDisconnect",
        "handleHeartBeat"
    ])

    faceLandmarks = None

    with UDPServer(5000) as server:
        def sendResults(result: FaceLandmarkerResult, output_image: mp.Image, timestamp_ms: int):
            global faceLandmarks

            # clear cache
            faceLandmarks = None

            try:
                if len(result.face_blendshapes) == 0:
                    return

                if len(result.facial_transformation_matrixes) == 0:
                    return

                if len(result.face_landmarks) == 0:
                    return

                server.send(FaceDataPacket(result))

                # face landmarks
                tempList = landmark_pb2.NormalizedLandmarkList()
                tempList.landmark.extend([
                    landmark_pb2.NormalizedLandmark(x=landmark.x, y=landmark.y, z=landmark.z)
                    for landmark in result.face_landmarks[0]
                ])
                faceLandmarks = tempList #! 处理线程竞争

                # print(timestamp_ms)
            except Exception as e:
                faceLandmarks = None
                print(e)

        def sendPoseResults(result: FaceLandmarkerResult, output_image: mp.Image, timestamp_ms: int):
            print('pose landmarker result: {}'.format(result))

        landmarkerOptions = FaceLandmarkerOptions(
            base_options=BaseOptions(model_asset_path=r"./Server/models/face_landmarker.task"),
            running_mode=VisionRunningMode.LIVE_STREAM,
            # running_mode=VisionRunningMode.VIDEO,
            min_face_detection_confidence = 0.8,
            output_face_blendshapes=True,
            output_facial_transformation_matrixes=True,
            result_callback=sendResults)
        # landmarkerOptions = PoseLandmarkerOptions(
        #     base_options=BaseOptions(model_asset_path=r"./Server/models/pose_landmarker_heavy.task"),
        #     running_mode=VisionRunningMode.LIVE_STREAM,
        #     min_pose_detection_confidence=0.8,
        #     min_pose_presence_confidence=0.8,
        #     min_tracking_confidence=0.8,
        #     result_callback=sendPoseResults)

        cap = None
        landmarker = None
        startTime = None

        while True:
            server.kickOfflineClients()

            if server.clientCount <= 0:
                if cap is not None:
                    cap.release()
                    landmarker.close()
                    cv2.destroyWindow(WINDOW_NAME)

                    cap = None
                    landmarker = None
                    startTime = None

                time.sleep(1)
                continue

            if cap is None:
                cap = cv2.VideoCapture(0)
                # cap = cv2.VideoCapture('https://10.198.54.110:8080/video')
                # cap = cv2.VideoCapture(r'')
                landmarker = FaceLandmarker.create_from_options(landmarkerOptions)
                cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_AUTOSIZE)
                # cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_NORMAL)
                cv2.setWindowProperty(WINDOW_NAME, cv2.WND_PROP_TOPMOST, 1)
                startTime = datetime.now()

            if (cv2.waitKey(1) & 0xFF) == ord('q'):
                exit()

            success, img = cap.read()

            if not success:
                continue

            timestampMS = int((datetime.now() - startTime).total_seconds() * 1000)
            landmarker.detect_async(mp.Image(image_format=mp.ImageFormat.SRGB, data=img), timestampMS)

            if faceLandmarks is not None:
                mp.solutions.drawing_utils.draw_landmarks(
                    image=img,
                    landmark_list=faceLandmarks,
                    connections=mp.solutions.face_mesh.FACEMESH_CONTOURS,
                    landmark_drawing_spec=None,
                    connection_drawing_spec=mp.solutions.drawing_styles.get_default_face_mesh_contours_style())

            cv2.imshow(WINDOW_NAME, img)
