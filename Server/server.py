import cv2
import mediapipe as mp
import numpy as np
import socket

from datetime import datetime
from mediapipe.framework.formats import landmark_pb2
from faceData_pb2 import FaceData

BaseOptions = mp.tasks.BaseOptions
FaceLandmarker = mp.tasks.vision.FaceLandmarker
FaceLandmarkerOptions = mp.tasks.vision.FaceLandmarkerOptions
FaceLandmarkerResult = mp.tasks.vision.FaceLandmarkerResult
VisionRunningMode = mp.tasks.vision.RunningMode

WINDOW_NAME = 'Live'

if __name__ == '__main__':
    client = None
    faceLandmarks = None

    def extractRotation(outQuaternion, matrix):
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

            outQuaternion.w = w
            outQuaternion.x = x
            outQuaternion.y = y
            outQuaternion.z = z
        except Exception as e:
            print(e)

    def sendResults(result: FaceLandmarkerResult, output_image: mp.Image, timestamp_ms: int):
        global client, faceLandmarks

        # clear cache
        faceLandmarks = None

        try:
            if client is None:
                return

            if len(result.face_blendshapes) == 0:
                return

            if len(result.facial_transformation_matrixes) == 0:
                return

            if len(result.face_landmarks) == 0:
                return

            faceData = FaceData()

            # head rotation
            extractRotation(faceData.headRotation, result.facial_transformation_matrixes[0])

            # blend shape value
            for blendShapeData in result.face_blendshapes[0]:
                item = faceData.blendShapes.add()
                item.name = blendShapeData.category_name
                item.value = round(blendShapeData.score, 4)

            # send
            byteData = faceData.SerializeToString()
            client.send(len(byteData).to_bytes(4, 'little', signed=True))
            client.send(byteData)

            # face landmarks
            tempList = landmark_pb2.NormalizedLandmarkList()
            tempList.landmark.extend([
                landmark_pb2.NormalizedLandmark(x=landmark.x, y=landmark.y, z=landmark.z)
                for landmark in result.face_landmarks[0]
            ])
            faceLandmarks = tempList #! 处理线程竞争

            # print(timestamp_ms)
        except Exception as e:
            client = None
            faceLandmarks = None
            print(e)

    landmarkerOptions = FaceLandmarkerOptions(
        base_options=BaseOptions(model_asset_path=r"./models/face_landmarker.task"),
        running_mode=VisionRunningMode.LIVE_STREAM,
        min_face_detection_confidence = 0.8,
        output_face_blendshapes=True,
        output_facial_transformation_matrixes=True,
        result_callback=sendResults)

    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server:
        server.bind(('', 5000))
        server.listen(1)
        print(f'Start listening {server.getsockname()}...')

        while True:
            client, addr = server.accept()
            print(f'Accept {addr}.')

            cap = cv2.VideoCapture(0)
            # cap = cv2.VideoCapture('https://10.198.206.155:8080/video')
            landmarker = FaceLandmarker.create_from_options(landmarkerOptions)
            cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_AUTOSIZE)
            # cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_NORMAL)
            cv2.setWindowProperty(WINDOW_NAME, cv2.WND_PROP_TOPMOST, 1)

            try:
                startTime = datetime.now()

                while client is not None:
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
            finally:
                cap.release()
                landmarker.close()
                cv2.destroyWindow(WINDOW_NAME)
