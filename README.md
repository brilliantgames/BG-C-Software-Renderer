Download the full Unity Project with examples scenes here: https://drive.google.com/file/d/1JKf1ZW7W_OUqzsKWVguHe41XVPzO5iWl/view?usp=drive_link

Download the demo builds here: https://drive.google.com/file/d/1t6gpV3ZIbOMLGHG3TpkWAMJzOXDZvr97/view?usp=drive_link 

Important Note About Unity:

Note: We highly reccomend downloading the full project with the link above. That said, to run this code in Unity, simply copy the 'Raster Engine' folder into your Unity project, it already contains the dll C++ engine in it's folder.  The Unity project provided is made in Unity 2018.4.2.6.  
However, it should still work in newer versions of Unity.

Support:
Since we are a full time game studio with a small team, we cannot afford time to offer support, this is why we made multiple demo scenes in Unity to show how everything works. We also added 
sufficient commenting and tips to the code itself.

Basic Setup:
-To set up a new scene, all that is required is that BG camera being attach to your main camera, as well a directional light must be assigned.  After that, you may add the BGRenderer script
to any of your mesh renderers in the scene and it should render that mesh given it's readable.
-Adding textures is simple.  Just assign a texture to the texture slot in the BG Renderer.  The textuer MUST be set to readable in the import settings.

C++ project.
All the rendering is done in a C++ DLL pluggin that Unity calls on.  We included the C++ code in the source files in the 'C++ Source Project' folder.  If you make any modifications
you will need to export the C++ dll to your Unity project(If you want to run it in Unity).  Make sure that if you are modifying the C++ 'mainengine' code in your own new C++ project that you enable
all the most ideal floating point optimization settings as well as debugging being disabled, otherwise the engine will run slower. If you are unsure about what settings to use, ask ChatGPT 
for help on that. 
