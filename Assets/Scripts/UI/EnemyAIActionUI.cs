using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyAIActionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAI enemyAI;

    [Header("UI")]
    [SerializeField] private Image clawImage;
    [SerializeField] private Image biteImage;

    [Header("Timing")]
    [SerializeField] private float showSeconds = 0.6f;

    private Coroutine clawRoutine;
    private Coroutine biteRoutine;

    private void OnEnable()
    {
        if (enemyAI == null) return;

        enemyAI.OnClawAttackUsed.AddListener(ShowClaw);
        enemyAI.OnBiteAttackUsed.AddListener(ShowBite);
    }

    private void OnDisable()
    {
        if (enemyAI == null) return;

        enemyAI.OnClawAttackUsed.RemoveListener(ShowClaw);
        enemyAI.OnBiteAttackUsed.RemoveListener(ShowBite);
    }

    private void ShowClaw()
    {
        if (clawImage == null) return;
        if (clawRoutine != null) StopCoroutine(clawRoutine);
        clawRoutine = StartCoroutine(ShowTemporarily(clawImage));
    }

    private void ShowBite()
    {
        if (biteImage == null) return;
        if (biteRoutine != null) StopCoroutine(biteRoutine);
        biteRoutine = StartCoroutine(ShowTemporarily(biteImage));
    }

    private IEnumerator ShowTemporarily(Image img)
    {
        img.gameObject.SetActive(true);
        yield return new WaitForSeconds(showSeconds);
        img.gameObject.SetActive(false);
    }
}
