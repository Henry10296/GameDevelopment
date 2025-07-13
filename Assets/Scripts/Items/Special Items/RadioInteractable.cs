using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadioInteractable : BaseInteractable
{
    [Header("无线电设置")]
    public bool isRadioStation = false;
    public bool isRadioClue = false;
   public override void OnInteract()
    {
        if (isRadioStation)
        {
            RadioManager.Instance?.FindRadio();
            
            if (UIManager.Instance)
                UIManager.Instance.ShowMessage("找到了无线电设备!", 3f);
            
            if (RadioManager.Instance?.CanBroadcast() == true)
            {
                RadioManager.Instance.BroadcastSignal();
            }
        }
        else if (isRadioClue)
        {
            JournalManager.Instance?.AddEntry("无线电线索", 
                "发现了关于无线电位置的线索...", JournalEntryType.Important);
        }
        
    }
}
