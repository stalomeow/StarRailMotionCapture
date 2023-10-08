# StarRailMotionCapture

Motion capture for the character models of Honkai: Star Rail base on Unity and MediaPipe.

**Currently face only.**

## Known limitations

- Face capture only supports character models **with facial skeleton**, e.g., the models ripped directly from StarRail.

## Protocol

Protocols between the server and the client are written in protobuf and compiled using my customized version of [protoc](https://github.com/stalomeow/protobuf).

## Server

**Developed with Python 3.10.**

It captures motion data from your webcam or a video file and sends them to clients.

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

### Requirements

- My [Protobuf-Unity](https://github.com/stalomeow/Protobuf-Unity) package.

### Setup

1. Prepare a character model in the scene.

2. Prepare a blend shape asset to drive the character's facial expression.

    If you are using a datamined model, after setting its `Scale Factor` to `150` in the Import Settings, you can directly use the blend shape asset in the [Client/BlendShapes](/Client/BlendShapes) folder.

    To create a custom blend shape asset, please read the '[Create Custom BlendShape Asset](#create-custom-blendshape-asset)' section.

3. Add a `Motion Actor (Game Model)` component to your character.

    ![setup-model](/Screenshots~/setup_model.png)

4. Add a `UDP Session` component to the scene and set the server's address and port. Then, drag your actors onto `Actors` field.

    ![setup-session](/Screenshots~/setup_session.png)

### Run the client

Simply enter play mode in your Unity editor.

### Create Custom BlendShape Asset

1. Create using context menu.

    ![create-bs-asset](/Screenshots~/create_bs_asset.png)

2. Double click the asset to open the editor. Assign the face renderer of the character to `Debug Skinned Mesh Renderer`.

    ![blend-shape-editor](/Screenshots~/blend_shape_editor.png)

3. This editor supports Record/Preview mode like Unity's AnimationWindow, so you can use it as if you were using AnimationWindow.

4. Use the tools to increase efficiency.

    ![blend-shape-editor-tools](/Screenshots~/blend_shape_editor_tools.png)
