using UnityEngine;

// 在玩家头顶显示一个简易血条（3D Quad 实现）
// 用法：把本脚本加到 Player 根物体或子物体；可指定 anchor 和偏移。
public class PlayerHealthBar : MonoBehaviour
{
    public PlayerHealth target;             // 目标（留空则向上查找）
    public Transform anchor;                // 锚点（留空则用本物体）
    public Vector3 worldOffset = new Vector3(0, 2.0f, 0);

    [Header("外观")]
    public float width = 1.4f;
    public float height = 0.14f;
    public Color backColor = new Color(0, 0, 0, 0.5f);
    public Color lowColor = new Color(1f, 0.2f, 0.2f, 0.95f);
    public Color midColor = new Color(1f, 1f, 0.2f, 0.95f);
    public Color fullColor = new Color(0.2f, 1f, 0.2f, 0.95f);

    [Header("显示逻辑")]
    public bool alwaysVisible = true;       // 玩家默认始终显示
    public bool hideWhenFull = false;       // 可选：满血是否隐藏
    public float visibleSecondsAfterHit = 1.5f;

    Transform _barRoot;
    Transform _backQuad;
    Transform _fillQuad;
    Material _backMat;
    Material _fillMat;
    float _lastHp;
    float _lastHitTime;
    Camera _cam;

    void Awake()
    {
        if (!target) target = GetComponentInParent<PlayerHealth>();
        if (!anchor) anchor = transform;
        _cam = Camera.main;
        BuildBar();
    }

    void Start()
    {
        if (target) _lastHp = target.hp;
        UpdateBarImmediate();
        _lastHitTime = float.NegativeInfinity;
        SetRenderersVisible(alwaysVisible || !hideWhenFull);
    }

    void LateUpdate()
    {
        if (!target) return;
        if (_cam == null) _cam = Camera.main;

        var worldPos = (anchor ? anchor.position : transform.position) + worldOffset;
        _barRoot.position = worldPos;
        if (_cam) _barRoot.rotation = Quaternion.LookRotation(_barRoot.position - _cam.transform.position);

        if (!Mathf.Approximately(_lastHp, target.hp))
        {
            _lastHp = target.hp;
            _lastHitTime = Time.time;
            UpdateBarImmediate();
        }

        bool shouldShow = true;
        float ratio = Mathf.Clamp01(target.hp / Mathf.Max(0.0001f, target.maxHP));
        if (!alwaysVisible)
        {
            if (hideWhenFull)
                shouldShow = ratio < 0.999f || (Time.time - _lastHitTime) < visibleSecondsAfterHit;
            else
                shouldShow = true;
        }
        SetRenderersVisible(shouldShow);
    }

    void BuildBar()
    {
        _barRoot = new GameObject("PlayerHealthBar").transform;
        _barRoot.SetParent(transform, false);

        _backQuad = CreateQuad("Back", backColor);
        _fillQuad = CreateQuad("Fill", fullColor);

        _backQuad.localScale = new Vector3(width, height, 1);
        _backQuad.localPosition = Vector3.zero;
        _backQuad.localRotation = Quaternion.identity;

        _fillQuad.localScale = new Vector3(width, height, 1);
        _fillQuad.localPosition = Vector3.zero;
        _fillQuad.localRotation = Quaternion.identity;
    }

    Transform CreateQuad(string name, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(_barRoot, false);
        var mr = go.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        var col = go.GetComponent<Collider>();
        if (col) Destroy(col);

        var shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.color = color;
        mr.sharedMaterial = mat;

        if (name == "Back") _backMat = mat; else _fillMat = mat;
        return go.transform;
    }

    void UpdateBarImmediate()
    {
        if (!target) return;
        float ratio = Mathf.Clamp01(target.hp / Mathf.Max(0.0001f, target.maxHP));

        float curW = Mathf.Max(0.0001f, width * ratio);
        _fillQuad.localScale = new Vector3(curW, height, 1);
        _fillQuad.localPosition = new Vector3(-(width - curW) * 0.5f, 0, -0.001f);

        Color to = ratio >= 0.5f ? Color.Lerp(midColor, fullColor, (ratio - 0.5f) / 0.5f)
                                 : Color.Lerp(lowColor, midColor, ratio / 0.5f);
        _fillMat.color = to;
    }

    void SetRenderersVisible(bool v)
    {
        if (_backQuad) { var r = _backQuad.GetComponent<Renderer>(); if (r) r.enabled = v; }
        if (_fillQuad) { var r = _fillQuad.GetComponent<Renderer>(); if (r) r.enabled = v; }
    }
}

