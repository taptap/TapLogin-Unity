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
            
            var parentFolder = Directory.GetParent(Application.dataPath)?.FullName;

            var plistFile = TapFileHelper.RecursionFilterFile(parentFolder + "/Assets/Plugins/", "TDS-Info.plist");

            if (!plistFile.Exists)
            {
                Debug.LogError("TapSDK Can't find TDS-Info.plist in Project/Assets/Plugins/!");
            }

            TapCommonCompile.HandlerPlist(path, plistFile.FullName);
        }
    }
}