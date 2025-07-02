using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("身高设置")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public float cameraHeightOffset = 0.1f; // 相对于角色高度的偏移
    
    [Header("移动速度")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float crouchSpeed = 3f;
    public float airSpeed = 8f; // 空中移动速度
    
    [Header("跳跃设置")]
    public float jumpForce = 8f;
    public float jumpCooldown = 0.1f;
    public bool allowAirJump = false;
    public int maxAirJumps = 1;
    
    [Header("鼠标设置")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 90f;
    public bool invertMouseY = false;
    
    [Header("物理设置")]
    public float gravity = 20f;
    public float friction = 8f;
    public float acceleration = 10f;
    public float airAcceleration = 2f;
    
    [Header("高级设置")]
    public bool enableBunnyHopping = true;
    public float bunnyHopThreshold = 0.1f;
    public bool enableStrafing = true;
    public float maxStrafeSpeed = 15f;
    
    [Header("音效")]
    public AudioClip[] footstepSounds;
    public AudioClip jumpSound;
    public AudioClip landSound;
}

