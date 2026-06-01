using System;
using System.Collections.Generic;
using UnityEngine;

public class UserListUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform content;
    [SerializeField] private UserItemUI userItemPrefab;
    [SerializeField] private GameObject noUsersText;

    private List<UserItemUI> items = new List<UserItemUI>();

    private async void Start()
    {
        try
        {
            await LoadUsers();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load users: {e.Message}");
            Debug.LogException(e);
        }
    }

    private void OnEnable()
    {
        if (UserManager.Instance != null)
            UserManager.Instance.OnUserChanged += HandleUserChanged;
    }

    private void OnDisable()
    {
        if (UserManager.Instance != null)
            UserManager.Instance.OnUserChanged -= HandleUserChanged;
    }

    private void HandleUserChanged(UserResponse user)
    {
        UpdateSelection(user);
    }

    public async System.Threading.Tasks.Task LoadUsers()
    {
        if (UserManager.Instance == null)
        {
            Debug.LogError("[UserListUI] UserManager is not available.");
            Populate(null);
            return;
        }

        var users = await UserManager.Instance.GetUsers();
        Populate(users);
    }

    private void Populate(List<UserResponse> users)
    {
        if (content == null || userItemPrefab == null)
        {
            Debug.LogError("[UserListUI] Content transform or user item prefab is not assigned.");
            return;
        }

        foreach (var item in items)
        {
            Destroy(item.gameObject);
        }
        items.Clear();

        if (users == null || users.Count == 0)
        {
            if (noUsersText != null)
                noUsersText.SetActive(true);

            return;
        }

        if (noUsersText != null)
            noUsersText.SetActive(false);

        foreach (var user in users)
        {
            var item = Instantiate(userItemPrefab, content);

            item.Setup(user, OnUserClicked);

            bool isSelected = UserManager.Instance.CurrentUser != null &&
                              UserManager.Instance.CurrentUser.Id == user.Id;

            item.SetSelected(isSelected);

            items.Add(item);
        }

        Debug.Log($"[UserListUI] Populated user list. Count={items.Count}");
    }

    private void OnUserClicked(UserResponse user)
    {
        UserManager.Instance.SelectUser(user);
        UpdateSelection(user);
    }

    private void UpdateSelection(UserResponse selectedUser)
    {
        foreach (var item in items)
        {
            bool isSelected = selectedUser != null &&
                              item.GetUserId() == selectedUser.Id;

            item.SetSelected(isSelected);
        }
    }
}
