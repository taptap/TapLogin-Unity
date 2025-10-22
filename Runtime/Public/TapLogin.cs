using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TapTap.Common;
using TapTap.Login.Internal;
using TapTap.Common.Internal.Utils;
using System;
using TapTap.Common.Internal;

namespace TapTap.Login
{
    public class TapLogin
    {
        public const string TAP_LOGIN_SCOPE_BASIC_INFO = "basic_info";
        public const string TAP_LOGIN_SCOPE_PUBLIC_PROFILE = "public_profile";
        public const string TAP_LOGIN_SCOPE_USER_FRIENDS = "user_friends";
        public const string TAP_LOGIN_SCOPE_EMAIL = "email";
        public const string TAP_LOGIN_SCOPE_PHONE = "phone";
        public const string TAP_LOGIN_SCOPE_COMPLIANCE = "compliance";
        public const string TAP_LOGIN_SCOPE_COMPLIANCE_BASIC = "compliance_basic";
        private static HashSet<string> defaultPermissions;

        public static HashSet<string> DefaultPermissions {
            get {
                if (defaultPermissions == null) {
                    defaultPermissions = new HashSet<string>();
                }
                return defaultPermissions;
            }
        }

        private static ITapLoginPlatform platformWrapper;

        static TapLogin() {
            platformWrapper = BridgeUtils.CreateBridgeImplementation(typeof(ITapLoginPlatform),
                "TapTap.Login") as ITapLoginPlatform;
        }

        public static void Init(string clientID) {
            InitDuration(clientID, true);
            TapTapSdk.SDKInitialize(clientID, true);
            platformWrapper.Init(clientID);
        }

        public static void Init(string clientID, bool isCn, bool roundCorner) {
            InitDuration(clientID, isCn);
            TapTapSdk.SDKInitialize(clientID, isCn);
            platformWrapper.Init(clientID, isCn, roundCorner);
        }

        private static void InitDuration(string clientId, bool isCn) {
             if(TapCommon.Config == null){
                var tapConfig = new TapConfig.Builder()
                    .ClientID(clientId)
                    .ClientToken("")
                    .RegionType(isCn ? RegionType.CN : RegionType.IO)
                    .ConfigBuilder();
                TapCommon.Init(tapConfig);
            }
        }
        
        

        public static void ChangeConfig(bool roundCorner, bool isPortrait) {
            platformWrapper.ChangeConfig(roundCorner, isPortrait);
        }

        public static Task<Profile> FetchProfile() {
            return platformWrapper.FetchProfile();
        }

        public static Task<Profile> GetProfile() {
            return platformWrapper.GetProfile();
        }

        public static Task<AccessToken> GetAccessToken() {
            return platformWrapper.GetAccessToken();
        }
        
        public static Task<AccessToken> Login() {
            var result = platformWrapper.Login();
            _ = TriggerTapLogin(result);
            return result;
        }

        public static Task<AccessToken> Login(string[] permissions) {
            
            var result = platformWrapper.Login(permissions);
            _ = TriggerTapLogin(result);
            return result;
        }
        

        public static Task<AccessToken> Login(TapLoginPermissionConfig config) {
            Task<AccessToken> result = platformWrapper.Login(config);
            _ = TriggerTapLogin(result);
            return result;
        }

        public static Task<AccessToken> Authorize(string[] permissions = null) {
            return platformWrapper.Authorize(permissions);
        }

        public static void Logout() {
            platformWrapper.Logout();
        }

        public static void AppendPermission(string permission) {
            if (string.IsNullOrWhiteSpace(permission)) {
                return;
            }
            DefaultPermissions.Add(permission);
        }
        
        public static void RemovePermission(string permission) {
            if (string.IsNullOrWhiteSpace(permission)) {
                return;
            }
            DefaultPermissions.Remove(permission);
        }
        
        private static async Task TriggerTapLogin(Task<AccessToken> accessTokenTask) {
            _ = await accessTokenTask;
        }
        
    }
}