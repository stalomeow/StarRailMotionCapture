import os
import sys
sys.path.append(os.path.join(os.path.dirname(__file__), r'protos'))

import cv2
import config
import mediapipe as mp
import time

from cvWindow import CVWindow
from landmarkerWrapper import LandmarkerWrapperGroup
from udpServer import UDPServer

def main():
    config.importServerPacketHandlers()

    with UDPServer(config.SERVER_UDP_PORT) as server:
        cap = None
        window = CVWindow(config.WINDOW_NAME, config.WINDOW_TOPMOST, config.WINDOW_ENABLE)
        landmarkers = LandmarkerWrapperGroup(*(ty(server) for ty in config.LANDMARKER_TYPES))

        while True:
            server.kickOfflineClients()

            if server.clientCount <= 0:
                if cap is not None:
                    cap.release()
                    cap = None
                window.hide()
                landmarkers.stop()
                time.sleep(1)
                continue

            if cap is None:
                cap = cv2.VideoCapture(*config.VIDEO_CAPTURE_ARGS)
            success, img = cap.read()

            if not success:
                exit(-1)

            landmarkers.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=img))
            landmarkers.drawLandmarks(img)
            window.showImage(img)

            if (cv2.waitKey(1) & 0xFF) == ord('q'):
                exit()

if __name__ == '__main__':
    main()
