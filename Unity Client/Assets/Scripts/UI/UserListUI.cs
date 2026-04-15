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
        var users = await UserManager.Instance.GetUsers();
        Populate(users);
    }

    private void Populate(List<UserResponse> users)
    {
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