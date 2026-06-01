using System.Collections;
using UnityEngine;

public class HideAfterDelay : MonoBehaviour
{
    public float duration = 2f;
    void OnEnable()
    {
        StartCoroutine(HideAfterSeconds(duration));
    }

    IEnumerator HideAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}