using TapTap.Common.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TapTap.Login.Editor
{
    public static class TapLoginIOSProcessor
    {
        [PostProcessBuild(102)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) return;
            
            var unityAppControllerPath = path + "/Classes/UnityAppController.mm";
            var unityAppController = new TapFileHelper(unityAppControllerPath);
            unityAppController.WriteBelow(@"#import <OpenGLES/ES2/glext.h>", @"#import <TapLoginSDK/TapLoginHelper.h>");
            unityAppController.WriteBelow(
                @"id sourceApplication = options[UIApplicationOpenURLOptionsSourceApplicationKey], annotation = options[UIApplicationOpenURLOptionsAnnotationKey];",
                @"if(url){[TapLoginHelper handleTapTapOpenURL:url];}");
            Debug.Log("TapLogin Change AppControler File!");
        }
    }
}