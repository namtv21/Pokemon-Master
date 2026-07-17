using System.Collections.Generic;
using UnityEngine;

public class PlayerParty : MonoBehaviour
{
    public static PlayerParty Instance { get; private set; }

    [Header("Passive Friendship")]
    [Tooltip("Số giây chơi để mỗi Pokemon đang trong party tự động nhận +1 friendship.")]
    [SerializeField, Min(1f)] private float passiveFriendshipIntervalSeconds = 180f;
    [Tooltip("Số friendship tối đa nhận tự động cho mỗi Pokemon trong một phiên. Đặt 0 để không giới hạn.")]
    [SerializeField, Min(0)] private int passiveFriendshipCapPerSession = 3;

    //[SerializeField] private List<Pokemon> pokemons = new List<Pokemon>();
    public List<Pokemon> Pokemons { get; private set; } = new List<Pokemon>();

    private readonly Dictionary<Pokemon, int> passiveFriendshipGained = new Dictionary<Pokemon, int>();
    private float passiveFriendshipTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DuplicateSystemRootUtility.DestroyDuplicate(this, Instance);
            return;
        }

        Instance = this;
    }

    /// Khởi tạo party với pikachu lv5
    void Start()
    {
        if (SaveLoadSystem.HasPendingLoadData())
            return;

        EnsureDefaultPokemonIfEmpty();
    }

    private void Update()
    {
        if (SaveLoadSystem.IsLoadInProgress || Pokemons.Count == 0)
            return;

        passiveFriendshipTimer += Time.deltaTime;
        if (passiveFriendshipTimer < passiveFriendshipIntervalSeconds)
            return;

        passiveFriendshipTimer %= passiveFriendshipIntervalSeconds;
        foreach (var pokemon in Pokemons)
        {
            if (pokemon == null)
                continue;

            passiveFriendshipGained.TryGetValue(pokemon, out int gained);
            if (passiveFriendshipCapPerSession > 0 && gained >= passiveFriendshipCapPerSession)
                continue;

            pokemon.AddFriendship(1);
            passiveFriendshipGained[pokemon] = gained + 1;
        }
    }

    public void EnsureDefaultPokemonIfEmpty()
    {
        if (Pokemons.Count == 0)
        {
            PokemonBase pikachuBase = Resources.Load<PokemonBase>("PokemonData/pikachu");
            if (pikachuBase == null)
            {
                Debug.LogError("[PlayerParty] Default Pokemon 'pikachu' could not be loaded.");
                return;
            }

            Pokemon pikachu = new Pokemon(pikachuBase, 5);
            AddPokemon(pikachu);

            var partyMenu = FindObjectOfType<PartyMenuUI>(); 
            if (partyMenu != null && partyMenu.gameObject.activeSelf) 
                partyMenu.Open(Pokemons, PartyMenuMode.Selection, null, null);
        }
    }

    /// Thêm Pokémon mới vào party
    public void AddPokemon(Pokemon newPokemon)
    {
        if (Pokemons.Count < 6)
        {
            Pokemons.Add(newPokemon);
            PokedexManager.GetOrCreate().MarkCaught(newPokemon);
        }
        else
        {
            StorageSystem.Instance?.AddPokemon(newPokemon);
        }
    }


    /// Xóa Pokémon khỏi party
    public void RemovePokemon(Pokemon pokemon)
    {
        Pokemons.Remove(pokemon);
        Debug.Log($"{pokemon.Base.Name} was removed from your party.");
    }

    /// Lấy Pokémon đầu tiên chưa fainted
    public Pokemon GetHealthyPokemon()
    {
        foreach (var p in Pokemons)
        {
            if (!p.IsFainted)
                return p;
        }
        return null; // tất cả đều fainted
    }

    /// Kiểm tra xem còn Pokémon nào chưa fainted
    public bool HasUsablePokemon()
    {
        return GetHealthyPokemon() != null;
    }

    /// Hồi phục tất cả Pokémon trong party
    public void HealAll()
    {
        foreach (var p in Pokemons)
        {
            p.HealAll(); // gọi hàm HealAll() của từng Pokemon
        }
        Debug.Log("All Pokémon in the party have been healed!");
    }

    public void RecordBattleParticipation()
    {
        foreach (var p in Pokemons)
        {
            p?.AddBattleParticipation(1);
        }
    }
    
}
