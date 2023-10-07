SERVER_CONFIG = {
    'port': 5000,
    'heartBeatTimeoutSecs': 10.0,
    'recvBufferSize': 2048,
}

CAPTURE_CONFIG = {
    'cameraIndexOrVideoFileName': 0,
    # 'cameraIndexOrVideoFileName': 1,
    # 'cameraIndexOrVideoFileName': r'./Server/test/ikun1.mp4',
}

WINDOW_CONFIG = {
    'name': 'Live',
    'topmost': True,
    'enable': True,
}

def getLandmarker(server):
    import landmarkers as ls

    return ls.LandmarkerGroup(
        ls.FaceLandmarker(server, model_asset_path=r'./Server/models/face_landmarker.task'),
        ls.PoseLandmarker(server, model_asset_path=r'./Server/models/pose_landmarker_heavy.task'),
    )
