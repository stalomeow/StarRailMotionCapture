import cv2
import functools

class LazyLiveCapture(object):
    def __init__(self, cameraIndexOrVideoFileName: int | str):
        self._createCap = functools.partial(cv2.VideoCapture, cameraIndexOrVideoFileName)
        self._cap = None

    def read(self):
        if self._cap is None:
            self._cap = self._createCap()
        return self._cap.read()

    def release(self):
        if self._cap is None:
            return
        self._cap.release()
        self._cap = None

class LazyLiveWindow(object):
    def __init__(self, name: str, topmost: bool, enable: bool):
        self._name = name
        self._topmost = topmost
        self._enable = enable
        self._hasWindow = False

    def showImage(self, img):
        if not self._enable:
            return

        if not self._hasWindow:
            cv2.namedWindow(self._name, cv2.WINDOW_NORMAL)
            if self._topmost:
                cv2.setWindowProperty(self._name, cv2.WND_PROP_TOPMOST, 1)
            self._hasWindow = True

        cv2.imshow(self._name, img)

    def close(self):
        if not self._enable:
            return

        if not self._hasWindow:
            return

        cv2.destroyWindow(self._name)
        self._hasWindow = False
