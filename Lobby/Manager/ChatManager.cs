using Photon.Pun;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class ChatManager : MonoBehaviourPunCallbacks
{
    [Header("Chat")]
    public Transform ChatContent;
    public ScrollRect ChatScrollRect;
    public TMP_InputField ChatInput;
    public PhotonView PV;

    public void Send()
    {
        if (!string.IsNullOrEmpty(ChatInput.text))
        {
            PV.RPC("ChatRPC", RpcTarget.All, PhotonNetwork.NickName + " : " + ChatInput.text);
            ChatInput.text = "";
        }
    }

    [PunRPC]
    public void ChatRPC(string msg)
    {
        GameObject chatMessage = Instantiate(Resources.Load("ChatPrefab") as GameObject, ChatContent);
        TMP_Text chatText = chatMessage.GetComponent<TMP_Text>();
        chatText.text = msg;
        Canvas.ForceUpdateCanvases();
        ChatScrollRect.verticalNormalizedPosition = 0f;
    }
}
