using System.Collections.Generic;
using System.Threading.Tasks;

public class UserApi
{
    private readonly ApiClient client;

    public UserApi(ApiClient client)
    {
        this.client = client;
    }

    public async Task<UserResponse> CreateUser(string username)
    {
        var request = new CreateUserRequest
        {
            Username = username
        };

        return await client.Post<UserResponse>("/users", request);
    }

    public async Task<List<UserResponse>> GetUsers()
    {
        return await client.Get<List<UserResponse>>("/users");
    }

    public async Task<UserResponse> GetUserById(int userId)
    {
        return await client.Get<UserResponse>($"/users/{userId}");
    }

    public async Task<UserResponse> UpdateUser(int userId, string username = null, bool? isActive = null)
    {
        var request = new UpdateUserRequest
        {
            Username = username,
            IsActive = isActive
        };

        return await client.Patch<UserResponse>($"/users/{userId}", request);
    }

    public async Task DeleteUser(int userId)
    {
        await client.Delete<string>($"/users/{userId}");
    }
}