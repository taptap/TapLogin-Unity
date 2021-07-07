using System.IO;
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

            var parentFolder = Directory.GetParent(Application.dataPath).FullName;

            var plistFile = TapFileHelper.RecursionFilterFile(parentFolder + "/Assets/Plugins/", "TDS-Info.plist");

            if (!plistFile.Exists)
            {
                Debug.LogError("TapLogin Can't find TDS-Info.plist in Project/Assets/Plugins/!");
            }

            TapCommonCompile.HandlerPlist(path, plistFile.FullName);

            // UnityAppController.mm 中对于 URLScheme 的处理
            var unityAppControllerPath = path + "/Classes/UnityAppController.mm";
            var unityAppController = new TapFileHelper(unityAppControllerPath);
            unityAppController.WriteBelow(@"#import ""UnityAppController.h""",
                @"#import <TapLoginSDK/TapLoginHelper.h>");
            unityAppController.WriteBelow(
                @"id sourceApplication = options[UIApplicationOpenURLOptionsSourceApplicationKey], annotation = options[UIApplicationOpenURLOptionsAnnotationKey];",
                @"if(url){[TapLoginHelper handleTapTapOpenURL:url];}");

            Debug.Log("TapLogin Change AppController File!");
        }
    }
}