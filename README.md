# StarRailMotionCapture

Motion capture for the character models of Honkai: Star Rail base on Unity and MediaPipe. **Currently face only.**

![example](/Screenshots~/example.png)

## Known limitations

- Face capture only supports character models **with facial skeleton**, e.g., the models ripped directly from StarRail.

## Protocol

Protocols between the server and the client are written in protobuf and compiled using my customized version of [protoc](https://github.com/stalomeow/protobuf).

## Server

**Developed with Python 3.10.**

It captures motion data using your webcam or from a video file and sends them to clients.

### Setup

All the requirements are listed in [Server/requirements.txt](/Server/requirements.txt).

Enter the [Server](/Server) folder and create a virtual environment. Then, run the command below.

``` bash
pip install -r requirements.txt
```

### Edit config

Edit [Server/src/config.py](/Server/src/config.py).

### Start the server

Run [Server/src/main.py](/Server/src/main.py).

## Client

**Developed with Unity 2022.3.**

### Demo

A demo unity project is provided in [Client/StarRailMotionCapture](Client/StarRailMotionCapture) folder.

### Create custom blend shape asset

1. Create asset using context menu.

    ![create-bs-asset](/Screenshots~/create_bs_asset.png)

2. Double click the asset to open the editor. Assign the face renderer of the character to `Debug Skinned Mesh Renderer`.

    ![blend-shape-editor](/Screenshots~/blend_shape_editor.png)

3. This editor supports Record/Preview mode like Unity's AnimationWindow, so you can use it as if you were using AnimationWindow.

4. Use the tools to increase efficiency.

    ![blend-shape-editor-tools](/Screenshots~/blend_shape_editor_tools.png)
