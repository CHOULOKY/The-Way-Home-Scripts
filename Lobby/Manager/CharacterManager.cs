using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviourPunCallbacks
{
    public GameObject descriptionObject;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Color highlightColor = Color.white;
    private Color otherSelectedColor = Color.red;

    private bool isSelected = false;

    // A dictionary that tracks the selected characters
    private static Dictionary<string, int> selectedCharacters = new Dictionary<string, int>();
    private static HashSet<int> clientsWithSelection = new HashSet<int>();

    private PhotonView PV;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        PV = GetComponent<PhotonView>();
    }

    private void Start()
    {
        animator.enabled = false;
        originalColor = originalColor == default ? spriteRenderer.color : originalColor;

        InitializeCharacterState();
    }

    private void InitializeCharacterState()
    {
        spriteRenderer.color = originalColor * 0.8f;
        descriptionObject.SetActive(false);

        // Reflecting other clients' selection status
        foreach (var character in selectedCharacters)
        {
            if (character.Value != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                UpdateCharacterSelection(character.Key, character.Value);
            }
        }
    }

    private void OnMouseEnter()
    {
        descriptionObject.SetActive(true);
        spriteRenderer.color = highlightColor;
    }

    private void OnMouseExit()
    {
        descriptionObject.SetActive(false);
        if (!isSelected && (!selectedCharacters.ContainsKey(gameObject.name) || selectedCharacters[gameObject.name] != PhotonNetwork.LocalPlayer.ActorNumber))
        {
            spriteRenderer.color = originalColor * 0.8f;
        }
    }

    private void OnMouseDown()
    {
        if (selectedCharacters.ContainsKey(gameObject.name) && selectedCharacters[gameObject.name] != PhotonNetwork.LocalPlayer.ActorNumber)
        {
            return;
        }

        if (isSelected)
        {
            // Deselect character
            DeselectCharacter();
        }
        else
        {
            // Cannot be selected if another character has already been selected
            if (clientsWithSelection.Contains(PhotonNetwork.LocalPlayer.ActorNumber))
            {
                return;
            }
            // Select Character
            SelectCharacter();
        }
    }

    private void SelectCharacter()
    {
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("isGround", true);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }

        isSelected = true;
        selectedCharacters[gameObject.name] = PhotonNetwork.LocalPlayer.ActorNumber;
        clientsWithSelection.Add(PhotonNetwork.LocalPlayer.ActorNumber);

        // Notify other clients of selection status
        PV.RPC("UpdateCharacterSelection", RpcTarget.OthersBuffered, gameObject.name, PhotonNetwork.LocalPlayer.ActorNumber);

        // Save selected characters to Custom Properties
        ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
        customProperties["selectedCharacter"] = gameObject.name;
        PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

        // 캐릭터 선택 상태 변경 알림
        FindObjectOfType<RoomManager>().UpdateGameButton(); // RoomManager에서 메서드 호출
    }

    private void DeselectCharacter()
    {
        if (animator != null)
        {
            animator.Rebind();
            animator.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor * 0.8f;
        }

        isSelected = false;
        selectedCharacters.Remove(gameObject.name);
        clientsWithSelection.Remove(PhotonNetwork.LocalPlayer.ActorNumber);

        if (!PhotonNetwork.IsMasterClient)
        {
            FindObjectOfType<RoomManager>().PV.RPC("GuestReady", RpcTarget.MasterClient, false);
        }

        // Notify other clients of deselect
        PV.RPC("UpdateCharacterDeselection", RpcTarget.OthersBuffered, gameObject.name);

        // Remove selected characters from Custom Properties
        ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
        customProperties.Remove("selectedCharacter");
        PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

        FindObjectOfType<RoomManager>().UpdateGameButton(); // RoomManager에서 메서드 호출
    }

    [PunRPC]
    private void UpdateCharacterSelection(string characterName, int playerID)
    {
        if (!selectedCharacters.ContainsKey(characterName))
        {
            selectedCharacters[characterName] = playerID;
            clientsWithSelection.Add(playerID);
        }

        // Change character color and disable click
        if (characterName == this.gameObject.name)
        {
            if (spriteRenderer)
                spriteRenderer.color = otherSelectedColor;
            GetComponent<CapsuleCollider2D>().enabled = false;
        }
    }

    [PunRPC]
    private void UpdateCharacterDeselection(string characterName)
    {
        if (selectedCharacters.ContainsKey(characterName))
        {
            int playerID = selectedCharacters[characterName];
            selectedCharacters.Remove(characterName);
            clientsWithSelection.Remove(playerID);
        }

        // Character color and click activation
        if (characterName == gameObject.name)
        {
            spriteRenderer.color = originalColor * 0.8f;
            GetComponent<CapsuleCollider2D>().enabled = true;
        }
    }

    public bool IsCharacterSelected(int actorNumber)
    {
        return selectedCharacters.ContainsValue(actorNumber);
    }

    public void HandlePlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        foreach (var character in new Dictionary<string, int>(selectedCharacters))
        {
            if (character.Value == otherPlayer.ActorNumber)
            {
                DeselectCharacter();
                PV.RPC("UpdateCharacterDeselection", RpcTarget.AllBuffered, character.Key);

                spriteRenderer.color = originalColor;
                animator.Rebind();
                animator.enabled = false;

                isSelected = false;
            }
        }
    }
}
