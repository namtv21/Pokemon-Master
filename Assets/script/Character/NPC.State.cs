using System.Collections;
using UnityEngine;

// Hiệu ứng fade away và lưu/khôi phục trạng thái runtime của NPC
// (đã battle hay chưa, vị trí, còn active hay không) qua SaveLoadSystem.
public partial class NPC
{
    public IEnumerator FadeAway(float duration, bool disableNpcAfterFade, bool returnToOverworldWhenDone)
    {
        if (isFadingAway)
            yield break;

        isFadingAway = true;
        StopPatrol();

        var colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        var originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                originalColors[i] = renderers[i].color;
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null)
                    continue;

                var c = originalColors[i];
                c.a = Mathf.Lerp(originalColors[i].a, 0f, t);
                r.color = c;
            }

            yield return null;
        }

        isFadingAway = false;

        if (disableNpcAfterFade)
        {
            RegisterRuntimeState();
            gameObject.SetActive(false);
            yield break;
        }

        RegisterRuntimeState();

        if (returnToOverworldWhenDone)
            FinishInteraction();
    }

    private void RegisterRuntimeState()
    {
        if (string.IsNullOrWhiteSpace(npcId))
            return;

        if (string.IsNullOrWhiteSpace(runtimeStateKey))
            runtimeStateKey = SaveLoadSystem.BuildNpcStateKey(gameObject.scene.name, npcId, transform.position);

        SaveLoadSystem.RegisterRuntimeNpcBattleState(runtimeStateKey, canBattle);
        SaveLoadSystem.RegisterRuntimeNpcTransformState(runtimeStateKey, npcId, gameObject.scene.name, transform.position, gameObject.activeSelf);
    }
}
