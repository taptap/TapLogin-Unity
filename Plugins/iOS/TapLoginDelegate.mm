//
//  TapDelegate.m
//  Unity-iPhone
//
//  Created by xe on 2021/7/14.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <UserNotifications/UserNotifications.h>
#include "AppDelegateListener.h"
#include "LifeCycleListener.h"
#import "TapLoginDelegate.h"
#import <TapLoginSDK/TapLoginHelper.h>

@implementation TapLoginDelegate

+(void) load{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken,^{
        
        NSNotificationCenter* nc = [NSNotificationCenter defaultCenter];
        [nc addObserverForName:kUnityOnOpenURL object:nil queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification * _Nonnull note) {
            if ([note.userInfo isKindOfClass: [NSMutableDictionary<NSString*, id> class]]) {
                NSURL* url = [note.userInfo objectForKey:@"url"];
                [TapLoginHelper handleTapTapOpenURL:url];
            }
        }];
        
    });
}

@end
