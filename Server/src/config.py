def _src(*paths):
    import os
    srcFolder = os.path.dirname(__file__)
    return os.path.normpath(os.path.join(srcFolder, *paths))

# 服务器设置
SERVER_CONFIG = {
    # 端口号
    'port': 5000,

    # 心跳包超时秒数
    # 超过指定时间收不到一个客户端的心跳包，该客户端就被判定为掉线
    'heartBeatTimeoutSecs': 10.0,

    # 接收消息的缓冲区的大小
    'recvBufferSize': 2048,
}

# 捕获设置
CAPTURE_CONFIG = {
    # 摄像头设备索引，或者视频文件的名称
    # 参考 OpenCV 的 VideoCapture
    'cameraIndexOrVideoFileName': 0,
}

# 视频窗口设置
WINDOW_CONFIG = {
    # 窗口名称
    'name': 'Live',

    # 是否顶置窗口
    'topmost': True,

    # 是否启用窗口
    'enable': True,
}

# 用到的 Landmarker
def getLandmarker(server):
    import landmarkers as ls

    return ls.LandmarkerGroup(
        ls.FaceLandmarker(server, model_asset_path=_src(r'../models/face_landmarker.task')),
        # ls.PoseLandmarker(server, model_asset_path=_src(r'../models/pose_landmarker_heavy.task')),
    )
