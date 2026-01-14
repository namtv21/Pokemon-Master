using System.Collections.Generic;
using UnityEngine;

public class PlayerParty : MonoBehaviour
{
    //[SerializeField] private List<Pokemon> pokemons = new List<Pokemon>();
    [SerializeField] private StorageSystem storage;
    public List<Pokemon> Pokemons { get; private set; } = new List<Pokemon>();

    /// Khởi tạo party với pikachu lv5
    void Start()
    {
        if (Pokemons.Count == 0)
        {
            PokemonBase pikachuBase = Resources.Load<PokemonBase>("PokemonData/pikachu");
            Pokemon pikachu = new Pokemon(pikachuBase, 5);
            AddPokemon(pikachu);
            PrintParty();

            var partyMenu = FindObjectOfType<PartyMenuUI>(); 
            if (partyMenu != null && partyMenu.gameObject.activeSelf) 
                partyMenu.Open(Pokemons, PartyMenuMode.Selection, null, null);
        }
    }

    /// Thêm Pokémon mới vào party
    public void AddPokemon(Pokemon newPokemon)
    {
        if (Pokemons.Count < 6)   // dùng property Pokemons
        {
            Pokemons.Add(newPokemon);
            Debug.Log($"{newPokemon.Base.Name} was added to your party!");
        }
        else
        {
            Debug.Log("Party is full! Send to storage instead.");
            storage.AddPokemon(newPokemon);
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
    
    /// Debug: In ra danh sách Pokémon trong party
    public void PrintParty()
    {
        foreach (var p in Pokemons)
        {
            Debug.Log($"{p.Base.Name} Lv.{p.Level} HP:{p.CurrentHp}/{p.MaxHp} Status:{p.Status}");
        }
    }
}