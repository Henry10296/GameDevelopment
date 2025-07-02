using UnityEngine;

public class RadioManager : Singleton<RadioManager>
{
    [Header("无线电设置")]
    public bool hasRadio = false;
    public bool radioBroadcasted = false;
    public bool[] broadcastDays = new bool[6]; // 记录哪天发送了信号

    public void FindRadio()
    {
        hasRadio = true;
        Debug.Log("找到了无线电!");
    }

    public bool CanBroadcast()
    {
        int day = GameManager.Instance.CurrentDay;
        return hasRadio && (day == 3 || day == 5) && !broadcastDays[day - 1];
    }

    public void BroadcastSignal()
    {
        if (CanBroadcast())
        {
            int day = GameManager.Instance.CurrentDay;
            broadcastDays[day - 1] = true;

            // 检查是否在第3和第5天都发送了信号
            if (broadcastDays[2] && broadcastDays[4]) // 第3天和第5天(索引2和4)
            {
                radioBroadcasted = true;
                Debug.Log("成功发送救援信号! 好结局解锁!");
            }

            Debug.Log($"第{day}天发送信号成功");
        }
    }
    
    public bool GetGoodEndingAchieved()
    {
        return radioBroadcasted;
    }
}