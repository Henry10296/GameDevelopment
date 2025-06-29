using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class MessageDisplay : MonoBehaviour
{
    [Header("消息显示")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    
    private Coroutine currentMessageCoroutine;
    
    public void Initialize()
    {
        if (messagePanel) messagePanel.SetActive(false);
    }
    
    public void ShowMessage(string message, float duration)
    {
        if (messagePanel && messageText)
        {
            messageText.text = message;
            messagePanel.SetActive(true);
            
            if (currentMessageCoroutine != null)
                StopCoroutine(currentMessageCoroutine);
            
            currentMessageCoroutine = StartCoroutine(HideMessageAfterDelay(duration));
        }
    }
    
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messagePanel) messagePanel.SetActive(false);
        currentMessageCoroutine = null;
    }
}

public class LoadingScreen : UIPanel { }