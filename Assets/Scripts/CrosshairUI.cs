using UnityEngine;
using UnityEngine.UI;

// 轻量准星（基于 UGUI），运行时自动在 Canvas 中创建
// 用法：
// - 将本脚本挂到场景任意对象（推荐挂到 Canvas 上）。
// - 运行时若未指定容器，会自动在第一个启用的 Canvas 下生成一个名为 "Crosshair" 的 UI 组。
// - 可在 Inspector 调整颜色、线长、粗细、间隙、是否显示中心点。
public class CrosshairUI : MonoBehaviour
{
    [Header("外观")]
    public Color color = new Color(1f, 1f, 1f, 0.9f);
    public float lineLength = 10f;   // 每个刻度的长度（像素）
    public float thickness = 2f;     // 线条粗细（像素）
    public float gap = 6f;           // 中心间隙（像素）
    public bool showCenterDot = true;
    public float dotSize = 3f;

    [Header("容器（可留空自动查找/创建）")]
    public Canvas targetCanvas;
    public RectTransform crosshairRoot;

    Image _top, _bottom, _left, _right, _dot;

    void Start()
    {
        if (!targetCanvas)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (!targetCanvas)
            {
                Debug.LogWarning("CrosshairUI: 场景中没有 Canvas，无法创建准星。");
                enabled = false; return;
            }
        }

        if (!crosshairRoot)
        {
            var go = new GameObject("Crosshair", typeof(RectTransform));
            crosshairRoot = go.GetComponent<RectTransform>();
            crosshairRoot.SetParent(targetCanvas.transform, false);
            crosshairRoot.anchorMin = crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchoredPosition = Vector2.zero;
            crosshairRoot.sizeDelta = Vector2.zero;
        }

        _top = CreateLine("Top");
        _bottom = CreateLine("Bottom");
        _left = CreateLine("Left");
        _right = CreateLine("Right");
        _dot = CreateDot("Dot");

        ApplyLayout();
    }

    Image CreateLine(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        rt.SetParent(crosshairRoot, false);
        img.color = color;
        return img;
    }

    Image CreateDot(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        rt.SetParent(crosshairRoot, false);
        img.color = color;
        return img;
    }

    void OnValidate()
    {
        if (Application.isPlaying && crosshairRoot)
            ApplyLayout();
    }

    public void SetVisible(bool v)
    {
        if (!_top) return;
        _top.enabled = _bottom.enabled = _left.enabled = _right.enabled = v;
        if (_dot) _dot.enabled = v && showCenterDot;
    }

    public void SetColor(Color c)
    {
        color = c;
        if (_top) { _top.color = c; _bottom.color = c; _left.color = c; _right.color = c; }
        if (_dot) _dot.color = c;
    }

    public void ApplyLayout()
    {
        if (!_top) return;
        // 竖线：宽=thickness，高=lineLength
        _top.rectTransform.sizeDelta = new Vector2(thickness, lineLength);
        _bottom.rectTransform.sizeDelta = new Vector2(thickness, lineLength);
        // 横线：宽=lineLength，高=thickness
        _left.rectTransform.sizeDelta = new Vector2(lineLength, thickness);
        _right.rectTransform.sizeDelta = new Vector2(lineLength, thickness);

        _top.rectTransform.anchoredPosition = new Vector2(0, gap + lineLength * 0.5f);
        _bottom.rectTransform.anchoredPosition = new Vector2(0, -(gap + lineLength * 0.5f));
        _left.rectTransform.anchoredPosition = new Vector2(-(gap + lineLength * 0.5f), 0);
        _right.rectTransform.anchoredPosition = new Vector2(gap + lineLength * 0.5f, 0);

        if (_dot)
        {
            _dot.enabled = showCenterDot;
            _dot.rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
            _dot.rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}

