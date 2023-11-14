using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Units
{
    public class Unit
    {
        private readonly string name;
        private int currentHitPoints;
        private readonly int maxHitPoints;
        private readonly int attack;
        private readonly int defense;
        private readonly int movementRange;
        private readonly int threatRange = 1; // Current version has a max threatrange of 1.
        private readonly Move[] moves;
        private readonly Type type;

        public string Name { get { return name; } }
        public int CurrentHitPoints { get { return currentHitPoints; } }
        public int MaxHitPoints { get { return maxHitPoints; } }
        public int Attack { get { return attack; } } 
        public int Defense { get { return defense; } }
        public int MovementRange { get { return movementRange; } }
        public int ThreatRange { get { return threatRange; } }
        public Move[] Moves { get { return moves; } }
        public Type Type { get { return type; } }

        public Unit(string _name, int _hp, int _atk, int _def, int _moveRange, Type _type, int _move1, int _move2, int _move3, int _move4)
        {
            this.name = _name;
            this.currentHitPoints = this.maxHitPoints = _hp;
            this.attack = _atk;
            this.defense = _def;
            this.movementRange = _moveRange;
            this.type = _type;

            this.moves = new Move[]
            {
                new Move(_move1), new Move(_move2), new Move(_move3), new Move(_move4)
            };
        }

        // Changing a unit's hit points by value. Positive to heal, negative to damage.
        public void ModHitPoints(int _value)
        {
            currentHitPoints += _value;
            if(currentHitPoints > maxHitPoints) { currentHitPoints = maxHitPoints; }
            if(currentHitPoints < 0) { currentHitPoints = 0; }
        }

        // Changing a unit's hit points by a percentage of their max hit points. Positive to heal, negative to damage. -1 <= x <= 1
        public void ModHitPoints(float _percent)
        {
            if(_percent > 1.0f) { _percent = 1.0f; }
            if(_percent < -1.0f) { _percent = -1.0f; }
            int final = (int)(maxHitPoints * _percent);
            ModHitPoints(final);
        }

        public void SwapMove(int indexOfMoveToReplace, int idOfMoveToAdd)
        {
            this.moves[indexOfMoveToReplace] = new(idOfMoveToAdd);
        }
    }

    public class Move
    {
        private readonly int moveID;
        private readonly string name;
        private readonly int power;
        private readonly int accuracy;
        private readonly bool isAttack; // False means status move, no damage.
        private readonly Type type;

        public int MoveID {  get { return moveID; } }
        public string Name { get { return name; } }
        public int Power { get { return power; } }
        public int Accuracy { get { return accuracy; } }
        public bool IsAttack { get { return isAttack; } }
        public Type Type { get { return type; } }

        public Move(int _id)
        {
            Move toCopy = Utility.allMoves.FirstOrDefault(x => x.moveID ==  _id) ?? new Move(1); // Safety net since this construction style isn't airtight.
            this.moveID = toCopy.moveID;
            this.name = toCopy.name;
            this.power = toCopy.power;
            this.accuracy = toCopy.accuracy;
            this.isAttack = toCopy.isAttack;
            this.type = toCopy.type;
        }

        public Move(int _id, string _name, int _power, int _acc, bool _isAtk, Type _type)
        {
            this.moveID = _id;
            this.name = _name;
            this.power = _power;
            this.accuracy = _acc;
            this.isAttack = _isAtk;
            this.type = _type;
        }
    }

    public enum Type
    {
        Anima,
        Aqua,
        Ferrum,
        Frigid,
        Fulmen,
        Ignis,
        Mentis,
        Mortis,
        Sonus,
        Terra,
        // Ventus,
        Virus,
        Buff,
        Debuff,
        Burn,
        Heal,
        NoType
    }

    public static class Utility
    {
        public readonly static List<Move> allMoves = new()
        { // Basic attacking moves.
            new Move(1, "Star Pulse", 75, 100, true, Type.Anima),
            new Move(2, "Water Jet", 75, 100, true, Type.Aqua),
            new Move(3, "Iron Strike", 75, 100, true, Type.Ferrum),
            new Move(4, "Frost Beam", 75, 100, true, Type.Frigid),
            new Move(5, "Lightning Bolt", 75, 100, true, Type.Fulmen),
            new Move(6, "Flamethrower", 75, 100, true, Type.Ignis),
            new Move(7, "Brain Hemmhorage", 75, 100, true, Type.Mentis),
            new Move(8, "Grave Killer", 75, 100, true, Type.Mortis),
            new Move(9, "Death Toll", 75, 100, true, Type.Sonus),
            new Move(10, "Earthquake", 75, 100, true, Type.Terra),
            //new Move(11, "Hurricane", 75, 100, true, Type.Ventus),
            new Move(11, "Toxic Bomb", 75, 100, true, Type.Virus),

            // Basic status moves.  
            new Move(12, "Burn", 0, 75, false, Type.Burn), // Burn status effect (DoT)
            new Move(13, "Amp Attack", 0, 100, false, Type.Buff), // Increase self attack
            new Move(14, "Amp Defense", 0, 100, false, Type.Buff), // Increase self defense
            new Move(15, "Amp Accuracy", 0, 100, false, Type.Buff), // Increase self accuracy
            new Move(16, "Decay Attack", 0, 100, false, Type.Debuff), // Decrease opponent attack
            new Move(17, "Decay Defense", 0, 100, false, Type.Debuff), // Decrease opponent defense
            new Move(18, "Decay Accuracy", 0, 75, false, Type.Debuff), // Decrease opponent accuracy.
            new Move(19, "Restore", 0, 100, false, Type.Heal), // Heal self by half.
        };

        public static Unit GenerateRandomUnit()
        {
            System.Random rng = new();
            int hp = rng.Next(125, 226);
            int atk = rng.Next(100, 201);
            int def = rng.Next(110, 211);
            int type = rng.Next(11);
            List<int> moves = new();
            while (moves.Count < 4)
            {
                int randomMove = rng.Next(1, 20);
                if (!moves.Contains(randomMove)) { moves.Add(randomMove); }
            }

            if (moves.TrueForAll(x => x >= 12)) // No attacking moves were generated
            {
                int moveToReplace = rng.Next(4);
                moves[moveToReplace] = rng.Next(1, 12); // Randomly replace a move with an attacking move. 
            }

            return new("", hp, atk, def, 0, (Type)type, moves[0], moves[1], moves[2], moves[3]);
        }
    }

    public static class TypeConverter
    {
        // Dictionary for type matchups. 
        static readonly Dictionary<(Type, Type), float> damageModifiers = new()
        { // First type is attacker, second type is defender. 
            // Attacking types will always be strong against 1 type and weak against 1 type (weak against the type strong against itself)
            // For now.
            { (Type.Anima, Type.Mortis), 2.0f },
            { (Type.Anima, Type.Virus), 0.5f },
            { (Type.Aqua, Type.Ignis), 2.0f },
            { (Type.Aqua, Type.Fulmen), 0.5f },
            { (Type.Ferrum, Type.Virus), 2.0f },
            { (Type.Ferrum, Type.Sonus), 0.5f },
            { (Type.Frigid, Type.Terra), 2.0f },
            { (Type.Frigid, Type.Ignis), 0.5f },
            { (Type.Fulmen, Type.Aqua), 2.0f },
            { (Type.Fulmen, Type.Terra), 0.5f },
            { (Type.Ignis, Type.Frigid), 2.0f },
            { (Type.Ignis, Type.Aqua), 0.5f },
            { (Type.Mentis, Type.Sonus), 2.0f },
            { (Type.Mentis, Type.Mortis), 0.5f },
            { (Type.Mortis, Type.Mentis), 2.0f },
            { (Type.Mortis, Type.Anima), 0.5f },
            { (Type.Sonus, Type.Ferrum), 2.0f },
            { (Type.Sonus, Type.Mentis), 0.5f },
            { (Type.Terra, Type.Fulmen), 2.0f },
            { (Type.Terra, Type.Frigid), 0.5f },
            { (Type.Virus, Type.Anima), 2.0f },
            { (Type.Virus, Type.Ferrum), 0.5f }
        };

        public static string TypeToString(Type type)
        {
            return type switch
            {
                Type.Anima => "Anima",
                Type.Aqua   => "Aqua",
                Type.Ferrum => "Ferrum",
                Type.Frigid => "Frigid",
                Type.Fulmen => "Fulmen",
                Type.Ignis  => "Ignis",
                Type.Mentis => "Mentis",
                Type.Mortis => "Mortis",
                Type.Sonus  => "Sonus",
                Type.Terra  => "Terra",
                //Type.Ventus => "Ventus",
                Type.Virus  => "Virus",
                Type.Burn => "Burn",
                Type.Buff => "Buff",
                Type.Debuff => "Debuff",
                Type.Heal => "Heal",
                _ => "NoType"
            };
        }

        public static Type StringToType(string type)
        {
            return type switch
            {
                "Anima" => Type.Anima,
                "Aqua" => Type.Aqua,
                "Ferrum" => Type.Ferrum,
                "Frigid" => Type.Frigid,
                "Fulmen" => Type.Fulmen,
                "Ignis" => Type.Ignis,
                "Mentis" => Type.Mentis,
                "Mortis" => Type.Mortis,
                "Sonus" => Type.Sonus,
                "Terra" => Type.Terra,
                //"Ventus" => Type.Ventus,
                "Virus" => Type.Virus,
                _ => Type.NoType
            };
        }

        public static float DamageMod(Type attackingType, Type defendingType)
        { // Seeks a matchup between the attacking and defending type, without one, returns a 1.0 damage mult. 
            return damageModifiers.TryGetValue((attackingType, defendingType), out float modifier) ? modifier : 1.0f;
        }
    }
}
