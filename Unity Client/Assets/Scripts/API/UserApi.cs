using System.Collections.Generic;
using System.Threading.Tasks;

public class UserApi
{
    private readonly ApiClient apiClient;

    public UserApi()
    {
        apiClient = ApiClient.Instance;
    }

    // =========================
    // CREATE USER
    // =========================
    public async Task<UserResponse> CreateUser(string username)
    {
        var request = new UserCreateRequest
        {
            Username = username
        };

        return await apiClient.Post<UserResponse>("/users/", request);
    }

    // =========================
    // GET ALL USERS
    // =========================
    public async Task<List<UserResponse>> GetUsers()
    {
        return await apiClient.Get<List<UserResponse>>("/users/");
    }

    // =========================
    // GET USER BY ID
    // =========================
    public async Task<UserResponse> GetUser(int userId)
    {
        return await apiClient.Get<UserResponse>($"/users/{userId}");
    }
}