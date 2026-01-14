using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattlePartyHandler : MonoBehaviour
{
    [SerializeField] private BattleDialogBox dialogBox;
    [SerializeField] private BattleUnit playerUnit;
    [SerializeField] private BattlePartyMenu battlePartyMenu;

    private PlayerParty playerParty;
    private BattleSystem battleSystem;

    public void Init(BattleDialogBox dialog, BattleUnit player, BattlePartyMenu menu, PlayerParty party, BattleSystem system)
    {
        dialogBox = dialog;
        playerUnit = player;
        battlePartyMenu = menu;
        playerParty = party;
        battleSystem = system;
    }

    public void OpenPartyMenu(bool forceSwitch = false)
    {
        battleSystem.SetState(BattleState.PlayerPokemonSelection);
        
        battlePartyMenu.Open(playerParty.Pokemons,
            // CALLBACK KHI CHỌN 1 POKEMON
            (selectedPokemon) =>
            {
                if (selectedPokemon.IsFainted)
                {
                    // Gọi Coroutine xử lý riêng để không làm gián đoạn luồng chính
                    StartCoroutine(HandleInvalidSelection("You can't send out a fainted Pokémon!"));
                    return; 
                }

                if (selectedPokemon == playerUnit.Pokemon)
                {
                    StartCoroutine(HandleInvalidSelection("That Pokémon is already in battle!"));
                    return;
                }

                // Nếu hợp lệ
                battlePartyMenu.Close();
                battleSystem.SetState(BattleState.Busy);
                StartCoroutine(SwitchPokemon(selectedPokemon));
            },
            // CALLBACK KHI NHẤN BACK
            () =>
            {
                if (forceSwitch)
                {
                    StartCoroutine(HandleInvalidSelection("You must choose a Pokémon to continue!"));
                    return;
                }

                battlePartyMenu.Close();
                battleSystem.SetState(BattleState.PlayerActionSelection);
                dialogBox.ShowActionMenu();
            });
    }

    // HÀM BỔ TRỢ: Xử lý khi chọn sai
    IEnumerator HandleInvalidSelection(string message)
    {
        // 1. Tạm thời khóa Input của BattleSystem
        battleSystem.SetState(BattleState.Busy);

        // 2. Hiện Dialog và đợi người dùng bấm Z
        yield return dialogBox.ShowDialogAndWait(message);

        // 3. QUAN TRỌNG: Hiện lại Menu Pokémon vì ShowDialog đã lỡ tắt nó đi
        // (Giả sử script BattlePartyMenu của bạn quản lý panel này)
        battlePartyMenu.gameObject.SetActive(true); 

        // 4. Trả lại trạng thái chọn Pokémon
        battleSystem.SetState(BattleState.PlayerPokemonSelection);
    }

    private IEnumerator SwitchPokemon(Pokemon newPokemon)
    {
        dialogBox.ShowDialog($"Come back {playerUnit.Pokemon.Base.Name}!");
        yield return new WaitForSeconds(1f);

        playerUnit.Setup(newPokemon);
        dialogBox.ShowDialog($"Go! {newPokemon.Base.Name}!");
        yield return new WaitForSeconds(1f);

        battleSystem.ProceedTurn();
    }

    public void HandleFaint()
    {
        dialogBox.ShowDialog($"{playerUnit.Pokemon.Base.Name} fainted!");
        StartCoroutine(CheckPartyAfterFaint());
    }

    private IEnumerator CheckPartyAfterFaint()
    {
        yield return new WaitForSeconds(1f);

        bool allFainted = true;
        foreach (var p in playerParty.Pokemons)
        {
            if (!p.IsFainted)
            {
                allFainted = false;
                break;
            }
        }

        if (allFainted)
        {
            if (playerParty.Pokemons.Count > 0)
            {
                var first = playerParty.Pokemons[0];
                first.ReviveToOneHP();
            }

            dialogBox.ShowDialog("All your Pokémon fainted... You lose!");
            yield return new WaitForSeconds(1f);

            battleSystem.EndBattle();
        }
        else
        {
            OpenPartyMenu(forceSwitch: true);
        }
    }
}