using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Battle;
using System;
using System.Linq;
using Units;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using TMPro;
using System.Threading;

public class GameManager : MonoBehaviour
{
    private Battle.Battle battle;
    private Unit player;
    private Unit opponent;
    private GameState gameState;
    private int moveChosenByPlayer;
    private int[] movesPlayerCanSwapIn;
    private bool roundOngoing;
    private bool playerChoseANewUnit;
    private ParticleSystem p_particles;
    private ParticleSystem o_particles;
    private List<Button> allButtons;
    // Tuple for messing with hit animations. Takes the type to switch the material to, and if it targets the player as a bool.
    Queue<(Units.Type, bool)> hitAnimationsQueue;
    [SerializeField] Sprite[] sprites;

    enum GameState
    {
        Initialize,
        SceneGeneration,
        PlayerMoveSelection,
        OpponentMoveSelection,
        KnockoutAnimation,
        PlayerSelectingNewMoveOrUnit,
        PlayerDied,
    }

    void Start()
    {
        gameState = GameState.Initialize;
        player = Utility.GenerateRandomUnit();
        roundOngoing = false;
        playerChoseANewUnit = false;
        movesPlayerCanSwapIn = new int[3];
        hitAnimationsQueue = new();
        allButtons = new();
        GameObject p = GameObject.Find($"MoveParticlesPlayer");
        p_particles = p.GetComponent<ParticleSystem>();
        GameObject o = GameObject.Find($"MoveParticlesOpponent");
        o_particles = o.GetComponent<ParticleSystem>();

        Button[] buttons = FindObjectOfType<Canvas>().GetComponentsInChildren<Button>();
        foreach(Button button in buttons) 
        { 
            button.onClick.AddListener(() => Event_ButtonClicked(button)); 
            allButtons.Add(button);
        }
        AdvanceGameState(GameState.SceneGeneration);
    }

    void Update()
    {
        switch (gameState)
        {
            case GameState.SceneGeneration:
                // Build scene stuff, refresh player and enemy and player moves.
                opponent = Utility.GenerateRandomUnit();
                battle = new(ref player, ref opponent, true);
                battle.SetGameManager(this);
                UI_SetVisibilityOfWinScreen(visible: false);
                UI_SetVisibilityOfPlayerMoves(visible: true);
                UI_UpdatePlayerOrOpponent(updatePlayer: true);
                UI_UpdatePlayerOrOpponent(updatePlayer: false);
                UI_UpdatePlayerMoves();
                AdvanceGameState();
                break;
            case GameState.PlayerMoveSelection:
                // Wait for player to choose move.
                break;
            case GameState.OpponentMoveSelection:
                int o_move = Opponent_ChooseMove();
                Debug.Log($"Opponent chose a move: {opponent.Moves[o_move].Name}");
                battle.UpdateMovesChosen(moveChosenByPlayer, o_move);
                AdvanceGameState();
                roundOngoing = true;
                if (battle.Round()) { AdvanceGameState(GameState.KnockoutAnimation); } // When Round() returns true, someone died.
                else { AdvanceGameState(GameState.PlayerMoveSelection); } // When Round() returns false, set up for a new round.
                break;
            case GameState.KnockoutAnimation:
                // Someone being knocked out animation is playing
                if (!p_particles.isPlaying && !o_particles.isPlaying)
                {
                    string who = player.CurrentHitPoints == 0 ? "Player" : "Opponent";
                    GameObject unit_obj = GameObject.Find($"{who}Object");
                    unit_obj.transform.Translate(2f * Time.deltaTime * Vector2.down);
                    if (unit_obj.transform.position.y < 1)
                    {
                        unit_obj.GetComponent<SpriteRenderer>().enabled = false;
                        UI_SetVisibilityOfPlayerMoves(visible: false);
                        UI_SetVisibilityOfWinScreen(visible: true);
                        ChooseRandomNewMoves();
                        GameState gs = opponent.CurrentHitPoints == 0 ? GameState.PlayerSelectingNewMoveOrUnit : GameState.PlayerDied;
                        AdvanceGameState(gs);
                    }
                }
                break;
            case GameState.PlayerSelectingNewMoveOrUnit:
                // Wait for player to swap a move or unit, or neither.
                Debug.Log("Opponent is dead");
                break;
            case GameState.PlayerDied:
                // Player dead show lose screen.
                Debug.Log("Player is dead");
                break;
        }
    }

    void UI_UpdatePlayerMoves() // Update moves when player switches a move or unit.
    {
        Button[] buttons = allButtons.Where(x => x.gameObject.name.Contains("UI_Move")).ToArray();
        for(int i = 0; i < 4; ++i)
        {
            Button b = buttons.First(x => x.gameObject.name == $"UI_Move{i}");
            string m = @$"Assets/Assets/Particles/{TypeConverter.TypeToString(player.Moves[i].Type).ToLower()}.png";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(m);
            Sprite icon = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0));
            b.GetComponentsInChildren<Image>().Where(x => x.gameObject.name == "Icon").First().sprite = icon;
            b.GetComponentInChildren<TextMeshProUGUI>().text = player.Moves[i].Name;
        }
    }

    void UI_UpdatePlayerOrOpponent(bool updatePlayer) // Update player or opponent sprite when switched. True means player is being updated. 
    {
        string who = updatePlayer ? "Player" : "Opponent";
        Units.Type t = updatePlayer ? player.Type : opponent.Type;

        GameObject unit_obj = GameObject.Find($"{who}Object");
        System.Random r = new();
        int idx = r.Next(1, sprites.Length);
        unit_obj.GetComponent<SpriteRenderer>().sprite = sprites[idx];

        string ic = @$"Assets/Assets/Particles/{TypeConverter.TypeToString(t).ToLower()}.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ic);
        Sprite icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        GameObject.Find($"{who}Icon").GetComponent<Image>().sprite = icon;
        unit_obj.GetComponent<SpriteRenderer>().enabled = true;
        unit_obj.transform.localPosition = new(unit_obj.transform.localPosition.x, 2);
        if (!updatePlayer || playerChoseANewUnit)
        {
            var bar = GameObject.Find($"UI_{who}_HPBar").GetComponent<Image>();
            bar.rectTransform.localScale = new(1, bar.rectTransform.localScale.y, bar.rectTransform.localScale.z);
        }
    }

    void UI_SetVisibilityOfWinScreen(bool visible) 
    {
        foreach(Button b in allButtons)
        {
            if(b.gameObject.CompareTag("WinButton"))
            {
                b.interactable = visible;
                b.gameObject.SetActive(visible);
            }
        }
    }

    void UI_SetVisibilityOfLoseScreen(bool visible) 
    {
        // The player was knocked out.
    }

    void UI_SetVisibilityOfPlayerMoves(bool visible)
    {
        foreach (Button b in allButtons)
        {
            if (b.gameObject.CompareTag("MoveButton"))
            {
                b.interactable = visible;
                b.gameObject.SetActive(visible);
            }
        }
    }

    void ChooseRandomNewMoves()
    {
        Button[] buttons = allButtons.Where(x => x.gameObject.name.Contains("NewMove")).ToArray();
        List<int> moves = new();
        System.Random rng = new();
        while (moves.Count < 3)
        {
            int randomMove = rng.Next(1, 20);
            if (!moves.Contains(randomMove)) { moves.Add(randomMove); }
        }
        for (int i = 0; i < 3; ++i)
        {
            Move move = new(moves[i]);
            Button b = buttons.First(x => x.gameObject.name == $"UI_NewMove{i}");
            string m = @$"Assets/Assets/Particles/{TypeConverter.TypeToString(move.Type).ToLower()}.png";
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(m);
            Sprite icon = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0));
            b.GetComponentsInChildren<Image>().Where(x => x.gameObject.name == "Icon").First().sprite = icon;
            b.GetComponentInChildren<TextMeshProUGUI>().text = move.Name;
            movesPlayerCanSwapIn[i] = moves[i];
        }
    }

    void Event_ButtonClicked(Button button)
    {
        switch(gameState)
        {
            case GameState.PlayerMoveSelection:
                moveChosenByPlayer = button.gameObject.name[^1] - '0'; // Get index from name
                Debug.Log($"Chose a move: {moveChosenByPlayer}");
                AdvanceGameState();
                break;
            case GameState.PlayerSelectingNewMoveOrUnit:
                switch(button.gameObject.name)
                {
                    case "UI_NewMove0":
                    case "UI_NewMove1":
                    case "UI_NewMove2":
                        int idx = button.gameObject.name[^1] - '0';
                        moveChosenByPlayer = movesPlayerCanSwapIn[idx];
                        UI_SetVisibilityOfPlayerMoves(visible: true);
                        UI_SetVisibilityOfWinScreen(visible: false);
                        break;
                    case "UI_NewUnit":
                        player = Utility.GenerateRandomUnit();
                        playerChoseANewUnit = true;
                        AdvanceGameState(GameState.SceneGeneration);
                        break;
                    case "UI_Continue":
                        AdvanceGameState(GameState.SceneGeneration);
                        break;
                    default: // Player was selecting a new move. 
                        int moveToReplace = button.gameObject.name[^1] - '0'; // Get index from name
                        player.Moves[moveToReplace] = new(moveChosenByPlayer);
                        AdvanceGameState(GameState.SceneGeneration);
                        break;
                }
                break;
            case GameState.PlayerDied:
                // There's only one button, the restart button lol.
                break;
        }
    }

    void AdvanceGameState() // Advance to next.
    {
        if(gameState == GameState.PlayerDied) { gameState = GameState.SceneGeneration; }
        else { ++gameState; }
    }

    void AdvanceGameState(GameState gs) // Advance to specific. 
    {
        this.gameState = gs;
    }

    int Opponent_ChooseMove()
    {
        // First priority: healing if opponent has Restore.
        if(opponent.CurrentHitPoints <= (opponent.MaxHitPoints / 3))
        {
            int idx = Array.FindIndex(opponent.Moves, o => o.MoveID == 19);
            if (idx != -1) return idx;
        }

        // Second priority: KILL
        for(int i = 0; i < 4; ++i)
        {
            if (TypeConverter.DamageMod(opponent.Moves[i].Type, player.Type) > 1) { return i; }
        }

        // Third priority: inverting self attack and defense debuffs.
        if(battle.AttackDecayFlag.Item2)
        {
            int idx = Array.FindIndex(opponent.Moves, o => o.MoveID == 13);
            if (idx != -1) return idx;
        }
        if (battle.DefenseDecayFlag.Item2)
        {
            int idx = Array.FindIndex(opponent.Moves, o => o.MoveID == 14);
            if (idx != -1) return idx;
        }

        // Forth priority: random move
        System.Random r = new();
        return r.Next(0, 4);
    }

    public void AddHitAnimationToQueue(Units.Type type, bool targetIsPlayer)
    {
        hitAnimationsQueue.Enqueue((type, targetIsPlayer));
    }

    public void RunAllAnimations()
    {
        for(int i = 0; i < hitAnimationsQueue.Count; ++i) { Invoke(nameof(Animation_HitUnit), i); }
    }

    private void Animation_HitUnit()
    {
        var item = hitAnimationsQueue.Dequeue();
        string m = @$"Assets/Assets/Particles/Materials/{TypeConverter.TypeToString(item.Item1)}Material.mat";
        Material material = Instantiate(AssetDatabase.LoadAssetAtPath<Material>(m));
        ParticleSystem particles = item.Item2 ? p_particles : o_particles;
        particles.GetComponent<Renderer>().material = material;
        particles.Play();
        AlterHealthBar(item.Item2);
    }

    public void Animation_Buff(bool targetIsPlayer) 
    { 
        AddHitAnimationToQueue(Units.Type.Buff, targetIsPlayer);
    }

    public void Animation_Debuff(bool targetIsPlayer)
    {
        AddHitAnimationToQueue(Units.Type.Debuff, targetIsPlayer);
    }

    public void Animation_Burn(bool targetIsPlayer)
    {
        AddHitAnimationToQueue(Units.Type.Burn, targetIsPlayer);
    }

    public void Animation_Restore(bool targetIsPlayer)
    {
        AddHitAnimationToQueue(Units.Type.Heal, targetIsPlayer);
    }

    private void AlterHealthBar(bool isPlayer)
    {
        string who = isPlayer ? "Player" : "Opponent";
        var bar = GameObject.Find($"UI_{who}_HPBar").GetComponent<Image>();
        int currentHP = isPlayer ? player.CurrentHitPoints : opponent.CurrentHitPoints;
        int maxHP = isPlayer ? player.MaxHitPoints : opponent.MaxHitPoints;
        float percent = (float)currentHP / (float)maxHP;
        bar.rectTransform.localScale = new(percent, bar.rectTransform.localScale.y, bar.rectTransform.localScale.z);
    }

    public void PrintHealth(bool isPlayer)
    {
        string who = isPlayer ? "player" : "opponent";
        int current = isPlayer ? player.CurrentHitPoints : opponent.CurrentHitPoints;
        int max = isPlayer ? player.MaxHitPoints : opponent.MaxHitPoints;
        Debug.Log($"Printing HP for {who}: {current}/{max}");
    }
}
