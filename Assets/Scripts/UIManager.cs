using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public TextMeshProUGUI scoreText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button retryButton;
    // 在 UIManager.cs 顶部字段加：
    public TextMeshProUGUI hpText;
    // UIManager.cs 顶部字段补充：
    public TMPro.TextMeshProUGUI ammoText;
    public UnityEngine.UI.Image hitMarker;
    public float hitMarkerShowTime = 0.08f;

    UnityEngine.Coroutine _hmCo;

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
        hpText.text = $"HP {Mathf.CeilToInt(cur)}/{Mathf.CeilToInt(max)}";
    }

    void Awake()
    {
        Instance = this;
        resultPanel.SetActive(false);
    }

    public void UpdateScore(int cur, int target)
    {
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
