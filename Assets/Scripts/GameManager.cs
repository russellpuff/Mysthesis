using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Battle;
using System;
using System.Linq;
using Units;

public class GameManager : MonoBehaviour
{
    private Battle.Battle battle;
    private Units.Unit player;
    private Units.Unit opponent;
    private GameState gameState;
    private int moveChosenByPlayer;
    private bool roundOngoing;

    enum GameState
    {
        SceneGeneration,
        PlayerMoveSelection,
        OpponentMoveSelection,
        MoveAnimations,
        HPDrainAnimations,
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
        ClickableSprite[] cs = FindObjectsOfType<ClickableSprite>();
        foreach(ClickableSprite c in cs) { c.OnClick += Event_ButtonClicked; }
    }

    void Update()
    {
        if(!roundOngoing)
        {
            switch (gameState)
            {
                case GameState.SceneGeneration:

                    AdvanceGameState();
                    break;
                case GameState.PlayerMoveSelection:
                    // Wait for player to choose move.
                    break;
                case GameState.OpponentMoveSelection:
                    battle.UpdateMovesChosen(moveChosenByPlayer, Opponent_ChooseMove());
                    AdvanceGameState();
                    roundOngoing = true;
                    if (battle.Round())
                        { AdvanceGameState(GameState.KnockOutAnimation); }
                    else
                        { AdvanceGameState(GameState.PlayerMoveSelection); }
                    break;
                case GameState.MoveAnimations:
                    break;
                case GameState.HPDrainAnimations:
                    break;
                case GameState.KnockOutAnimation:
                    break;
                case GameState.PlayerSelectingNewMoveOrUnit:
                    // Wait for player to swap a move or unit, or neither.
                    break;
                case GameState.PlayerDied:
                    break;
            }
        }
    }

    void UI_UpdatePlayerMoves() // When the player swaps a move or unit.
    {

    }

    void UI_FlipVisibilityOfWinScreen() 
    {
        // The player knocks out an opponent. 

        // The player selected a new move or unit. 
    }

    void UI_FlipVisibilityOfLoseScreen() 
    {
        // The player was knocked out.
    }

    void UI_FlipVisibilityOfPlayerMoves()
    {
        GameObject[] playerMoves = GameObject.FindGameObjectsWithTag("MoveButton");
        foreach (GameObject move in playerMoves) { move.SetActive(!move.activeSelf); }
    }

    void Event_ButtonClicked(GameObject button)
    {
        if(gameState == GameState.PlayerMoveSelection)
        {
            Debug.Log("Clicked " + button.name);
            moveChosenByPlayer = button.name[name.Length - 1] - '0'; // Get index from name
            AdvanceGameState();
        } 
        else if(gameState == GameState.PlayerSelectingNewMoveOrUnit)
        {
            // Select new move, unit, or neither.
            AdvanceGameState(GameState.SceneGeneration);
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
        // Play animation based on type and whether the target is the player. 
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
