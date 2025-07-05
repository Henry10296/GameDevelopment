using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractionEnhancer : MonoBehaviour
{
    [Header("交互检测")]
    public float interactionRange = 2f;
    public LayerMask interactableLayers = -1;
    
    [Header("UI提示")]
    public GameObject interactionPrompt;
    public TextMeshPro promptText;
    
    private Camera playerCamera;
    private IInteractable currentInteractable;
    private WeaponDisplay weaponDisplay;
    
    void Start()
    {
        playerCamera = Camera.main;
        weaponDisplay = FindObjectOfType<WeaponDisplay>();
    }
    
    void Update()
    {
        CheckForInteractables();
        HandleInteractionInput();
    }
    
    void CheckForInteractables()
    {
        if (playerCamera == null) return;
        
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayers))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    ShowInteractionPrompt(interactable.GetInteractionText());
                }
            }
            else
            {
                HideInteractionPrompt();
            }
        }
        else
        {
            HideInteractionPrompt();
        }
    }
    
    void HandleInteractionInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            currentInteractable.Interact();
            
            // 触发手部交互动画
            if (weaponDisplay != null)
            {
                weaponDisplay.OnInteraction();
            }
        }
    }
    
    void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            if (promptText != null)
            {
                promptText.text = text;
            }
        }
    }
    
    void HideInteractionPrompt()
    {
        currentInteractable = null;
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
}