using UnityEngine;
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

    [Header("参数")]
    public float damage = 25f;
    public float range = 200f;
    public float fireRate = 10f;        // 每秒发射数（10 = 600RPM）
    public int magSize = 30;            // 弹匣容量
    public int reserveAmmo = 90;        // 备用弹
    public float reloadTime = 1.8f;
    public float headshotMul = 2.0f;

    [Header("散布&后坐力")]
    public float hipfireSpread = 1.2f;  // 度（在屏幕中心锥角）
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
        tpc = cam.GetComponent<ThirdPersonCamera>();
        defaultFov = cam.fieldOfView;
        if (tpc) defaultCamDist = tpc.distance;
        UIManager.Instance.UpdateAmmo(mag, reserveAmmo, magSize);
    }

    void OnEnable()
    {
        // 确保 UI 更新
        UIManager.Instance.UpdateAmmo(mag, reserveAmmo, magSize);
    }

    void Update()
    {
        // 开火（左键）
        if (Input.GetMouseButton(0)) TryFire();

        // 瞄准（右键）
        if (Input.GetMouseButtonDown(1)) SetADS(true);
        if (Input.GetMouseButtonUp(1)) SetADS(false);

        // 换弹（R）
        if (Input.GetKeyDown(KeyCode.R)) TryReload();
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

        // 枪口特效/声音
        if (muzzleFlash) muzzleFlash.Play(true);
        if (audioSrc && fireClip) audioSrc.PlayOneShot(fireClip, 0.9f);

        // 计算带散布的方向（屏幕中心小锥形）
        float spread = (ads ? adsSpread : hipfireSpread);
        Vector3 dir = GetSpreadDirection(spread);

        // 从摄像机发射射线，命中处再用于命中特效
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        ray.direction = dir;

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            bool head = hit.collider.CompareTag("Head");
            float finalDmg = head ? damage * headshotMul : damage;

            var eh = hit.collider.GetComponentInParent<EnemyHealth>();
            if (eh)
            {
                eh.TakeDamage(finalDmg, hit.point, hit.normal, head);
                UIManager.Instance.PulseHitMarker();
            }
            else
            {
                // 打到环境：贴个火花
                if (hitFxPrefab) Instantiate(hitFxPrefab, hit.point, Quaternion.LookRotation(hit.normal)).SetActive(true);
            }
        }

        // 简易后坐力：轻推相机旋转
        RecoilKick();
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
