using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrologueDirector : MonoBehaviour
{
    [Header("Dialog")]
    [SerializeField] private Dialog dlgAttack;
    [SerializeField] private Dialog PlayerDialog1;
    [SerializeField] private Dialog dlgPikachuHelp;
    [SerializeField] private Dialog PlayerDialog2;
    [SerializeField] private Dialog dlgLoseRetry;

    [Header("Speaker Portraits (optional)")]
    [SerializeField] private Sprite spearowPortrait;
    [SerializeField] private Sprite playerPortrait;
    [SerializeField] private Sprite pikachuPortrait;

    [Header("Animation")]
    [SerializeField] private Animator pikachuAnimator;
    [SerializeField] private string runTriggerName = "Run";
    [SerializeField] private float pikachuRunDelay = 0.8f;

    [Header("Battle")]
    [SerializeField] private PokemonBase spearowBase;
    [SerializeField] private int spearowLevel = 3;

    [Header("Flow")]
    [SerializeField] private string nextSceneName = "Town01";
    [SerializeField] private bool skipIfDone = false;
    [SerializeField] private float fadeToBlackDuration = 0.35f;

    [Header("Pikachu Move (optional)")]
    [SerializeField] private Transform pikachuTransform;     // sprite/object Pikachu
    [SerializeField] private Transform pikachuTargetPoint;   // điểm đứng cạnh player
    [SerializeField] private float pikachuMoveSpeed = 3.5f;
    [SerializeField] private bool useMoveByTransform = true;

    private bool running;
    private bool lastBattleWon;

    private void Start()
    {
        if (skipIfDone && StoryFlags.Instance != null && StoryFlags.Instance.PrologueDone)
        {
            StartCoroutine(LoadNextSceneWithFade());
            return;
        }

        StartCoroutine(RunPrologue());
    }

    private IEnumerator RunPrologue()
    {
        if (running) yield break;
        running = true;

        // đợi 1 frame để mọi thứ initialize
        yield return null;

        if (DialogManager.Instance == null)
        {
            Debug.LogError("DialogManager.Instance is null!");
            yield break;
        }

        if (GameController.Instance == null)
        {
            Debug.LogError("GameController.Instance is null! Dialog won't lock input.");
        }

        Debug.Log("[Prologue] Starting...GameController=" + (GameController.Instance != null ? "OK" : "NULL"));

        yield return ShowDialogAndWait(dlgAttack, "Spearow", spearowPortrait);
        yield return ShowDialogAndWait(PlayerDialog1, "Player(kid)", ResolvePlayerPortrait());
        yield return ShowDialogAndWait(dlgPikachuHelp, "Pikachu", pikachuPortrait);

        if (useMoveByTransform && pikachuTransform != null && pikachuTargetPoint != null)
            yield return MovePikachuToTarget();
        else
        {
            if (pikachuAnimator != null && !string.IsNullOrWhiteSpace(runTriggerName))
                pikachuAnimator.SetTrigger(runTriggerName);

            yield return new WaitForSeconds(pikachuRunDelay);
        }

        yield return StartAndWaitBattle();

        // dùng kết quả đã ghi lại trong battle, không đọc lại EnemyUnit sau khi reset
        if (!lastBattleWon)
        {
            if (dlgLoseRetry != null)
                yield return ShowDialogAndWait(dlgLoseRetry, "Player(kid)", ResolvePlayerPortrait());

            yield return StartAndWaitBattle();
            if (!lastBattleWon)
            {
                CompletePrologue();
                yield break;
            }
        }

        yield return ShowDialogAndWait(PlayerDialog2, "Player(kid)", ResolvePlayerPortrait());
        CompletePrologue();
    }

    private Sprite ResolvePlayerPortrait()
    {
        if (PlayerController.Instance != null && PlayerController.Instance.Portrait != null)
            return PlayerController.Instance.Portrait;

        return playerPortrait;
    }

    private IEnumerator MovePikachuToTarget()
    {
        while (Vector3.Distance(pikachuTransform.position, pikachuTargetPoint.position) > 0.05f)
        {
            pikachuTransform.position = Vector3.MoveTowards(
                pikachuTransform.position,
                pikachuTargetPoint.position,
                pikachuMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        pikachuTransform.position = pikachuTargetPoint.position;
    }

    private IEnumerator StartAndWaitBattle()
    {
        if (spearowBase == null)
        {
            Debug.LogWarning("spearowBase is null.");
            yield break;
        }

        lastBattleWon = false;
        var wild = new Pokemon(spearowBase, spearowLevel);

        // Gọi qua GameController để load BattleScene và bind BattleSystem
        if (GameController.Instance != null)
            GameController.Instance.StartWildBattle(wild, false);
        else
        {
            Debug.LogError("GameController.Instance is null!");
            yield break;
        }

        // Chờ vào battle state
        yield return new WaitUntil(() =>
            GameController.Instance.State == GameState.Battle
        );

        // Chờ BattleSystem được bind (được load qua StartWildBattle)
        yield return new WaitUntil(() =>
            GameController.Instance.BattleSystem != null
        );

        var battleSystem = GameController.Instance.BattleSystem;

        // Trong lúc battle, nếu enemy faint thì đánh dấu win
        while (GameController.Instance.State == GameState.Battle)
        {
            yield return null;
        }

        // Chờ quay lại overworld
        yield return new WaitUntil(() =>
            GameController.Instance.State == GameState.Overworld
        );

        // Wild battle setup reuses the same Pokemon instance, so this is a reliable win signal.
        lastBattleWon = wild != null && wild.IsFainted;
    }

    private void CompletePrologue()
    {
        StartCoroutine(LoadNextSceneWithFade());
    }

    private IEnumerator LoadNextSceneWithFade()
    {
        var fadeController = SceneFadeController.Instance;
        if (fadeController != null)
            yield return fadeController.Fade(1f, fadeToBlackDuration);

        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator ShowDialogAndWait(Dialog dialog, string speaker, Sprite portrait = null)
    {
        if (dialog == null) 
        {
            Debug.LogWarning($"Dialog is null for speaker: {speaker}");
            yield break;
        }

        bool finished = false;

        // đảm bảo handler gọi clean, không bị capture issue
        void OnFinish()
        {
            finished = true;
            Debug.Log($"[Dialog Finish] {speaker}");
        }

        DialogManager.Instance.OnDialogFinished += OnFinish;
        
        Debug.Log($"[Dialog Start] {speaker} - GameState={GameController.Instance?.State}");
        DialogManager.Instance.ShowDialog(speaker, portrait, dialog, GameState.Overworld);

        // chờ dialog finish
        float timeout = Time.time + 30f; // 30s timeout safety
        while (!finished)
        {
            if (Time.time > timeout)
            {
                Debug.LogError($"Dialog timeout for {speaker}!");
                break;
            }
            yield return null;
        }

        DialogManager.Instance.OnDialogFinished -= OnFinish;
    }
}
