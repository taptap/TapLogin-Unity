using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TapTap.Common;
using TapTap.Common.Internal.Utils;
using TapTap.Login.Internal;
using TapTap.Login.Internal.Http;
using UnityEngine;

namespace TapTap.Login.Standalone {
    public class TapLoginStandalone : ITapLoginPlatform {

        private static bool isOverseas = false;
        internal static bool IsOverseas => isOverseas;
        public async void Init(string clientID) {
            await CheckAndRefreshToken(clientID);
        }

        public async void Init(string clientID, bool isCn, bool roundCorner) {
            isOverseas = !isCn;
            await CheckAndRefreshToken(clientID);
        }

        public void ChangeConfig(bool roundCorner, bool isPortrait) {
        }

        public Task<Profile> FetchProfile() {
            return LoginHelper.GetProfile();
        }

        public Task<Profile> GetProfile() {
            return LoginHelper.GetProfile();
        }

        public Task<AccessToken> GetAccessToken() {
            return LoginHelper.GetAccessToken();
        }
        
        public Task<AccessToken> Authorize(string[] permissions = null) {
            List<string> allPermissions = MergePerssions(permissions, false);
            return AuthorizeInternal(allPermissions);
        }

        public Task<AccessToken> Login() {
            return Login(new string[] {});
        }
        private List<string> MergePerssions(string[] permissions, bool isLogin = true){
            HashSet<string> allPermissions = new HashSet<string>();
            
            if (isLogin)
                allPermissions.Add(TapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE);
            
            if (permissions != null) {
                allPermissions.UnionWith(permissions);
            }

            if (TapLogin.DefaultPermissions != null) {
                allPermissions.UnionWith(TapLogin.DefaultPermissions);
            }
            
            if (IsOversea()) {
                allPermissions.Remove(TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE);
            }
            
            return allPermissions.ToList();
        }

        private static bool IsOversea() {
            if (TapCommon.Config != null)
                return TapCommon.Config.RegionType == RegionType.IO;
            return !TapTapSdk.CurrentRegion.WebHost().Contains(".cn");
        }
        public async Task<AccessToken> Login(string[] permissions) {
            List<string> allPermissions = MergePerssions(permissions);
            AccessToken token = await AuthorizeInternal(allPermissions);
            Profile profile = null;
            try {
                ProfileData profileData = await LoginService.GetProfile(TapTapSdk.ClientId, token);
                profile = ConvertToProfile(profileData);

                SaveTapUser(token, profile);
            } catch (TapException e) {
                throw e;
            } catch (Exception) {
                throw new TapException((int) TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error");
            }
            return token;
        }

        public async Task<AccessToken> Login(TapLoginPermissionConfig config) { 
            Tuple<AccessToken, Profile> result = await AuthorizeInternal(config);
            SaveTapUser(result.Item1, result.Item2);
            return result.Item1;
        }

        public void Logout() {
            LoginHelper.Logout();
        }

        private Task<AccessToken> AuthorizeInternal(IEnumerable<string> permissions) {
            TaskCompletionSource<AccessToken> tcs = new TaskCompletionSource<AccessToken>();
            LoginPanelController.OpenParams openParams = new LoginPanelController.OpenParams {
                ClientId = TapTapSdk.ClientId,
                Permissions = new HashSet<string>(permissions).ToArray(),
                OnAuth = tokenData => {
                    if (tokenData == null) {
                        tcs.TrySetException(new TapException((int) TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error"));
                    } else {
                        // 将 TokenData 转化为 AccessToken
                        AccessToken accessToken = ConvertToAccessToken(tokenData);
                        tcs.TrySetResult(accessToken);
                    }
                },
                OnError = e => {
                    tcs.TrySetException(e);
                },
                OnClose = () => {
                    tcs.TrySetException(
                        new TapException((int) TapErrorCode.ERROR_CODE_LOGIN_CANCEL, "Login Cancel"));
                }
            };
            TapTap.UI.UIManager.Instance.OpenUI<LoginPanelController>("Prefabs/TapLogin/LoginPanel", openParams);
            return tcs.Task;
        }

        private Task<Tuple<AccessToken, Profile>> AuthorizeInternal(TapLoginPermissionConfig config) {
            TaskCompletionSource<Tuple<AccessToken, Profile>> tcs = new TaskCompletionSource<Tuple<AccessToken, Profile>>();
            LoginWithPermissionsPanelController.OpenParams openParams = new LoginWithPermissionsPanelController.OpenParams {
                ClientId = TapTapSdk.ClientId,
                Name = config.Name,
                Permissions = config.Permissions,
                OnAuth = (tokenData, profileData) => {
                    // 将 TokenData 转化为 AccessToken
                    AccessToken accessToken = ConvertToAccessToken(tokenData);
                    Profile profile = ConvertToProfile(profileData);
                    tcs.TrySetResult(new Tuple<AccessToken, Profile>(accessToken, profile));
                }
            };
            TapTap.UI.UIManager.Instance.OpenUI<LoginWithPermissionsPanelController>("Prefabs/TapLogin/LoginWithPermissionPanel", openParams);
            return tcs.Task;
        }

        private static AccessToken ConvertToAccessToken(TokenData tokenData) {
            return new AccessToken {
                kid = tokenData.Kid,
                accessToken = tokenData.Token,
                tokenType = tokenData.TokenType,
                macKey = tokenData.MacKey,
                macAlgorithm = tokenData.MacAlgorithm,
                scopeSet = tokenData.Scopes
            };
        }

        private static Profile ConvertToProfile(ProfileData profileData) {
            return new Profile {
                openid = profileData.OpenId,
                unionid = profileData.UnionId,
                name = profileData.Name,
                avatar = profileData.Avatar,
                gender = profileData.Gender
            };
        }

        private static void SaveTapUser(AccessToken accessToken, Profile profile) {
            DataStorage.SaveString("taptapsdk_accesstoken", JsonConvert.SerializeObject(accessToken));
            DataStorage.SaveString("taptapsdk_profile", JsonConvert.SerializeObject(profile));
        }

        /**
        * 检查 token 是否有效及 refresh  token
        */
        private async Task CheckAndRefreshToken(string clientId){
            Debug.Log("start CheckAndRefreshToken");
            try{
                AccessToken accessToken = await GetAccessToken();
                Debug.Log("start CheckAndRefreshToken currentToken = " + accessToken);
                if(accessToken != null){
                    TokenData tokenData = null;
                    try{
                        tokenData =  await LoginService.RefreshToken(clientId, accessToken.accessToken);
                        Debug.Log("start CheckAndRefreshToken check result = " + tokenData);
                    }catch(TapException e){
                        Debug.Log("start CheckAndRefreshToken check fail code = " + e.code + " msg = " + e.message);
                        //清除本地缓存
                        if(e.code < 0 ){
                            Debug.Log("start CheckAndRefreshToken clear local token");
                            Logout();
                        }
                        return;
                    }
                    AccessToken refreshToken = ConvertToAccessToken(tokenData);
                    if(refreshToken != null){
                        Debug.Log("refresh token success");
                        Profile profile = await GetProfile();
                        if(profile == null){
                            Debug.Log("local don't hava valid profile so fetch");
                            ProfileData profileData = await LoginService.GetProfile(TapTapSdk.ClientId, refreshToken);
                            profile = ConvertToProfile(profileData);
                        }
                        if(profile != null){
                            Debug.Log("save refresh token and profile ");
                            SaveTapUser(refreshToken, profile);
                        }  
                    }
                }else{
                    Debug.Log("local don't have accessToken");
                }
            }catch(Exception e){
                Debug.Log("refresh TapToken fail reason : " + e.Message + "\n stack = " + e.StackTrace);
            }
        }
    }
}
