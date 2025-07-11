
using UnityEngine;

public class WeaponWorldDisplay : WorldItemDisplay
{
    [Header("武器专用设置")]
    public bool useWeaponOrientation = true;  // 武器有特定朝向
    public Vector3 weaponRotation = Vector3.zero;
    public bool enableSpecialEffects = true;   // 武器特效
    
    private WeaponData weaponData;
    private ParticleSystem glowEffect;
    
    public void SetWeaponData(WeaponData data)
    {
        weaponData = data;
        if (data != null)
        {
            // 使用世界贴图而不是背包图标
            Sprite worldSprite = data?.GetWorldSprite() ?? data.weaponIcon;
            SetItemSprite(worldSprite);
            
            // 设置武器特殊朝向
            if (useWeaponOrientation)
            {
                transform.rotation = Quaternion.Euler(weaponRotation);
            }
            
            // 设置颜色和特效
            SetupWeaponVisuals(data);
            
            gameObject.name = $"WorldWeapon_{data.weaponName}";
        }
    }
    
    void SetupWeaponVisuals(WeaponData data)
    {
        if (data.visualConfig == null) return;
        
        // 设置颜色
        SetColor(data.visualConfig.worldTint);
        
        // 发光效果
        if (data.visualConfig.glowEffect && enableSpecialEffects)
        {
            CreateGlowEffect(data.visualConfig.glowColor);
        }
        
        // 武器特殊旋转
        if (data.visualConfig.rotateInWorld)
        {
            rotateSpeed = data.visualConfig.rotationSpeed;
            animateRotate = true;
        }
        
        // 浮动动画
        if (data.visualConfig.floatAnimation)
        {
            floatAmplitude = data.visualConfig.floatAmplitude;
            animateFloat = true;
        }
    }
    
    void CreateGlowEffect(Color glowColor)
    {
        if (glowEffect == null)
        {
            GameObject glowObj = new GameObject("WeaponGlow");
            glowObj.transform.SetParent(transform);
            glowObj.transform.localPosition = Vector3.zero;
            
            glowEffect = glowObj.AddComponent<ParticleSystem>();
            
            var main = glowEffect.main;
            main.startColor = glowColor;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            var emission = glowEffect.emission;
            emission.rateOverTime = 10f;
            
            var shape = glowEffect.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            
            var colorOverLifetime = glowEffect.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(glowColor, 0.0f), new GradientColorKey(glowColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = gradient;
        }
    }
}
