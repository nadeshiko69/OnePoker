using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections;




[System.Serializable]
public class AWSConfig
{
    public string AccessKey;
    public string SecretKey;
    public string ClientId;
    public string Region;
    public string UserPoolId;
}

[System.Serializable]
public class RegisterRequest
{
    public string username;
    public string email;
    public string password;
}

public class AwsManager : MonoBehaviour
{
    private TitleManager titleManager;
    private const string CONFIG_FILE_PATH = "Assets/Config/aws-config.json";

    void Start(){
        titleManager = FindObjectOfType<TitleManager>();
    }


    IEnumerator RegisterUser(string username, string email, string password)
    {
        var json = JsonUtility.ToJson(new RegisterRequest { username = username, email = email, password = password });
        var request = new UnityWebRequest("https://your-api-endpoint/register", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 登録成功
        }
        else
        {
            // エラー処理
        }
    }

    private bool IsValidEmail(string email)
    {
        try {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch {
            return false;
        }
    }
}
