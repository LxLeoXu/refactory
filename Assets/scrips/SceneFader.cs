using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CanvasGroup))]
public class SceneFader : MonoBehaviour
{
    [SerializeField] private float fadeTime = 0.5f;
    private CanvasGroup group;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
    }

    private void Start()
    {
        StartCoroutine(Fade(1f, 0f, null));
    }

    public void FadeToScene(string sceneName)
    {
        StopAllCoroutines();
        StartCoroutine(Fade(group.alpha, 1f, sceneName));
    }

    private IEnumerator Fade(float from, float to, string sceneName)
    {
        group.blocksRaycasts = true;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        group.alpha = to;
        group.blocksRaycasts = to > 0f;
        if (sceneName != null) SceneManager.LoadScene(sceneName);
    }
}
