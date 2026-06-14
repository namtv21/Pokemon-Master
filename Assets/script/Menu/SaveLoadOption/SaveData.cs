using System.Collections.Generic;


[System.Serializable]
public class SaveData
{
    public List<PokemonData> partyPokemons;
    public List<PokemonData> storagePokemons;
    public int money;
    public string sceneName;
    public float playerX, playerY, playerZ;
    public QuestSaveSnapshot questSnapshot;
    public bool storyPrologueDone;
    public bool storyFirstMainQuestAccepted;
    public bool storyStarterChosen;
    public string storyStarterPokemonId;
    public bool storyInCave;
    public bool storyAfterGrassGym;
    public bool storyAfterWaterGym;
    public bool storyAfterFireGym;
    public int storyMainSequenceIndex;
    public int storyMainStepIndex;
    public PokedexSaveData pokedex;
    public List<NPCStateSaveData> npcStates;
    public List<ItemStackSaveData> inventoryItems;
    public List<string> triggeredTriggers;
    public List<string> capturedOverworldPokemonIds;
}

[System.Serializable]
public class NPCStateSaveData
{
    public string stateKey;
    public string npcId;
    public bool canBattle;
    public string sceneName;
    public float posX;
    public float posY;
    public float posZ;
    public bool isActive = true;
}

[System.Serializable]
public class QuestSaveSnapshot
{
    public List<string> completedQuestIds = new();
    public List<string> readyToTurnInQuestIds = new();
    public List<QuestStateSaveData> activeQuests = new();
}

[System.Serializable]
public class QuestStateSaveData
{
    public string questId;
    public List<ObjectiveProgressSaveData> objectives = new();
}

[System.Serializable]
public class ObjectiveProgressSaveData
{
    public int current;
    public bool completed;
}

[System.Serializable]
public class PokedexSaveData
{
    public List<string> seenPokemonIds = new();
    public List<string> caughtPokemonIds = new();
}

[System.Serializable]
public class ItemStackSaveData
{
    public string itemName;
    public int count;
    public int storedExp;
}
