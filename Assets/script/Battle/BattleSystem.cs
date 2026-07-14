using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum BattleContext
{
    Grass = 0,
    Cave = 1,
    Water = 2,
    Indoor = 3,
    Fishing = 4
}

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
    [Header("Battle Units")]
    [SerializeField] private BattleUnit playerUnit;
    public BattleUnit PlayerUnit => playerUnit;
    [SerializeField] private BattleUnit enemyUnit;
    public BattleUnit EnemyUnit => enemyUnit;

    [Header("UI References")]
    [SerializeField] private GameObject battleUI;
    [SerializeField] private BattleDialogBox dialogBox;
    [SerializeField] private BattleItemMenu battleItemMenu;
    [SerializeField] private BattlePartyMenu battlePartyMenu;
    [SerializeField] private MoveLearnUI moveLearnUI;

    [Header("Audio")]
    [SerializeField] private AudioClip trainerBattleClip;
    [SerializeField] private AudioClip wildBattleClip;

    [Header("Background")]
    [SerializeField] private UnityEngine.UI.Image backgroundImage;
    [SerializeField] private Sprite[] contextBackgrounds; // index = BattleContext enum value

    [Header("Portrait")]
    [SerializeField] private UnityEngine.UI.Image trainerPortraitImage;
    [SerializeField] private UnityEngine.UI.Image playerPortraitImage;
    [SerializeField] private Sprite playerBackSprite;
    [SerializeField] private GameObject portraitPanel;

    // Auto-find in scene
    private BattleItemHandler itemHandler;
    private BattlePartyHandler partyHandler;
    private int currentEnemyIndex;          // chỉ số Pokemon hiện tại của Trainer
    private List<Pokemon> trainerParty;     // danh sách Pokemon của Trainer

    private BattleState state;
    private NPC currentTrainer;
    private bool isTrainerBattle;
    private bool isEndingBattle;
    private bool allowRun = true;
    private Pokemon subscribedMoveLearnPokemon;
    public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;
    public bool IsEndingBattle => isEndingBattle;

    private MoveLearnUI ResolveMoveLearnUI()
    {
        if (!this)
            return null;

        if (moveLearnUI != null)
            return moveLearnUI;

        moveLearnUI = Object.FindObjectOfType<MoveLearnUI>(true);
        return moveLearnUI;
    }

    private void OnDestroy()
    {
        UnbindMoveLearnHandler();
        isEndingBattle = true;
        moveLearnUI = null;
        itemHandler = null;
        partyHandler = null;
    }
    
    void Awake()
    {
        // Auto-find components in BattleScene
        if (itemHandler == null)
            itemHandler = GetComponentInChildren<BattleItemHandler>(true);
        if (partyHandler == null)
            partyHandler = GetComponentInChildren<BattlePartyHandler>(true);
        if (dialogBox == null)
            dialogBox = GetComponentInChildren<BattleDialogBox>(true);
        if (battleUI == null)
            battleUI = gameObject; // fallback
        
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
        // Hien portrait trainer / player trong giay dau
        SetupPortraits();
        yield return new WaitForSeconds(1.2f);
        HidePortraits();

        // Ca hai Pokemon truot vao dong thoi
        StartCoroutine(enemyUnit.PlayEnterAnimation());
        yield return StartCoroutine(playerUnit.PlayEnterAnimation());

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
    }
    public void SetBackground(BattleContext context)
    {
        if (backgroundImage == null || contextBackgrounds == null) return;
        int idx = (int)context;
        if (idx >= 0 && idx < contextBackgrounds.Length && contextBackgrounds[idx] != null)
            backgroundImage.sprite = contextBackgrounds[idx];
    }

    public void StartWildBattle(Pokemon wildPokemon, bool allowRun)
    {
        isEndingBattle = false;
        Outcome = BattleOutcome.None;
        this.allowRun = allowRun;

        if (wildPokemon == null || wildPokemon.Base == null)
        {
            AbortBattleStart("Wild battle has no valid opponent.");
            return;
        }

        if (!TryResolvePlayerBattleData(out var playerParty, out var playerPokemon, out var inventory))
            return;

        MusicManager.Instance?.PlayMusic(wildBattleClip);
        battleUI.SetActive(true);
        isTrainerBattle = false;
        playerUnit.Setup(playerPokemon);
        BindMoveLearnHandler(playerPokemon);
        enemyUnit.Setup(wildPokemon);
        itemHandler.Init(dialogBox, battleUI, inventory, true, wildPokemon, this);
        partyHandler.Init(dialogBox, playerUnit, battlePartyMenu, playerParty, this);
        state = BattleState.Start;
        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(NPC trainer, bool allowRun)
    {
        isEndingBattle = false;
        Outcome = BattleOutcome.None;
        this.allowRun = allowRun;

        if (trainer == null || trainer.Party == null)
        {
            AbortBattleStart("Trainer battle has no trainer party.");
            return;
        }

        if (!TryResolvePlayerBattleData(out var playerParty, out var playerPokemon, out var inventory))
            return;

        MusicManager.Instance?.PlayMusic(trainerBattleClip);
        battleUI.SetActive(true);
        isTrainerBattle = true;
        currentTrainer = trainer;
        playerUnit.Setup(playerPokemon);
        BindMoveLearnHandler(playerPokemon);

        trainerParty = trainer.Party.GetPokemons();
        trainerParty.RemoveAll(p => p == null);
        var enemyPokemon = trainerParty.Count > 0 ? trainerParty[0] : null;
        if (enemyPokemon == null)
        {
            Debug.LogWarning("[BattleSystem] Trainer battle started without any valid trainer Pokemon.");
            SetBattleOutcome(BattleOutcome.Win);
            EndBattle();
            return;
        }

        enemyUnit.Setup(enemyPokemon);
        currentEnemyIndex = 0;
        enemyUnit.Pokemon.HealAll();

        itemHandler.Init(dialogBox, battleUI, inventory, false, null, this);
        partyHandler.Init(dialogBox, playerUnit, battlePartyMenu, playerParty, this);
        SetState(BattleState.Start);
        StartCoroutine(SetupBattle());
    }

    private bool TryResolvePlayerBattleData(out PlayerParty playerParty, out Pokemon playerPokemon, out Inventory inventory)
    {
        playerParty = PlayerParty.Instance;
        playerPokemon = playerParty != null ? playerParty.GetHealthyPokemon() : null;
        inventory = Inventory.Instance != null
            ? Inventory.Instance
            : MenuController.Instance != null ? MenuController.Instance.Inventory : null;

        if (playerParty == null || playerPokemon == null)
        {
            AbortBattleStart("Player has no healthy Pokemon available for battle.");
            return false;
        }

        if (playerUnit == null || enemyUnit == null || dialogBox == null || battleUI == null ||
            itemHandler == null || partyHandler == null || battlePartyMenu == null || inventory == null)
        {
            AbortBattleStart("BattleScene is missing one or more required references.");
            return false;
        }

        return true;
    }

    private void AbortBattleStart(string reason)
    {
        Debug.LogError($"[BattleSystem] {reason}", this);
        ToastNotificationManager.Instance?.Show("Battle could not start.", Color.red);
        SetBattleOutcome(BattleOutcome.None);
        EndBattle();
    }

    public NPC CurrentTrainer => currentTrainer;

    public void SetBattleOutcome(BattleOutcome outcome)
    {
        Outcome = outcome;
    }

    public void BindMoveLearnHandler(Pokemon pokemon)
    {
        if (subscribedMoveLearnPokemon == pokemon)
            return;

        UnbindMoveLearnHandler();

        subscribedMoveLearnPokemon = pokemon;
        if (subscribedMoveLearnPokemon != null)
            subscribedMoveLearnPokemon.OnMoveLearnRequested += HandleMoveLearn;
    }

    private void UnbindMoveLearnHandler()
    {
        if (subscribedMoveLearnPokemon == null)
            return;

        subscribedMoveLearnPokemon.OnMoveLearnRequested -= HandleMoveLearn;
        subscribedMoveLearnPokemon = null;
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
                if (ResolveMoveLearnUI() != null)
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
    // Hiện thông báo (blocking) trong ô battle rồi mở lại menu chọn và đưa FSM về đúng state.
    // Dùng cho phản hồi từ chối input (hết PP, không chạy được) — thay cho toast để nhất quán
    // với văn bản trong trận và giống Pokemon gốc. Trong lúc chờ đặt Busy nên không loạn input.
    private IEnumerator ShowSelectionMessage(string message, BattleState returnState)
    {
        SetState(BattleState.Busy);
        yield return dialogBox.ShowDialogAndWait(message);

        if (returnState == BattleState.PlayerMoveSelection)
            dialogBox.ShowMoveMenu(playerUnit.Pokemon.Moves);
        else
            dialogBox.ShowActionMenu();

        SetState(returnState);
    }

    void HandleActionInput()
    {
        dialogBox.HandleActionSelection();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            int actionIndex = dialogBox.GetSelectedAction();
            if (actionIndex == 0) // Fight
            {
                if (playerUnit.Pokemon.Moves == null || playerUnit.Pokemon.Moves.Count == 0)
                {
                    StartCoroutine(ShowSelectionMessage("This Pokemon has no moves!", BattleState.PlayerActionSelection));
                    return;
                }

                SetState(BattleState.PlayerMoveSelection);
                dialogBox.ShowMoveMenu(playerUnit.Pokemon.Moves);
            }
            else if (actionIndex == 1) // Pokemon
            {
                SetState(BattleState.PlayerPokemonSelection);
                OnPartySelected();
            }
            else if (actionIndex == 2) // Run
            {
                if (allowRun)
                {
                    SetBattleOutcome(BattleOutcome.Escape);
                    SetState(BattleState.BattleOver);
                    EndBattle();
                }
                else
                {
                    StartCoroutine(ShowSelectionMessage("You can't run from this battle!", BattleState.PlayerActionSelection));
                }
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

        if (item.itemType == ItemType.KeyItem)
        {
            StartCoroutine(itemHandler.UseItemOnPokemon(item, null, null));
            battleItemMenu.CloseMenu();
            return;
        }

        // Mở PartyMenu để chọn Pokemon target
        state = BattleState.PlayerPokemonSelection;
        battlePartyMenu.Open(PlayerParty.Instance.Pokemons,
            (pokemon) =>
            {
            // Nếu chọn đúng con đang ra trận -> dùng trực tiếp BattleUnit
            if (pokemon == playerUnit.Pokemon)
            {
                StartCoroutine(itemHandler.UseItemOnPokemon(item, playerUnit.Pokemon, playerUnit));
            }
            else
            {
                // Nếu chọn Pokemon khác trong party -> hồi máu trực tiếp trên đối tượng Pokemon
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

    //---------Dùng Pokemon trong battle -------------//

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
            if (playerUnit.Pokemon.Moves == null || moveIndex < 0 || moveIndex >= playerUnit.Pokemon.Moves.Count)
            {
                StartCoroutine(ShowSelectionMessage("No valid move was selected.", BattleState.PlayerActionSelection));
                return;
            }

            var move = playerUnit.Pokemon.Moves[moveIndex];
            if (move.PP <= 0)
            {
                StartCoroutine(ShowSelectionMessage("Không còn PP cho đòn này!", BattleState.PlayerMoveSelection));
                return;
            }
            SetState(BattleState.Busy);
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

    private void ApplyDrainIfNeeded(Pokemon attacker, Pokemon defender, BattleUnit attackerUnit, Move move, int targetHpBeforeHit)
    {
        if (attacker == null || defender == null || attackerUnit == null || move == null || move.Base == null)
            return;

        if (!move.Base.IsDamagingMove || move.Base.DrainRatio <= 0f)
            return;

        int actualDamageDealt = Mathf.Max(0, targetHpBeforeHit - defender.CurrentHp);
        if (actualDamageDealt <= 0)
            return;

        int hpBeforeHeal = attacker.CurrentHp;
        int healAmount = Mathf.Max(1, Mathf.FloorToInt(actualDamageDealt * move.Base.DrainRatio));
        attacker.Heal(healAmount);

        int actualHealed = attacker.CurrentHp - hpBeforeHeal;
        if (actualHealed <= 0)
            return;

        attackerUnit.Hud.UpdateHP();
        dialogBox.ShowDialog($"{attacker.Base.Name} absorbed {actualHealed} HP!");
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
        // Nếu không bị block hoặc miss thì tiếp tục thực hiện move
        dialogBox.ShowDialog($"{playerUnit.Pokemon.Base.Name} used {move.Base.MoveName}!");
        yield return new WaitForSeconds(1f);
        move.UseMove();
        yield return StartCoroutine(playerUnit.PlayAttackAnimation());

        if (move.Base.Power >0){
            //
            if (enemyUnit.Pokemon.Status == StatusEffect.Protected)
            {
                dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} is protected!");
                yield return new WaitForSeconds(1f);
                ProceedTurn();
                yield break;
            }
            bool isCritical = false;
            int targetHpBeforeHit = enemyUnit.Pokemon.CurrentHp;
            int damage = CalculateDamage(playerUnit.Pokemon, enemyUnit.Pokemon, move, out isCritical);
            float effectiveness = TypeChart.GetEffectiveness(move.Base.Type, enemyUnit.Pokemon.Base.Type1, enemyUnit.Pokemon.Base.Type2);
            if (effectiveness > 0f)
                enemyUnit.ShowDamagePopup(damage, isCritical, effectiveness);   // số sát thương bay lên trong lúc hit anim
            yield return StartCoroutine(effectiveness == 0f
                ? enemyUnit.PlayNoDamageAnimation()
                : enemyUnit.PlayHitAnimation());
            enemyUnit.Pokemon.TakeDamage(damage);
            enemyUnit.Hud.UpdateHP();
            if (isCritical)
            {
                dialogBox.ShowDialog("A critical hit!");
                yield return new WaitForSeconds(1f);
            }
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

            ApplyDrainIfNeeded(playerUnit.Pokemon, enemyUnit.Pokemon, playerUnit, move, targetHpBeforeHit);
            if (move.Base.DrainRatio > 0f)
                yield return new WaitForSeconds(1f);

            if (enemyUnit.Pokemon.Status == StatusEffect.Sleep)
            {
                enemyUnit.Pokemon.CureStatus();
                enemyUnit.Hud.SetStatus(enemyUnit.Pokemon.Status);
                dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} woke up!");
                yield return new WaitForSeconds(1f);
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
            yield return StartCoroutine(enemyUnit.PlayFaintAnimation());
            dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} fainted!");
            yield return new WaitForSeconds(1f);

            // EXP cho player
            int expGain = CalculateExp(enemyUnit.Pokemon);
            playerUnit.Pokemon.GainExp(expGain);
            dialogBox.ShowDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} EXP!");
            playerUnit.Hud.SetData(playerUnit.Pokemon);

            // Kiểm tra NGAY sau GainExp, trước yield ầu tiên.
            // Nếu GainExp trigger move learn → state = NewMoveSelection (synchronous, chưa có frame nào chạy).
            // Nếu check sau WaitForSeconds: player có thể chọn move trong lúc yield,
            // ContinueAfterMoveLearn set WaitForNextTrainerPokemon → Update set Busy+StartCoroutine(SendNext...) →
            // coroutine thức dậy thấy Busy → không break → gọi SetState(WaitForNextTrainerPokemon) lần 2 → bug.
            if (state != BattleState.Busy)
                yield break;

            yield return new WaitForSeconds(1f);

            // Kiểm tra nếu là Trainer battle
            if (isTrainerBattle)
            {
                SetState(BattleState.WaitForNextTrainerPokemon);
            }
            else
            {
                yield return new WaitForSeconds(1f);
                SetBattleOutcome(BattleOutcome.Win);
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
        if (enemyUnit.Pokemon.Moves == null || enemyUnit.Pokemon.Moves.Count == 0)
        {
            yield return dialogBox.ShowDialogAndWait($"{enemyUnit.Pokemon.Base.Name} has no moves and cannot attack!");
            PlayerAction();
            yield break;
        }

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
        // Nếu không bị block hoặc miss thì tiếp tục thực hiện move
        dialogBox.ShowDialog($"{enemyUnit.Pokemon.Base.Name} used {move.Base.MoveName}!");
        yield return new WaitForSeconds(1f);
        move.UseMove();
        yield return StartCoroutine(enemyUnit.PlayAttackAnimation());
        if (move.Base.Power >0){
            bool isCritical;
            int targetHpBeforeHit = playerUnit.Pokemon.CurrentHp;
            int damage = CalculateDamage(enemyUnit.Pokemon, playerUnit.Pokemon, move, out isCritical);
            float effectiveness = TypeChart.GetEffectiveness(move.Base.Type, playerUnit.Pokemon.Base.Type1, playerUnit.Pokemon.Base.Type2);
            if (effectiveness > 0f)
                playerUnit.ShowDamagePopup(damage, isCritical, effectiveness);
            yield return StartCoroutine(effectiveness == 0f
                ? playerUnit.PlayNoDamageAnimation()
                : playerUnit.PlayHitAnimation());
            playerUnit.Pokemon.TakeDamage(damage);
            playerUnit.Hud.UpdateHP();
            if (isCritical)
            {
                dialogBox.ShowDialog("A critical hit!");
                yield return new WaitForSeconds(1f);
            }
            ApplyDrainIfNeeded(enemyUnit.Pokemon, playerUnit.Pokemon, enemyUnit, move, targetHpBeforeHit);
            if (move.Base.DrainRatio > 0f)
                yield return new WaitForSeconds(1f);
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
            yield return StartCoroutine(playerUnit.PlayFaintAnimation());
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
        // 1. boost/debuff
        if (move.Base.StatBoosts != null && move.Base.StatBoosts.Count > 0)
        {
            if (move.Base.Target == MoveTarget.Self)
                ApplyBoosts(attacker, move.Base.StatBoosts);
            else
                ApplyBoosts(defender, move.Base.StatBoosts);
        }
        else
        // 2. status effect
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
        // 3. Nếu không có cả 2 thì báo move không có hiệu quả
        {
            if (IsSplitFollowUpMove(move))
                dialogBox.ShowDialog("but it failed.");
            else
                dialogBox.ShowDialog($"{attacker.Base.Name} used {move.Base.MoveName}, but it had no effect.");
        }
    }

    private bool IsSplitFollowUpMove(Move move)
    {
        if (move == null || move.Base == null)
            return false;

        return string.Equals(move.Base.MoveName, "Teleport", System.StringComparison.OrdinalIgnoreCase);
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
            pokemon.IncrementStatusTurns();
            if (pokemon.StatusTurns > 2)
            {
                pokemon.CureStatus();
                unit.Hud.SetStatus(pokemon.Status);
                dialogBox.ShowDialog($"{pokemon.Base.Name} woke up!");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                dialogBox.ShowDialog($"{pokemon.Base.Name} is asleep and can't move!");
                unit.Hud.SetData(pokemon);
                yield return new WaitForSeconds(1f);
                proceedTurnCallback();
                resultCallback(true);
                yield break;
            }
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
            pokemon.IncrementStatusTurns();
            if (pokemon.StatusTurns > 2)
            {
                pokemon.CureStatus();
                unit.Hud.SetStatus(pokemon.Status);
                dialogBox.ShowDialog($"{pokemon.Base.Name} snapped out of confusion!");
                yield return new WaitForSeconds(1f);
            }
            else if (Random.value < 0.33f)
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
        if (poke == null || newMove == null)
        {
            ContinueAfterMoveLearn(poke);
            return;
        }

        SetState(BattleState.NewMoveSelection);

        var learnUi = ResolveMoveLearnUI();
        if (learnUi == null)
        {
            poke.ResolvePendingMoveLearn(-1);
            playerUnit.Hud.SetData(poke);
            ContinueAfterMoveLearn(poke);
            return;
        }

        learnUi.Show(poke, newMove, (selectedIndex) =>
        {
            string message = poke.ResolvePendingMoveLearn(selectedIndex);
            if (!string.IsNullOrWhiteSpace(message))
                dialogBox.ShowDialog(message);

            playerUnit.Hud.SetData(poke);
            ContinueAfterMoveLearn(poke);
        });
    }

    private void ContinueAfterMoveLearn(Pokemon poke)
    {
        if (poke != null && poke.HasPendingMoveLearn)
        {
            poke.DispatchNextPendingMoveLearn();
            return;
        }

        if (!isTrainerBattle)
        {
            if (Outcome == BattleOutcome.None)
                SetBattleOutcome(BattleOutcome.Win);
            EndBattle();
        }
        else
            SetState(BattleState.WaitForNextTrainerPokemon);
    }
    private IEnumerator SendNextTrainerPokemon()
    {
        if (TryGetNextTrainerPokemon(out var nextPokemon))
        {
            currentEnemyIndex++;
            nextPokemon.HealAll();
            enemyUnit.Setup(nextPokemon);
            string trainerName = currentTrainer != null && !string.IsNullOrWhiteSpace(currentTrainer.npcName)
                ? currentTrainer.npcName
                : "Trainer";
            dialogBox.ShowDialog($"{trainerName} sent out {nextPokemon.Base.Name}!");
            yield return StartCoroutine(enemyUnit.PlayEnterAnimation());
            yield return new WaitForSeconds(0.5f);
            SetState(BattleState.PlayerActionSelection);
            dialogBox.ShowActionMenu();
        }
        else
        {
            string trainerName = currentTrainer != null && !string.IsNullOrWhiteSpace(currentTrainer.npcName)
                ? currentTrainer.npcName
                : "Trainer";
            dialogBox.ShowDialog($"{trainerName} has no more Pokemon!");
            yield return new WaitForSeconds(1f);
            if (currentTrainer != null && currentTrainer.IsGymLeader)
            {
                string badgeName = !string.IsNullOrWhiteSpace(currentTrainer.BadgeName)
                    ? currentTrainer.BadgeName
                    : currentTrainer.BadgeItem != null && !string.IsNullOrWhiteSpace(currentTrainer.BadgeItem.itemName)
                        ? currentTrainer.BadgeItem.itemName
                        : "Badge";
                SaveBadgeLocal(badgeName);

                if (currentTrainer.BadgeItem != null)
                {
                    Inventory.Instance?.AddItem(currentTrainer.BadgeItem, 1);
                    dialogBox.ShowDialog($"You received {currentTrainer.BadgeItem.name}!");
                }
                else
                {
                    dialogBox.ShowDialog($"You received the {badgeName}!");
                }

                TrySetGymStoryFlag(currentTrainer);

                // If this NPC should only battle once, disable further battles.
                if (currentTrainer.CanBattleOnce)
                    currentTrainer.CanBattle = false;
            }
            else
            {
                int rewardMoney = currentTrainer != null ? Mathf.Max(0, currentTrainer.RewardMoney) : 20;
                dialogBox.ShowDialog($"You got {rewardMoney} money from {trainerName}!");
                Inventory.Instance?.AddMoney(rewardMoney);
            }
            yield return new WaitForSeconds(1f);
            SetBattleOutcome(BattleOutcome.Win);
            EndBattle();
        }
    }

    private bool TryGetNextTrainerPokemon(out Pokemon nextPokemon)
    {
        nextPokemon = null;

        if (trainerParty == null || trainerParty.Count == 0)
            return false;

        for (int i = currentEnemyIndex + 1; i < trainerParty.Count; i++)
        {
            var candidate = trainerParty[i];
            if (candidate != null)
            {
                nextPokemon = candidate;
                return true;
            }
        }

        return false;
    }
    public void EndBattle()
    {
        UnbindMoveLearnHandler();

        if (isEndingBattle)
            return;

        isEndingBattle = true;
        SetState(BattleState.BattleOver);

        if (playerUnit != null && playerUnit.Pokemon != null)
            playerUnit.Pokemon.ResetStatBoosts();

        if (enemyUnit != null && enemyUnit.Pokemon != null)
            enemyUnit.Pokemon.ResetStatBoosts();

        if (battleUI != null)
            battleUI.SetActive(false);

        MusicManager.Instance?.PlayMusic(null);
        GameController.Instance?.EndBattle();
    }

    // Badge progress belongs to the current save slot, not global PlayerPrefs.
    private void SaveBadgeLocal(string badgeId)
    {
        if (SaveLoadSystem.RegisterRuntimeBadge(badgeId))
            ToastNotificationManager.Instance?.Show($"Received badge: {badgeId}", Color.cyan);
    }

    private void TrySetGymStoryFlag(NPC trainer)
    {
        if (trainer == null || !trainer.SetStoryFlagAfterBadge)
            return;

        var flags = StoryFlags.Instance != null ? StoryFlags.Instance : StoryFlags.GetOrCreate();
        if (flags == null)
            return;

        flags.SetFlag(trainer.StoryFlagAfterBadge, trainer.StoryFlagAfterBadgeValue);
    }

    // ─── Portrait helpers ─────────────────────────────────────────────────────

    private void SetupPortraits()
    {
        if (portraitPanel != null)
            portraitPanel.SetActive(true);

        if (playerPortraitImage != null)
            playerPortraitImage.sprite = playerBackSprite;

        if (trainerPortraitImage != null)
        {
            bool hasPortrait = isTrainerBattle && currentTrainer != null && currentTrainer.Portrait != null;
            trainerPortraitImage.gameObject.SetActive(hasPortrait);
            if (hasPortrait)
                trainerPortraitImage.sprite = currentTrainer.Portrait;
        }
    }

    private void HidePortraits()
    {
        if (portraitPanel != null)
            portraitPanel.SetActive(false);
    }
}
