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

    public System.Action<UserResponse> OnUserChanged;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadUser();
    }

    private void Start()
    {
        userApi = new UserApi();
    }

    // =========================
    // LOAD USER (PlayerPrefs)
    // =========================
    public void LoadUser()
    {
        if (PlayerPrefs.HasKey(USER_ID_KEY))
        {
            CurrentUser = new UserResponse
            {
                Id = PlayerPrefs.GetInt(USER_ID_KEY),
                Username = PlayerPrefs.GetString(USERNAME_KEY)
            };

            OnUserChanged?.Invoke(CurrentUser);

            Debug.Log($"Loaded user: {CurrentUser.Username}");
        }
        else
        {
            Debug.Log("No saved user found.");
        }
    }

    // =========================
    // SAVE USER
    // =========================
    private void SaveUser(UserResponse user)
    {
        PlayerPrefs.SetInt(USER_ID_KEY, user.Id);
        PlayerPrefs.SetString(USERNAME_KEY, user.Username);
        PlayerPrefs.Save();

        CurrentUser = user;

        OnUserChanged?.Invoke(user);

        Debug.Log($"Saved user: {user.Username}");
    }

    // =========================
    // CREATE USER
    // =========================
    public async Task<UserResponse> CreateUser(string username)
    {
        var user = await userApi.CreateUser(username);

        SaveUser(user);

        return user;
    }

    // =========================
    // GET USERS
    // =========================
    public async Task<List<UserResponse>> GetUsers()
    {
        return await userApi.GetUsers();
    }

    // =========================
    // SELECT USER
    // =========================
    public void SelectUser(UserResponse user)
    {
        SaveUser(user);
    }

    // =========================
    // CLEAR USER (optional)
    // =========================
    public void ClearUser()
    {
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.DeleteKey(USERNAME_KEY);

        CurrentUser = null;

        Debug.Log("User cleared.");
    }
}