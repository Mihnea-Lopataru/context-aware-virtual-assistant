using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance;

    private const string USER_ID_KEY = "user_id";
    private const string USERNAME_KEY = "username";

    private UserApi userApi;

    public UserResponse CurrentUser { get; private set; }

    public Action<UserResponse> OnUserChanged;

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            await WaitForApiClient();

            userApi = new UserApi(ApiClient.Instance);

            LoadUserFromPrefs();
            await ValidateSavedUser();

            Debug.Log($"[UserManager] Initialized. CurrentUserId={CurrentUser?.Id.ToString() ?? "<none>"}");
        }
        else
        {
            Debug.LogWarning("[UserManager] Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void LoadUserFromPrefs()
    {
        if (PlayerPrefs.HasKey(USER_ID_KEY))
        {
            CurrentUser = new UserResponse
            {
                Id = PlayerPrefs.GetInt(USER_ID_KEY),
                Username = PlayerPrefs.GetString(USERNAME_KEY)
            };

            Debug.Log($"[UserManager] Loaded saved user from PlayerPrefs. UserId={CurrentUser.Id}, Username={CurrentUser.Username}");
        }
    }

    private void SaveUser(UserResponse user)
    {
        if (user == null)
        {
            Debug.LogWarning("[UserManager] SaveUser called with null user. Clearing selection.");
            ClearUser();
            return;
        }

        PlayerPrefs.SetInt(USER_ID_KEY, user.Id);
        PlayerPrefs.SetString(USERNAME_KEY, user.Username);
        PlayerPrefs.Save();

        SetCurrentUser(user);
        Debug.Log($"[UserManager] Saved current user. UserId={user.Id}, Username={user.Username}");
    }

    private void ClearUser()
    {
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.DeleteKey(USERNAME_KEY);

        SetCurrentUser(null);
        Debug.Log("[UserManager] Cleared current user.");
    }

    private void SetCurrentUser(UserResponse user)
    {
        CurrentUser = user;
        OnUserChanged?.Invoke(user);
    }

    private async Task ValidateSavedUser()
    {
        if (CurrentUser == null)
            return;

        try
        {
            var user = await userApi.GetUserById(CurrentUser.Id);

            if (user == null)
            {
                Debug.LogWarning($"[UserManager] Saved user no longer exists. UserId={CurrentUser.Id}");
                ClearUser();
            }
            else
            {
                SetCurrentUser(user);
                Debug.Log($"[UserManager] Saved user validated. UserId={user.Id}, Username={user.Username}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UserManager] Failed to validate saved user. Clearing local selection: {e.Message}");
            ClearUser();
        }
    }

    public async Task<UserResponse> CreateUser(string username)
    {
        var user = await userApi.CreateUser(username);

        SaveUser(user);
        Debug.Log($"[UserManager] Created user. UserId={user.Id}, Username={user.Username}");

        return user;
    }

    public async Task<List<UserResponse>> GetUsers()
    {
        var users = await userApi.GetUsers();
        Debug.Log($"[UserManager] Loaded users. Count={users?.Count ?? 0}");
        return users;
    }

    public void SelectUser(UserResponse user)
    {
        SaveUser(user);
        Debug.Log($"[UserManager] Selected user. UserId={user?.Id.ToString() ?? "<none>"}");
    }

    public async Task DeleteUser(int userId)
    {
        await userApi.DeleteUser(userId);
        Debug.Log($"[UserManager] Deleted user. UserId={userId}");

        if (CurrentUser != null && CurrentUser.Id == userId)
        {
            ClearUser();
        }
    }

    public async Task<UserResponse> UpdateUser(int userId, string username = null, bool? isActive = null)
    {
        var updatedUser = await userApi.UpdateUser(userId, username, isActive);

        if (CurrentUser != null && CurrentUser.Id == userId)
        {
            SaveUser(updatedUser);
        }

        return updatedUser;
    }

    public void Logout()
    {
        ClearUser();
    }

    private async Task WaitForApiClient()
    {
        while (ApiClient.Instance == null)
            await Task.Yield();
    }
}
