import cv2

class CVWindow(object):
    def __init__(self, windowName: str, topmost: bool, enable: bool):
        self._windowName = windowName
        self._topmost = topmost
        self._enable = enable
        self._hasWindow = False

    def showImage(self, img):
        if not self._enable:
            return

        if not self._hasWindow:
            cv2.namedWindow(self._windowName, cv2.WINDOW_NORMAL)
            if self._topmost:
                cv2.setWindowProperty(self._windowName, cv2.WND_PROP_TOPMOST, 1)
            self._hasWindow = True

        cv2.imshow(self._windowName, img)

    def hide(self):
        if not self._enable:
            return

        if not self._hasWindow:
            return

        cv2.destroyWindow(self._windowName)
        self._hasWindow = False