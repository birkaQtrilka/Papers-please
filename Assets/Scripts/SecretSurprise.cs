using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SecretSurprise : MonoBehaviour
{
    [SerializeField] string SecretCombination = "latina";
    [SerializeField] Image secretImage;
    [SerializeField] float imageStayTime = 1f;
    [SerializeField] float imageFadeDuration = 1f;
    [SerializeField] AnimationCurve fadeCurve;

    public UnityEvent OnSecretCombination;

    string currentInput = "";

    private void Start()
    {
        SecretCombination = SecretCombination.ToUpper();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.anyKeyDown)
        {
            currentInput += Input.inputString;
            if (currentInput.Length > SecretCombination.Length)
            {
                ClearInput();
            }
            if (currentInput == SecretCombination)
            {
                OnMatch();
                ClearInput();
            }
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            ClearInput();
        }
    }

    void ClearInput()
    {
        currentInput = "";
    }

    public void OnMatch()
    {
        OnSecretCombination.Invoke();
        StartCoroutine(SecretAnimation());
    }

    IEnumerator SecretAnimation()
    {
        secretImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(imageStayTime);
        float timeElapsed = 0f;
        while (timeElapsed <= imageFadeDuration)
        {
            float t = 1 - (timeElapsed / imageFadeDuration);
            secretImage.color = new Color(secretImage.color.r, secretImage.color.g, secretImage.color.b, t);
            yield return null;
            timeElapsed += Time.deltaTime;
        }
        secretImage.gameObject.SetActive(false);

    }
}
