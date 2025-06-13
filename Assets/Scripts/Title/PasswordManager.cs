using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PasswordController : MonoBehaviour
{
    public TMP_InputField  passField;
    public GameObject maskingOffButton;
    public GameObject maskingOnButton;
    
    void Start()
    {
        OnClickMaskingOnButton();
    }

    public void OnClickMaskingOffButton()
    {
        maskingOffButton.SetActive(false);
        maskingOnButton.SetActive(true);        
        passField.contentType = TMP_InputField .ContentType.Standard;
        StartCoroutine(ReloadInputField());
    }

    public void OnClickMaskingOnButton()
    {
        maskingOffButton.SetActive(true);
        maskingOnButton.SetActive(false);        
        passField.contentType = TMP_InputField .ContentType.Password;
        StartCoroutine(ReloadInputField());
    }

    private IEnumerator ReloadInputField()
    {
        passField.ActivateInputField();
        yield return null;
        passField.MoveTextEnd(true);
    }
}