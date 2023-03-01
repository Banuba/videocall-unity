# Face AR SDK for Unity Videocall example   
  
The example of Banuba SDK and Agora.io SDK integration to enable augmented reality filters in video calls for Unity.  
  
## Getting Started

1) Get the client token for Banuba SDK. Please fill in our form on banuba.com website, or contact us via info@banuba.com. 
2) Open the the project.
3) Download and import the [Agora SDK package from Unity Asset Store](https://assetstore.unity.com/packages/tools/video/agora-video-sdk-for-unity-134502) with Unity Package Manager.
4) Download and import the [BanubaSDK-vx.x.x-import.unitypackage](https://github.com/Banuba/quickstart-unity/releases)
4) Visit agora.io to sign up and get token, app and channel ID.
5) Find the Assets/Resources/BanubaClientToken.txt and past your client token here.
6) Open the scene VideoCallDemo/demo/MainScene.scene.
7) Find the the VideoCanvas object in the scene and set AppID, Token, and your channel name in the properties of the DemoVideoCall script.
8) Run the project in the Editor.
9) For Android/Windows Standalone build you need to add Assets/BanubaFaceAR/BaseAssets/Scenes/LoaderScene and change property 'Scene' of SceneLoader.cs of Canvas entity to 'MainScene'

## Help Resources

 - https://docs.banuba.com/face-ar-sdk-v1/unity/unity_getting_started
 - https://www.agora.io/en/blog/agora-video-sdk-for-unity-quick-start-programming-guide/

## Developer Environment Requirements

Minimum Unity Editor Version: 2019.3.10f1

## License

MIT

