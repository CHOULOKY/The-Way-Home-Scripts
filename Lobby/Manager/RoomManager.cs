using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    #region Variables
    [Header("RoomPanel")]
    public GameObject RoomPanel;
    public TMP_Text ListText;
    public TMP_Text RoomInfoText;
    private string roomName = "";

    public Button GameBtn;
    private bool isGuestReady = false;

    [Header("Chapter Selection")]
    public Button ChatperNext;
    public Button ChatperPrevious;
    public Text chapterText;
    public ChapterDatabase chapterDB;
    private int selectedOption = 0;

    [Header("ETC")]
    public PhotonView PV;
    public Image fadeImage;
    private ChatManager chatManager;

    private void Start()
    {
        chatManager = FindObjectOfType<ChatManager>();
    }
    #endregion

    #region Room
    public void CreateRoom()
    {
        string hostName = PhotonNetwork.LocalPlayer.NickName;
        roomName = hostName + "의 방";
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 2 });
    }

    public void JoinRandomRoom() => PhotonNetwork.JoinRandomRoom();
    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnJoinedRoom()
    {
        RoomPanel.SetActive(true);
        RoomRenewal();

        // 채팅 초기화
        foreach (Transform child in chatManager.ChatContent)
        {
            Destroy(child.gameObject);
        }

        GameBtn.onClick.RemoveAllListeners();

        bool isHost = PhotonNetwork.IsMasterClient;
        ChatperNext.interactable = isHost;
        ChatperPrevious.interactable = isHost;

        if (isHost)
        {
            GameBtn.interactable = false;
            GameBtn.GetComponentInChildren<TMP_Text>().text = "Start Game";
            GameBtn.onClick.AddListener(OnHostGameStart);
        }
        else
        {
            GameBtn.interactable = false;
            GameBtn.GetComponentInChildren<TMP_Text>().text = "Ready";
            GameBtn.onClick.AddListener(OnGuestReady);
        }

        #region Scene Load
        PlayerPrefs.DeleteKey("SavePoint.x");
        PlayerPrefs.DeleteKey("SavePoint.y");
        PlayerPrefs.DeleteKey("Selected");
        #endregion
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        roomName = "";
        CreateRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        roomName = "";
        CreateRoom();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        RoomRenewal();
        chatManager?.ChatRPC("<color=yellow>" + newPlayer.NickName + "님이 참가하셨습니다</color>");
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        RoomRenewal();
        chatManager?.ChatRPC("<color=yellow>" + otherPlayer.NickName + "님이 퇴장하셨습니다</color>");

        // CharacterSelection 클래스 인스턴스를 찾아와서 나간 플레이어의 선택 해제 처리
        FindObjectOfType<CharacterManager>().HandlePlayerLeftRoom(otherPlayer);

        if (!PhotonNetwork.IsMasterClient)
        {
            PV.RPC("GuestReady", RpcTarget.MasterClient, false);
        }
    }

    void RoomRenewal()
    {
        ListText.text = "";
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
            ListText.text += PhotonNetwork.PlayerList[i].NickName + ((i + 1 == PhotonNetwork.PlayerList.Length) ? "" : ", ");
        RoomInfoText.text = PhotonNetwork.CurrentRoom.Name + " / 현재 " + PhotonNetwork.CurrentRoom.PlayerCount + " / 최대 " + PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer == newMasterClient)
        {
            PhotonNetwork.LeaveRoom();
            RoomPanel.SetActive(false);
        }
    }
    #endregion

    #region Room settings
    public void NextOption()
    {
        selectedOption++;
        if (selectedOption >= chapterDB.ChapterCount)
        {
            selectedOption = 0;
        }
        photonView.RPC("UpdateChapter", RpcTarget.AllBuffered, selectedOption);
    }
    public void BackOption()
    {
        selectedOption--;
        if (selectedOption < 0)
        {
            selectedOption = chapterDB.ChapterCount - 1;
        }
        photonView.RPC("UpdateChapter", RpcTarget.AllBuffered, selectedOption);
    }
    [PunRPC]
    private void UpdateChapter(int selectedOption)
    {
        Chapter chapter = chapterDB.GetChapter(selectedOption);
        //artworkSprite.sprite = chapter.chapterSprite;
        chapterText.text = chapter.chapterNum;
    }
    #endregion

    #region GameStart Btn
    public void UpdateGameButton()
    {
        CharacterManager characterSelection = FindObjectOfType<CharacterManager>();
        bool isCharacterSelected = characterSelection.IsCharacterSelected(PhotonNetwork.LocalPlayer.ActorNumber);
        if (PhotonNetwork.IsMasterClient)
        {
            GameBtn.interactable = isCharacterSelected && isGuestReady;
        }
        else
        {
            GameBtn.interactable = isCharacterSelected;
            isGuestReady = false;
            GameBtn.GetComponentInChildren<TMP_Text>().text = "Ready";
        }
    }
    public void OnGuestReady()
    {
        isGuestReady = !isGuestReady;
        PV.RPC("GuestReady", RpcTarget.MasterClient, isGuestReady);
        GameBtn.GetComponentInChildren<TMP_Text>().text = isGuestReady ? "Cancel" : "Ready";
    }
    [PunRPC]
    public void GuestReady(bool guestReady)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            isGuestReady = guestReady;
            CharacterManager characterSelection = FindObjectOfType<CharacterManager>();
            bool isHostCharacterSelected = characterSelection.IsCharacterSelected(PhotonNetwork.LocalPlayer.ActorNumber);
            GameBtn.interactable = isHostCharacterSelected && isGuestReady;
        }
    }
    private void OnHostGameStart()
    {
        if (PhotonNetwork.IsMasterClient && isGuestReady)
        {
            SoundManager.instance.PlayBgm(false);
            StartCoroutine(StartGameCoroutine("Chapter 1"));
        }
    }

    private IEnumerator StartGameCoroutine(string sceneName)
    {
        ScreenFader screenFader = FindObjectOfType<ScreenFader>();
        PV.RPC("Fade", RpcTarget.All, 1.0f, 1.0f);
        yield return new WaitForSeconds(1.0f);

        PV.RPC("SetRoomPanelActive", RpcTarget.All);
        PV.RPC("LoadGameScene", RpcTarget.Others, sceneName);
        yield return new WaitForSeconds(0.5f);
        PhotonNetwork.LoadLevel(sceneName);
    }

    [PunRPC]
    public void SetRoomPanelActive()
    {
        RoomPanel.SetActive(false);
    }
    [PunRPC]
    private void LoadGameScene(string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }
    #endregion
}
