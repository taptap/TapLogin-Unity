//
//  TapLoginHelper.h
//  TapTapLoginSource
//
//  Created by Bottle K on 2020/12/2.
//

#import <UIKit/UIKit.h>
#import <TapLoginSDK/TTSDKConfig.h>
#import <TapLoginSDK/TTSDKAccessToken.h>
#import <TapLoginSDK/TTSDKProfile.h>
#import <TapLoginSDK/TTSDKLoginResult.h>
#import <TapLoginSDK/AccountGlobalError.h>
#import <TapLoginSDK/TapTapLoginResultDelegate.h>

#define TapLoginSDK @"TapLogin"
#define TapLoginSDK_VERSION_NUMBER @"32906001"
#define TapLoginSDK_VERSION        @"3.29.6"

NS_ASSUME_NONNULL_BEGIN

@interface TapLoginHelper : NSObject

/// 初始化
/// @param clientID clientID
+ (void)initWithClientID:(NSString *)clientID;

/// 初始化
/// @param clientID clientID
/// @param config 配置项
+ (void)initWithClientID:(NSString *)clientID config:(TTSDKConfig *_Nullable)config;

/// 修改登录配置
/// @param config 配置项
+ (void)changeTapLoginConfig:(TTSDKConfig *_Nullable)config;

/// 设置登录回调
/// @param delegate 回调
+ (void)registerLoginResultDelegate:(id <TapTapLoginResultDelegate>)delegate;

/// 移除登录回调
+ (void)unregisterLoginResultDelegate;

/// 获取当前设置的登录回调
+ (id <TapTapLoginResultDelegate> _Nullable)getLoginResultDelegate;

/// 开始登录流程
/// @param permissions 权限列表
+ (void)startTapLogin:(NSArray *)permissions;

/// 开始登录流程
/// @param targetViewController 需要展现的页面
/// @param permissions 权限列表
+ (void)startTapLogin:(UIViewController *_Nullable)targetViewController permissions:(NSArray *)permissions;

/// 获取当前 Token
+ ( TTSDKAccessToken * _Nullable )currentAccessToken;

/// 获取当前 Profile
+ ( TTSDKProfile * _Nullable )currentProfile;

/// 获取当前服务器上最新的 Profile
/// @param callback 回调
+ (void)fetchProfileForCurrentAccessToken:(void (^)(TTSDKProfile *profile, NSError *error))callback;

/// 登出
+ (void)logout;

/// 当前是否有国内客户端支持
+ (BOOL)isTapTapClientSupport;

/// 当前是否有国外客户端支持
+ (BOOL)isTapTapGlobalClientSupport;

/// 监听 url 回调
/// @param url url
+ (BOOL)handleTapTapOpenURL:(NSURL *)url __attribute__((deprecated("Please use [TDSHandleUrl handleOpenURL:]")));

/// 添加预设的登录请求权限
+ (void)appendPermission:(NSString *)permission;

@end

NS_ASSUME_NONNULL_END
