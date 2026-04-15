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
        }
        else
        {
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

            Debug.Log($"Loaded user from prefs: {CurrentUser.Username}");
        }
    }

    private void SaveUser(UserResponse user)
    {
        PlayerPrefs.SetInt(USER_ID_KEY, user.Id);
        PlayerPrefs.SetString(USERNAME_KEY, user.Username);
        PlayerPrefs.Save();

        SetCurrentUser(user);

        Debug.Log($"Saved user: {user.Username}");
    }

    private void ClearUser()
    {
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.DeleteKey(USERNAME_KEY);

        SetCurrentUser(null);

        Debug.Log("User cleared.");
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
                Debug.Log("Saved user not found. Clearing...");
                ClearUser();
            }
            else
            {
                SetCurrentUser(user);
            }
        }
        catch
        {
            Debug.Log("Saved user invalid. Clearing...");
            ClearUser();
        }
    }

    public async Task<UserResponse> CreateUser(string username)
    {
        var user = await userApi.CreateUser(username);

        SaveUser(user);

        return user;
    }

    public async Task<List<UserResponse>> GetUsers()
    {
        return await userApi.GetUsers();
    }

    public void SelectUser(UserResponse user)
    {
        SaveUser(user);
    }

    public async Task DeleteUser(int userId)
    {
        await userApi.DeleteUser(userId);

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