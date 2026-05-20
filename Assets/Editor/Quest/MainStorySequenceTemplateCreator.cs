using UnityEditor;
using UnityEngine;

public static class MainStorySequenceTemplateCreator
{
    private const string IntroSceneName = "Town01";
    private const string OakLabSceneName = "Oke_Lab";
    private const string OakLabTriggerId = "oak_trigger";

    [MenuItem("Assets/Create/Quest/Main Story Sequence Template", priority = 1)]
    public static void CreateTemplate()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Main Story Sequence",
            "MainStory_FirstQuest",
            "asset",
            "Choose where to save the main story sequence asset."
        );

        if (string.IsNullOrWhiteSpace(path))
            return;

        var sequence = ScriptableObject.CreateInstance<MainStorySequence>();
        AssetDatabase.CreateAsset(sequence, path);

        var so = new SerializedObject(sequence);
        var steps = so.FindProperty("steps");
        steps.arraySize = 2;

        ConfigureIntroStep(steps.GetArrayElementAtIndex(0));
        ConfigureOakStep(steps.GetArrayElementAtIndex(1));

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sequence);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = sequence;
    }

    private static void ConfigureIntroStep(SerializedProperty step)
    {
        step.FindPropertyRelative("stepId").stringValue = "first_main_quest_intro";
        step.FindPropertyRelative("sceneName").stringValue = IntroSceneName;
        step.FindPropertyRelative("triggerId").stringValue = string.Empty;
        step.FindPropertyRelative("triggerOnSceneLoad").boolValue = true;
        step.FindPropertyRelative("requirePrologueDone").boolValue = false;
        step.FindPropertyRelative("oneShot").boolValue = true;

        var actions = step.FindPropertyRelative("actions");
        actions.arraySize = 2;

        var dialogAction = actions.GetArrayElementAtIndex(0);
        dialogAction.FindPropertyRelative("type").enumValueIndex = (int)MainStoryActionType.ShowDialog;
        dialogAction.FindPropertyRelative("speakerName").stringValue = "Narrator";
        dialogAction.FindPropertyRelative("dialogText").stringValue = "Tien si Oke goi minh hom nay.";

        var setFlagAction = actions.GetArrayElementAtIndex(1);
        setFlagAction.FindPropertyRelative("type").enumValueIndex = (int)MainStoryActionType.SetStoryFlag;
        setFlagAction.FindPropertyRelative("storyFlag").enumValueIndex = (int)StoryFlagKey.FirstMainQuestAccepted;
        setFlagAction.FindPropertyRelative("storyFlagValue").boolValue = true;
    }

    private static void ConfigureOakStep(SerializedProperty step)
    {
        step.FindPropertyRelative("stepId").stringValue = "meet_professor_oak";
        step.FindPropertyRelative("sceneName").stringValue = OakLabSceneName;
        step.FindPropertyRelative("triggerId").stringValue = OakLabTriggerId;
        step.FindPropertyRelative("triggerOnSceneLoad").boolValue = false;
        step.FindPropertyRelative("requirePrologueDone").boolValue = false;
        step.FindPropertyRelative("oneShot").boolValue = true;

        var actions = step.FindPropertyRelative("actions");
        actions.arraySize = 2;

        var dialogAction = actions.GetArrayElementAtIndex(0);
        dialogAction.FindPropertyRelative("type").enumValueIndex = (int)MainStoryActionType.ShowDialog;
        dialogAction.FindPropertyRelative("speakerName").stringValue = "Professor Oak";
        dialogAction.FindPropertyRelative("dialogText").stringValue = "Welcome, trainer!\nPick your first Pokemon.";

        var choiceAction = actions.GetArrayElementAtIndex(1);
        choiceAction.FindPropertyRelative("type").enumValueIndex = (int)MainStoryActionType.ShowChoice;
        choiceAction.FindPropertyRelative("speakerName").stringValue = "Professor Oak";
        choiceAction.FindPropertyRelative("choicePrompt").stringValue = "Choose your first Pokemon:";

        var choiceOptions = choiceAction.FindPropertyRelative("choiceOptions");
        choiceOptions.arraySize = 3;

        ConfigureChoiceOption(choiceOptions.GetArrayElementAtIndex(0), "Bulbasaur", "bulbasaur");
        ConfigureChoiceOption(choiceOptions.GetArrayElementAtIndex(1), "Charmander", "charmander");
        ConfigureChoiceOption(choiceOptions.GetArrayElementAtIndex(2), "Squirtle", "squirtle");
    }

    private static void ConfigureChoiceOption(SerializedProperty option, string label, string pokemonId)
    {
        option.FindPropertyRelative("optionLabel").stringValue = label;
        option.FindPropertyRelative("givePokemon").boolValue = true;
        option.FindPropertyRelative("pokemonResourceId").stringValue = pokemonId;
        option.FindPropertyRelative("pokemonLevel").intValue = 5;
        option.FindPropertyRelative("submitQuestEvent").boolValue = true;
        option.FindPropertyRelative("eventType").enumValueIndex = (int)QuestEventType.PokemonOwned;
        option.FindPropertyRelative("targetId").stringValue = "starter_received";
        option.FindPropertyRelative("amount").intValue = 1;
        option.FindPropertyRelative("setStoryFlag").boolValue = true;
        option.FindPropertyRelative("storyFlag").enumValueIndex = (int)StoryFlagKey.StarterChosen;
        option.FindPropertyRelative("storyFlagValue").boolValue = true;
        option.FindPropertyRelative("starterPokemonId").stringValue = pokemonId;
        option.FindPropertyRelative("resultLine").stringValue = "Great choice! {pokemon} joins your team!";
    }

    [MenuItem("Assets/Create/Quest/Main Story Sequence Template", true)]
    private static bool ValidateCreateTemplate()
    {
        return true;
    }
}
