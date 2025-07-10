using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleMouseCrosshair : MonoBehaviour
{
    [Header("准星UI")] public Image crosshairImage;

    [Header("风格设置")] public Color normalColor = Color.white;
    public Color enemyColor = Color.red;
    public Color aimingColor = Color.yellow;
    public Color hitColor = Color.green;

    [Header("动态效果")] public float minSize = 16f;
    public float maxSize = 32f;

    [Header("参考风格")] [Range(0f, 1f)] public float gameStyle = 0.5f; // 0=神探鼠杰克, 1=Doom

    private bool isAiming = false;
    private bool enemyInSight = false;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // 修复：正确检查GameConfig和weaponConfig
        SetupCrosshairFromConfig();
    }

    void SetupCrosshairFromConfig()
    {
        // 安全地获取GameConfig
        if (GameManager.Instance != null)
        {
            var gameConfig = GameManager.Instance.gameConfig;
            if (gameConfig != null)
            {
                var weaponConfig = gameConfig.WeaponConfig;
                if (weaponConfig != null && weaponConfig.crosshairDefault != null)
                {
                    crosshairImage.sprite = weaponConfig.crosshairDefault;
                }
            }
        }
    }

    void Update()
    {
        CheckEnemyInSight();
        UpdateCrosshairAppearance();
        UpdateCrosshairSize();
        CheckAimingInput();
    }

    void CheckEnemyInSight()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            enemyInSight = hit.collider.CompareTag("Enemy");
        }
        else
        {
            enemyInSight = false;
        }
    }

    void CheckAimingInput()
    {
        // 检测瞄准输入（右键）
        isAiming = Input.GetMouseButton(1);
    }

    void UpdateCrosshairAppearance()
    {
        if (crosshairImage == null) return;

        Color targetColor = normalColor;

        if (enemyInSight)
        {
            targetColor = enemyColor;
        }
        else if (isAiming)
        {
            targetColor = aimingColor;
        }

        crosshairImage.color = Color.Lerp(crosshairImage.color, targetColor, Time.deltaTime * 8f);
    }

    void UpdateCrosshairSize()
    {
        if (rectTransform == null) return;

        float targetSize = minSize;

        // 根据玩家状态调整
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            if (player.IsRunning())
                targetSize += 8f;
            if (!player.IsGrounded())
                targetSize += 6f;
            if (player.IsCrouching())
                targetSize -= 4f;
        }

        // 根据武器散布调整
        WeaponController weapon = FindObjectOfType<WeaponController>();
        if (weapon != null)
        {
            targetSize += weapon.GetCurrentSpread() * 200f;
        }

        // 瞄准时缩小
        if (isAiming)
        {
            float aimMultiplier = Mathf.Lerp(0.6f, 0.3f, gameStyle);
            targetSize *= aimMultiplier;
        }

        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);

        Vector2 newSize = Vector2.one * targetSize;
        rectTransform.sizeDelta = Vector2.Lerp(rectTransform.sizeDelta, newSize, Time.deltaTime * 6f);
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    public void ShowHitFeedback()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFeedbackCoroutine());
        }
    }

    IEnumerator HitFeedbackCoroutine()
    {
        if (crosshairImage == null) yield break;

        Color originalColor = crosshairImage.color;
        crosshairImage.color = hitColor;

        yield return new WaitForSeconds(0.1f);

        crosshairImage.color = originalColor;
    }

    // 公共方法供武器系统调用
    public void OnTargetHit()
    {
        ShowHitFeedback();
    }
}