### New Feature

- 新增 TapTap OAuth 相关接口
  ```
  // 登陆  
  TapLogin.Login();
  // 登出
  TapLogin.Logout();
  ```
- 新增篝火测试资格
  ```
  var boolean = await TapLogin.GetTestQualification();
  ```
### Dependencies

- TapTap.Common v3.0.0