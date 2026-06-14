using UnityEditor;
using UnityEngine;
using System;

[CustomEditor(typeof(MainStorySequence))]
public class MainStorySequenceEditor : Editor
{
    private SerializedProperty stepsProp;

    private void OnEnable()
    {
        stepsProp = serializedObject.FindProperty("steps");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("Data-driven Main Story Sequence. Add steps in order and add actions per step.", MessageType.Info);

        DrawSteps();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("+ Add Step", GUILayout.Height(28f)))
            AddStep();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSteps()
    {
        for (int i = 0; i < stepsProp.arraySize; i++)
        {
            var stepProp = stepsProp.GetArrayElementAtIndex(i);
            var stepIdProp = stepProp.FindPropertyRelative("stepId");
            string title = string.IsNullOrWhiteSpace(stepIdProp.stringValue)
                ? $"Step {i + 1}"
                : $"Step {i + 1}: {stepIdProp.stringValue}";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            stepProp.isExpanded = EditorGUILayout.Foldout(stepProp.isExpanded, title, true);

            GUI.enabled = i > 0;
            if (GUILayout.Button("Up", GUILayout.Width(40f)))
                stepsProp.MoveArrayElement(i, i - 1);

            GUI.enabled = i < stepsProp.arraySize - 1;
            if (GUILayout.Button("Dn", GUILayout.Width(40f)))
                stepsProp.MoveArrayElement(i, i + 1);

            GUI.enabled = true;
            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                stepsProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (stepProp.isExpanded)
            {
                DrawStepProperties(stepProp);
                EditorGUILayout.Space(6f);
                DrawActions(stepProp);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }

    private void DrawStepProperties(SerializedProperty stepProp)
    {
        EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("stepId"));
        EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("description"));
        EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("sceneName"));
        EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("triggerId"));
        EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("triggerOnSceneLoad"));
        var requireStoryFlagProp = stepProp.FindPropertyRelative("requireStoryFlag");
        EditorGUILayout.PropertyField(requireStoryFlagProp);
        if (requireStoryFlagProp.boolValue)
        {
            DrawFilteredStoryFlagField(stepProp.FindPropertyRelative("requiredStoryFlag"), "Required Story Flag");
            EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("requiredStoryFlagValue"));
        }
        EditorGUILayout.PropertyField(stepProp.FindPropertyRelative("oneShot"));
    }

    private void DrawActions(SerializedProperty stepProp)
    {
        var actionsProp = stepProp.FindPropertyRelative("actions");
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        for (int i = 0; i < actionsProp.arraySize; i++)
        {
            var actionProp = actionsProp.GetArrayElementAtIndex(i);
            var typeProp = actionProp.FindPropertyRelative("type");
            var actionType = GetActionType(typeProp);
            string actionLabel = GetEnumDisplayName(typeProp, actionType);

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();
            actionProp.isExpanded = EditorGUILayout.Foldout(actionProp.isExpanded, $"Action {i + 1}: {actionLabel}", true);

            GUI.enabled = i > 0;
            if (GUILayout.Button("Up", GUILayout.Width(40f)))
                actionsProp.MoveArrayElement(i, i - 1);

            GUI.enabled = i < actionsProp.arraySize - 1;
            if (GUILayout.Button("Dn", GUILayout.Width(40f)))
                actionsProp.MoveArrayElement(i, i + 1);

            GUI.enabled = true;
            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                actionsProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (actionProp.isExpanded)
            {
                EditorGUILayout.PropertyField(typeProp);
                DrawActionByType(actionProp, GetActionType(typeProp));
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }

        if (GUILayout.Button("+ Add Action"))
            ShowAddActionMenu(actionsProp);
    }

    private void ShowAddActionMenu(SerializedProperty actionsProp)
    {
        var menu = new GenericMenu();
        foreach (MainStoryActionType type in System.Enum.GetValues(typeof(MainStoryActionType)))
        {
            menu.AddItem(new GUIContent(type.ToString()), false, () =>
            {
                int index = actionsProp.arraySize;
                actionsProp.InsertArrayElementAtIndex(index);
                var newAction = actionsProp.GetArrayElementAtIndex(index);
                newAction.FindPropertyRelative("type").intValue = (int)type;
                serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }

    private static MainStoryActionType GetActionType(SerializedProperty typeProp)
    {
        return typeProp == null
            ? MainStoryActionType.ShowDialog
            : (MainStoryActionType)typeProp.intValue;
    }

    private static string GetEnumDisplayName(SerializedProperty typeProp, MainStoryActionType actionType)
    {
        if (typeProp == null)
            return actionType.ToString();

        var values = (MainStoryActionType[])Enum.GetValues(typeof(MainStoryActionType));
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == actionType)
                return i >= 0 && i < typeProp.enumDisplayNames.Length ? typeProp.enumDisplayNames[i] : actionType.ToString();
        }

        return actionType.ToString();
    }

    private void DrawActionByType(SerializedProperty actionProp, MainStoryActionType type)
    {
        switch (type)
        {
            case MainStoryActionType.ShowDialog:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("speakerName"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("portrait"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("dialogText"));
                break;

            case MainStoryActionType.AcceptQuest:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("useCurrentMainStoryQuest"));
                if (!actionProp.FindPropertyRelative("useCurrentMainStoryQuest").boolValue)
                    EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("questToAccept"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("acceptOnceOnly"));
                break;

            case MainStoryActionType.SubmitEvent:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("submitQuestEvent"));
                if (actionProp.FindPropertyRelative("submitQuestEvent").boolValue)
                {
                    EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("eventType"));
                    EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetId"));
                    EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("amount"));
                }
                break;

            case MainStoryActionType.Wait:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("waitSeconds"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("freezePlayerInput"));
                break;

            case MainStoryActionType.MoveNpc:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("npcId"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("moveTargetId"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("moveSpeed"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("faceTargetOnArrive"));
                break;

            case MainStoryActionType.PlayAnimationTrigger:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("animator"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("animationTrigger"));
                break;

            case MainStoryActionType.SetStoryFlag:
                DrawFilteredStoryFlagField(actionProp.FindPropertyRelative("storyFlag"), "Story Flag");
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("storyFlagValue"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("starterPokemonId"));
                break;

            case MainStoryActionType.GivePokemon:
                DrawPokemonResourceField(actionProp.FindPropertyRelative("pokemonResourceId"), "Pokemon");
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("pokemonLevel"));
                break;

            case MainStoryActionType.ShowChoice:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("speakerName"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("portrait"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("choicePrompt"));
                DrawChoiceOptions(actionProp.FindPropertyRelative("choiceOptions"));
                break;

            case MainStoryActionType.StartBattle:
                var battleType = actionProp.FindPropertyRelative("battleType");
                EditorGUILayout.PropertyField(battleType);
                if ((MainStoryBattleType)battleType.enumValueIndex == MainStoryBattleType.Wild)
                {
                    DrawPokemonResourceField(actionProp.FindPropertyRelative("wildPokemonResourceId"), "Wild Pokemon");
                    EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("wildPokemonLevel"));
                }
                else
                {
                    EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("trainerNpcId"));
                }
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("waitForBattleEnd"));
                if (actionProp.FindPropertyRelative("waitForBattleEnd").boolValue)
                    EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("continueOnlyIfWon"));
                break;

            case MainStoryActionType.FadeNpc:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("npcId"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("npcFadeDuration"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("disableNpcAfterFade"));
                break;

            case MainStoryActionType.GiveItem:
                DrawItemField(actionProp.FindPropertyRelative("item"), "Item");
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("itemCount"));
                break;

            case MainStoryActionType.TakeItem:
                DrawItemField(actionProp.FindPropertyRelative("item"), "Item");
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("itemCount"));
                break;
        }
    }

    private void DrawPokemonResourceField(SerializedProperty resourceIdProp, string label)
    {
        var currentPokemon = LoadPokemonBase(resourceIdProp.stringValue);
        var selectedPokemon = (PokemonBase)EditorGUILayout.ObjectField(label, currentPokemon, typeof(PokemonBase), false);

        if (selectedPokemon != currentPokemon)
            resourceIdProp.stringValue = GetPokemonResourceId(selectedPokemon);

        if (selectedPokemon == null)
            EditorGUILayout.PropertyField(resourceIdProp, new GUIContent($"{label} Resource Id"));
    }

    private PokemonBase LoadPokemonBase(string resourceId)
    {
        string normalizedId = TextKeyUtility.NormalizeResourceId(resourceId);
        if (string.IsNullOrEmpty(normalizedId))
            return null;

        return Resources.Load<PokemonBase>($"PokemonData/{normalizedId}");
    }

    private string GetPokemonResourceId(PokemonBase pokemonBase)
    {
        if (pokemonBase == null)
            return string.Empty;

        string assetPath = AssetDatabase.GetAssetPath(pokemonBase);
        if (!string.IsNullOrWhiteSpace(assetPath))
            return TextKeyUtility.NormalizeResourceId(System.IO.Path.GetFileNameWithoutExtension(assetPath));

        return TextKeyUtility.NormalizeResourceId(pokemonBase.name);
    }

    private void DrawItemField(SerializedProperty itemProp, string label)
    {
        EditorGUILayout.PropertyField(itemProp, new GUIContent(label));
    }

    private void DrawChoiceOptions(SerializedProperty choiceOptionsProp)
    {
        EditorGUILayout.LabelField("Choice Options", EditorStyles.boldLabel);

        for (int i = 0; i < choiceOptionsProp.arraySize; i++)
        {
            var option = choiceOptionsProp.GetArrayElementAtIndex(i);
            var labelProp = option.FindPropertyRelative("optionLabel");
            string title = string.IsNullOrWhiteSpace(labelProp.stringValue)
                ? $"Option {i + 1}"
                : $"Option {i + 1}: {labelProp.stringValue}";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            option.isExpanded = EditorGUILayout.Foldout(option.isExpanded, title, true);

            GUI.enabled = i > 0;
            if (GUILayout.Button("Up", GUILayout.Width(40f)))
                choiceOptionsProp.MoveArrayElement(i, i - 1);

            GUI.enabled = i < choiceOptionsProp.arraySize - 1;
            if (GUILayout.Button("Dn", GUILayout.Width(40f)))
                choiceOptionsProp.MoveArrayElement(i, i + 1);

            GUI.enabled = true;
            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                choiceOptionsProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (option.isExpanded)
            {
                EditorGUILayout.PropertyField(labelProp);

                EditorGUILayout.PropertyField(option.FindPropertyRelative("givePokemon"));
                if (option.FindPropertyRelative("givePokemon").boolValue)
                {
                    DrawPokemonResourceField(option.FindPropertyRelative("pokemonResourceId"), "Pokemon");
                    EditorGUILayout.PropertyField(option.FindPropertyRelative("pokemonLevel"));
                }

                EditorGUILayout.PropertyField(option.FindPropertyRelative("submitQuestEvent"));
                if (option.FindPropertyRelative("submitQuestEvent").boolValue)
                {
                    EditorGUILayout.PropertyField(option.FindPropertyRelative("eventType"));
                    EditorGUILayout.PropertyField(option.FindPropertyRelative("targetId"));
                    EditorGUILayout.PropertyField(option.FindPropertyRelative("amount"));
                }

                EditorGUILayout.PropertyField(option.FindPropertyRelative("setStoryFlag"));
                if (option.FindPropertyRelative("setStoryFlag").boolValue)
                {
                    DrawFilteredStoryFlagField(option.FindPropertyRelative("storyFlag"), "Story Flag");
                    EditorGUILayout.PropertyField(option.FindPropertyRelative("storyFlagValue"));
                    EditorGUILayout.PropertyField(option.FindPropertyRelative("starterPokemonId"));
                }

            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("+ Add Choice Option"))
            choiceOptionsProp.InsertArrayElementAtIndex(choiceOptionsProp.arraySize);
    }

    private void AddStep()
    {
        int index = stepsProp.arraySize;
        stepsProp.InsertArrayElementAtIndex(index);
        var step = stepsProp.GetArrayElementAtIndex(index);
        step.FindPropertyRelative("stepId").stringValue = $"step_{index + 1:00}";
        step.FindPropertyRelative("sceneName").stringValue = string.Empty;
        step.FindPropertyRelative("triggerId").stringValue = string.Empty;
        step.FindPropertyRelative("triggerOnSceneLoad").boolValue = true;
        step.FindPropertyRelative("requirePrologueDone").boolValue = false;
        step.FindPropertyRelative("requireStoryFlag").boolValue = false;
        step.FindPropertyRelative("requiredStoryFlag").enumValueIndex = (int)StoryFlagKey.StarterChosen;
        step.FindPropertyRelative("requiredStoryFlagValue").boolValue = true;
        step.FindPropertyRelative("oneShot").boolValue = true;

        var actions = step.FindPropertyRelative("actions");
        actions.arraySize = 0;
    }

    private void DrawFilteredStoryFlagField(SerializedProperty property, string label)
    {
        var allValues = (StoryFlagKey[])Enum.GetValues(typeof(StoryFlagKey));
        var filteredValues = new System.Collections.Generic.List<StoryFlagKey>();
        var filteredNames = new System.Collections.Generic.List<string>();

        foreach (var value in allValues)
        {
            if (value == StoryFlagKey.PrologueDone || value == StoryFlagKey.FirstMainQuestAccepted)
                continue;

            filteredValues.Add(value);
            filteredNames.Add(ObjectNames.NicifyVariableName(value.ToString()));
        }

        var currentValue = (StoryFlagKey)property.enumValueIndex;
        int selectedIndex = Mathf.Max(0, filteredValues.IndexOf(currentValue));
        int nextIndex = EditorGUILayout.Popup(label, selectedIndex, filteredNames.ToArray());
        property.enumValueIndex = (int)filteredValues[Mathf.Clamp(nextIndex, 0, filteredValues.Count - 1)];
    }
}
