using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    // 玩家对象可不绑定；本类内部直接管理玩家血量
    public TextMeshProUGUI scoreText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button retryButton;
    // 在 UIManager.cs 顶部字段加：
    public TextMeshProUGUI hpText;
    // 固定在屏幕上的玩家血条（条形），可选绑定
    public Image hpBarFill;                 // 建议 Type=Simple（圆角用遮罩/外框），或 Type=Filled
    public bool hpBarUseWidth = true;       // true: 改宽度填充；false: 用 fillAmount
    public Color hpFullColor = new Color(0.2f, 1f, 0.2f, 1f);
    public Color hpLowColor = new Color(1f, 0.2f, 0.2f, 1f);
    // UIManager.cs 顶部字段补充：
    public TMPro.TextMeshProUGUI ammoText;
    public UnityEngine.UI.Image hitMarker;
    public float hitMarkerShowTime = 0.08f;

    UnityEngine.Coroutine _hmCo;

    [Header("Player HP（UIManager 统一管理）")]
    public float playerMaxHP = 100f;
    public float playerHP = -1f; // <0 表示未初始化
    float _hpBarFullWidth;        // 初始宽度（宽度填充模式用）

    // 新方法：
    public void UpdateAmmo(int mag, int reserve, int magSize)
    {
        if (ammoText)
            ammoText.text = $"{mag}/{reserve}  (x{magSize})";
    }

    public void PulseHitMarker()
    {
        if (!hitMarker) return;
        if (_hmCo != null) StopCoroutine(_hmCo);
        _hmCo = StartCoroutine(_HitMarker());
    }

    System.Collections.IEnumerator _HitMarker()
    {
        hitMarker.enabled = true;
        yield return new WaitForSecondsRealtime(hitMarkerShowTime);
        hitMarker.enabled = false;
    }

    // 新方法：
    public void UpdateHP(float cur, float max)
    {
        if (hpText)
            hpText.text = $"HP {Mathf.CeilToInt(cur)}/{Mathf.CeilToInt(max)}";
        if (hpBarFill)
        {
            float ratio = max > 0 ? Mathf.Clamp01(cur / max) : 0f;
            if (hpBarUseWidth)
            {
                var rt = hpBarFill.rectTransform;
                if (_hpBarFullWidth <= 0f) _hpBarFullWidth = Mathf.Max(1f, rt.rect.width);
                if (!Mathf.Approximately(rt.pivot.x, 0f)) rt.pivot = new Vector2(0f, rt.pivot.y);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0.0001f, _hpBarFullWidth * ratio));
            }
            else
            {
                // 需要将 hpBarFill 的 Image.type 设为 Filled + Horizontal
                hpBarFill.fillAmount = ratio;
            }
            hpBarFill.color = Color.Lerp(hpLowColor, hpFullColor, ratio);
        }
    }

    void Awake()
    {
        Instance = this;
        resultPanel.SetActive(false);
    }

    void Start()
    {
        // 初始化内部血量（当不使用 PlayerHealth 时由 UIManager 管理）
        if (playerHP < 0f) playerHP = playerMaxHP;
        UpdateHP(playerHP, playerMaxHP);

        // 记录血条初始宽度（用于宽度填充模式）
        if (hpBarFill)
        {
            var rt = hpBarFill.rectTransform;
            _hpBarFullWidth = rt.rect.width;
        }

        // 不再依赖 PlayerHealth 组件
    }

    // 取消对外部 PlayerHealth 的订阅逻辑

    // 供其他系统直接操作玩家血量（当未使用 PlayerHealth 组件时）
    public void PlayerTakeDamage(float dmg)
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameOver) return;
        playerHP = Mathf.Max(0f, playerHP - Mathf.Max(0f, dmg));
        UpdateHP(playerHP, playerMaxHP);
        if (playerHP <= 0f && GameManager.Instance != null)
            GameManager.Instance.Lose();
    }

    public void PlayerHeal(float amount)
    {
        playerHP = Mathf.Min(playerMaxHP, playerHP + Mathf.Max(0f, amount));
        UpdateHP(playerHP, playerMaxHP);
    }

    public void UpdateScore(int cur, int target)
    {
        if (scoreText)
            scoreText.text = $"Score {cur}/{target}";
    }

    public void ShowResult(string msg)
    {
        resultText.text = msg;
        resultPanel.SetActive(true);
        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(() => GameManager.Instance.Restart());
    }
}
