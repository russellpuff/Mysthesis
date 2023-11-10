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
    private bool roundOngoing;
    private bool playerChoseANewUnit;
    [SerializeField] Sprite[] sprites;
    public ParticleSystem particles;

    enum GameState
    {
        SceneGeneration,
        PlayerMoveSelection,
        OpponentMoveSelection,
        MoveAnimations,
        KnockOutAnimation,
        PlayerSelectingNewMoveOrUnit,
        PlayerDied,
    }

    void Start()
    {
        player = Utility.GenerateRandomUnit();
        opponent = Utility.GenerateRandomUnit();
        battle = new(ref player, ref opponent, true);
        battle.SetGameManager(this);
        gameState = GameState.SceneGeneration;
        roundOngoing = false;
        playerChoseANewUnit = false;
        particles = FindObjectOfType<ParticleSystem>();

        Button[] buttons = FindObjectOfType<Canvas>().GetComponentsInChildren<Button>();
        foreach(Button button in buttons) { button.onClick.AddListener(() => Event_ButtonClicked(button)); }
    }

    void Update()
    {
        switch (gameState)
        {
            case GameState.SceneGeneration:
                // Build scene stuff, refresh player and enemy and player moves.
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
                Debug.Log($"Opponent chose a move: {o_move}");
                battle.UpdateMovesChosen(moveChosenByPlayer, o_move);
                AdvanceGameState();
                roundOngoing = true;
                if (battle.Round()) { AdvanceGameState(GameState.KnockOutAnimation); } // When Round() returns true, someone died.
                else { AdvanceGameState(GameState.PlayerMoveSelection); } // When Round() returns false, set up for a new round.
                break;
            case GameState.MoveAnimations:
                // Move animations are playing.
                
                break;
            case GameState.KnockOutAnimation:
                // Someone being knocked out animation is playing
                AdvanceGameState(GameState.PlayerMoveSelection);
                break;
            case GameState.PlayerSelectingNewMoveOrUnit:
                // Wait for player to swap a move or unit, or neither.
                break;
            case GameState.PlayerDied:
                // Player dead show lose screen.
                break;
        }
    }

    void UI_UpdatePlayerMoves() // Update moves when player switches a move or unit.
    {
        Button[] buttons = FindObjectOfType<Canvas>().GetComponentsInChildren<Button>().Where(x => x.gameObject.name.Contains("UI_Move")).ToArray();
        for(int i = 0; i < 4; ++i)
        {
            Button b = buttons.First(x => x.gameObject.name == $"UI_Move{i}");
            if (player.Moves[i].Type != Units.Type.NoType) 
            {
                string m = @$"Assets/Assets/Particles/{TypeConverter.TypeToString(player.Moves[i].Type).ToLower()}.png";
                Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(m);
                Sprite icon = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0));
                b.GetComponentsInChildren<Image>().Where(x => x.gameObject.name == "Icon").First().sprite = icon;
            } else
            {
                b.GetComponentsInChildren<Image>().Where(x => x.gameObject.name == "Icon").First().sprite = null;
            }
            b.GetComponentInChildren<TextMeshProUGUI>().text = player.Moves[i].Name;
        }
    }

    void UI_UpdatePlayerOrOpponent(bool updatePlayer) // Update player or opponent sprite when switched. True means player switched. 
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
    }

    void UI_SetVisibilityOfWinScreen(bool visible) 
    {
        
    }

    void UI_SetVisibilityOfLoseScreen(bool visible) 
    {
        // The player was knocked out.
    }

    void UI_SetVisibilityOfPlayerMoves(bool visible)
    {
        
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
                        break;
                    case "UI_NewMove1":
                        break;
                    case "UI_NewMove2":
                        break;
                    case "UI_NewUnit":
                        break;
                    case "UI_Continue":
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

    public void Animation_HitUnit(Units.Type type, bool targetIsPlayer)
    {
        StartCoroutine(HitUnitAndWait(type, targetIsPlayer));
    }

    private IEnumerator HitUnitAndWait(Units.Type type, bool targetIsPlayer)
    {
        Debug.Log($"Playing hit animation: {TypeConverter.TypeToString(type)}");
        // Play animation based on type and whether the target is the player. 
        string m = @$"Assets/Assets/Particles/Materials/{TypeConverter.TypeToString(type)}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(m);
        particles.GetComponent<Renderer>().material = mat;
        int x = targetIsPlayer ? -3 : 3;
        particles.transform.position = new Vector2(x, 2);
        var emission = particles.emission;
        emission.enabled = true;
        //
        // FIGURE OUT HOW TO TIME THIS PROPERLY
        //
        yield return new WaitForSeconds(1);
    }

    public void Animation_Buff(bool targetIsPlayer) 
    { 

    }

    public void Animation_Debuff(bool targetIsPlayer)
    {

    }

    public void Animation_Burn(bool targetIsPlayer)
    {

    }

    public void Animation_Restore(bool targetIsPlayer)
    {

    }
}
