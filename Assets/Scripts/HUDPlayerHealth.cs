using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 固定在屏幕上的玩家血条（Canvas 内 UI Image）
// 用法：
// 1) 在 Canvas 顶部放置一个 Image（Type=Filled, Fill Method=Horizontal）作为前景条；
// 2) 可选再放一个背景条（纯色 Image）；
// 3) 将本脚本挂在任一物体上（通常挂在前景条），把字段拖好；
// 4) 直接读取 UIManager 的玩家血量（不依赖 PlayerHealth 脚本）。
public class HUDPlayerHealth : MonoBehaviour
{
    public Image fill;                 // 前景填充条（Type=Filled）
    public TMP_Text hpLabel;           // 可选：显示 "HP 100/100"
    public Color fullColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color lowColor = new Color(1f, 0.2f, 0.2f, 1f);

    void LateUpdate()
    {
        if (UIManager.Instance == null) return;
        float max = UIManager.Instance.playerMaxHP;
        float cur = UIManager.Instance.playerHP;
        float ratio = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
        if (fill)
        {
            fill.fillAmount = ratio;
            fill.color = Color.Lerp(lowColor, fullColor, ratio);
        }
        if (hpLabel)
        {
            int icur = Mathf.CeilToInt(cur);
            int imax = Mathf.CeilToInt(max);
            hpLabel.text = "HP " + icur + "/" + imax;
        }
    }
}
