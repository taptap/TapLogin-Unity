using TapTap.Login.Internal;
using UnityEngine;
using UnityEngine.UI;

public class TitleController {
    private readonly Text leftText;
    private readonly Image logoImage;
    private readonly Text rightText;

    public TitleController(Transform transform) {
        leftText = transform.Find("LeftText").GetComponent<Text>();
        logoImage = transform.Find("Logo").GetComponent<Image>();
        rightText = transform.Find("RightText").GetComponent<Text>();
    }

    public void Load() {
        ILoginLang lang = LoginLanguage.GetCurrentLang();
        leftText.text = lang.TitleUse();
        leftText.gameObject.SetActive(!string.IsNullOrEmpty(leftText.text));
        rightText.text = lang.TitleLogin();
        rightText.gameObject.SetActive(!string.IsNullOrEmpty(rightText.text));
    }
}
