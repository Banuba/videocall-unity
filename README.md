# Face AR SDK for Unity Videocall example  
  
**Important**  
Please use [v0.x](../../tree/v0.x) branch for SDK version 0.x (e.g. v0.38).  
  
The example of Banuba SDK and Agora.io SDK integration to enable augmented reality filters in video calls for Unity.  
  
## Getting Started

1) Get the latest Banuba SDK archive for Unity and the client token. Please fill in our form on banuba.com website, or contact us via info@banuba.com. Copy and Past BanubaSDK/Assets folder to the videocall-unity/Assets/BanubaSDK folder.
2) Download the Agora SDK package in either of the following two ways:
    - From Unity Asset Store download and import the Agora Video SDK
    - Download the Agora Gaming SDK from Agora.io SDK. Unzip the downloaded SDK package and copy the files from samples/Hello-Video-Unity-Agora/Assets/AgoraEngine/ in SDK to videocall-unity/Assets in project
3) Move StreamingAssets folder from the videocall-unity/Assets/BanubaSDK to the videocall-unity/Assets folder.
4) Visit agora.io to sign up and get token, app and channel ID
5) Find the Assets/BanubaSDK/BanubaFaceAR/BaseAssets/Resources/BanubaClientToken.txt and past your client token here.
6) Open the the project in the Unity Editor and open the scene VideoCallDemo/demo/MainScene.scene
7) Find the the VideoCanvas object in the scene and set AppID, Token(optional) and your channel name in the properties of the DemoVideoCall script.
8) Run the project in the Editor.

## Help Resources

 - https://docs.banuba.com/face-ar-sdk-v1/unity/unity_getting_started
 - https://www.agora.io/en/blog/agora-video-sdk-for-unity-quick-start-programming-guide/

## Developer Environment Requirements

Minimum Unity Editor Version: 2019.3.10f1

## License

MIT

