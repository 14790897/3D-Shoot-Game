using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Weapon : MonoBehaviour
{
    [Header("绑定")]
    public Camera cam;                 // 拖 Main Camera
    public Transform muzzle;           // 枪口位置（用于特效/射线起点可用摄像机）
    public LayerMask hitMask;          // 只打 环境+Enemy，务必把 Player 排除
    public ParticleSystem muzzleFlash; // 可选
    public GameObject hitFxPrefab;     // 命中特效，可选
    public AudioSource audioSrc;       // 播放开火/换弹音
    public AudioClip fireClip, reloadClip, dryClip;

    [Header("可视化/弹道")]
    public bool showTracer = true;     // 是否显示弹道线
    public float tracerWidth = 0.3f;  // 线宽（米）
    public float tracerDuration = 0.6f; // 持续时间（秒）
    public Color tracerColor = new Color(1f, 0.9f, 0.2f, 0.9f); // 发光黄
    static Material s_tracerMat;       // 共享材质（Sprites/Default）
    [Header("调试")]
    public bool logHits = true;        // 是否输出命中调试日志
    public bool debugHitMarker = false;    // 命中调试：在命中点生成小标记
    public float debugMarkerSize = 0.12f;
    public float debugMarkerLife = 0.6f;

    [Header("参数")]
    public float damage = 25f;
    public float range = 200f;
    public float fireRate = 10f;        // 每秒发射数（10 = 600RPM）
    public int magSize = 180;            // 弹匣容量
    public int reserveAmmo = 1000;        // 备用弹
    public float reloadTime = 1.8f;
    public float headshotMul = 2.0f;

    [Header("抛射物（有限速度，可选）")]
    public bool useProjectile = true;     // 设为 true 使用抛射物而非命中扫描
    public float projectileSpeed = 80f;    // 子弹初速度（m/s）
    public float projectileLife = 3f;      // 子弹生存时长
    public float projectileGravity = 0f;   // 额外重力（m/s^2），0 表示无下坠
    public float projectileRadius = 0.03f; // 子弹碰撞半径（米）
    public GameObject projectilePrefab;    // 可选：子弹外观（若为空将用一个小球代替）

    [Header("散布&后坐力")]
    public float hipfireSpread = 0.3f;  // 度（在屏幕中心锥角）
    public float adsSpread = 0.25f;
    public float recoilPitch = 0.6f;    // 每发上跳
    public float recoilYaw = 0.2f;      // 每发左右微抖
    public float adsFov = 45f;          // ADS 视野
    public float adsCamDist = 2.2f;     // 你 ThirdPersonCamera 用 distance，可在外部改

    int mag;
    bool reloading;
    float nextFireTime;
    bool ads;

    // 第三人称相机（可选联动）
    ThirdPersonCamera tpc;
    float defaultFov;
    float defaultCamDist;

    void Awake()
    {
        mag = magSize;
        // 相机容错：未绑定则尝试 Camera.main
        if (!cam) cam = Camera.main;
        if (cam)
        {
            tpc = cam.GetComponent<ThirdPersonCamera>();
            defaultFov = cam.fieldOfView;
            if (tpc) defaultCamDist = tpc.distance;
        }
        else
        {
            Debug.LogWarning("Weapon: 未找到 Camera，请在 Weapon.cam 绑定主相机。");
        }
        // 若存在 Projectile 层，则让同层之间忽略碰撞，避免子弹互撞
        int __projLayer = LayerMask.NameToLayer("Projectile");
        if (__projLayer >= 0)
            Physics.IgnoreLayerCollision(__projLayer, __projLayer, true);
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAmmo(mag, reserveAmmo, magSize);
    }

    void OnEnable()
    {
        // 确保 UI 更新
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAmmo(mag, reserveAmmo, magSize);
    }

    void Update()
    {
        // 开火（鼠标左键 / 手柄右扳机/肩键）
        bool fireHeld = (Mouse.current != null && Mouse.current.leftButton.isPressed)
                        || (Keyboard.current != null && Keyboard.current.jKey.isPressed) // 键盘J发射
                        || (Gamepad.current != null && (Gamepad.current.rightTrigger.ReadValue() > 0.5f || Gamepad.current.rightShoulder.isPressed));
        if (fireHeld) TryFire();

        // 瞄准（鼠标右键 / 手柄左扳机）
        bool adsPressed = (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                          || (Gamepad.current != null && Gamepad.current.leftTrigger.ReadValue() > 0.5f && !ads);
        bool adsReleased = (Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame)
                           || (Gamepad.current != null && Gamepad.current.leftTrigger.ReadValue() < 0.4f && ads);
        if (adsPressed) SetADS(true);
        if (adsReleased) SetADS(false);

        // 换弹（键盘R / 手柄X）
        bool reloadPressed = (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                             || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        if (reloadPressed) TryReload();
    }

    void SetADS(bool on)
    {
        ads = on;
        cam.fieldOfView = on ? adsFov : defaultFov;
        if (tpc) tpc.distance = on ? adsCamDist : defaultCamDist;
    }

    void TryFire()
    {
        if (GameManager.Instance.isGameOver || reloading) return;

        if (Time.time < nextFireTime) return;

        if (mag <= 0)
        {
            if (audioSrc && dryClip) audioSrc.PlayOneShot(dryClip, 0.8f);
            nextFireTime = Time.time + 0.15f;
            return;
        }

        nextFireTime = Time.time + 1f / fireRate;
        mag--;
        UIManager.Instance.UpdateAmmo(mag, reserveAmmo, magSize);

        // 枪口声音（粒子将在确定发射方向后播放）
        if (audioSrc && fireClip) audioSrc.PlayOneShot(fireClip, 0.9f);

        // 计算带散布的方向（屏幕中心小锥形）
        float spread = (ads ? adsSpread : hipfireSpread);
        Vector3 dir = GetSpreadDirection(spread);

        // 计算有效层：若未配置则用默认层；并排除 Player 层避免自击中
        int maskUsed = hitMask.value != 0 ? hitMask : Physics.DefaultRaycastLayers;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) maskUsed &= ~(1 << playerLayer);

        if (useProjectile && muzzle)
        {
            // 先用相机方向获取瞄准点，再让子弹从枪口朝该点发射
            Ray camRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            camRay.direction = dir;
            Vector3 aimPoint = cam.transform.position + dir * range;
            if (Physics.Raycast(camRay, out RaycastHit hit, range, maskUsed, QueryTriggerInteraction.Collide))
                aimPoint = hit.point;

            // 枪口到瞄准点之间若被近处物体挡住，改为撞点
            Vector3 shotDir = (aimPoint - muzzle.position).sqrMagnitude > 1e-6f ? (aimPoint - muzzle.position).normalized : cam.transform.forward;
            float dist = Vector3.Distance(muzzle.position, aimPoint);
            if (Physics.SphereCast(muzzle.position, Mathf.Max(0.001f, projectileRadius), shotDir, out RaycastHit block, dist, maskUsed, QueryTriggerInteraction.Collide))
            {
                aimPoint = block.point;
                shotDir = (aimPoint - muzzle.position).normalized;
            }

            // 对齐并播放枪口粒子
            if (muzzleFlash)
            {
                var t = muzzleFlash.transform;
                t.position = muzzle ? muzzle.position : t.position;
                t.rotation = Quaternion.LookRotation(shotDir);
                muzzleFlash.Play(true);
            }

            if (logHits)
            {
                Debug.Log($"[Weapon] 发射抛射物 origin={muzzle.position} dir={shotDir} speed={projectileSpeed}");
            }
            LaunchProjectile(muzzle.position, shotDir);
        }
        else
        {
            // 从摄像机发射射线，命中处用于结算；弹道线仅作可视化
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            ray.direction = dir;

            Vector3 startPos = muzzle ? muzzle.position : cam.transform.position;
            Vector3 endPos = cam.transform.position + dir * range;

            if (Physics.Raycast(ray, out RaycastHit hit, range, maskUsed, QueryTriggerInteraction.Collide))
            {
            // 使用 tag 字符串比较，避免在未定义 "Head" 标签时触发 Unity 的 CompareTag 警告
            bool head = hit.collider && hit.collider.tag == "Head";
                float finalDmg = head ? damage * headshotMul : damage;

                var eh = hit.collider.GetComponentInParent<EnemyHealth>();
                if (eh)
                {
                    eh.TakeDamage(finalDmg, hit.point, hit.normal, head);
                    UIManager.Instance.PulseHitMarker();
                    if (logHits)
                    {
                        string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                        Debug.Log($"[Weapon] 命中敌人: {hit.collider.name} 层={layerName} 点={hit.point} 法线={hit.normal} 距离={hit.distance:F2}");
                    }
                }
                else
                {
                    // 打到环境：贴个火花/弹坑；稍微沿法线偏移避免Z冲突，并挂到被击中物体上

                    if (hitFxPrefab)
                    {
                        var fx = Instantiate(hitFxPrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
                        fx.transform.SetParent(hit.collider.transform, true);
                        fx.SetActive(true);
                    }
                    if (logHits)
                    {
                        string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                        Debug.Log($"[Weapon] 命中环境: {hit.collider.name} 层={layerName} 点={hit.point} 法线={hit.normal} 距离={hit.distance:F2}");
                    }
                }

                if (debugHitMarker) SpawnDebugMarker(hit.point, hit.normal);

                endPos = hit.point;
            }
            else if (debugHitMarker)
            {
                // 未命中：打印日志并在最大射程处放一个标记，便于确认方向
                if (logHits)
                {
                    Debug.Log($"[Weapon] 未命中。中心方向={dir}，射程={range}，标记位置={endPos}");
                }
                SpawnDebugMarker(endPos, Vector3.up);
            }

            // 对齐并播放枪口粒子（沿弹道方向）
            if (muzzleFlash)
            {
                Vector3 startPos2 = muzzle ? muzzle.position : cam.transform.position;
                Vector3 dir2 = (endPos - startPos2).sqrMagnitude > 1e-6f ? (endPos - startPos2).normalized : dir;
                var t = muzzleFlash.transform;
                t.position = startPos2;
                t.rotation = Quaternion.LookRotation(dir2);
                muzzleFlash.Play(true);
            }

            if (showTracer)
                SpawnTracer(startPos, endPos);
        }

        // 简易后坐力：轻推相机旋转
        RecoilKick();
    }

    void LaunchProjectile(Vector3 origin, Vector3 dir)
    {
        GameObject go;
        if (projectilePrefab)
        {
            go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        }
        else
        {
            // 兜底：生成一个小球体当作子弹
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bullet";
            go.transform.position = origin;
            go.transform.rotation = Quaternion.LookRotation(dir);
            // 调整碰撞半径与外观大小
            var sr = go.GetComponent<SphereCollider>();
            sr.radius = Mathf.Max(0.005f, projectileRadius);
            go.transform.localScale = Vector3.one * (projectileRadius * 2f);
            // 简单的无光材质
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1, 0.9f, 0.2f, 1);
            mr.sharedMaterial = mat;
        }

        // 将子弹放入 Projectile 层（若存在），否则退回 Ignore Raycast，减少误碰撞与误射线
        int projLayer = LayerMask.NameToLayer("Projectile");
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        if (projLayer >= 0) go.layer = projLayer; else if (ignoreRaycast >= 0) go.layer = ignoreRaycast;

        var rb = go.GetComponent<Rigidbody>();
        if (!rb)
        {
            rb = go.AddComponent<Rigidbody>();
        }
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false; // 自定义重力在 Bullet 里处理
        // 兼容版本：统一使用 velocity 赋值
        rb.linearVelocity = dir * projectileSpeed;

        var b = go.GetComponent<Bullet>();
        if (!b) b = go.AddComponent<Bullet>();
        // 计算用于 CCD 的有效层：排除 Player 与 Projectile 层
        int maskUsed = hitMask.value != 0 ? hitMask : Physics.DefaultRaycastLayers;
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) maskUsed &= ~(1 << playerLayer);
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        if (projectileLayer >= 0) maskUsed &= ~(1 << projectileLayer);
        b.Init(this, damage, headshotMul, hitFxPrefab, projectileGravity, projectileLife, projectileRadius, maskUsed);
        // 忽略与玩家自身的碰撞
        if (cam)
        {
            var root = cam.transform.root;
            var bulletCol = go.GetComponent<Collider>();
            foreach (var col in root.GetComponentsInChildren<Collider>())
            {
                if (col && bulletCol) Physics.IgnoreCollision(bulletCol, col, true);
            }
        }
    }

    void SpawnTracer(Vector3 start, Vector3 end)
    {
        StartCoroutine(TracerCo(start, end));
    }

    void SpawnDebugMarker(Vector3 pos, Vector3 normal)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "HitDebug";
        go.transform.position = pos + normal * 0.01f;
        go.transform.localScale = Vector3.one * Mathf.Max(0.02f, debugMarkerSize);
        var mr = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        var hasBase = mat.HasProperty("_BaseColor");
        if (hasBase) mat.SetColor("_BaseColor", new Color(1f, 0.2f, 0.6f, 1f)); else mat.color = new Color(1f, 0.2f, 0.6f, 1f);
        mr.sharedMaterial = mat;
        var col = go.GetComponent<Collider>();
        if (col) col.enabled = false;
        Destroy(go, Mathf.Max(0.05f, debugMarkerLife));
    }

    IEnumerator TracerCo(Vector3 start, Vector3 end)
    {
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = tracerWidth;
        lr.endWidth = tracerWidth;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.startColor = tracerColor;
        lr.endColor = tracerColor;
        if (s_tracerMat == null)
        {
            // 在 URP 下优先使用 Unlit，兼容内置管线则退回 Sprites/Default
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            bool isURP = shader != null;
            if (!isURP)
                shader = Shader.Find("Sprites/Default");

            s_tracerMat = new Material(shader);
            s_tracerMat.enableInstancing = true;
            if (isURP)
                s_tracerMat.SetColor("_BaseColor", tracerColor);
            else
                s_tracerMat.color = tracerColor;
        }
        lr.material = s_tracerMat;

        yield return new WaitForSeconds(tracerDuration);
        if (go) Destroy(go);
    }

    Vector3 GetSpreadDirection(float degree)
    {
        // 以摄像机前方为基向量，随机在单位圆锥内扰动
        float rad = degree * Mathf.Deg2Rad;
        // 在屏幕像素系里偏移一丢丢，再转回世界射线更直观：
        Vector2 jitter = Random.insideUnitCircle * Mathf.Tan(rad);
        Vector3 dir = cam.transform.forward + cam.transform.right * jitter.x + cam.transform.up * jitter.y;
        return dir.normalized;
    }

    void RecoilKick()
    {
        // 直接修改相机欧拉角（温和）。更高级可交给相机脚本做插值。
        Vector3 e = cam.transform.eulerAngles;
        e.x = e.x - recoilPitch;                         // 上抬
        e.y = e.y + Random.Range(-recoilYaw, recoilYaw); // 左右随机
        cam.transform.rotation = Quaternion.Euler(e);

        // 若未绑定枪口特效，给出一次性提示，方便排查“看不到特效”
        if (!muzzleFlash)
        {
            Debug.unityLogger.LogWarning("Weapon", "未绑定 muzzleFlash（枪口粒子）。请在 Weapon 组件上拖入一个 ParticleSystem 到 muzzleFlash 字段。");
        }
    }

    public void TryReload()
    {
        if (reloading) return;
        if (mag == magSize) return;
        if (reserveAmmo <= 0) return;
        StartCoroutine(ReloadCo());
    }

    IEnumerator ReloadCo()
    {
        reloading = true;
        if (audioSrc && reloadClip) audioSrc.PlayOneShot(reloadClip, 0.9f);
        yield return new WaitForSeconds(reloadTime);

        int need = magSize - mag;
        int take = Mathf.Min(need, reserveAmmo);
        mag += take;
        reserveAmmo -= take;
        UIManager.Instance.UpdateAmmo(mag, reserveAmmo, magSize);
        reloading = false;
    }

    // 供外部（拾弹包）补给
    public void AddReserve(int amount)
    {
        reserveAmmo += Mathf.Max(0, amount);
        UIManager.Instance.UpdateAmmo(mag, reserveAmmo, magSize);
    }
}
