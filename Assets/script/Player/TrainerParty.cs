using System.Collections.Generic;
using UnityEngine;

public class TrainerParty : MonoBehaviour
{
    [Header("Danh sách Pokémon của Trainer")]
    [SerializeField] private List<PokemonSaveData> pokemons; // dữ liệu thô (Base + Level)

    [System.Serializable]
    public class PokemonSaveData
    {
        public PokemonBase baseData;
        public int level;
    }

    /// 👉 Khởi tạo danh sách Pokémon thực sự từ dữ liệu thô
    public List<Pokemon> GetTrainerPokemons()
    {
        List<Pokemon> result = new List<Pokemon>();
        foreach (var data in pokemons)
        {
            if (data.baseData != null)
                result.Add(new Pokemon(data.baseData, data.level)); // gọi constructor để khởi tạo Moves, HP...
        }
        return result;
    }

    /// 👉 Lấy Pokémon đầu tiên (nếu có)
    public Pokemon GetFirstPokemon()
    {
        var party = GetTrainerPokemons();
        return party.Count > 0 ? party[0] : null;
    }

    /// 👉 Lấy Pokémon tiếp theo chưa faint
    public Pokemon GetNextHealthyPokemon()
    {
        var party = GetTrainerPokemons();
        foreach (var pokemon in party)
        {
            if (!pokemon.IsFainted)
                return pokemon;
        }
        return null; // nếu tất cả đều faint
    }

    /// 👉 Kiểm tra Trainer còn Pokémon chưa faint không
    public bool HasHealthyPokemon()
    {
        var party = GetTrainerPokemons();
        foreach (var pokemon in party)
        {
            if (!pokemon.IsFainted)
                return true;
        }
        return false;
    }
    /// 👉 Lấy toàn bộ danh sách Pokémon
    public List<Pokemon> GetPokemons()
    {
        return GetTrainerPokemons();
    }
}