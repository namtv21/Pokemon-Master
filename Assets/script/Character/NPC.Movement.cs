using System.Collections;
using UnityEngine;

// Facing, patrol và các coroutine di chuyển của NPC.
// Lưu ý quan trọng: guard trong MoveTo/MoveTransformTo phải cho phép di chuyển
// trong cả GameState.Cutscene, nếu không các bước cutscene sẽ bị deadlock.
public partial class NPC
{
    public void FacePlayer()
    {
        var player = PlayerController.Instance != null ? PlayerController.Instance.transform : GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
            return;

        FaceDirection(player.position - transform.position, true);
    }

    public void FaceDirection(Vector2 worldDirection, bool idle = true)
    {
        CacheComponents();

        if (directionalAnimator != null)
            directionalAnimator.SetFacing(worldDirection, idle);
    }

    public void StartPatrol()
    {
        if (!enableMovement)
            return;

        if (patrolRoutine != null)
            StopCoroutine(patrolRoutine);

        patrolRoutine = StartCoroutine(PatrolRoutine());
    }

    public void StopPatrol()
    {
        if (patrolRoutine == null)
            return;

        StopCoroutine(patrolRoutine);
        patrolRoutine = null;
    }

    private IEnumerator PatrolRoutine()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            yield break;

        do
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var point = patrolPoints[i];
                if (point == null)
                    continue;

                yield return MoveTo(point.position, patrolMoveSpeed, true);

                if (waitAtPatrolPoint > 0f)
                    yield return new WaitForSeconds(waitAtPatrolPoint);
            }
        }
        while (loopPatrol);

        patrolRoutine = null;
    }

    public IEnumerator MoveTo(Vector3 targetPosition, float moveSpeed, bool faceTargetOnArrive)
    {
        CacheComponents();

        if (cachedRigidbody == null)
        {
            yield return MoveTransformTo(targetPosition, moveSpeed, faceTargetOnArrive);
            yield break;
        }

        Vector2 current = cachedRigidbody.position;
        Vector2 target = targetPosition;
        Vector2 lastDirection = target - current;

        // Register target position immediately so scene reload lands NPC at destination
        if (!string.IsNullOrWhiteSpace(npcId) && !string.IsNullOrWhiteSpace(runtimeStateKey))
            SaveLoadSystem.RegisterRuntimeNpcTransformState(runtimeStateKey, npcId, gameObject.scene.name, targetPosition, gameObject.activeSelf);

        if (directionalAnimator != null)
            directionalAnimator.SetMoving(true, lastDirection);

        while ((target - current).sqrMagnitude > 0.0001f)
        {
            while (GameController.Instance != null &&
                   GameController.Instance.State != GameState.Overworld &&
                   GameController.Instance.State != GameState.Cutscene)
            {
                if (directionalAnimator != null)
                    directionalAnimator.SetMoving(false, target - current);

                yield return null;
            }

            lastDirection = target - current;
            Vector2 next = Vector2.MoveTowards(current, target, Mathf.Max(0.01f, moveSpeed) * Time.fixedDeltaTime);
            cachedRigidbody.MovePosition(next);
            current = next;

            if (directionalAnimator != null)
                directionalAnimator.SetMoving(true, target - current);

            yield return new WaitForFixedUpdate();
        }

        cachedRigidbody.MovePosition(target);

        if (directionalAnimator != null)
        {
            if (faceTargetOnArrive)
                directionalAnimator.SetFacing(lastDirection, true);
            else
                directionalAnimator.SetMoving(false, lastDirection);
        }

        RegisterRuntimeState();
    }

    private IEnumerator MoveTransformTo(Vector3 targetPosition, float moveSpeed, bool faceTargetOnArrive)
    {
        Vector3 current = transform.position;
        Vector2 lastDirection = targetPosition - current;

        if (!string.IsNullOrWhiteSpace(npcId) && !string.IsNullOrWhiteSpace(runtimeStateKey))
            SaveLoadSystem.RegisterRuntimeNpcTransformState(runtimeStateKey, npcId, gameObject.scene.name, targetPosition, gameObject.activeSelf);

        if (directionalAnimator != null)
            directionalAnimator.SetMoving(true, lastDirection);

        while ((targetPosition - current).sqrMagnitude > 0.0001f)
        {
            while (GameController.Instance != null &&
                   GameController.Instance.State != GameState.Overworld &&
                   GameController.Instance.State != GameState.Cutscene)
            {
                if (directionalAnimator != null)
                    directionalAnimator.SetMoving(false, targetPosition - current);

                yield return null;
            }

            lastDirection = targetPosition - current;
            current = Vector3.MoveTowards(current, targetPosition, Mathf.Max(0.01f, moveSpeed) * Time.deltaTime);
            transform.position = current;

            if (directionalAnimator != null)
                directionalAnimator.SetMoving(true, targetPosition - current);

            yield return null;
        }

        transform.position = targetPosition;

        if (directionalAnimator != null)
        {
            if (faceTargetOnArrive)
                directionalAnimator.SetFacing(lastDirection, true);
            else
                directionalAnimator.SetMoving(false, lastDirection);
        }

        RegisterRuntimeState();
    }

    private IEnumerator MoveAfterDialogFinished(Vector3 targetPos, float speed, bool faceOnArrive)
    {
        yield return new WaitUntil(() => DialogManager.Instance == null || !DialogManager.Instance.IsShowing);
        StartCoroutine(MoveTo(targetPos, speed, faceOnArrive));
    }
}
