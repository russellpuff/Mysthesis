using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Units;
using System;

namespace Battle
{
    public class Battle
    {
        Unit unitWithInitiative;
        Unit other;
        Move a_move;
        Move o_move;
        readonly bool playerHasInit;
        bool movesUpdatedThisRound;

        // Status effect flags. Item1 is the unit with advantage, Item2 is the other unit. 
        public (bool, bool) BurnFlag { get; set; }
        public (bool, bool) AttackAmpFlag { get; set; }
        public (bool, bool) DefenseAmpFlag { get; set; }
        public (bool, bool) AccuracyAmpFlag { get; set; }
        public (bool, bool) AttackDecayFlag { get; set; }
        public (bool, bool) DefenseDecayFlag { get; set; }
        public (bool, bool) AccuracyDecayFlag { get; set; }

        public Battle(ref Unit _unitWithInitiative, ref Unit _other, bool _playerInit)
        {
            this.unitWithInitiative = _unitWithInitiative;
            this.other = _other;
            this.playerHasInit = _playerInit;
            movesUpdatedThisRound = false;
        }

        // Returns true if there was a knockout. False means the battle continues. 
        public bool Round()
        {
            if(!movesUpdatedThisRound) { throw new NotImplementedException(); } // idk lol replace with a better check

            Turn a_turn = new(ref unitWithInitiative, ref other, a_move, true);
            Turn o_turn = new(ref other, ref unitWithInitiative, o_move, false);
            movesUpdatedThisRound = false;
            return a_turn.Act(this) || o_turn.Act(this); // Will short circuit return true if someone is knocked out during a_turn, instantly ending the battle.
        }

        // Matches the moves displayed in UI positions 1, 2, 3, 4 to the index of the same move in the Unit class. 
        public void UpdateMovesChosen(int p_move, int e_move)
        {
            if(playerHasInit)
            {
                a_move = unitWithInitiative.Moves[p_move - 1];
                o_move = other.Moves[e_move - 1];
            } else
            {
                o_move = unitWithInitiative.Moves[p_move - 1];
                a_move = other.Moves[e_move - 1];
            }

            movesUpdatedThisRound = true;
        }
    }

    public class Turn
    {
        readonly Unit attacker;
        readonly Unit defender;
        readonly Move move;
        bool moveHits = false;
        readonly bool attackerHasInit;

        public Turn(ref Unit _attacker, ref Unit defender, Move _move, bool _attackerHasInit)
        {
            this.attacker = _attacker;
            this.defender = defender;
            this.move = _move;
            attackerHasInit = _attackerHasInit;
        }

        // True means there was a knockout. False means the battle continues. 
        public bool Act(Battle battle)
        {
            // Trigger animation for attack. 
            float attacker_attack_mod = 1.0f;
            float defender_defense_mod = 1.0f;
            float attacker_accuracy_mod = 1.0f;
            System.Random rng = new();

            //
            // Roll to hit logic
            //
            if (attackerHasInit ? battle.AccuracyAmpFlag.Item1 : battle.AccuracyAmpFlag.Item2)
                { attacker_accuracy_mod += 0.25f; } // Attacker has accuracy amp.

            if (attackerHasInit ? battle.AccuracyDecayFlag.Item1 : battle.AccuracyDecayFlag.Item2)
                { attacker_accuracy_mod -= 0.25f; } // Attacker has accuracy decay.

            moveHits = rng.Next(1, 101) <= (attacker_accuracy_mod * move.Accuracy);
            //
            // End roll to hit logic
            //

            if(moveHits)
            {
                if (move.IsAttack) // Attacking move.
                {
                    //
                    // On hit logic
                    //
                    if (attackerHasInit ? battle.AttackAmpFlag.Item1 : battle.AttackAmpFlag.Item2)
                    { attacker_attack_mod += 0.55f; } // Attacker has attack amp.

                    if (attackerHasInit ? battle.AttackDecayFlag.Item1 : battle.AttackDecayFlag.Item2)
                    { attacker_attack_mod -= 0.55f; } // Attacker has attack decay.

                    if (attackerHasInit ? battle.DefenseAmpFlag.Item2 : battle.DefenseAmpFlag.Item1)
                    { defender_defense_mod += 0.5f; } // Defender has defense amp.

                    if (attackerHasInit ? battle.DefenseDecayFlag.Item2 : battle.DefenseDecayFlag.Item1)
                    { defender_defense_mod -= 0.5f; } // Defender has defense decay.

                    float random_factor = rng.Next(85, 101) / 100.0f; // Decide random factor.
                    int crit = 1; // Roll for crit.
                    if (rng.Next(1, 25) == 1) { crit = 2; } // Update UI on hit to indicate a crit occurred. 
                    if (attacker.Type == move.Type) { attacker_attack_mod *= 1.25f; } // Check same-type attack bonus. 

                    float damage = 20 * (random_factor / 100.0f) * crit * move.Power * // Mods galore
                        TypeConverter.DamageMod(move.Type, defender.Type) * // Type matchup
                        ((attacker.Attack * attacker_attack_mod) / (defender.Defense * defender_defense_mod * 50)); // Atk/def matchup. 

                    int finalDamage = (int)damage; // Truncate decimal.
                    if (finalDamage <= 0) { finalDamage = 0; }
                    finalDamage *= -1;

                    defender.ModHitPoints(finalDamage);
                    // Play damage taking animation.
                    // Switch on the type of the move to get a generic damage animation.

                    //
                    // End on hit logic
                    //
                }
                else // Status move.
                {
                    switch(move.MoveID) // Abstract this if any complex moves are created. 
                    {
                        case 12: // Burn
                            battle.BurnFlag = attackerHasInit ? 
                                (battle.BurnFlag.Item1, true) : (true, battle.BurnFlag.Item2);
                            // Play opponent burned animation.
                            break;
                        case 13: // Amp Attack
                            battle.AttackAmpFlag = attackerHasInit ? 
                                (true, battle.AttackAmpFlag.Item2) : (battle.AttackAmpFlag.Item1, true);
                            // Play self buff animation.
                            break;
                        case 14: // Amp Defense
                            battle.DefenseAmpFlag = attackerHasInit ? 
                                (true, battle.DefenseAmpFlag.Item2) : (battle.DefenseAmpFlag.Item1, true);
                            // Play self buff animation.
                            break;
                        case 15: // Amp Accuracy
                            battle.AccuracyAmpFlag = attackerHasInit ? 
                                (true, battle.AccuracyAmpFlag.Item2) : (battle.AccuracyAmpFlag.Item1, true);
                            // Play self buff animation.
                            break;
                        case 16: // Decay Attack 
                            battle.AttackDecayFlag = attackerHasInit ? 
                                (battle.AttackDecayFlag.Item1, true) : (true, battle.AttackDecayFlag.Item2);
                            // Play opponent debuff animation.
                            break;
                        case 17: // Decay Defense
                            battle.DefenseDecayFlag = attackerHasInit ? 
                                (battle.DefenseDecayFlag.Item1, true) : (true, battle.DefenseDecayFlag.Item2);
                            // Play opponent debuff animation.
                            break;
                        case 18: // Decay Accuracy
                            battle.AccuracyDecayFlag = attackerHasInit ? 
                                (battle.AccuracyDecayFlag.Item1, true) : (true, battle.AccuracyDecayFlag.Item2);
                            // Play opponent debuff animation.
                            break;
                        case 19: // Restore
                            attacker.ModHitPoints(0.5f); // Restores 50% hp
                            // Play hp restoration animation.
                            break;
                    }
                }
            } else
            {
                // Play miss animation, update UI to indicate move missed. Pass turn. 
            }

            if (attackerHasInit ? battle.BurnFlag.Item1 : battle.BurnFlag.Item2)
            {
                // Burn damage against attacker at the end of their turn. Occurs whether or not the move hit. 
                attacker.ModHitPoints(0.1f); // Deals 10% max hp per turn. 
                // Play animation for burn damage. 
            }

            return attacker.CurrentHitPoints == 0 || defender.CurrentHitPoints == 0;
        }
    }
}
