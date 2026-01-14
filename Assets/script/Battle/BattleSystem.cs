using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum BattleState
{
    Start,
    PlayerActionSelection,
    PlayerMoveSelection,
    PlayerItemSelection,
    PlayerPokemonSelection,
    NewMoveSelection,
    WaitForNextTrainerPokemon,
    EnemyMove,
    Busy,
    BattleOver
}

public class BattleSystem : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private BattleItemHandler itemHandler;
    [SerializeField] private BattleItemMenu battleItemMenu;
    [SerializeField] private BattlePartyHandler partyHandler;
    [SerializeField] private BattlePartyMenu battlePartyMenu;
    [SerializeField] private BattleDialogBox dialogBox;
    [SerializeField] private PlayerParty playerParty;
    [SerializeField] private BattleUnit playerUnit;
    public BattleUnit PlayerUnit => playerUnit;
    [SerializeField] private BattleUnit enemyUnit;
    public BattleUnit EnemyUnit => enemyUnit;

    [SerializeField] private GameObject battleUI;
    [SerializeField] private AudioClip trainerBattleClip;
    [SerializeField] private AudioClip wildBattleClip;
    [SerializeField] private MoveLearnUI moveLearnUI;
    private int currentEnemyIndex;          // chỉ số Pokémon hiện tại của Trainer
    private List<Pokemon> trainerParty;     // danh sách Pokémon của Trainer

    private BattleState state;
    private NPC currentTrainer;
    private bool isTrainerBattle;
    
    void Awake()
    {
        battleUI.SetActive(false);
    }
    public event System.Action<BattleState> OnStateChanged;

    public void SetState(BattleState newState)
    {
        state = newState;
        OnStateChanged?.Invoke(state);
    }
    IEnumerator SetupBattle()
    {
        // Hiện dialog chào mừng
        dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} appeared!");
        yield return new WaitForSeconds(0.5f);

        dialogBox.ShowDialog($"Go, {playerUnit.Pokemon.Base.Name}!");
        yield return new WaitForSeconds(0.5f);

        if (playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed)
        {
            PlayerAction();
        }
        else
        {
            yield return StartCoroutine(EnemyTurn());
        }
        playerUnit.Pokemon.ResetStatBoosts();
        enemyUnit.Pokemon.ResetStatBoosts();
        //playerUnit.Pokemon.OnMoveLearnRequested += HandleMoveLearn;
    }
    public void StartWildBattle(Pokemon wildPokemon)
    {
        MusicManager.Instance.PlayMusic(wildBattleClip);
        battleUI.SetActive(true);
        isTrainerBattle = false;
        var playerPokemon = playerParty.GetHealthyPokemon();
        playerUnit.Setup(playerPokemon);
        enemyUnit.Setup(wildPokemon);
        itemHandler.Init(dialogBox, battleUI, MenuController.Instance.Inventory, true, wildPokemon, this);
        partyHandler.Init(dialogBox, playerUnit, battlePartyMenu, playerParty, this);
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(NPC trainer)
    {
        MusicManager.Instance.PlayMusic(trainerBattleClip);
        battleUI.SetActive(true);
        isTrainerBattle = true;
        currentTrainer = trainer;
        var playerPokemon = playerParty.GetHealthyPokemon();
        playerUnit.Setup(playerPokemon);

        var enemyPokemon = trainer.Party.GetFirstPokemon();
        enemyUnit.Setup(enemyPokemon);
        trainerParty = trainer.Party.GetPokemons();
        currentEnemyIndex = 0;
        enemyUnit.Pokemon.HealAll();

        itemHandler.Init(dialogBox, battleUI, MenuController.Instance.Inventory, false, null, this);
        partyHandler.Init(dialogBox, playerUnit, battlePartyMenu, playerParty, this);
        SetState(BattleState.Start);
        StartCoroutine(SetupBattle());
    }

    public void PlayerAction()
    {
        SetState(BattleState.PlayerActionSelection);
        dialogBox.ShowActionMenu(); // Hiện menu Fight/Run/Pokemon/Item
    }

    void Update()
    {
        switch (state)
        {
            case BattleState.PlayerActionSelection:
                HandleActionInput();
                break;

            case BattleState.PlayerMoveSelection:
                HandleMoveInput();
                break;

            case BattleState.PlayerItemSelection:
                HandleItemInput();
                break;
            
            case BattleState.PlayerPokemonSelection:
                if (battlePartyMenu.gameObject.activeSelf)
                    battlePartyMenu.HandleUpdate();
                // Gọi menu tự xử lý input
                break;

            case BattleState.NewMoveSelection:
                moveLearnUI.HandleUpdate();
                break;
            
            case BattleState.WaitForNextTrainerPokemon:
                if (isTrainerBattle)
                {
                    state = BattleState.Busy;
                    StartCoroutine(SendNextTrainerPokemon());
                }
                else
                {
                    EndBattle();
                }
                break;

            // Busy, EnemyMove, không nhận input
        }
    }
    void HandleActionInput()
    {
        dialogBox.HandleActionSelection();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            int actionIndex = dialogBox.GetSelectedAction();
            if (actionIndex == 0) // Fight
            {
                SetState(BattleState.PlayerMoveSelection);
                dialogBox.ShowMoveMenu(playerUnit.Pokemon.Moves);
            }
            else if (actionIndex == 1) // Pokémon
            {
                SetState(BattleState.PlayerPokemonSelection);
                OnPartySelected();
            }
            else if (actionIndex == 2) // Run
            {
                SetState(BattleState.BattleOver);
                EndBattle();
            }
            else if (actionIndex == 3) // Item
            {
                SetState(BattleState.PlayerItemSelection);
                battleItemMenu.OpenMenu(
                    MenuController.Instance.Inventory.GetSlots(),
                    OnItemSelected,
                    OnItemMenuClosed
                );
            }
        }
    }

    //---------Dùng item trong battle -------------//
    void HandleItemInput()
    {
        // Gọi menu tự xử lý input
        battleItemMenu.HandleUpdate();
    }

    // Callback khi chọn item
    void OnItemSelected(ItemBase item)
    {
        if (item.itemType == ItemType.Pokeball)
        {
            // trực tiếp dùng item lên enemyUnit
            StartCoroutine(itemHandler.UseItemOnPokemon(item, enemyUnit.Pokemon, enemyUnit));
            battleItemMenu.CloseMenu();
            return;
        }

        // Mở PartyMenu để chọn Pokémon target
        state = BattleState.PlayerPokemonSelection;
        battlePartyMenu.Open(playerParty.Pokemons,
            (pokemon) =>
            {
            // Nếu chọn đúng con đang ra trận → dùng trực tiếp BattleUnit
            if (pokemon == playerUnit.Pokemon)
            {
                StartCoroutine(itemHandler.UseItemOnPokemon(item, playerUnit.Pokemon, playerUnit));
            }
            else
            {
                // Nếu chọn Pokémon khác trong party → hồi máu trực tiếp trên đối tượng Pokemon
                StartCoroutine(itemHandler.UseItemOnPokemon(item, pokemon));
                battlePartyMenu.RefreshSlots(); // cập nhật UI
            }

            battleItemMenu.CloseMenu();
            battlePartyMenu.Close();
        },
        () =>
        {
            state = BattleState.PlayerActionSelection;
            dialogBox.ShowActionMenu();
            battleItemMenu.CloseMenu();
            battlePartyMenu.Close();
        });

    }


    // Callback khi đóng menu (nhấn X)
    void OnItemMenuClosed()
    {
        state = BattleState.PlayerActionSelection;
        dialogBox.ShowActionMenu();
        battleItemMenu.CloseMenu();
        battlePartyMenu.Close();
    }

    //---------Dùng Pokémon trong battle -------------//

    void HandlePartyInput()
    {
        // Gọi menu tự xử lý input
        battlePartyMenu.HandleUpdate();
    }
    void OnPartySelected()
    {
        state = BattleState.PlayerPokemonSelection;
        partyHandler.OpenPartyMenu(false);
    }

    //---------Dùng move trong battle -------------//
    void HandleMoveInput()
    {
        dialogBox.HandleMoveSelection();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            int moveIndex = dialogBox.GetSelectedMove();
            var move = playerUnit.Pokemon.Moves[moveIndex];
            SetState(BattleState.Busy); // changed
            StartCoroutine(PerformPlayerMove(move));
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            state = BattleState.PlayerActionSelection;
            dialogBox.ShowActionMenu();
        }
    }

    // tính sát thương
    int CalculateDamage(Pokemon attacker, Pokemon defender, Move move, out bool isCritical)
    {
        isCritical = false;

        bool isSpecial = move.Base.Category.ToLower() == "special";
        float attack = isSpecial ? attacker.SpAttack : attacker.Attack;
        float defense = isSpecial ? defender.SpDefense : defender.Defense;
        
        float damage = (2f * attacker.Level / 5f + 2) * move.Base.Power * (attack / defense) / 50f + 2;
        float effectiveness = TypeChart.GetEffectiveness(move.Base.Type, defender.Base.Type1, defender.Base.Type2);

        damage *= effectiveness;
        if (Random.value < 0.05f)
        {
            damage *= 1.5f;
            isCritical = true;
        }

        return Mathf.FloorToInt(damage);
    }

    IEnumerator PerformPlayerMove(Move move)
    {
        state = BattleState.Busy;
        // Kiểm tra status trước khi hành động
        bool blocked = false;
        yield return StartCoroutine(CheckStatusBeforeMove(
            playerUnit.Pokemon,
            playerUnit,
            ProceedTurn,
            (res) => blocked = res
        ));

        if (blocked)
            yield break;

        if (Random.Range(1, 101) > move.Base.Accuracy * playerUnit.Pokemon.AccuracyModifier)
        {
            dialogBox.ShowDialog($"{playerUnit.Pokemon.Base.Name}'s attack missed!");
            yield return new WaitForSeconds(1f);
            ProceedTurn();
            yield break;
        }
        // 👉 Nếu không bị block, miss thì tiếp tục thực hiện move
        dialogBox.ShowDialog($"{playerUnit.Pokemon.Base.Name} used {move.Base.MoveName}!");
        yield return new WaitForSeconds(1f);

        if (move.Base.Power >0){
            // Tính sát thương và trừ máu
            move.UseMove();
            if (enemyUnit.Pokemon.Status == StatusEffect.Protected)
            {
                dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} is protected!");
                proceddTurn: yield return new WaitForSeconds(1f);
                goto proceddTurn;
            }
            bool isCritical = false;
            int damage = CalculateDamage(playerUnit.Pokemon, enemyUnit.Pokemon, move, out isCritical);      
            enemyUnit.Pokemon.TakeDamage(damage);
            enemyUnit.Hud.UpdateHP();
            if (isCritical)
            {
                dialogBox.ShowDialog("A critical hit!");
                yield return new WaitForSeconds(1f);
            }
            float effectiveness = TypeChart.GetEffectiveness(move.Base.Type, enemyUnit.Pokemon.Base.Type1, enemyUnit.Pokemon.Base.Type2);
            if (effectiveness > 1f)
            {
                dialogBox.ShowDialog("It's super effective!");
                yield return new WaitForSeconds(1.5f);
            }
            else if (effectiveness < 1f && effectiveness > 0f)
            {
                dialogBox.ShowDialog("It's not very effective...");
                yield return new WaitForSeconds(1.5f);
            }
            else if (effectiveness == 0f)
            {
                dialogBox.ShowDialog("It had no effect...");
                yield return new WaitForSeconds(1.5f);
            }

            if (enemyUnit.Pokemon.Status == StatusEffect.Sleep)
            {
                enemyUnit.Pokemon.CureStatus();
                dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} woke up !");
                enemyUnit.Hud.SetStatus(enemyUnit.Pokemon.Status);
            }
        }
        else
        {
            HandleNonDamageMove(playerUnit.Pokemon, enemyUnit.Pokemon, move);
            yield return new WaitForSeconds(1f);
        }

        // Kiểm tra faint
        if (enemyUnit.Pokemon.IsFainted)
        {
            dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} fainted!");
            yield return new WaitForSeconds(1f);
            
            // EXP cho player
            int expGain = CalculateExp(enemyUnit.Pokemon);
            playerUnit.Pokemon.OnMoveLearnRequested += HandleMoveLearn;
            playerUnit.Pokemon.GainExp(expGain);
            dialogBox.ShowDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} EXP!");
            
            yield return new WaitForSeconds(1f);
            playerUnit.Hud.SetData(playerUnit.Pokemon);

            if (state == BattleState.NewMoveSelection)
                yield break;

            // 👉 Kiểm tra nếu là Trainer battle
            if (isTrainerBattle)
            {
                SetState(BattleState.WaitForNextTrainerPokemon);
            }
            else
            {
                yield return new WaitForSeconds(1f);
                EndBattle();
            }
        }
        else
        {
            yield return StartCoroutine(EnemyTurn());
        }
    }

    
    public IEnumerator EnemyTurn()
    {
        SetState(BattleState.EnemyMove); // changed
        var enemyMove = enemyUnit.Pokemon.Moves[Random.Range(0, enemyUnit.Pokemon.Moves.Count)];

        yield return StartCoroutine(PerformEnemyMove(enemyMove));
    }

    IEnumerator PerformEnemyMove(Move move)
    {
        // Kiểm tra status trước khi hành động
        bool blocked = false;
        yield return StartCoroutine(CheckStatusBeforeMove(
            enemyUnit.Pokemon,
            enemyUnit,
            ProceedTurn,
            (res) => blocked = res
        ));
        if (blocked)
            yield break;
        if (Random.Range(1, 101) > move.Base.Accuracy * enemyUnit.Pokemon.AccuracyModifier)
        {
            dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name}'s attack missed!");
            yield return new WaitForSeconds(1f);
            PlayerAction();
            yield break;
        }
        // Nếu không bị block, miss thì tiếp tục thực hiện move
        dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} used {move.Base.MoveName}!");
        yield return new WaitForSeconds(1f);
        if (move.Base.Power >0){
            // Tính sát thương và trừ máu
            bool isCritical;
            int damage = CalculateDamage(enemyUnit.Pokemon, playerUnit.Pokemon, move, out isCritical);
            playerUnit.Pokemon.TakeDamage(damage);
            playerUnit.Hud.UpdateHP();
            if (isCritical)
            {
                dialogBox.ShowDialog("A critical hit!");
                yield return new WaitForSeconds(1f);
            }
            if (playerUnit.Pokemon.Status == StatusEffect.Sleep)
            {
                playerUnit.Pokemon.CureStatus();
                dialogBox.ShowDialog($"{playerUnit.Pokemon.Base.Name} woke up !");
                playerUnit.Hud.SetStatus(playerUnit.Pokemon.Status);
            }
        }
        else
        {
            HandleNonDamageMove(enemyUnit.Pokemon, playerUnit.Pokemon, move);
            yield return new WaitForSeconds(1f);
        }
        // Kiểm tra faint
        if (playerUnit.Pokemon.IsFainted)
        {
            partyHandler.HandleFaint();
        }
        else
        {
            PlayerAction();
        }
    }

   public void ProceedTurn()
    {
        if (state == BattleState.EnemyMove)
            PlayerAction();
        else if (state == BattleState.Busy)
            StartCoroutine(EnemyTurn());
    }

    private void HandleNonDamageMove(Pokemon attacker, Pokemon defender, Move move)
    {
        // 1. Nếu có boost/debuff
        if (move.Base.StatBoosts != null && move.Base.StatBoosts.Count > 0)
        {
            if (move.Base.Target == MoveTarget.Self)
                ApplyBoosts(attacker, move.Base.StatBoosts);
            else
                ApplyBoosts(defender, move.Base.StatBoosts);
        }
        else
        // 2. Nếu có status effect
        if (!string.IsNullOrEmpty(move.Base.StatusEffect))
        {
            switch (move.Base.StatusEffect.ToLower())
            {
                case "poison":
                    defender.ApplyStatus(StatusEffect.Poison);
                    if (defender == playerUnit.Pokemon)
                        playerUnit.Hud.SetStatus(defender.Status);
                    else
                        enemyUnit.Hud.SetStatus(defender.Status);
                    dialogBox.ShowDialog($"{defender.Base.Name} was poisoned!");
                    break;

                case "confusion":
                    defender.ApplyStatus(StatusEffect.Confusion);
                    if (defender == playerUnit.Pokemon)
                        playerUnit.Hud.SetStatus(defender.Status);
                    else
                        enemyUnit.Hud.SetStatus(defender.Status);
                    dialogBox.ShowDialog($"{defender.Base.Name} became confused!");
                    break;

                case "sleep":
                    defender.ApplyStatus(StatusEffect.Sleep);
                    if (defender == playerUnit.Pokemon)
                        playerUnit.Hud.SetStatus(defender.Status);
                    else
                        enemyUnit.Hud.SetStatus(defender.Status);
                    dialogBox.ShowDialog($"{defender.Base.Name} fell asleep!");
                    break;

                case "paralyze":
                    defender.ApplyStatus(StatusEffect.Paralyze);
                    if (defender == playerUnit.Pokemon)
                        playerUnit.Hud.SetStatus(defender.Status);
                    else
                        enemyUnit.Hud.SetStatus(defender.Status);
                    dialogBox.ShowDialog($"{defender.Base.Name} was paralyzed!");
                    break;

                case "protected":
                    attacker.ApplyStatus(StatusEffect.Protected);
                    if (attacker == playerUnit.Pokemon)
                        playerUnit.Hud.SetStatus(attacker.Status);
                    else
                        enemyUnit.Hud.SetStatus(attacker.Status);
                    dialogBox.ShowDialog($"{attacker.Base.Name} is protected!");
                    break;

                default:
                    Debug.Log($"Status {move.Base.StatusEffect} not implemented yet.");
                    break;
            }
        }
        else
        // 3. Nếu có drain
        if (move.Base.DrainRatio > 0)
        {
            int healAmount = Mathf.FloorToInt(attacker.MaxHp * move.Base.DrainRatio);
            attacker.Heal(healAmount);
            dialogBox.ShowDialog($"{attacker.Base.Name} drained and healed {healAmount} HP!");
        }
        else
        {
            dialogBox.ShowDialog($"{move.Base.MoveName} had no effect.");
        }
    }
    private void ApplyBoosts(Pokemon target, List<StatBoost> boosts)
    {
        foreach (var boost in boosts)
        {
            int currentStacks = target.GetCurrentStacks(boost.stat);
            if (currentStacks >= boost.maxStacks)
            {
                dialogBox.ShowDialog($"{target.Base.Name}'s {boost.stat} can't be changed more than that!");
                continue;
            }

            // Áp dụng buff/debuff
            target.ModifyStat(boost.stat, boost.multiplier);
            target.IncrementStacks(boost.stat);

            dialogBox.ShowDialog($"{target.Base.Name}'s {boost.stat} changed by {boost.amount * 10}%!");
        }
    }

    
    private IEnumerator CheckStatusBeforeMove(Pokemon pokemon, BattleUnit unit, System.Action proceedTurnCallback, System.Action<bool> resultCallback)
    {
        if (pokemon.Status == StatusEffect.Sleep)
        {
            dialogBox.ShowDialog($"{pokemon.Base.Name} is asleep and can't move!");
            unit.Hud.SetData(pokemon);
            yield return new WaitForSeconds(1f);
            proceedTurnCallback();
            resultCallback(true);
            yield break;
        }

        if (pokemon.Status == StatusEffect.Paralyze)
        {
            if (Random.value < 0.25f)
            {
                dialogBox.ShowDialog($"{pokemon.Base.Name} is paralyzed and can't move!");
                unit.Hud.SetData(pokemon);
                yield return new WaitForSeconds(1f);
                proceedTurnCallback();
                resultCallback(true);
                yield break;
            }
        }

        if (pokemon.Status == StatusEffect.Poison)
        {
            int poisonDamage = Mathf.FloorToInt(pokemon.MaxHp / 8f);
            pokemon.TakeDamage(poisonDamage);
            unit.Hud.SetData(pokemon);
            dialogBox.ShowDialog($"{pokemon.Base.Name} is hurt by poison!");
            yield return new WaitForSeconds(1f);
        }

        if (pokemon.Status == StatusEffect.Confusion)
        {
            if (Random.value < 0.33f)
            {
                int selfDamage = CalculateConfusionDamage(pokemon);
                pokemon.TakeDamage(selfDamage);
                unit.Hud.SetData(pokemon);
                dialogBox.ShowDialog($"{pokemon.Base.Name} is confused and hurt itself!");
                yield return new WaitForSeconds(1f);
                proceedTurnCallback();
                resultCallback(true);
                yield break;
            }
        }

        if (pokemon.Status == StatusEffect.Protected)
        {
            dialogBox.ShowDialog($"{pokemon.Base.Name} is protected!");
            yield return new WaitForSeconds(1f);
            proceedTurnCallback();
            resultCallback(true);
            yield break;
        }

        // Nếu không bị chặn bởi status
        resultCallback(false);
    }

    private int CalculateConfusionDamage(Pokemon target)
    {
        // Damage confusion thường là như một move Physical 40 power
        int power = 40;
        float attack = target.Attack;
        float defense = target.Defense;
        float modifier = UnityEngine.Random.Range(0.85f, 1f);

        int damage = Mathf.FloorToInt(((2 * target.Level / 5 + 2) * power * attack / defense) / 50 + 2);
        return Mathf.FloorToInt(damage * modifier);
    }

    int CalculateExp(Pokemon defeatedPokemon)
    {
        int level = defeatedPokemon.Level;
        int exp = Mathf.FloorToInt(level * level);
        return exp;
    }

    private void HandleMoveLearn(Pokemon poke, MoveBase newMove)
    {
        SetState(BattleState.NewMoveSelection);

        moveLearnUI.Show(poke, newMove, (selectedIndex) =>
        {
            if (selectedIndex == 2) // slot giữa là chiêu mới
            {
                if (poke.Moves.Count < 4)
                {
                    // Nếu còn slot trống thì thêm
                    poke.Moves.Add(new Move(newMove));
                    dialogBox.ShowDialog($"{poke.Base.Name} learned {newMove.MoveName}!");
                }
                else
                {
                    // Nếu đã đủ 4 chiêu thì phải chọn chiêu cũ để thay thế
                    dialogBox.ShowDialog($"{poke.Base.Name} already knows 4 moves. Choose one to replace.");
                    // ở đây bạn có thể mở lại UI để chọn chiêu cũ thay thế
                }
            }
            else if (selectedIndex >= 0 && selectedIndex < poke.Moves.Count)
            {
                // Người chơi chọn chiêu cũ để thay thế
                poke.Moves[selectedIndex] = new Move(newMove);
                dialogBox.ShowDialog($"{poke.Base.Name} forgot a move and learned {newMove.MoveName}!");
            }
            else
            {
                // Người chơi hủy học chiêu
                dialogBox.ShowDialog($"{poke.Base.Name} chose not to learn {newMove.MoveName}");
            }

            playerUnit.Hud.SetData(poke);

            // Sau khi chọn xong, quay lại state Battle
            if (!isTrainerBattle)
                EndBattle();
            else
                SetState(BattleState.WaitForNextTrainerPokemon);
        });
    }

    private IEnumerator SendNextTrainerPokemon()
    {
        if (currentEnemyIndex + 1 < trainerParty.Count)
        {
            currentEnemyIndex++;
            var nextPokemon = trainerParty[currentEnemyIndex];
            enemyUnit.Setup(nextPokemon);
            dialogBox.ShowDialog($"Trainer sent out {nextPokemon.Base.Name}!");
            yield return new WaitForSeconds(1f);
            partyHandler.OpenPartyMenu(forceSwitch: true);
            SetState(BattleState.PlayerActionSelection);
            dialogBox.ShowActionMenu();
        }
        else
        {
            dialogBox.ShowDialog("Trainer has no more Pokémon!");
            yield return new WaitForSeconds(1f);
            dialogBox.ShowDialog("You got money from the Trainer!");
            yield return new WaitForSeconds(1f);
            Inventory.Instance.AddMoney(20);
            EndBattle();
        }
    }
    public void EndBattle()
    {
        SetState(BattleState.BattleOver);
        playerUnit.Pokemon.ResetStatBoosts();
        enemyUnit.Pokemon.ResetStatBoosts();
        GameController.Instance.EndBattle();
        battleUI.SetActive(false);
        MusicManager.Instance.PlayMusic(null);
    }
}