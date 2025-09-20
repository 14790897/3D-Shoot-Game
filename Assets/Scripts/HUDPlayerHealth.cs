using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 固定在屏幕上的玩家血条（Canvas 内 UI Image）
// 用法：
// 1) 在 Canvas 顶部放置一个 Image（Type=Filled, Fill Method=Horizontal）作为前景条；
// 2) 可选再放一个背景条（纯色 Image）；
// 3) 将本脚本挂在任一物体上（通常挂在前景条），把字段拖好；
// 4) 不需要修改现有 UIManager/PlayerHealth 逻辑。
public class HUDPlayerHealth : MonoBehaviour
{
    public PlayerHealth player;        // 留空会自动查找
    public Image fill;                 // 前景填充条（Type=Filled）
    public TMP_Text hpLabel;           // 可选：显示 "HP 100/100"
    public Color fullColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color lowColor = new Color(1f, 0.2f, 0.2f, 1f);

    void Start()
    {
        if (!player)
        {
#if UNITY_2023_1_OR_NEWER || UNITY_2022_2_OR_NEWER
            player = Object.FindAnyObjectByType<PlayerHealth>();
#else
            player = FindObjectOfType<PlayerHealth>();
#endif
        }
    }

    void LateUpdate()
    {
        if (!player) return;
        float ratio = player.maxHP > 0 ? Mathf.Clamp01(player.hp / player.maxHP) : 0f;
        if (fill)
        {
            fill.fillAmount = ratio;
            fill.color = Color.Lerp(lowColor, fullColor, ratio);
        }
        if (hpLabel)
        {
            int cur = Mathf.CeilToInt(player.hp);
            int max = Mathf.CeilToInt(player.maxHP);
            hpLabel.text = "HP " + cur + "/" + max;
        }
    }
}

