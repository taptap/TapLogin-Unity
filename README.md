## [TapTap.Login](./Documentation/README.md)

## 使用前提

使用 TapTap.Bootstrap 前提是必须依赖以下库:
* [TapTap.Common](https://github.com/TapTap/TapCommon-Unity.git)

> 如果开发者在游戏中同时接入了多家第三方（例如支持苹果、微信、Facebook 等账户登录），只把 TapTap 当成一个普通的登录渠道，那么在客户端可以只依赖 `TapLogin、TapCommon` 这 2  个模块，并按照如下的流程来接入：

### 1.初始化

#### 如果配合 `TapBoostrap` 使用，则不需要调用初始化接口

```c#
TapLogin.Init(string clientID);
```

### 2.唤起 TapTap 网页 或者 TapTap 客户端进行登陆

登陆成功之后，会返回 `AccessToken` 

```c#
var accessToken = await TapLogin.Login();
```

### 3. 获取 TapTap AccessToken

```c#
var accessToken = await TapLogin.GetAccessToken();
```

### 4. 获取 TapTap Profile

```c#
var profile = await TapLogin.FetchProfile();
```

### 5. 获取篝火测试资格
```c#
var boolean = await TapLogin.GetTestQualification();
```

### 6. 退出登陆

```c#
TapLogin.Logout();
```