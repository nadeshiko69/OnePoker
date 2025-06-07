using UnityEngine;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using System.Threading.Tasks;
using System;

public class AuthManager : MonoBehaviour
{
    private static AuthManager instance;
    public static AuthManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AuthManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("AuthManager");
                    instance = obj.AddComponent<AuthManager>();
                }
            }
            return instance;
        }
    }

    // AWS Cognito設定
    [SerializeField] private string userPoolId = "YOUR_USER_POOL_ID";
    [SerializeField] private string clientId = "YOUR_CLIENT_ID";
    [SerializeField] private string region = "YOUR_REGION"; // 例: "ap-northeast-1"

    private AmazonCognitoIdentityProviderClient cognitoClient;
    private AWSCredentials credentials;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeCognitoClient();
    }

    private void InitializeCognitoClient()
    {
        credentials = new AnonymousAWSCredentials();
        cognitoClient = new AmazonCognitoIdentityProviderClient(
            credentials,
            RegionEndpoint.GetBySystemName(region)
        );
    }

    public async Task<bool> SignUpAsync(string username, string password, string email)
    {
        try
        {
            var signUpRequest = new SignUpRequest
            {
                ClientId = clientId,
                Username = username,
                Password = password,
                UserAttributes = new System.Collections.Generic.List<AttributeType>
                {
                    new AttributeType
                    {
                        Name = "email",
                        Value = email
                    }
                }
            };

            var response = await cognitoClient.SignUpAsync(signUpRequest);
            Debug.Log($"User {username} signed up successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error signing up: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ConfirmSignUpAsync(string username, string confirmationCode)
    {
        try
        {
            var confirmRequest = new ConfirmSignUpRequest
            {
                ClientId = clientId,
                Username = username,
                ConfirmationCode = confirmationCode
            };

            await cognitoClient.ConfirmSignUpAsync(confirmRequest);
            Debug.Log($"User {username} confirmed successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error confirming sign up: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SignInAsync(string username, string password)
    {
        try
        {
            var authRequest = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = clientId,
                AuthParameters = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "USERNAME", username },
                    { "PASSWORD", password }
                }
            };

            var response = await cognitoClient.InitiateAuthAsync(authRequest);
            Debug.Log($"User {username} signed in successfully!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error signing in: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ForgotPasswordAsync(string username)
    {
        try
        {
            var forgotPasswordRequest = new ForgotPasswordRequest
            {
                ClientId = clientId,
                Username = username
            };

            await cognitoClient.ForgotPasswordAsync(forgotPasswordRequest);
            Debug.Log($"Password reset initiated for user {username}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initiating password reset: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ConfirmForgotPasswordAsync(string username, string confirmationCode, string newPassword)
    {
        try
        {
            var confirmRequest = new ConfirmForgotPasswordRequest
            {
                ClientId = clientId,
                Username = username,
                ConfirmationCode = confirmationCode,
                Password = newPassword
            };

            await cognitoClient.ConfirmForgotPasswordAsync(confirmRequest);
            Debug.Log($"Password reset confirmed for user {username}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error confirming password reset: {ex.Message}");
            return false;
        }
    }
} 