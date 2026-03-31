using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LC.Newtonsoft.Json;
using TapTap.Common;
using TapTap.Common.Internal.Utils;
using TapTap.Login.Internal;
using TapTap.Login.Internal.Http;
using UnityEngine;
using TapTap.Common.Standalone;
using System.Collections.Specialized;
using System.Threading;
using TapTap.Login;

namespace TapTap.Login.Standalone
{
    public class TapLoginStandalone : ITapLoginPlatform
    {

        private static bool isOverseas = false;
        internal static bool IsOverseas => isOverseas;

        // 本地缓存的用户信息是否和 Tap 启动器一致
        internal static bool isCacheUserSameWithTapClient = true;
        public async void Init(string clientID)
        {
            TapLogger.Debug("RegisterListenerForTapClientCheck ");
            RegisterListenerForTapClientCheck();
            await CheckAndRefreshToken(clientID);

        }

        public async void Init(string clientID, bool isCn, bool roundCorner)
        {
            isOverseas = !isCn;
            TapLogger.Debug("RegisterListenerForTapClientCheck ");
            RegisterListenerForTapClientCheck();
            await CheckAndRefreshToken(clientID);

        }

        public void ChangeConfig(bool roundCorner, bool isPortrait)
        {
        }

        public Task<Profile> FetchProfile()
        {
            return LoginHelper.GetProfile();
        }

        public Task<Profile> GetProfile()
        {
            return LoginHelper.GetProfile();
        }

        public Task<AccessToken> GetAccessToken()
        {
            return LoginHelper.GetAccessToken();
        }

        public Task<AccessToken> Authorize(string[] permissions = null)
        {
            List<string> allPermissions = MergePerssions(permissions, false);
            return AuthorizeInternal(allPermissions);
        }

        public Task<AccessToken> Login()
        {
            return Login(new string[] { });
        }
        private List<string> MergePerssions(string[] permissions, bool isLogin = true)
        {
            HashSet<string> allPermissions = new HashSet<string>();

            if (isLogin)
                allPermissions.Add(TapLogin.TAP_LOGIN_SCOPE_PUBLIC_PROFILE);

            if (permissions != null)
            {
                allPermissions.UnionWith(permissions);
            }

            if (TapLogin.DefaultPermissions != null)
            {
                allPermissions.UnionWith(TapLogin.DefaultPermissions);
            }

            if (IsOversea())
            {
                allPermissions.Remove(TapLogin.TAP_LOGIN_SCOPE_COMPLIANCE);
            }

            return allPermissions.ToList();
        }

        private static bool IsOversea()
        {
            if (TapCommon.Config != null)
                return TapCommon.Config.RegionType == RegionType.IO;
            return !TapTapSdk.CurrentRegion.WebHost().Contains(".cn");
        }
        public async Task<AccessToken> Login(string[] permissions)
        {
            List<string> allPermissions = MergePerssions(permissions);
            AccessToken token = await AuthorizeInternal(allPermissions);
            Profile profile = null;
            try
            {
                ProfileData profileData = await LoginService.GetProfile(TapTapSdk.ClientId, token);
                profile = ConvertToProfile(profileData);

                SaveTapUser(token, profile);
            }
            catch (TapException e)
            {
                throw e;
            }
            catch (Exception)
            {
                throw new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error");
            }
            TapLogger.Debug("LoginWithScopes end login  mainthread = " + Thread.CurrentThread.ManagedThreadId);
            return token;
        }

        public async Task<AccessToken> Login(TapLoginPermissionConfig config)
        {
            Tuple<AccessToken, Profile> result = await AuthorizeInternal(config);
            SaveTapUser(result.Item1, result.Item2);
            return result.Item1;
        }

        public void Logout()
        {
            LoginHelper.Logout();
        }

        private Task<AccessToken> AuthorizeInternal(IEnumerable<string> permissions)
        {

            // 是否使用 Tap 启动器登录
            bool isNeedLoginByClient = false;
#if UNITY_STANDALONE_WIN
            isNeedLoginByClient = TapClientStandalone.IsNeedLoginByTapClient();
            
            if (isNeedLoginByClient)
            {
                async Task<AccessToken> innerLogin()
                {
                    try
                    {
                        AccessToken token = await AuthorizeByTapPCClient(permissions.ToArray());
                        return token;
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                return innerLogin();
            }
#endif
            TaskCompletionSource<AccessToken> tcs = new TaskCompletionSource<AccessToken>();
            LoginPanelController.OpenParams openParams = new LoginPanelController.OpenParams
            {
                ClientId = TapTapSdk.ClientId,
                Permissions = new HashSet<string>(permissions).ToArray(),
                OnAuth = tokenData =>
                {
                    if (tokenData == null)
                    {
                        tcs.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, "UnKnow Error"));
                    }
                    else
                    {
                        // 将 TokenData 转化为 AccessToken
                        AccessToken accessToken = ConvertToAccessToken(tokenData);
                        tcs.TrySetResult(accessToken);
                    }
                },
                OnError = e =>
                {
                    tcs.TrySetException(e);
                },
                OnClose = () =>
                {
                    tcs.TrySetException(
                        new TapException((int)TapErrorCode.ERROR_CODE_LOGIN_CANCEL, "Login Cancel"));
                }
            };
            TapTap.UI.UIManager.Instance.OpenUI<LoginPanelController>("Prefabs/TapLogin/LoginPanel", openParams);
        
            return tcs.Task;
        }

#if UNITY_STANDALONE_WIN   
        private async Task<AccessToken> AuthorizeByTapPCClient(string[] permissions = null) {
            string info = "{\"device_id\":\"" + SystemInfo.deviceModel + "\"}";
            string sdkUA = "client_id=" + TapTapSdk.ClientId + "&uuid=" + SystemInfo.deviceUniqueIdentifier;
            TapLogger.Debug("LoginWithScopes start in mainthread = " + Thread.CurrentThread.ManagedThreadId);
            TaskCompletionSource<AccessToken> taskCompletionSource = new TaskCompletionSource<AccessToken>();
            
            string responseType = "code";
            string redirectUri = "tapoauth://authorize";
            string state = Guid.NewGuid().ToString("N");
            string codeVerifier = CodeUtil.GenerateCodeVerifier();
            string codeChallenge = CodeUtil.GetCodeChallenge(codeVerifier);
            string versionCode = TapCommon.SDKVersion;
            string codeChallengeMethod = "S256";
            TapLoginClientBridge.TapLoginResponseByTapClient response = await TapLoginClientBridge.LoginWithScopesAsync(permissions,
            responseType, redirectUri, codeChallenge, state, codeChallengeMethod, versionCode, sdkUA, info);
            TapLogger.Debug("start handle login result");
            TapLogger.Debug("LoginWithScopes handle in mainthread = " + Thread.CurrentThread.ManagedThreadId);

            if (response.isCancel)
            {
                taskCompletionSource.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_LOGIN_CANCEL, "Login Cancel"));
            }
            else if (response.isFail || string.IsNullOrEmpty(response.redirectUri))
            {
                taskCompletionSource.TrySetException(new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, response.errorMsg ?? "未知错误"));
            }
            else
            {
                TapLogger.Debug("login success prepare get token");
                try
                {
                    Uri uri = new Uri(response.redirectUri);
                    NameValueCollection queryPairs = UrlUtils.ParseQueryString(uri.Query);
                    string code = queryPairs["code"];
                    string uriState = queryPairs["state"];
                    string error = queryPairs["error"];
                    if (string.IsNullOrEmpty(error) && uriState == state && !string.IsNullOrEmpty(code))
                    {
                        TokenData tokenData = await LoginService.Authorize(TapTapSdk.ClientId, code, codeVerifier);
                        taskCompletionSource.TrySetResult( ConvertToAccessToken(tokenData));
                    }
                    else
                    {
                        TapLogger.Debug("login success prepare get token but get  error " + error);
                        throw new TapException((int)TapErrorCode.ERROR_CODE_UNDEFINED, error ?? "数据解析异常");
                    }
                }
                catch (Exception ex)
                {
                    TapLogger.Debug("login success prepare get token  fail " + ex.StackTrace);
                    taskCompletionSource.TrySetException(ex);
                }
            }

            return await taskCompletionSource.Task;
        }
#endif

        private Task<Tuple<AccessToken, Profile>> AuthorizeInternal(TapLoginPermissionConfig config)
        {
            TaskCompletionSource<Tuple<AccessToken, Profile>> tcs = new TaskCompletionSource<Tuple<AccessToken, Profile>>();
            LoginWithPermissionsPanelController.OpenParams openParams = new LoginWithPermissionsPanelController.OpenParams
            {
                ClientId = TapTapSdk.ClientId,
                Name = config.Name,
                Permissions = config.Permissions,
                OnAuth = (tokenData, profileData) =>
                {
                    // 将 TokenData 转化为 AccessToken
                    AccessToken accessToken = ConvertToAccessToken(tokenData);
                    Profile profile = ConvertToProfile(profileData);
                    tcs.TrySetResult(new Tuple<AccessToken, Profile>(accessToken, profile));
                }
            };
            TapTap.UI.UIManager.Instance.OpenUI<LoginWithPermissionsPanelController>("Prefabs/TapLogin/LoginWithPermissionPanel", openParams);
            return tcs.Task;
        }

        internal static AccessToken ConvertToAccessToken(TokenData tokenData)
        {
            return new AccessToken
            {
                kid = tokenData.Kid,
                accessToken = tokenData.Token,
                tokenType = tokenData.TokenType,
                macKey = tokenData.MacKey,
                macAlgorithm = tokenData.MacAlgorithm,
                scopeSet = tokenData.Scopes
            };
        }

        private static Profile ConvertToProfile(ProfileData profileData)
        {
            return new Profile
            {
                openid = profileData.OpenId,
                unionid = profileData.UnionId,
                name = profileData.Name,
                avatar = profileData.Avatar,
                gender = profileData.Gender
            };
        }

        private static void SaveTapUser(AccessToken accessToken, Profile profile)
        {
            DataStorage.SaveString("taptapsdk_accesstoken", JsonConvert.SerializeObject(accessToken));
            DataStorage.SaveString("taptapsdk_profile", JsonConvert.SerializeObject(profile));
        }
        /// <summary>
        /// 注册启动器检查完成事件，内部处理本地缓存与启动器用户信息的一致性问题
        /// </summary>
        private void RegisterListenerForTapClientCheck()
        {
            EventManager.AddListener(EventConst.IsLaunchedFromTapTapPCFinished, (openId) =>
            {
                TapLogger.Debug("receive IsLaunchedFromTapTapPCFinished event");
                if (openId is string userId && !string.IsNullOrEmpty(userId))
                {
                    CheckLoginStateWithTapClient(userId);
                }
            });
        }

        /// <summary>
        /// 校验缓存中的用户信息是否与 Tap 启动器中的一致, 不一致时清空本地缓存
        /// 本地无用户信息时，默认为与启动器一致
        /// </summary>
        /// <param name="openId"></param>
        private async void CheckLoginStateWithTapClient(string openId)
        {
            Profile profile = await GetProfile();
            if (profile != null)
            {
                if (profile.openid != openId)
                {
                    isCacheUserSameWithTapClient = false;
                    TapLogger.Debug("receive IsLaunchedFromTapTapPCFinished event and not same");
                    Logout();
                }
                else
                {
                    isCacheUserSameWithTapClient = true;
                }
            }
            else
            {
                isCacheUserSameWithTapClient = true;
            }
        }

        /**
        * 检查 token 是否有效及 refresh  token
        */
        private async Task CheckAndRefreshToken(string clientId)
        {
            TapLogger.Debug("start CheckAndRefreshToken");
            try
            {
                AccessToken accessToken = await GetAccessToken();
                TapLogger.Debug("start CheckAndRefreshToken currentToken = " + accessToken);
                if (accessToken != null)
                {
                    TokenData tokenData = null;
                    try
                    {
                        tokenData = await LoginService.RefreshToken(clientId, accessToken.accessToken);
                        TapLogger.Debug("start CheckAndRefreshToken check result = " + tokenData);
                    }
                    catch (TapException e)
                    {
                        TapLogger.Debug("start CheckAndRefreshToken check fail code = " + e.code + " msg = " + e.message);
                        //清除本地缓存
                        if (e.message != null && e.message.Equals("invalid_grant"))
                        {
                            TapLogger.Debug("start CheckAndRefreshToken clear local token");
                            Logout();
                        }
                        return;
                    }
                    AccessToken refreshToken = ConvertToAccessToken(tokenData);
                    if (refreshToken != null)
                    {
                        TapLogger.Debug("refresh token success");
                        Profile profile = await GetProfile();
                        if (profile == null)
                        {
                            TapLogger.Debug("local don't hava valid profile so fetch");
                            ProfileData profileData = await LoginService.GetProfile(TapTapSdk.ClientId, refreshToken);
                            profile = ConvertToProfile(profileData);
                        }
                        // 如果缓存的用户信息与启动器不一致，这里不再重新保存
                        if (profile != null && isCacheUserSameWithTapClient)
                        {
                            TapLogger.Debug("save refresh token and profile ");
                            SaveTapUser(refreshToken, profile);
                        }
                        else
                        {
                            TapLogger.Debug("dont save refresh token and profile isCacheUserSameWithTapClient = " + isCacheUserSameWithTapClient);
                        }
                    }
                }
                else
                {
                    TapLogger.Debug("local don't have accessToken");
                }
            }
            catch (Exception e)
            {
                TapLogger.Debug("refresh TapToken fail reason : " + e.Message + "\n stack = " + e.StackTrace);
            }
        }
    }
}
