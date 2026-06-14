using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private float typingSpeed = 0.03f;

    private Coroutine typingCoroutine;

    public void ShowDialog(string message)
    {
        gameObject.SetActive(true);
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeDialog(message));
    }

    private IEnumerator TypeDialog(string message)
    {
        dialogText.text = "";
        foreach (char letter in message)
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public void HideDialog()
    {
        dialogText.text = "";
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        gameObject.SetActive(false);
    }
}