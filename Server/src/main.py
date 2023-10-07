import os
import sys

def _addProtoImportPath():
    srcFolder = os.path.dirname(__file__)
    sys.path.insert(1, os.path.join(srcFolder, r'protos'))

def main():
    _addProtoImportPath()

    import time
    import cv2
    import mediapipe as mp
    import config
    import cvutils
    import handlers
    import server as sv

    capture = cvutils.LazyLiveCapture(**config.CAPTURE_CONFIG)
    window = cvutils.LazyLiveWindow(**config.WINDOW_CONFIG)
    server = sv.UDPServer(**config.SERVER_CONFIG)
    landmarker = config.getLandmarker(server)

    try:
        server.start()

        while True:
            server.tick()

            if server.clientCount <= 0:
                capture.release()
                window.close()
                landmarker.stop()
                time.sleep(1)
                continue

            success, img = capture.read()
            if not success:
                exit(-1)

            landmarker.detect(mp.Image(image_format=mp.ImageFormat.SRGB, data=img))
            landmarker.drawLandmarks(img)
            window.showImage(img)

            if (cv2.waitKey(1) & 0xFF) == ord('q'):
                exit()
    finally:
        capture.release()
        window.close()
        landmarker.stop()
        server.close()

if __name__ == '__main__':
    main()
