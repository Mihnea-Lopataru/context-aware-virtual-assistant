using System.Collections.Generic;
using System.Threading.Tasks;

public class UserApi
{
    private readonly ApiClient apiClient;

    public UserApi()
    {
        apiClient = ApiClient.Instance;
    }

    public async Task<UserResponse> CreateUser(string username)
    {
        var request = new UserCreateRequest
        {
            Username = username
        };

        return await apiClient.Post<UserResponse>("/users/", request);
    }

    public async Task<List<UserResponse>> GetUsers()
    {
        return await apiClient.Get<List<UserResponse>>("/users/");
    }

    public async Task<UserResponse> GetUser(int userId)
    {
        return await apiClient.Get<UserResponse>($"/users/{userId}");
    }
}