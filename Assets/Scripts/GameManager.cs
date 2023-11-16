using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Units;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.SceneManagement;
using System.Drawing;

public class GameManager : MonoBehaviour
{
    private Battle.Battle battle;
    private Unit player;
    private Unit opponent;
    private GameState gameState;
    private int moveChosenByPlayer;
    private int[] movesPlayerCanSwapIn;
    private bool animationsArePlaying;
    private bool playerChoseANewUnit;
    private ParticleSystem p_particles;
    private ParticleSystem o_particles;
    private List<Button> allButtons;
    private int knockouts;
    // Tuple for messing with hit animations.
    // Takes the type to switch the material to, the percentage of health remaining for the target,
    // whether the target was the player as a bool, and a UI_InfoText string.
    Queue<(Units.Type, float, bool, string)> hitAnimationsQueue;
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
        knockouts = 0;
        gameState = GameState.Initialize;
        player = Utility.GenerateRandomUnit();
        animationsArePlaying = false;
        movesPlayerCanSwapIn = new int[3];
        hitAnimationsQueue = new();
        allButtons = new();
        p_particles = GameObject.Find($"MoveParticlesPlayer").GetComponent<ParticleSystem>();
        o_particles = GameObject.Find($"MoveParticlesOpponent").GetComponent<ParticleSystem>();
        playerChoseANewUnit = true; // Allows the player to be initialized.

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
                UI_SetVisibilityOfLoseScreen(visible: false);
                UI_SetVisibilityOfPlayerMoves(visible: true);
                UI_UpdatePlayerOrOpponent(updatePlayer: true);
                UI_UpdatePlayerOrOpponent(updatePlayer: false);
                UI_UpdatePlayerMoves();
                GameObject.Find($"UI_InfoText").GetComponent<TextMeshProUGUI>().text = "";
                playerChoseANewUnit = false;
                AdvanceGameState();
                break;
            case GameState.PlayerMoveSelection:
                // Wait for player to choose move.
                break;
            case GameState.OpponentMoveSelection:
                int o_move = Opponent_ChooseMove();
                //Debug.Log($"Opponent chose a move: {opponent.Moves[o_move].Name}");
                battle.UpdateMovesChosen(moveChosenByPlayer, o_move);
                AdvanceGameState();
                animationsArePlaying = true;
                if (battle.Round()) // When Round() returns true, someone died.
                { 
                    AdvanceGameState(GameState.KnockoutAnimation);
                    string who = player.CurrentHitPoints == 0 ? "Player" : "Opponent";
                    GameObject.Find($"UI_InfoText").GetComponent<TextMeshProUGUI>().text = $"{who} was killed!";
                } 
                else { AdvanceGameState(GameState.PlayerMoveSelection); } // When Round() returns false, set up for a new round.
                GameObject.Find($"UI_InfoText").GetComponent<TextMeshProUGUI>().text = "";
                break;
            case GameState.KnockoutAnimation:
                // Someone being knocked out animation is playing
                if (!animationsArePlaying)
                {
                    string who = player.CurrentHitPoints == 0 ? "Player" : "Opponent";
                    GameObject unit_obj = GameObject.Find($"{who}Object");
                    unit_obj.transform.Translate(2f * Time.deltaTime * Vector2.down);
                    if (unit_obj.transform.position.y < 1)
                    {
                        unit_obj.GetComponent<SpriteRenderer>().enabled = false;
                        GameState gs = opponent.CurrentHitPoints == 0 ? GameState.PlayerSelectingNewMoveOrUnit : GameState.PlayerDied;
                        AdvanceGameState(gs);
                        if(gs == GameState.PlayerSelectingNewMoveOrUnit)
                        {
                            ++knockouts;
                            UI_SetVisibilityOfPlayerMoves(visible: false);
                            UI_SetVisibilityOfWinScreen(visible: true);
                            ChooseRandomNewMoves();
                            GameObject.Find($"UI_InfoText").GetComponent<TextMeshProUGUI>().text = "Choose a new move, or get a random new unit.";
                        } else
                        {
                            UI_SetVisibilityOfPlayerMoves(visible: false);
                            UI_SetVisibilityOfLoseScreen(visible: true);
                            string text = $"You died! Knocked out {knockouts} opponent";
                            text += (knockouts > 1 || knockouts == 0) ? "s." : ".";
                            GameObject.Find($"UI_InfoText").GetComponent<TextMeshProUGUI>().text = text;

                        }
                    }
                }
                break;
            case GameState.PlayerSelectingNewMoveOrUnit:
                // Wait for player to swap a move or unit, or neither.
                //Debug.Log("Opponent is dead");
                break;
            case GameState.PlayerDied:
                // Player dead show lose screen.
                //Debug.Log("Player is dead");
                break;
        }
    }

    // UI stuff
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
        if (!updatePlayer || playerChoseANewUnit)
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

            var bar = GameObject.Find($"UI_{who}_HPBar").GetComponent<Image>();
            bar.rectTransform.localScale = new(1, bar.rectTransform.localScale.y, bar.rectTransform.localScale.z);
        }
    }

    void UI_SetVisibilityOfWinScreen(bool visible) 
    { 
        // Player knocked out opponent.
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
        foreach (Button b in allButtons)
        {
            if (b.gameObject.CompareTag("LoseButton"))
            {
                b.interactable = visible;
                b.gameObject.SetActive(visible);
            }
        }
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

    // Method for generating random new moves after successfully knocking out an opponent. 
    void ChooseRandomNewMoves()
    {
        Button[] buttons = allButtons.Where(x => x.gameObject.name.Contains("NewMove")).ToArray();
        List<int> moves = new();
        System.Random rng = new();
        while (moves.Count < 3)
        {
            int randomMove = rng.Next(1, 21);
            if (!moves.Contains(randomMove) && !player.Moves.Any(x => x.MoveID == randomMove) ) { moves.Add(randomMove); }
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

    // Button click stuff. 
    void Event_ButtonClicked(Button button)
    {
        switch(gameState)
        {
            case GameState.PlayerMoveSelection:
                if(!animationsArePlaying)
                {
                    moveChosenByPlayer = button.gameObject.name[^1] - '0'; // Get index from name
                    //Debug.Log($"Chose a move: {moveChosenByPlayer}");
                    AdvanceGameState();
                }
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
                        GameObject.Find($"UI_InfoText").GetComponent<TextMeshProUGUI>().text = "Choose a move to replace.";
                        break;
                    case "UI_NewUnit":
                        player = Utility.GenerateRandomUnit();
                        playerChoseANewUnit = true;
                        AdvanceGameState(GameState.SceneGeneration);
                        break;
                    case "UI_ChooseNothing":
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
                SceneManager.LoadScene(0);
                break;
        }
    }

    // Opponent picking a move during combat phase. 
    int Opponent_ChooseMove()
    {
        // First priority: healing if opponent has Restore.
        if(opponent.CurrentHitPoints <= (opponent.MaxHitPoints / 3))
        {
            int idx = Array.FindIndex(opponent.Moves, o => o.MoveID == 19);
            if (idx != -1) { return idx; }
        }

        // Second priority: KILL
        for(int i = 0; i < 4; ++i)
        {
            if (TypeConverter.DamageMod(opponent.Moves[i].Type, player.Type) > 1) { return i; }
        }

        if(opponent.CurrentHitPoints >= (opponent.MaxHitPoints / 2) && player.CurrentHitPoints >= (player.MaxHitPoints / 2))
        {
            // Third priority: inverting self attack and defense debuffs. But only if both sides hp is good.
            if (battle.AttackDecayFlag.Item2)
            {
                int idx = Array.FindIndex(opponent.Moves, o => o.MoveID == 13);
                if (idx != -1) { return idx; }
            }
            if (battle.DefenseDecayFlag.Item2)
            {
                int idx = Array.FindIndex(opponent.Moves, o => o.MoveID == 14);
                if (idx != -1) { return idx; }
            }
        }

        // Forth priority: STAB move
        for (int i = 0; i < 4; ++i)
        {
            if (opponent.Moves[i].Type == opponent.Type) { return i; }
        }

        // Fifth priority: choose a random move based on usefulness. 
        int ineffectiveMove = -1;
        for(int i = 0; i < 4; ++i)
        {
            if (TypeConverter.DamageMod(opponent.Moves[i].Type, player.Type) == 0.5) { ineffectiveMove = i; break; }
        }

        List<int> bannedMoves = new(); // Try to ban moves that won't do anything useful. 
        for(int i = 0; i < 4; ++i)
        {
            switch(opponent.Moves[i].MoveID)
            {
                case 12: if(battle.BurnFlag.Item1) { bannedMoves.Add(i); } break;
                case 13: if (battle.AttackAmpFlag.Item2) { bannedMoves.Add(i); } break;
                case 14: if (battle.DefenseAmpFlag.Item2) { bannedMoves.Add(i); } break;
                case 15: if (battle.AccuracyAmpFlag.Item2) { bannedMoves.Add(i); } break;
                case 16: if(battle.AttackDecayFlag.Item1) { bannedMoves.Add(i); } break;
                case 17: if (battle.DefenseDecayFlag.Item1) { bannedMoves.Add(i); } break;
                case 18: if (battle.AccuracyDecayFlag.Item1) { bannedMoves.Add(i); } break;
                case 19: if (opponent.CurrentHitPoints == opponent.MaxHitPoints) { bannedMoves.Add(i); } break; // Restore
            }
        }
        if (bannedMoves.Count == 3) { return ineffectiveMove; }
        System.Random r = new();
        while (true)
        {
            int randMove = r.Next(0, 4);
            if(!bannedMoves.Contains(randMove)) { return randMove; }
        }
    }

    // Animation stuff.
    public void AddHitAnimationToQueue(Units.Type type, float healthPercentage, bool targetIsPlayer, string uiText)
    {
        hitAnimationsQueue.Enqueue((type, healthPercentage, targetIsPlayer, uiText));
    }

    public void RunAllAnimations()
    {
        for(int i = 0; i < hitAnimationsQueue.Count; ++i) 
        { 
            Invoke(nameof(Animation_HitUnit), i); 
            if(i  == hitAnimationsQueue.Count - 1)
            {
                Invoke(nameof(IndicateEndOfAnimations), i + 0.5f);
            }
        }
    }

    private void Animation_HitUnit()
    {
        var item = hitAnimationsQueue.Dequeue();
        if(item.Item1 != Units.Type.NoType)
        {
            string m = @$"Assets/Assets/Particles/Materials/{TypeConverter.TypeToString(item.Item1)}Material.mat";
            Material material = Instantiate(AssetDatabase.LoadAssetAtPath<Material>(m));
            ParticleSystem particles = item.Item3 ? p_particles : o_particles;
            particles.GetComponent<Renderer>().material = material;
            particles.Play();
            // Alter health bar
            string who = item.Item3 ? "Player" : "Opponent";
            var bar = GameObject.Find($"UI_{who}_HPBar").GetComponent<Image>();
            bar.rectTransform.localScale = new(item.Item2, bar.rectTransform.localScale.y, bar.rectTransform.localScale.z);
        }
        GameObject.Find($"UI_InfoText").GetComponent<TextMeshProUGUI>().text = item.Item4;
    }

    private void IndicateEndOfAnimations()
    {
        animationsArePlaying = false;
    }

    // Tools
    void AdvanceGameState() // Advance to next.
    {
        if (gameState == GameState.PlayerDied) { gameState = GameState.SceneGeneration; }
        else { ++gameState; }
    }

    void AdvanceGameState(GameState gs) // Advance to specific. 
    {
        this.gameState = gs;
    }

    public void PrintHealth(bool isPlayer)
    {
        string who = isPlayer ? "player" : "opponent";
        int current = isPlayer ? player.CurrentHitPoints : opponent.CurrentHitPoints;
        int max = isPlayer ? player.MaxHitPoints : opponent.MaxHitPoints;
        Debug.Log($"Printing HP for {who}: {current}/{max}");
    }
}
