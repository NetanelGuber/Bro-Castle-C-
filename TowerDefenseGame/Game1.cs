using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TowerDefenseGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _pixel;

        // Game Constants
        private int maxHealth = 100;
        private int maxMana = 50;
        private float health = 100;
        private float mana = 50;
        private int wave = 1;
        private bool waving = false;
        private int numOfEnemies = 0;
        private int numLeftToSpawn = 10;
        private int numOfEnemiesInWave = 10;
        private bool spawnedBoss = false;
        private float xp = 0;
        private float xpRequired = 100;
        private int level = 1;

        private int archerLevel = 1;
        private float archerDamage = 4;
        private int archerPrice = 50;
        private float archerCritChance = 0.01f; // 1% base crit chance
        private float archerCritDamage = 1.5f; // 150% crit damage

        private int castleLevel = 1;
        private int castlePrice = 100;
        private float castleDefense = 0f; // Castle defense percentage

        private int gold = 0;
        private int crystals = 0;

        private const int ABILITY_BAR_WIDTH = 34;
        private const int ABILITY_BAR_HEIGHT = 7;

        // ===== TEXT OUTLINE CONFIGURATION =====
        private const float TEXT_OUTLINE_THICKNESS = 0.05f;

        // ===== FONT SIZE CONFIGURATION =====
        private const float FONT_SIZE_HP_MP_LABELS = 1.0f;
        private const float FONT_SIZE_HP_MP_VALUES = 1.0f;
        private const float FONT_SIZE_LEVEL = 1.0f;
        private const float FONT_SIZE_GOLD_CRYSTALS = 1.25f;
        private const float FONT_SIZE_WAVE_LABEL = 1.0f;
        private const float FONT_SIZE_WAVE_NUMBER = 1.0f;
        private const float FONT_SIZE_UPGRADE_LABELS = 1.0f;
        private const float FONT_SIZE_UPGRADE_COSTS = 1.0f;
        private const float FONT_SIZE_ARCHER_DAMAGE = 1.0f;
        private const float FONT_SIZE_BATTLE_BUTTON = 1.0f;
        private const float FONT_SIZE_UNIT_LIST_NAME = 0.9f;
        private const float FONT_SIZE_UNIT_LIST_COST = 1.0f;
        private const float FONT_SIZE_UNIT_INFO_STATS = 1.0f;
        private const float FONT_SIZE_UNIT_INFO_DESC = 0.85f;
        private const float FONT_SIZE_UNIT_INFO_MP = 0.9f;
        private const float FONT_SIZE_UNIT_INFO_BUTTONS = 0.75f;
        private const float FONT_SIZE_UNIT_INFO_COSTS = 0.65f;
        private const float FONT_SIZE_CLOSE_BUTTON = 1.0f;
        private const float FONT_SIZE_SPEED_BUTTON = 1.0f;
        private const float FONT_SIZE_ELEMENT = 0.75f;

        // Lists
        private List<Enemy> enemies = new List<Enemy>();
        private List<Arrow> arrows = new List<Arrow>();
        private List<Projectile> projectiles = new List<Projectile>();
        private List<Effect> effects = new List<Effect>();
        private List<Unit> unitsList = new List<Unit>();
        private List<Summon> summons = new List<Summon>();

        private int speed = 1;
        
        // Buff tracking
        private Dictionary<int, BuffState> slotBuffs = new Dictionary<int, BuffState>();
        private BuffState archerBuffs = new BuffState();
        private float globalDefenseReduction = 0f;
        private int globalDefenseReductionTimer = 0;
        private float summonDamageBonus = 0f;
        private int summonDamageTimer = 0;

        // Slots
        private const int MAX_SLOTS = 9;
        private const int SLOT_W = 40;
        private const int SLOT_H = 50;
        private const int SLOT_X0 = 130;
        private const int SLOT_Y_BOTTOM = 200;
        private const int SLOT_DX = 50;
        private const int SLOT_DY = 55;

        private List<Rectangle> slotRects = new List<Rectangle>();
        private int activeSlots = 3;
        private int selectedSlot = -1;
        private bool menuOpen = false;
        private int selectedUnitIndex = -1;
        private bool equipMode = true;
        private int menuScrollOffset = 0;
        private const int MENU_ITEM_HEIGHT = 64;
        private int selectedTab = 0; // 0 = General, 1 = Ability, 2 = Passive

        private List<int?> slotAssignments = new List<int?>();
        private List<List<Enemy>> unitTargets = new List<List<Enemy>>();
        private List<int> unitAbilityCooldowns = new List<int>();
        private List<int> passiveTimers = new List<int>(); // For periodic passives

        private List<Enemy> archerTargets = new List<Enemy>();

        private long frameCount = 0;
        private float spawnTimer = 0;

        private MouseState previousMouseState;
        private Random random = new Random();

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 720;
            _graphics.PreferredBackBufferHeight = 480;
        }

        protected override void Initialize()
        {
            BuildSlotGrid();
            InitializeUnits();
            InitializeSlotArrays();
            SetupWave();
            previousMouseState = Mouse.GetState();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            try
            {
                _font = Content.Load<SpriteFont>("Font");
            }
            catch
            {
                // Font loading error handled
            }
        }

        private void InitializeUnits()
        {
            // Archer - Physical
            unitsList.Add(new Unit
            {
                Name = "Archer",
                Damage = 10,
                AttackSpeed = 1.2f,
                Targets = 1,
                Col = new Color(210, 180, 140),
                Ability = "rapid fire",
                AbilityMpCost = 30,
                AbilityCooldown = 8,
                PassiveAbility = "boss slayer",
                Proj = true,
                Element = "Physical",
                Desc = "Skilled archer with deadly aim.\n\nAbility: Shoots with 100% more attack speed for 5 seconds.\nPassive: Deals 100% bonus damage to bosses.",
                Lvl = 1,
                UnlockCost = 0,
                Unlocked = true,
                MaxLevel = 0
            });

            // Hunter - Physical
            unitsList.Add(new Unit
            {
                Name = "Hunter",
                Damage = 8,
                AttackSpeed = 1.0f,
                Targets = 1,
                Col = new Color(139, 69, 19),
                Ability = "rally archers",
                AbilityMpCost = 25,
                AbilityCooldown = 10,
                PassiveAbility = "none",
                Proj = true,
                Element = "Physical",
                Desc = "Master of the hunt.\n\nAbility: Increases town archer attack speed by 100% for 3 seconds.",
                Lvl = 1,
                UnlockCost = 800,
                Unlocked = false,
                MaxLevel = 0
            });

            // Elf - Physical
            unitsList.Add(new Unit
            {
                Name = "Elf",
                Damage = 9,
                AttackSpeed = 1.5f,
                Targets = 1,
                Col = new Color(144, 238, 144),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "mana steal",
                Proj = true,
                Element = "Physical",
                Desc = "Swift forest guardian.\n\nPassive: 2% of damage dealt stolen as MP.",
                Lvl = 1,
                UnlockCost = 1500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Dark Skeleton - Ice
            unitsList.Add(new Unit
            {
                Name = "Dark Skeleton",
                Damage = 7,
                AttackSpeed = 1.0f,
                Targets = 1,
                Col = new Color(128, 128, 128),
                Ability = "death mark",
                AbilityMpCost = 20,
                AbilityCooldown = 12,
                PassiveAbility = "none",
                Proj = true,
                Element = "Ice",
                Desc = "Undead marksman from the frozen wastes.\n\nAbility: Increases town archer critical chance by 10% for 3 seconds.",
                Lvl = 1,
                UnlockCost = 1200,
                Unlocked = false,
                MaxLevel = 0
            });

            // Ice Mage - Ice
            unitsList.Add(new Unit
            {
                Name = "Ice Mage",
                Damage = 11,
                AttackSpeed = 0.9f,
                Targets = 1,
                Col = new Color(135, 206, 250),
                Ability = "deep freeze",
                AbilityMpCost = 40,
                AbilityCooldown = 15,
                PassiveAbility = "none",
                Proj = true,
                Element = "Ice",
                Desc = "Master of ice magic.\n\nAbility: Freezes all monsters for 4 seconds and deals 50% of hero damage.",
                Lvl = 1,
                UnlockCost = 2500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Lightning Mage - Lightning
            unitsList.Add(new Unit
            {
                Name = "Lightning Mage",
                Damage = 14,
                AttackSpeed = 0.8f,
                Targets = 1,
                Col = Color.Yellow,
                Ability = "thunderstorm",
                AbilityMpCost = 35,
                AbilityCooldown = 10,
                PassiveAbility = "none",
                Proj = true,
                Element = "Lightning",
                Desc = "Wielder of lightning.\n\nAbility: Hit 8 random monsters with a thunderstorm dealing 200% damage.",
                Lvl = 1,
                UnlockCost = 2000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Fire Mage - Fire
            unitsList.Add(new Unit
            {
                Name = "Fire Mage",
                Damage = 12,
                AttackSpeed = 0.85f,
                Targets = 1,
                Col = Color.OrangeRed,
                Ability = "meteor",
                AbilityMpCost = 50,
                AbilityCooldown = 12,
                PassiveAbility = "none",
                Proj = true,
                Element = "Fire",
                Desc = "Master of flames.\n\nAbility: Hit 5 random monsters with a meteor dealing 500% damage.",
                Lvl = 1,
                UnlockCost = 3000,
                Unlocked = false,
                MaxLevel = 0
            });

            // White Mage - Support
            unitsList.Add(new Unit
            {
                Name = "White Mage",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = Color.White,
                Ability = "cooldown reset",
                AbilityMpCost = 60,
                AbilityCooldown = 20,
                PassiveAbility = "none",
                Proj = false,
                Element = "Support",
                Desc = "Divine support mage.\n\nAbility: Reduces all heroes cooldown by 3 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 5000,
                Unlocked = false,
                MaxLevel = 31
            });

            // Ogre - Physical
            unitsList.Add(new Unit
            {
                Name = "Ogre",
                Damage = 20,
                AttackSpeed = 0.6f,
                Targets = 1,
                Col = new Color(160, 82, 45),
                Ability = "shockwave",
                AbilityMpCost = 35,
                AbilityCooldown = 10,
                PassiveAbility = "none",
                Proj = true,
                Element = "Physical",
                Desc = "Brutal giant warrior.\n\nAbility: Knockbacks all monsters on the field back a bit.",
                Lvl = 1,
                UnlockCost = 2200,
                Unlocked = false,
                MaxLevel = 0
            });

            // Necromancer - Support
            unitsList.Add(new Unit
            {
                Name = "Necromancer",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(75, 0, 130),
                Ability = "curse",
                AbilityMpCost = 30,
                AbilityCooldown = 12,
                PassiveAbility = "none",
                Proj = false,
                Element = "Support",
                Desc = "Master of dark magic.\n\nAbility: Decreases defense of all monsters by 50% for 3 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 4000,
                Unlocked = false,
                MaxLevel = 31
            });

            // Military Band - Support
            unitsList.Add(new Unit
            {
                Name = "Military Band",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(218, 165, 32),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "prosperity",
                Proj = false,
                Element = "Support",
                Desc = "Inspiring musicians.\n\nPassive: Increases gold and xp gain by 2% increasing by 2% each level.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 3500,
                Unlocked = false,
                MaxLevel = 31
            });

            // Priest - Support
            unitsList.Add(new Unit
            {
                Name = "Priest",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(255, 215, 0),
                Ability = "bless summons",
                AbilityMpCost = 40,
                AbilityCooldown = 15,
                PassiveAbility = "none",
                Proj = false,
                Element = "Support",
                Desc = "Holy support priest.\n\nAbility: Increases attack damage of summoned units by 30% for 3 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 4500,
                Unlocked = false,
                MaxLevel = 31
            });

            // Tiny Giant - Summoner/Physical
            unitsList.Add(new Unit
            {
                Name = "Tiny Giant",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(205, 133, 63),
                Ability = "summon giant",
                AbilityMpCost = 50,
                AbilityCooldown = 18,
                PassiveAbility = "none",
                Proj = false,
                Element = "Summoner/Physical",
                Desc = "Giant summoner.\n\nAbility: Summons 1 giant that deals 400% damage per hit and despawns after 15 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 5000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Slinger - Summoner/Physical
            unitsList.Add(new Unit
            {
                Name = "Slinger",
                Damage = 8,
                AttackSpeed = 1.0f,
                Targets = 1,
                Col = new Color(184, 134, 11),
                Ability = "summon slingers",
                AbilityMpCost = 45,
                AbilityCooldown = 16,
                PassiveAbility = "none",
                Proj = true,
                Element = "Summoner/Physical",
                Desc = "Summoner of slingers.\n\nAbility: Summons 5 slingers at the castle that do not move forward dealing 100% damage per hit and despawn after 15 seconds.",
                Lvl = 1,
                UnlockCost = 3500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Smith - Support
            unitsList.Add(new Unit
            {
                Name = "Smith",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(192, 192, 192),
                Ability = "repair",
                AbilityMpCost = 40,
                AbilityCooldown = 25,
                PassiveAbility = "none",
                Proj = false,
                Element = "Support",
                Desc = "Master craftsman.\n\nAbility: Repairs the castle by 20% of its max HP.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 3000,
                Unlocked = false,
                MaxLevel = 31
            });

            // Voodoo - Poison
            unitsList.Add(new Unit
            {
                Name = "Voodoo",
                Damage = 8,
                AttackSpeed = 1.2f,
                Targets = 3,
                Col = new Color(148, 0, 211),
                Ability = "poison cloud",
                AbilityMpCost = 35,
                AbilityCooldown = 12,
                PassiveAbility = "none",
                Proj = true,
                Element = "Poison",
                Desc = "Dark witch doctor.\n\nAbility: Creates a poison cloud that deals 200% damage over 3 seconds to all ground enemies.\n\nAttack: Shoots 3 darts that deal 100% damage each.",
                Lvl = 1,
                UnlockCost = 2800,
                Unlocked = false,
                MaxLevel = 0
            });

            // Knight - Summoner/Physical
            unitsList.Add(new Unit
            {
                Name = "Knight",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(169, 169, 169),
                Ability = "summon knights",
                AbilityMpCost = 45,
                AbilityCooldown = 16,
                PassiveAbility = "none",
                Proj = false,
                Element = "Summoner/Physical",
                Desc = "Noble knight commander.\n\nAbility: Summons 5 knights that deal 100% damage each and despawn after 15 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 4000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Lisa - Summoner
            unitsList.Add(new Unit
            {
                Name = "Lisa",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(255, 192, 203),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "summon melee",
                Proj = false,
                Element = "Summoner",
                Desc = "Necromancer apprentice.\n\nPassive: Summons 2 melee skeletons that deal 100% damage per hit and despawn after 15 seconds every 10 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 5500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Alice - Summoner
            unitsList.Add(new Unit
            {
                Name = "Alice",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(255, 160, 122),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "summon bow",
                Proj = false,
                Element = "Summoner",
                Desc = "Skeleton archer master.\n\nPassive: Summons 2 bow skeletons that deal 100% damage per hit and despawn after 15 seconds every 10 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 5500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Dorothy - Summoner
            unitsList.Add(new Unit
            {
                Name = "Dorothy",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(221, 160, 221),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "summon mage",
                Proj = false,
                Element = "Summoner",
                Desc = "Skeleton mage master.\n\nPassive: Summons 2 mage skeletons that deal 100% damage per hit and despawn after 15 seconds every 10 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 5500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Druid - Summoner/Physical
            unitsList.Add(new Unit
            {
                Name = "Druid",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(34, 139, 34),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "summon ent",
                Proj = false,
                Element = "Summoner/Physical",
                Desc = "Ancient forest protector.\n\nPassive: Summons 1 immortal ent at the beginning of a wave dealing 200% damage and increasing castle defense by 5% for each ent.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 7000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Assassin - Lightning
            unitsList.Add(new Unit
            {
                Name = "Assassin",
                Damage = 16,
                AttackSpeed = 1.8f,
                Targets = 1,
                Col = new Color(47, 79, 79),
                Ability = "shadow strike",
                AbilityMpCost = 40,
                AbilityCooldown = 11,
                PassiveAbility = "none",
                Proj = true,
                Element = "Lightning",
                Desc = "Swift shadow warrior.\n\nAbility: Hits all monsters on the field for 80% damage.",
                Lvl = 1,
                UnlockCost = 3500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Windy - None
            unitsList.Add(new Unit
            {
                Name = "Windy",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(176, 224, 230),
                Ability = "tornado",
                AbilityMpCost = 50,
                AbilityCooldown = 15,
                PassiveAbility = "none",
                Proj = false,
                Element = "None",
                Desc = "Wind spirit.\n\nAbility: Creates a tornado that goes from left to right across the field over a span of 3 seconds dealing 100% damage to all enemies.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 4500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Angel - Summoner/Lightning
            unitsList.Add(new Unit
            {
                Name = "Angel",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(255, 250, 250),
                Ability = "summon angels",
                AbilityMpCost = 55,
                AbilityCooldown = 18,
                PassiveAbility = "divine wings",
                Proj = false,
                Element = "Summoner/Lightning",
                Desc = "Heavenly messenger.\n\nAbility: Summons 5 angels that fly and can hit both ground and air enemies dealing 100% damage per hit and despawn after 15 seconds.\nPassive: Deal 100% bonus damage to flying enemies.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 6000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Zeus - Lightning
            unitsList.Add(new Unit
            {
                Name = "Zeus",
                Damage = 18,
                AttackSpeed = 0.8f,
                Targets = 5,
                Col = Color.Gold,
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "thunder god",
                Proj = false,
                Element = "Lightning",
                Desc = "King of the gods.\n\nPassive: Every 10 seconds, hit 5 enemies for 150% damage (Instant).\nAttack: Hit 5 enemies for 100% damage (Instant).",
                Lvl = 1,
                UnlockCost = 8000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Golem Master - Summoner/Physical
            unitsList.Add(new Unit
            {
                Name = "Golem Master",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(105, 105, 105),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "summon golem",
                Proj = false,
                Element = "Summoner/Physical",
                Desc = "Master of stone golems.\n\nPassive: Summons 1 immortal stone golem dealing 400% damage per hit.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 9000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Succubus - Poison
            unitsList.Add(new Unit
            {
                Name = "Succubus",
                Damage = 15,
                AttackSpeed = 1.5f,
                Targets = 1,
                Col = new Color(199, 21, 133),
                Ability = "demonic speed",
                AbilityMpCost = 45,
                AbilityCooldown = 13,
                PassiveAbility = "charm",
                Proj = false,
                Element = "Poison",
                Desc = "Seductive demon.\n\nPassive: Every 10 seconds, converts 4 random enemies to our side for 3 seconds before they switch back to attack us.\nAbility: Increase attack speed by 200% for 5 seconds.\nAttack: A slash at an enemy (Instant).",
                Lvl = 1,
                UnlockCost = 7500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Elizabeth - Ice
            unitsList.Add(new Unit
            {
                Name = "Elizabeth",
                Damage = 22,
                AttackSpeed = 1.0f,
                Targets = 1,
                Col = new Color(100, 149, 237),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "deadly precision",
                Proj = false,
                Element = "Ice",
                Desc = "Elite gunslinger.\n\nPassive: Increase own critical damage by 200%.\nAttack: Shoots a gun (Instant).",
                Lvl = 1,
                UnlockCost = 6500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Defender - Support
            unitsList.Add(new Unit
            {
                Name = "Defender",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(70, 130, 180),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "fortify",
                Proj = false,
                Element = "Support",
                Desc = "Castle defender.\n\nPassive: Increases castle defense by 10% going up by 0.1% per level.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 5000,
                Unlocked = false,
                MaxLevel = 31
            });

            // Goblin - Fire
            unitsList.Add(new Unit
            {
                Name = "Goblin",
                Damage = 13,
                AttackSpeed = 1.1f,
                Targets = 1,
                Col = new Color(107, 142, 35),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "none",
                Proj = true,
                Element = "Fire",
                Desc = "Explosive goblin.\n\nAttack: Throws a tnt dealing 100% damage in a radius.",
                Lvl = 1,
                UnlockCost = 2500,
                Unlocked = false,
                MaxLevel = 0
            });

            // Alchemist - Support/Poison
            unitsList.Add(new Unit
            {
                Name = "Alchemist",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(50, 205, 50),
                Ability = "polymorph",
                AbilityMpCost = 40,
                AbilityCooldown = 20,
                PassiveAbility = "none",
                Proj = false,
                Element = "Support/Poison",
                Desc = "Master of transformation.\n\nAbility: Transform 2 random enemies into frogs that do nothing and disappear after 15 seconds.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 5500,
                Unlocked = false,
                MaxLevel = 31
            });

            // Rogue - Poison
            unitsList.Add(new Unit
            {
                Name = "Rogue",
                Damage = 14,
                AttackSpeed = 1.6f,
                Targets = 1,
                Col = new Color(72, 61, 139),
                Ability = "shadow clone",
                AbilityMpCost = 35,
                AbilityCooldown = 14,
                PassiveAbility = "none",
                Proj = true,
                Element = "Poison",
                Desc = "Master of deception.\n\nAbility: Creates 4 clones of itself to attack 4 random enemies once for 200% damage.",
                Lvl = 1,
                UnlockCost = 3800,
                Unlocked = false,
                MaxLevel = 0
            });

            // Chrono - Support
            unitsList.Add(new Unit
            {
                Name = "Chrono",
                Damage = 10,
                AttackSpeed = 0f,
                Targets = 0,
                Col = new Color(138, 43, 226),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "time warp",
                Proj = false,
                Element = "Support",
                Desc = "Master of time.\n\nPassive: Increases game speed by 10%.\n\nHas no attack.",
                Lvl = 1,
                UnlockCost = 10000,
                Unlocked = false,
                MaxLevel = 31
            });

            // Poseidon - Ice
            unitsList.Add(new Unit
            {
                Name = "Poseidon",
                Damage = 17,
                AttackSpeed = 0.9f,
                Targets = 5,
                Col = new Color(0, 191, 255),
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "none",
                Proj = false,
                Element = "Ice",
                Desc = "God of the sea.\n\nAttack: Hits 5 random enemies with a water splash for 100% damage (Instant).",
                Lvl = 1,
                UnlockCost = 7000,
                Unlocked = false,
                MaxLevel = 0
            });

            // Ice Sorceress - Ice
            unitsList.Add(new Unit
            {
                Name = "Ice Sorceress",
                Damage = 13,
                AttackSpeed = 1.0f,
                Targets = 1,
                Col = new Color(173, 216, 230),
                Ability = "blizzard",
                AbilityMpCost = 45,
                AbilityCooldown = 13,
                PassiveAbility = "frostbite",
                Proj = true,
                Element = "Ice",
                Desc = "Mistress of ice.\n\nAbility: Hits 8 random enemies with a blizzard for 100% damage each.\nPassive: Has a 20% chance to freeze hit monsters for 2 seconds.",
                Lvl = 1,
                UnlockCost = 4500,
                Unlocked = false,
                MaxLevel = 0
            });
        }

        private void BuildSlotGrid()
        {
            slotRects.Clear();
            for (int r = 0; r < 3; r++)
            {
                int y = SLOT_Y_BOTTOM - r * SLOT_DY;
                for (int c = 0; c < 3; c++)
                {
                    slotRects.Add(new Rectangle(SLOT_X0 + c * SLOT_DX, y, SLOT_W, SLOT_H));
                }
            }
        }

        private void InitializeSlotArrays()
        {
            slotAssignments = new List<int?>(new int?[activeSlots]);
            unitTargets = new List<List<Enemy>>();
            unitAbilityCooldowns = new List<int>(new int[activeSlots]);
            passiveTimers = new List<int>(new int[activeSlots]);
            
            for (int i = 0; i < activeSlots; i++)
            {
                unitTargets.Add(new List<Enemy>());
                slotBuffs[i] = new BuffState();
            }
        }

        private void EnsureSlotArraysSize()
        {
            while (slotAssignments.Count < activeSlots)
                slotAssignments.Add(null);
            
            while (slotAssignments.Count > activeSlots)
                slotAssignments.RemoveAt(slotAssignments.Count - 1);

            while (unitTargets.Count < activeSlots)
                unitTargets.Add(new List<Enemy>());
            
            while (unitTargets.Count > activeSlots)
                unitTargets.RemoveAt(unitTargets.Count - 1);

            while (unitAbilityCooldowns.Count < activeSlots)
                unitAbilityCooldowns.Add(0);
            
            while (unitAbilityCooldowns.Count > activeSlots)
                unitAbilityCooldowns.RemoveAt(unitAbilityCooldowns.Count - 1);

            while (passiveTimers.Count < activeSlots)
                passiveTimers.Add(0);
            
            while (passiveTimers.Count > activeSlots)
                passiveTimers.RemoveAt(passiveTimers.Count - 1);

            for (int i = 0; i < activeSlots; i++)
            {
                if (!slotBuffs.ContainsKey(i))
                    slotBuffs[i] = new BuffState();
            }
        }

        private void MaybeUnlockSlotOnCastleMilestone()
        {
            if (castleLevel % 5 == 0 && activeSlots < MAX_SLOTS)
            {
                activeSlots++;
                EnsureSlotArraysSize();
            }
        }

        private void SetupWave()
        {
            int enemiesInWave = Math.Min(10 * (int)Math.Round(wave / 2.0), 200);
            numLeftToSpawn = enemiesInWave;
            numOfEnemiesInWave = enemiesInWave;
            spawnedBoss = false;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            frameCount++;

            // Update stats
            archerDamage = 4 + (archerLevel / 2f);
            maxHealth = 100 + ((castleLevel - 1) * 50);
            maxMana = 50 + ((castleLevel - 1) * 10);
            archerPrice = 50 + ((archerLevel - 1) * 25);
            castlePrice = 100 + ((castleLevel - 1) * 50);
            xpRequired = 100 + (level - 1) * 25;

            // Calculate castle defense from Defender and Druid ents
            castleDefense = 0f;
            for (int i = 0; i < slotAssignments.Count; i++)
            {
                var unitIdx = slotAssignments[i];
                if (unitIdx != null)
                {
                    var unit = unitsList[unitIdx.Value];
                    if (unit.PassiveAbility == "fortify")
                    {
                        int lvl = Math.Min(unit.Lvl, 51);
                        castleDefense += 0.10f + (lvl - 1) * 0.001f;
                    }
                }
            }
            
            // Count ents for castle defense bonus
            int entCount = summons.Count(s => s.Type == "ent" && s.Immortal);
            castleDefense += entCount * 0.05f;

            if (!waving)
            {
                health = maxHealth;
                mana = maxMana;
            }

            if (xp >= xpRequired)
            {
                level++;
                xp = 0;
            }

            // Handle mouse input
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                HandleMouseClick(mouseState.X, mouseState.Y);
            }
            
            // Handle mouse wheel scrolling for menu
            if (menuOpen && selectedUnitIndex == -1)
            {
                int scrollDelta = mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue;
                if (scrollDelta != 0)
                {
                    menuScrollOffset -= scrollDelta / 10;
                    
                    // Clamp scroll offset
                    int maxScroll = Math.Max(0, (unitsList.Count * MENU_ITEM_HEIGHT) - 450);
                    menuScrollOffset = Math.Max(0, Math.Min(menuScrollOffset, maxScroll));
                }
            }
            
            previousMouseState = mouseState;

            // Combat tick
            if (waving)
            {
                HandleWave();
                if (mana < maxMana)
                    mana += (maxMana / 1000f) * GetEffectiveSpeed();
            }

            // Update projectiles and effects
            UpdateProjectilesAndEffects();

            // Update ability cooldowns and buffs
            UpdateCooldownsAndBuffs();

            base.Update(gameTime);
        }

        private float GetEffectiveSpeed()
        {
            float effectiveSpeed = speed;
            
            // Apply Chrono time warp
            for (int i = 0; i < slotAssignments.Count; i++)
            {
                var unitIdx = slotAssignments[i];
                if (unitIdx != null)
                {
                    var unit = unitsList[unitIdx.Value];
                    if (unit.PassiveAbility == "time warp")
                    {
                        effectiveSpeed *= 1.1f;
                    }
                }
            }
            
            return effectiveSpeed;
        }

        private void UpdateProjectilesAndEffects()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            
            // Arrows
            for (int i = arrows.Count - 1; i >= 0; i--)
            {
                var arrow = arrows[i];
                arrow.X += 20 * effectiveSpeed;
                arrow.Y = GetParabolaY(arrow.X, arrow.FromX, arrow.FromY, arrow.GoingToX, arrow.GoingToY, 0.001f);
                
                if (arrow.X > arrow.GoingToX)
                {
                    if (arrow.Enemy != null && enemies.Contains(arrow.Enemy))
                    {
                        // Calculate crit
                        bool isCrit = random.NextDouble() < archerCritChance;
                        float damage = archerDamage * (isCrit ? archerCritDamage : 1f);
                        
                        // Apply defense reduction
                        damage *= (1f + globalDefenseReduction);
                        
                        arrow.Enemy.Health -= damage;
                    }
                    arrows.RemoveAt(i);
                }
            }

            // Projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                proj.X += 20 * effectiveSpeed;
                proj.Y = GetLineY(proj.X, proj.FromX, proj.FromY, proj.GoingToX, proj.GoingToY);
                
                if (proj.X > proj.GoingToX)
                {
                    if (proj.Enemy != null && enemies.Contains(proj.Enemy) && !proj.Enemy.IsCharmed)
                    {
                        float damage = GetUnitStat(proj.Tower, "damage", proj.SlotIndex);
                        
                        // Apply defense reduction
                        damage *= (1f + globalDefenseReduction);
                        
                        // Check for crit
                        bool isCrit = random.NextDouble() < GetUnitCritChance(proj.Tower, proj.SlotIndex);
                        if (isCrit)
                        {
                            damage *= GetUnitCritDamage(proj.Tower, proj.SlotIndex);
                        }
                        
                        proj.Enemy.Health -= damage;
                        
                        // Elf mana steal
                        if (proj.Tower.PassiveAbility == "mana steal" && mana < maxMana)
                        {
                            mana += damage * 0.02f;
                            if (mana > maxMana) mana = maxMana;
                        }
                        
                        // Ice Sorceress freeze chance
                        if (proj.Tower.PassiveAbility == "frostbite" && random.NextDouble() < 0.2)
                        {
                            proj.Enemy.FreezeTimer = 120; // 2 seconds
                        }
                    }
                    projectiles.RemoveAt(i);
                }
            }

            // Effects
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                effects[i].Timer -= (int)effectiveSpeed;
                if (effects[i].Timer <= 0)
                    effects.RemoveAt(i);
            }
        }

        private void UpdateCooldownsAndBuffs()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            
            // Update ability cooldowns
            for (int i = 0; i < unitAbilityCooldowns.Count; i++)
            {
                if (unitAbilityCooldowns[i] > 0)
                    unitAbilityCooldowns[i] -= (int)effectiveSpeed;
                if (unitAbilityCooldowns[i] < 0)
                    unitAbilityCooldowns[i] = 0;
            }

            // Update passive timers
            for (int i = 0; i < passiveTimers.Count; i++)
            {
                if (passiveTimers[i] > 0)
                    passiveTimers[i] -= (int)effectiveSpeed;
                if (passiveTimers[i] < 0)
                    passiveTimers[i] = 0;
            }

            // Update unit buffs
            foreach (var kvp in slotBuffs.ToList())
            {
                var buff = kvp.Value;
                if (buff.AttackSpeedBonusTimer > 0)
                {
                    buff.AttackSpeedBonusTimer -= (int)effectiveSpeed;
                    if (buff.AttackSpeedBonusTimer <= 0)
                    {
                        buff.AttackSpeedBonus = 0f;
                        buff.DamageBonus = 0f;
                    }
                }
            }

            // Update archer buffs
            if (archerBuffs.AttackSpeedBonusTimer > 0)
            {
                archerBuffs.AttackSpeedBonusTimer -= (int)effectiveSpeed;
                if (archerBuffs.AttackSpeedBonusTimer <= 0)
                {
                    archerBuffs.AttackSpeedBonus = 0f;
                }
            }
            
            if (archerBuffs.CritChanceBonusTimer > 0)
            {
                archerBuffs.CritChanceBonusTimer -= (int)effectiveSpeed;
                if (archerBuffs.CritChanceBonusTimer <= 0)
                {
                    archerBuffs.CritChanceBonus = 0f;
                }
            }

            // Update global buffs
            if (globalDefenseReductionTimer > 0)
            {
                globalDefenseReductionTimer -= (int)effectiveSpeed;
                if (globalDefenseReductionTimer <= 0)
                {
                    globalDefenseReduction = 0f;
                }
            }

            if (summonDamageTimer > 0)
            {
                summonDamageTimer -= (int)effectiveSpeed;
                if (summonDamageTimer <= 0)
                {
                    summonDamageBonus = 0f;
                }
            }
        }

        private void HandleWave()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            
            // Enemy movement and combat
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                
                // Update freeze timer
                if (enemy.FreezeTimer > 0)
                {
                    enemy.FreezeTimer -= (int)effectiveSpeed;
                    if (enemy.FreezeTimer < 0) enemy.FreezeTimer = 0;
                }
                
                // Update frog timer
                if (enemy.IsFrog && enemy.FrogTimer > 0)
                {
                    enemy.FrogTimer -= (int)effectiveSpeed;
                    if (enemy.FrogTimer <= 0)
                    {
                        enemies.RemoveAt(i);
                        numOfEnemies--;
                        continue;
                    }
                }
                
                // Update charm timer
                if (enemy.IsCharmed && enemy.CharmTimer > 0)
                {
                    enemy.CharmTimer -= (int)effectiveSpeed;
                    if (enemy.CharmTimer <= 0)
                    {
                        enemy.IsCharmed = false;
                        enemy.AttackingTarget = null;
                        enemy.CharmTarget = null;
                    }
                }
                
                // Skip movement if frozen or frog
                if (enemy.FreezeTimer <= 0 && !enemy.IsFrog)
                {
                    if (!enemy.IsCharmed)
                    {
                        // Regular enemy behavior
                        // Check if blocked by summon
                        bool blockedBySummon = false;
                        Summon blockingSummon = null;
                        
                        foreach (var summon in summons)
                        {
                            if (!summon.Immortal && !summon.IsStationary && Math.Abs(enemy.XPos - summon.XPos) < 30)
                            {
                                blockedBySummon = true;
                                blockingSummon = summon;
                                break;
                            }
                        }
                        
                        if (!blockedBySummon)
                        {
                            enemy.XPos -= enemy.Speed * effectiveSpeed;
                            enemy.AttackingTarget = null;

                            if (enemy.XPos < 275)
                            {
                                enemy.Speed = 0;
                                float damageToHealth = (enemy.Damage / (60f / enemy.AttackSpeed)) * effectiveSpeed;
                                damageToHealth *= (1f - castleDefense);
                                health -= damageToHealth;
                            }
                        }
                        else
                        {
                            // Enemy attacks summon (one-to-one targeting)
                            if (enemy.AttackingTarget == null || !summons.Contains(enemy.AttackingTarget))
                            {
                                // Find a summon that's not being attacked
                                var availableSummon = summons.FirstOrDefault(s => !s.Immortal && !s.IsStationary && 
                                    Math.Abs(enemy.XPos - s.XPos) < 30 && 
                                    !enemies.Any(e => e.AttackingTarget == s && e != enemy));
                                
                                if (availableSummon != null)
                                {
                                    enemy.AttackingTarget = availableSummon;
                                }
                            }
                            
                            if (enemy.AttackingTarget != null && summons.Contains(enemy.AttackingTarget))
                            {
                                float damageToSummon = (enemy.Damage / (60f / enemy.AttackSpeed)) * effectiveSpeed;
                                enemy.AttackingTarget.TimeLeft -= (int)damageToSummon;
                            }
                            else
                            {
                                enemy.AttackingTarget = null;
                            }
                        }
                    }
                    else
                    {
                        // Charmed enemy behaves like a summon - moves forward and attacks enemies
                        var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog && e != enemy).ToList();
                        
                        if (enemy.CharmTarget == null || !validTargets.Contains(enemy.CharmTarget) || enemy.CharmTarget.Health <= 0)
                        {
                            // Find a new target that's not being attacked by another charmed enemy
                            var availableTarget = validTargets
                                .Where(e => !enemies.Any(ce => ce.IsCharmed && ce.CharmTarget == e && ce != enemy))
                                .OrderBy(e => e.XPos)
                                .FirstOrDefault();
                            
                            enemy.CharmTarget = availableTarget;
                        }
                        
                        if (enemy.CharmTarget != null)
                        {
                            if (Math.Abs(enemy.XPos - enemy.CharmTarget.XPos) > 25)
                            {
                                // Move toward target
                                enemy.XPos += enemy.Speed * effectiveSpeed;
                            }
                            else
                            {
                                // Attack target
                                if (frameCount % Math.Max(1, (int)(60 / (enemy.AttackSpeed * effectiveSpeed))) == 0)
                                {
                                    enemy.CharmTarget.Health -= enemy.Damage;
                                }
                            }
                        }
                        else
                        {
                            // No target, just move forward
                            enemy.XPos += enemy.Speed * effectiveSpeed;
                        }
                    }
                }

                if (enemy.Health <= 0)
                {
                    // Apply Military Band bonus
                    float goldBonus = GetMilitaryBandBonus();
                    float xpBonus = GetMilitaryBandBonus();
                    
                    gold += (int)Math.Round(enemy.GoldGiven * (1f + goldBonus));
                    xp += enemy.XpGiven * (1f + xpBonus);
                    
                    enemies.RemoveAt(i);
                    numOfEnemies--;
                }
            }

            // Summon updates
            UpdateSummons();

            // Archer firing
            UpdateArcherFiring();

            // Unit firing
            UpdateUnitFiring();

            // Unit passive abilities
            UpdateUnitPassives();

            // Enemy spawning
            spawnTimer += effectiveSpeed;
            if (spawnTimer >= 10 && numLeftToSpawn > 0)
            {
                SelectRandomEnemy();
                numLeftToSpawn--;
                numOfEnemies++;
                spawnTimer = 0;
            }

            if (wave % 5 == 0 && numLeftToSpawn == 0 && !spawnedBoss)
            {
                SelectRandomBoss();
                numOfEnemies++;
                spawnedBoss = true;
            }

            // Wave end
            if (numOfEnemies <= 0 && numLeftToSpawn <= 0)
            {
                wave++;
                health = maxHealth;
                mana = maxMana;
                crystals++;
                SetupWave();
                enemies.Clear();
                waving = false;
                archerTargets.Clear();
                
                unitTargets.Clear();
                for (int i = 0; i < activeSlots; i++)
                    unitTargets.Add(new List<Enemy>());
                
                unitAbilityCooldowns = new List<int>(new int[activeSlots]);
                passiveTimers = new List<int>(new int[activeSlots]);

                // Clear ALL summons at wave end (including immortal ones)
                summons.Clear();
                
                // Reset buffs
                foreach (var buff in slotBuffs.Values)
                {
                    buff.AttackSpeedBonus = 0f;
                    buff.DamageBonus = 0f;
                    buff.AttackSpeedBonusTimer = 0;
                }
                archerBuffs = new BuffState();
                globalDefenseReduction = 0f;
                globalDefenseReductionTimer = 0;
                summonDamageBonus = 0f;
                summonDamageTimer = 0;
            }

            // Player defeat
            if (health <= 0)
            {
                health = maxHealth;
                mana = maxMana;
                SetupWave();
                enemies.Clear();
                numOfEnemies = 0;
                waving = false;
                archerTargets.Clear();
                
                unitTargets.Clear();
                for (int i = 0; i < activeSlots; i++)
                    unitTargets.Add(new List<Enemy>());
                
                unitAbilityCooldowns = new List<int>(new int[activeSlots]);
                passiveTimers = new List<int>(new int[activeSlots]);

                summons.Clear();
                
                // Reset buffs
                foreach (var buff in slotBuffs.Values)
                {
                    buff.AttackSpeedBonus = 0f;
                    buff.DamageBonus = 0f;
                    buff.AttackSpeedBonusTimer = 0;
                }
                archerBuffs = new BuffState();
                globalDefenseReduction = 0f;
                globalDefenseReductionTimer = 0;
                summonDamageBonus = 0f;
                summonDamageTimer = 0;
            }
        }

        private float GetMilitaryBandBonus()
        {
            float bonus = 0f;
            for (int i = 0; i < slotAssignments.Count; i++)
            {
                var unitIdx = slotAssignments[i];
                if (unitIdx != null)
                {
                    var unit = unitsList[unitIdx.Value];
                    if (unit.PassiveAbility == "prosperity")
                    {
                        int lvl = Math.Min(unit.Lvl, 51);
                        bonus += 0.02f + (lvl - 1) * 0.02f;
                    }
                }
            }
            return bonus;
        }

        private void UpdateSummons()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            
            for (int i = summons.Count - 1; i >= 0; i--)
            {
                var summon = summons[i];
                
                // Update lifetime
                if (!summon.Immortal)
                {
                    summon.TimeLeft -= (int)effectiveSpeed;
                    if (summon.TimeLeft <= 0)
                    {
                        summons.RemoveAt(i);
                        continue;
                    }
                }
                
                // Find target (one-to-one targeting)
                var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                
                if (summon.Target == null || !validTargets.Contains(summon.Target) || summon.Target.Health <= 0)
                {
                    // Find a new target that's not being attacked by another summon
                    var availableTarget = validTargets
                        .Where(e => !summons.Any(s => s.Target == e && s != summon))
                        .OrderBy(e => e.XPos)
                        .FirstOrDefault();
                    
                    summon.Target = availableTarget;
                }
                
                // Movement and combat
                if (summon.Target != null)
                {
                    if (!summon.IsStationary)
                    {
                        // Determine if this is a ranged summon
                        bool isRanged = summon.Type == "bow skeleton" || summon.Type == "mage skeleton";
                        float attackRange = 0;
                        
                        if (isRanged)
                        {
                            attackRange = summon.Type == "bow skeleton" ? 150 : 100; // Alice has longer range than Dorothy
                        }
                        else
                        {
                            attackRange = 25; // Melee range
                        }
                        
                        float distanceToTarget = Math.Abs(summon.XPos - summon.Target.XPos);
                        
                        if (distanceToTarget > attackRange)
                        {
                            summon.XPos += summon.Speed * effectiveSpeed;
                        }
                        else
                        {
                            // Attack
                            if (frameCount % Math.Max(1, (int)(60 / (summon.AttackSpeed * effectiveSpeed))) == 0)
                            {
                                float damage = summon.Damage * (1f + summonDamageBonus);
                                damage *= (1f + globalDefenseReduction);
                                summon.Target.Health -= damage;
                                
                                // Create projectile for ranged summons
                                if (isRanged)
                                {
                                    projectiles.Add(new Projectile
                                    {
                                        FromX = summon.XPos,
                                        FromY = summon.YPos,
                                        X = summon.XPos,
                                        Y = summon.YPos,
                                        GoingToX = summon.Target.XPos,
                                        GoingToY = summon.Target.YPos,
                                        Tower = new Unit { Col = summon.Col },
                                        Enemy = summon.Target,
                                        SlotIndex = -1
                                    });
                                }
                                else
                                {
                                    effects.Add(new Effect
                                    {
                                        X = summon.Target.XPos,
                                        Y = summon.Target.YPos - 15,
                                        Timer = 15,
                                        Col = summon.Col
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        // Stationary summon attack (slinger)
                        if (frameCount % Math.Max(1, (int)(60 / (summon.AttackSpeed * effectiveSpeed))) == 0)
                        {
                            float damage = summon.Damage * (1f + summonDamageBonus);
                            damage *= (1f + globalDefenseReduction);
                            summon.Target.Health -= damage;
                            
                            // Create projectile visual
                            projectiles.Add(new Projectile
                            {
                                FromX = summon.XPos,
                                FromY = summon.YPos,
                                X = summon.XPos,
                                Y = summon.YPos,
                                GoingToX = summon.Target.XPos,
                                GoingToY = summon.Target.YPos,
                                Tower = new Unit { Col = summon.Col },
                                Enemy = summon.Target,
                                SlotIndex = -1
                            });
                        }
                    }
                }
            }
        }

        private void UpdateArcherFiring()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            
            if (enemies.Count > 0 && frameCount % (60 / (int)effectiveSpeed) == 0)
            {
                int numArchers = Math.Min(archerLevel, 20);
                while (archerTargets.Count < numArchers)
                    archerTargets.Add(null);
                while (archerTargets.Count > numArchers)
                    archerTargets.RemoveAt(archerTargets.Count - 1);

                float archerAttackSpeedBonus = archerBuffs.AttackSpeedBonus;
                int effectiveArchers = (int)(numArchers * (1f + archerAttackSpeedBonus));
                effectiveArchers = Math.Min(effectiveArchers, 20);

                for (int i = 0; i < effectiveArchers; i++)
                {
                    var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                    if (validTargets.Count == 0) break;
                    
                    var target = archerTargets[i % numArchers];
                    if (target == null || !validTargets.Contains(target) || target.Health <= 0)
                    {
                        target = validTargets[random.Next(validTargets.Count)];
                        archerTargets[i % numArchers] = target;
                    }

                    arrows.Add(new Arrow
                    {
                        GoingToX = target.XPos - target.Speed * 20,
                        GoingToY = target.YPos,
                        X = 5 + (i % 5) * 24,
                        Y = 260 + (i / 5) * 35 + 14,
                        FromX = 5 + (i % 5) * 24,
                        FromY = 260 + (i / 5) * 35,
                        Enemy = target
                    });
                }
            }
        }

        private void UpdateUnitFiring()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            
            if (enemies.Count > 0)
            {
                for (int j = 0; j < slotAssignments.Count; j++)
                {
                    var unitIdx = slotAssignments[j];
                    if (unitIdx == null) continue;
                    
                    var unit = unitsList[unitIdx.Value];
                    
                    // Skip non-attacking units
                    if (unit.AttackSpeed == 0) continue;
                    
                    float attacksPerSecond = GetUnitStat(unit, "attackSpeed", j) * effectiveSpeed;
                    int framesBetweenAttacks = Math.Max(1, (int)Math.Round(60 / attacksPerSecond));
                    bool attackReady = frameCount % framesBetweenAttacks == 0;

                    unitTargets[j] = CleanAndRefillTargets(unitTargets[j], unit.Targets);

                    if (unit.Proj)
                    {
                        if (attackReady)
                        {
                            var slot = slotRects[j];
                            float towerX = slot.X + slot.Width / 2f;
                            float towerY = slot.Y + slot.Height / 2f;

                            foreach (var t in unitTargets[j])
                            {
                                projectiles.Add(new Projectile
                                {
                                    FromX = towerX,
                                    FromY = towerY,
                                    X = towerX,
                                    Y = towerY,
                                    GoingToX = t.XPos - t.Speed * 20,
                                    GoingToY = t.YPos,
                                    Tower = unit,
                                    Enemy = t,
                                    SlotIndex = j
                                });
                            }
                        }
                    }
                    else // Instant attack
                    {
                        if (attackReady)
                        {
                            foreach (var t in unitTargets[j])
                            {
                                float damage = GetUnitStat(unit, "damage", j);
                                damage *= (1f + globalDefenseReduction);
                                
                                // Check for crit
                                bool isCrit = random.NextDouble() < GetUnitCritChance(unit, j);
                                if (isCrit)
                                {
                                    damage *= GetUnitCritDamage(unit, j);
                                }
                                
                                // Apply boss slayer
                                if (unit.PassiveAbility == "boss slayer" && t.IsBoss)
                                {
                                    damage *= GetAbilityScaling(unit, "damageMultiplier");
                                }
                                
                                t.Health -= damage;
                                effects.Add(new Effect
                                {
                                    X = t.XPos,
                                    Y = t.YPos - 15,
                                    Timer = 15,
                                    Col = unit.Col
                                });
                            }
                        }
                    }
                }
            }
        }

        private void UpdateUnitPassives()
        {
            float effectiveSpeed = GetEffectiveSpeed();
            
            for (int j = 0; j < slotAssignments.Count; j++)
            {
                var unitIdx = slotAssignments[j];
                if (unitIdx == null) continue;
                
                var unit = unitsList[unitIdx.Value];
                
                // Zeus passive
                if (unit.PassiveAbility == "thunder god")
                {
                    if (passiveTimers[j] <= 0)
                    {
                        passiveTimers[j] = 600; // 10 seconds
                        
                        var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                        if (validTargets.Count > 0)
                        {
                            int targetCount = Math.Min(5, validTargets.Count);
                            var targets = validTargets.OrderBy(x => random.Next()).Take(targetCount).ToList();
                            
                            float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                            float damage = GetUnitStat(unit, "damage", j) * damageMultiplier;
                            damage *= (1f + globalDefenseReduction);
                            
                            foreach (var t in targets)
                            {
                                t.Health -= damage;
                                effects.Add(new Effect
                                {
                                    X = t.XPos,
                                    Y = t.YPos - 15,
                                    Timer = 15,
                                    Col = Color.Gold
                                });
                            }
                        }
                    }
                }
                
                // Succubus charm passive
                if (unit.PassiveAbility == "charm")
                {
                    if (passiveTimers[j] <= 0)
                    {
                        passiveTimers[j] = 600; // 10 seconds
                        
                        var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                        if (validTargets.Count > 0)
                        {
                            int targetCount = Math.Min(4, validTargets.Count);
                            var targets = validTargets.OrderBy(x => random.Next()).Take(targetCount).ToList();
                            
                            int duration = (int)(180 * GetAbilityScaling(unit, "duration")); // Base 3 seconds
                            
                            foreach (var t in targets)
                            {
                                t.IsCharmed = true;
                                t.CharmTimer = duration;
                                t.AttackingTarget = null;
                            }
                        }
                    }
                }
                
                // Summon passives (Lisa, Alice, Dorothy)
                if (unit.PassiveAbility == "summon melee" || unit.PassiveAbility == "summon bow" || unit.PassiveAbility == "summon mage")
                {
                    if (passiveTimers[j] <= 0)
                    {
                        passiveTimers[j] = 600; // 10 seconds
                        
                        string summonType = unit.PassiveAbility == "summon melee" ? "melee skeleton" :
                                          unit.PassiveAbility == "summon bow" ? "bow skeleton" : "mage skeleton";
                        
                        float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                        
                        for (int k = 0; k < 2; k++)
                        {
                            summons.Add(new Summon
                            {
                                Type = summonType,
                                XPos = 250,
                                YPos = 280 + k * 20,
                                Speed = 1.5f,
                                Damage = GetUnitStat(unit, "damage", j) * damageMultiplier,
                                AttackSpeed = 1f,
                                TimeLeft = 900, // 15 seconds
                                Col = unit.Col,
                                IsStationary = false,
                                Immortal = false,
                                Target = null
                            });
                        }
                    }
                }
            }
        }

        private float GetAbilityScaling(Unit unit, string scalingType)
        {
            int lvl = Math.Min(unit.Lvl, 51);
            float scaling = 1f;
            
            // Base formula: linear from 1.0 at level 1 to 2.0 at level 51
            float levelProgress = (lvl - 1) / 50f; // 0.0 to 1.0
            
            switch (scalingType)
            {
                case "damageMultiplier":
                    // Damage multipliers scale from base to 2x base (e.g., 100% -> 200% or 150% -> 300%)
                    scaling = 1f + levelProgress;
                    break;
                    
                case "duration":
                    // Duration scales from 1x to 2x (e.g., 3 seconds -> 6 seconds)
                    scaling = 1f + levelProgress;
                    break;
                    
                case "attackSpeedMultiplier":
                    // Attack speed bonuses scale from base to 2x base (e.g., 100% -> 200%)
                    scaling = 1f + levelProgress;
                    break;
                    
                case "critChanceBonus":
                    // Crit chance bonuses scale from base to 2x base (e.g., 10% -> 20%)
                    scaling = 1f + levelProgress;
                    break;
                    
                case "critDamageBonus":
                    // Crit damage bonuses scale from base to 2x base (e.g., 150% -> 300%)
                    scaling = 1f + levelProgress;
                    break;
            }
            
            return scaling;
        }

        private void SelectRandomEnemy()
        {
            var enemyTypes = new[]
            {
                new { Name = "slime", Health = 10f, Damage = 5f, AttackSpeed = 1f, Speed = 1.5f, GoldGiven = 10, XpGiven = 5f, IsBoss = false },
                new { Name = "zombie", Health = 20f, Damage = 10f, AttackSpeed = 1f, Speed = 1.15f, GoldGiven = 15, XpGiven = 10f, IsBoss = false },
                new { Name = "brute", Health = 40f, Damage = 20f, AttackSpeed = 0.8f, Speed = 0.85f, GoldGiven = 30, XpGiven = 15f, IsBoss = false }
            };

            var enemyTemplate = enemyTypes[random.Next(enemyTypes.Length)];
            float damageMultiplier = 1 + (wave - 1) * 0.15f;
            
            enemies.Add(new Enemy
            {
                Name = enemyTemplate.Name,
                Health = enemyTemplate.Health * (float)Math.Pow(1.06, wave - 1),
                MaxHealth = enemyTemplate.Health * (float)Math.Pow(1.06, wave - 1),
                Damage = enemyTemplate.Damage * damageMultiplier,
                AttackSpeed = enemyTemplate.AttackSpeed,
                Speed = enemyTemplate.Speed,
                GoldGiven = enemyTemplate.GoldGiven + wave,
                XpGiven = enemyTemplate.XpGiven,
                XPos = 720 + 30,
                YPos = 250 + (float)(random.NextDouble() * 100),
                IsBoss = false,
                FreezeTimer = 0,
                IsFrog = false,
                FrogTimer = 0,
                IsCharmed = false,
                CharmTimer = 0,
                AttackingTarget = null,
                CharmTarget = null
            });
        }

        private void SelectRandomBoss()
        {
            var bossTypes = new[]
            {
                new { Name = "king slime", Health = 100f, Damage = 30f, AttackSpeed = 0.5f, Speed = 0.8f, GoldGiven = 100, XpGiven = 50f },
                new { Name = "giant king", Health = 100f, Damage = 30f, AttackSpeed = 0.5f, Speed = 0.8f, GoldGiven = 100, XpGiven = 50f },
                new { Name = "zombie boss", Health = 100f, Damage = 30f, AttackSpeed = 0.5f, Speed = 0.8f, GoldGiven = 100, XpGiven = 50f }
            };

            var bossTemplate = bossTypes[random.Next(bossTypes.Length)];
            float damageMultiplier = 1 + (wave - 1) * 0.15f;
            
            enemies.Add(new Enemy
            {
                Name = bossTemplate.Name,
                Health = bossTemplate.Health * (float)Math.Pow(1.06, wave - 1),
                MaxHealth = bossTemplate.Health * (float)Math.Pow(1.06, wave - 1),
                Damage = bossTemplate.Damage * damageMultiplier,
                AttackSpeed = bossTemplate.AttackSpeed,
                Speed = bossTemplate.Speed,
                GoldGiven = bossTemplate.GoldGiven + wave * 5,
                XpGiven = bossTemplate.XpGiven,
                XPos = 720 + 30,
                YPos = 250 + (float)(random.NextDouble() * 50),
                IsBoss = true,
                FreezeTimer = 0,
                IsFrog = false,
                FrogTimer = 0,
                IsCharmed = false,
                CharmTimer = 0,
                AttackingTarget = null,
                CharmTarget = null
            });
        }

        private List<Enemy> CleanAndRefillTargets(List<Enemy> arr, int maxTargets)
        {
            arr = arr.Where(e => enemies.Contains(e) && e.Health > 0 && !e.IsCharmed && !e.IsFrog).ToList();
            
            int need = maxTargets - arr.Count;
            if (need <= 0) return arr;

            var pool = enemies.Where(e => !arr.Contains(e) && !e.IsCharmed && !e.IsFrog).OrderBy(x => random.Next()).ToList();
            arr.AddRange(pool.Take(need));
            return arr;
        }

        private float GetUnitStat(Unit unit, string stat, int slotIndex = -1)
        {
            int lvl = Math.Min(unit.Lvl, 51);
            
            if (stat == "damage")
            {
                float baseDamage = (float)Math.Round(unit.Damage * Math.Pow(1.075, lvl - 1));
                
                // Apply slot-specific buffs
                if (slotIndex >= 0 && slotBuffs.ContainsKey(slotIndex))
                {
                    baseDamage *= (1f + slotBuffs[slotIndex].DamageBonus);
                }
                
                return baseDamage;
            }
            
            if (stat == "attackSpeed")
            {
                float baseSpeed;
                if (lvl <= 10)
                    baseSpeed = (float)(unit.AttackSpeed * Math.Pow(1.01, lvl - 1));
                else if (lvl <= 20)
                    baseSpeed = (float)(unit.AttackSpeed * Math.Pow(1.01, 9) * Math.Pow(1.005, lvl - 10));
                else
                    baseSpeed = (float)(unit.AttackSpeed * Math.Pow(1.01, 9) * Math.Pow(1.005, 10) * Math.Pow(1.001, lvl - 20));
                
                // Apply slot-specific buffs
                if (slotIndex >= 0 && slotBuffs.ContainsKey(slotIndex))
                {
                    baseSpeed *= (1f + slotBuffs[slotIndex].AttackSpeedBonus);
                }
                
                return baseSpeed;
            }
            
            if (stat == "abilityMpCost")
                return (float)Math.Round(unit.AbilityMpCost * Math.Pow(1.025, lvl - 1));
            
            if (stat == "abilityCooldown")
                return unit.AbilityCooldown;
            
            return 0;
        }

        private float GetUnitCritChance(Unit unit, int slotIndex)
        {
            float baseCrit = 0.01f; // 1% base
            
            // No scaling based on level
            
            return baseCrit;
        }

        private float GetUnitCritDamage(Unit unit, int slotIndex)
        {
            float baseCritDamage = 1.5f; // 150% base
            
            // Elizabeth passive
            if (unit.PassiveAbility == "deadly precision")
            {
                baseCritDamage += 2f; // +200%
            }
            
            return baseCritDamage;
        }

        private int GetUnitUpgradePrice(Unit unit)
        {
            int lvl = unit.Lvl;
            
            // Check if unit has max level
            if (unit.MaxLevel > 0 && lvl >= unit.MaxLevel)
                return 999999; // Cannot upgrade
            
            if (lvl < 20)
                return 250 * lvl;
            else
                return 10000 + (1250 * (lvl - 20));
        }

        private void HandleMouseClick(int mouseX, int mouseY)
        {
            // Ability activation during battle
            if (waving && !menuOpen)
            {
                for (int i = 0; i < activeSlots; i++)
                {
                    var slot = slotRects[i];
                    if (mouseX >= slot.X && mouseX <= slot.X + slot.Width &&
                        mouseY >= slot.Y && mouseY <= slot.Y + slot.Height)
                    {
                        var unitIdx = slotAssignments[i];
                        if (unitIdx != null)
                        {
                            var unit = unitsList[unitIdx.Value];
                            if (unit.Ability != "none" && unitAbilityCooldowns[i] == 0 &&
                                mana >= GetUnitStat(unit, "abilityMpCost"))
                            {
                                mana -= GetUnitStat(unit, "abilityMpCost");
                                unitAbilityCooldowns[i] = (int)Math.Round((double)(unit.AbilityCooldown * 60));
                                TriggerUnitAbility(unit, i);
                            }
                        }
                        return;
                    }
                }

                // Speed button
                if (mouseX >= 5 && mouseX <= 45 && mouseY >= 430 && mouseY <= 470)
                {
                    speed = speed == 1 ? 2 : 1;
                    return;
                }
            }

            // Menu interactions
            if (menuOpen && selectedUnitIndex != -1)
            {
                // X button
                int panelW = 500, panelH = 360;
                int panelX = 720 / 2 - panelW / 2, panelY = 480 / 2 - panelH / 2;
                
                if (mouseX >= panelX + panelW - 38 && mouseX <= panelX + panelW - 10 &&
                    mouseY >= panelY + 10 && mouseY <= panelY + 38)
                {
                    selectedUnitIndex = -1;
                    selectedTab = 0;
                    return;
                }

                // Tab clicks
                int tabAreaX = panelX + 240;
                int tabAreaY = panelY + 35;
                int tabWidth = 80;
                int tabHeight = 25;
                
                for (int i = 0; i < 3; i++)
                {
                    if (mouseX >= tabAreaX + i * (tabWidth + 5) && 
                        mouseX <= tabAreaX + i * (tabWidth + 5) + tabWidth &&
                        mouseY >= tabAreaY && mouseY <= tabAreaY + tabHeight)
                    {
                        selectedTab = i;
                        return;
                    }
                }

                // Buttons
                int descW = 240, descH = 200;
                int descX = tabAreaX, descY = tabAreaY + tabHeight + 10;
                int btnW = 100, btnH = 34, gap = 12;
                int btnY = descY + descH + 16;
                int rightBtnX = descX + descW - btnW;
                int leftBtnX = rightBtnX - btnW - gap;
                var unit = unitsList[selectedUnitIndex];

                // Equip/Unequip button
                if (mouseX >= leftBtnX && mouseX <= leftBtnX + btnW &&
                    mouseY >= btnY && mouseY <= btnY + btnH && unit.Unlocked)
                {
                    if (equipMode)
                    {
                        for (int s = 0; s < slotAssignments.Count; s++)
                        {
                            if (slotAssignments[s] == selectedUnitIndex)
                                slotAssignments[s] = null;
                        }
                        slotAssignments[selectedSlot] = selectedUnitIndex;
                    }
                    else
                    {
                        slotAssignments[selectedSlot] = null;
                    }
                    menuOpen = false;
                    selectedSlot = -1;
                    selectedUnitIndex = -1;
                    selectedTab = 0;
                    return;
                }

                // Upgrade/Unlock button
                if (mouseX >= rightBtnX && mouseX <= rightBtnX + btnW &&
                    mouseY >= btnY && mouseY <= btnY + btnH)
                {
                    if (unit.Unlocked)
                    {
                        int upgradePrice = GetUnitUpgradePrice(unit);
                        if (gold >= upgradePrice && upgradePrice < 999999)
                        {
                            gold -= upgradePrice;
                            unit.Lvl++;
                        }
                    }
                    else
                    {
                        if (gold >= unit.UnlockCost)
                        {
                            gold -= unit.UnlockCost;
                            unit.Unlocked = true;
                        }
                    }
                    return;
                }
                return;
            }

            // Unit list menu
            if (menuOpen && selectedUnitIndex == -1)
            {
                int menuX = 720 - 240;
                
                for (int i = 0; i < unitsList.Count; i++)
                {
                    int itemY = 30 + i * MENU_ITEM_HEIGHT - menuScrollOffset;
                    int itemHeight = 56;
                    int itemX = menuX + 12;
                    int itemWidth = 216;
                    
                    // Check if item is visible and clicked
                    if (itemY + itemHeight >= 30 && itemY <= 480 &&
                        mouseX >= itemX && mouseX <= itemX + itemWidth &&
                        mouseY >= itemY && mouseY <= itemY + itemHeight)
                    {
                        selectedUnitIndex = i;
                        equipMode = slotAssignments[selectedSlot] != i;
                        selectedTab = 0; // Reset to General tab
                        return;
                    }
                }

                if (mouseX <= 480)
                {
                    menuOpen = false;
                    selectedSlot = -1;
                    selectedUnitIndex = -1;
                    menuScrollOffset = 0;
                }
                return;
            }

            // Slot selection
            if (!waving && !menuOpen)
            {
                for (int i = 0; i < activeSlots; i++)
                {
                    var slot = slotRects[i];
                    if (mouseX >= slot.X && mouseX <= slot.X + slot.Width &&
                        mouseY >= slot.Y && mouseY <= slot.Y + slot.Height)
                    {
                        selectedSlot = i;
                        menuOpen = true;
                        menuScrollOffset = 0;
                        selectedTab = 0;
                        if (slotAssignments[i] != null)
                        {
                            selectedUnitIndex = slotAssignments[i].Value;
                            equipMode = false;
                        }
                        else
                        {
                            selectedUnitIndex = -1;
                            equipMode = true;
                        }
                        return;
                    }
                }

                // Battle button
                if (mouseX >= 635 && mouseX <= 710 && mouseY >= 415 && mouseY <= 465)
                {
                    waving = true;
                    archerTargets = new List<Enemy>(new Enemy[Math.Min(archerLevel, 20)]);
                    
                    unitTargets.Clear();
                    for (int i = 0; i < activeSlots; i++)
                        unitTargets.Add(new List<Enemy>());
                    
                    unitAbilityCooldowns = new List<int>(new int[activeSlots]);
                    passiveTimers = new List<int>(new int[activeSlots]);
                    
                    // Spawn immortal summons AT THE START OF WAVE
                    for (int i = 0; i < slotAssignments.Count; i++)
                    {
                        var unitIdx = slotAssignments[i];
                        if (unitIdx != null)
                        {
                            var unit = unitsList[unitIdx.Value];
                            
                            float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                            
                            // Druid ent
                            if (unit.PassiveAbility == "summon ent")
                            {
                                summons.Add(new Summon
                                {
                                    Type = "ent",
                                    XPos = 250,
                                    YPos = 300,
                                    Speed = 1f,
                                    Damage = GetUnitStat(unit, "damage", i) * (2f * damageMultiplier), // 200% base damage, scaled
                                    AttackSpeed = 0.8f,
                                    TimeLeft = 999999,
                                    Col = Color.Brown,
                                    IsStationary = false,
                                    Immortal = true,
                                    Target = null
                                });
                            }
                            
                            // Golem Master golem
                            if (unit.PassiveAbility == "summon golem")
                            {
                                summons.Add(new Summon
                                {
                                    Type = "golem",
                                    XPos = 250,
                                    YPos = 280,
                                    Speed = 0.8f,
                                    Damage = GetUnitStat(unit, "damage", i) * (4f * damageMultiplier), // 400% base damage, scaled
                                    AttackSpeed = 0.6f,
                                    TimeLeft = 999999,
                                    Col = Color.Gray,
                                    IsStationary = false,
                                    Immortal = true,
                                    Target = null
                                });
                            }
                        }
                    }
                    
                    return;
                }

                // Upgrade buttons
                if (mouseX >= 395 && mouseX <= 695)
                {
                    if (mouseY >= 55 && mouseY <= 155 && gold >= castlePrice)
                    {
                        gold -= castlePrice;
                        castleLevel++;
                        MaybeUnlockSlotOnCastleMilestone();
                    }
                    else if (mouseY >= 165 && mouseY <= 265 && gold >= archerPrice)
                    {
                        gold -= archerPrice;
                        archerLevel++;
                    }
                }
            }
        }

        private void TriggerUnitAbility(Unit unit, int slotIndex)
        {
            float damage = GetUnitStat(unit, "damage", slotIndex);
            
            // Rapid Fire (Archer)
            if (unit.Ability == "rapid fire")
            {
                float attackSpeedMultiplier = GetAbilityScaling(unit, "attackSpeedMultiplier");
                int duration = (int)(300 * GetAbilityScaling(unit, "duration")); // Base 5 seconds
                
                slotBuffs[slotIndex].AttackSpeedBonus = attackSpeedMultiplier;
                slotBuffs[slotIndex].DamageBonus = 0f;
                slotBuffs[slotIndex].AttackSpeedBonusTimer = duration;
            }
            // Rally Archers (Hunter)
            else if (unit.Ability == "rally archers")
            {
                float attackSpeedMultiplier = GetAbilityScaling(unit, "attackSpeedMultiplier");
                int duration = (int)(180 * GetAbilityScaling(unit, "duration")); // Base 3 seconds
                
                archerBuffs.AttackSpeedBonus = attackSpeedMultiplier;
                archerBuffs.AttackSpeedBonusTimer = duration;
            }
            // Death Mark (Dark Skeleton)
            else if (unit.Ability == "death mark")
            {
                float critChanceBonus = 0.1f * GetAbilityScaling(unit, "critChanceBonus"); // Base 10%
                int duration = (int)(180 * GetAbilityScaling(unit, "duration")); // Base 3 seconds
                
                archerBuffs.CritChanceBonus = critChanceBonus;
                archerBuffs.CritChanceBonusTimer = duration;
                archerCritChance = 0.01f + archerBuffs.CritChanceBonus;
            }
            // Deep Freeze (Ice Mage)
            else if (unit.Ability == "deep freeze")
            {
                float damageMultiplier = 0.5f * GetAbilityScaling(unit, "damageMultiplier"); // Base 50%
                int duration = (int)(240 * GetAbilityScaling(unit, "duration")); // Base 4 seconds
                
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsCharmed && !enemy.IsFrog)
                    {
                        enemy.FreezeTimer = duration;
                        enemy.Health -= damage * damageMultiplier;
                        effects.Add(new Effect
                        {
                            X = enemy.XPos,
                            Y = enemy.YPos - 20,
                            Timer = 18,
                            Col = Color.LightBlue
                        });
                    }
                }
            }
            // Thunderstorm (Lightning Mage)
            else if (unit.Ability == "thunderstorm")
            {
                float damageMultiplier = 2f * GetAbilityScaling(unit, "damageMultiplier"); // Base 200%
                
                var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                int targetCount = Math.Min(8, validTargets.Count);
                var targets = validTargets.OrderBy(x => random.Next()).Take(targetCount).ToList();
                
                foreach (var t in targets)
                {
                    t.Health -= damage * damageMultiplier;
                    effects.Add(new Effect
                    {
                        X = t.XPos,
                        Y = t.YPos - 20,
                        Timer = 18,
                        Col = Color.Yellow
                    });
                }
            }
            // Meteor (Fire Mage)
            else if (unit.Ability == "meteor")
            {
                float damageMultiplier = 5f * GetAbilityScaling(unit, "damageMultiplier"); // Base 500%
                
                var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                int targetCount = Math.Min(5, validTargets.Count);
                var targets = validTargets.OrderBy(x => random.Next()).Take(targetCount).ToList();
                
                foreach (var t in targets)
                {
                    t.Health -= damage * damageMultiplier;
                    effects.Add(new Effect
                    {
                        X = t.XPos,
                        Y = t.YPos - 20,
                        Timer = 18,
                        Col = Color.OrangeRed
                    });
                }
            }
            // Cooldown Reset (White Mage)
            else if (unit.Ability == "cooldown reset")
            {
                for (int i = 0; i < unitAbilityCooldowns.Count; i++)
                {
                    unitAbilityCooldowns[i] = Math.Max(0, unitAbilityCooldowns[i] - 180); // -3 seconds
                }
            }
            // Shockwave (Ogre)
            else if (unit.Ability == "shockwave")
            {
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsCharmed && !enemy.IsFrog)
                    {
                        enemy.XPos += 50; // Knockback
                    }
                }
            }
            // Curse (Necromancer)
            else if (unit.Ability == "curse")
            {
                float defenseReduction = 0.5f * GetAbilityScaling(unit, "damageMultiplier"); // Base 50%
                int duration = (int)(180 * GetAbilityScaling(unit, "duration")); // Base 3 seconds
                
                globalDefenseReduction = defenseReduction;
                globalDefenseReductionTimer = duration;
            }
            // Bless Summons (Priest)
            else if (unit.Ability == "bless summons")
            {
                float damageBonus = 0.3f * GetAbilityScaling(unit, "damageMultiplier"); // Base 30%
                int duration = (int)(180 * GetAbilityScaling(unit, "duration")); // Base 3 seconds
                
                summonDamageBonus = damageBonus;
                summonDamageTimer = duration;
            }
            // Summon Giant (Tiny Giant)
            else if (unit.Ability == "summon giant")
            {
                float damageMultiplier = 4f * GetAbilityScaling(unit, "damageMultiplier"); // Base 400%
                
                summons.Add(new Summon
                {
                    Type = "giant",
                    XPos = 250,
                    YPos = 280,
                    Speed = 1.2f,
                    Damage = damage * damageMultiplier,
                    AttackSpeed = 0.7f,
                    TimeLeft = 900, // 15 seconds
                    Col = Color.SaddleBrown,
                    IsStationary = false,
                    Immortal = false,
                    Target = null
                });
            }
            // Summon Slingers (Slinger)
            else if (unit.Ability == "summon slingers")
            {
                float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                
                for (int i = 0; i < 5; i++)
                {
                    summons.Add(new Summon
                    {
                        Type = "slinger",
                        XPos = 220,
                        YPos = 260 + i * 20, // Spawn vertically aligned
                        Speed = 0f,
                        Damage = damage * damageMultiplier,
                        AttackSpeed = 1f,
                        TimeLeft = 900, // 15 seconds
                        Col = Color.Peru,
                        IsStationary = true,
                        Immortal = false,
                        Target = null
                    });
                }
            }
            // Repair (Smith)
            else if (unit.Ability == "repair")
            {
                health += maxHealth * 0.2f;
                if (health > maxHealth) health = maxHealth;
            }
            // Poison Cloud (Voodoo)
            else if (unit.Ability == "poison cloud")
            {
                float damageMultiplier = 2f * GetAbilityScaling(unit, "damageMultiplier"); // Base 200%
                
                // Deal damage over 3 seconds (180 frames)
                int damagePerFrame = 180 / (int)GetEffectiveSpeed();
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsCharmed && !enemy.IsFrog)
                    {
                        enemy.Health -= (damage * damageMultiplier) / damagePerFrame;
                        effects.Add(new Effect
                        {
                            X = enemy.XPos,
                            Y = enemy.YPos - 20,
                            Timer = 18,
                            Col = Color.Purple
                        });
                    }
                }
            }
            // Summon Knights (Knight)
            else if (unit.Ability == "summon knights")
            {
                float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                
                for (int i = 0; i < 5; i++)
                {
                    summons.Add(new Summon
                    {
                        Type = "knight",
                        XPos = 250,
                        YPos = 270 + i * 20,
                        Speed = 1.5f,
                        Damage = damage * damageMultiplier,
                        AttackSpeed = 1f,
                        TimeLeft = 900, // 15 seconds
                        Col = Color.Silver,
                        IsStationary = false,
                        Immortal = false,
                        Target = null
                    });
                }
            }
            // Shadow Strike (Assassin)
            else if (unit.Ability == "shadow strike")
            {
                float damageMultiplier = 0.8f * GetAbilityScaling(unit, "damageMultiplier"); // Base 80%
                
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsCharmed && !enemy.IsFrog)
                    {
                        enemy.Health -= damage * damageMultiplier;
                        effects.Add(new Effect
                        {
                            X = enemy.XPos,
                            Y = enemy.YPos - 20,
                            Timer = 18,
                            Col = Color.DarkSlateGray
                        });
                    }
                }
            }
            // Tornado (Windy)
            else if (unit.Ability == "tornado")
            {
                float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                
                // This would need special visual handling, simplified here
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsCharmed && !enemy.IsFrog)
                    {
                        enemy.Health -= damage * damageMultiplier;
                        effects.Add(new Effect
                        {
                            X = enemy.XPos,
                            Y = enemy.YPos - 20,
                            Timer = 18,
                            Col = Color.LightCyan
                        });
                    }
                }
            }
            // Summon Angels (Angel)
            else if (unit.Ability == "summon angels")
            {
                float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                
                for (int i = 0; i < 5; i++)
                {
                    summons.Add(new Summon
                    {
                        Type = "angel",
                        XPos = 250,
                        YPos = 260 + i * 20,
                        Speed = 2f,
                        Damage = damage * damageMultiplier,
                        AttackSpeed = 1.2f,
                        TimeLeft = 900, // 15 seconds
                        Col = Color.White,
                        IsStationary = false,
                        Immortal = false,
                        Target = null
                    });
                }
            }
            // Demonic Speed (Succubus)
            else if (unit.Ability == "demonic speed")
            {
                float attackSpeedMultiplier = 2f * GetAbilityScaling(unit, "attackSpeedMultiplier"); // Base 200%
                int duration = (int)(300 * GetAbilityScaling(unit, "duration")); // Base 5 seconds
                
                slotBuffs[slotIndex].AttackSpeedBonus = attackSpeedMultiplier;
                slotBuffs[slotIndex].DamageBonus = 0f;
                slotBuffs[slotIndex].AttackSpeedBonusTimer = duration;
            }
            // Polymorph (Alchemist)
            else if (unit.Ability == "polymorph")
            {
                var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                int targetCount = Math.Min(2, validTargets.Count);
                var targets = validTargets.OrderBy(x => random.Next()).Take(targetCount).ToList();
                
                foreach (var t in targets)
                {
                    t.IsFrog = true;
                    t.FrogTimer = 900; // 15 seconds
                    t.Speed = 0;
                }
            }
            // Shadow Clone (Rogue)
            else if (unit.Ability == "shadow clone")
            {
                float damageMultiplier = 2f * GetAbilityScaling(unit, "damageMultiplier"); // Base 200%
                
                var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                int targetCount = Math.Min(4, validTargets.Count);
                var targets = validTargets.OrderBy(x => random.Next()).Take(targetCount).ToList();
                
                foreach (var t in targets)
                {
                    t.Health -= damage * damageMultiplier;
                    effects.Add(new Effect
                    {
                        X = t.XPos,
                        Y = t.YPos - 20,
                        Timer = 18,
                        Col = Color.Indigo
                    });
                }
            }
            // Blizzard (Ice Sorceress)
            else if (unit.Ability == "blizzard")
            {
                float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
                
                var validTargets = enemies.Where(e => !e.IsCharmed && !e.IsFrog).ToList();
                int targetCount = Math.Min(8, validTargets.Count);
                var targets = validTargets.OrderBy(x => random.Next()).Take(targetCount).ToList();
                
                foreach (var t in targets)
                {
                    t.Health -= damage * damageMultiplier;
                    effects.Add(new Effect
                    {
                        X = t.XPos,
                        Y = t.YPos - 20,
                        Timer = 18,
                        Col = Color.LightBlue
                    });
                }
            }
        }

        private float GetParabolaY(float x, float x0, float y0, float x1, float y1, float a)
        {
            float b = ((y1 - y0) - a * (x1 * x1 - x0 * x0)) / (x1 - x0);
            float c = y0 - a * x0 * x0 - b * x0;
            return a * x * x + b * x + c;
        }

        private float GetLineY(float xCurr, float x1, float y1, float x2, float y2)
        {
            if (Math.Abs(x2 - x1) < 0.01f)
                return y1;
            
            float slope = (y2 - y1) / (x2 - x1);
            float yInt = y1 - slope * x1;
            return slope * xCurr + yInt;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(16, 172, 16));

            _spriteBatch.Begin();

            DrawMap();
            DrawUI();
            DrawSlots();
            DrawAbilityBars();
            DrawPassiveBars();
            DrawArchers();
            DrawEnemies();
            DrawSummons();
            DrawProjectiles();
            DrawEffects();
            
            if (menuOpen)
            {
                DrawMenuOverlay();
                if (selectedUnitIndex != -1)
                    DrawUnitInfoMenu();
                else
                    DrawUnitsListMenu();
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawMap()
        {
            // Path
            DrawRect(150, 250, 720, 150, new Color(255, 255, 175));
            DrawRect(0, 200, 150, 250, new Color(255, 255, 175));

            // Sky
            DrawRect(0, 0, 720, 160, new Color(165, 255, 255));

            // Castle
            int rowsUsed = (int)Math.Ceiling(activeSlots / 3.0);
            int castleTopBase = 225;
            int castleHeightBase = 150;
            int castleY = castleTopBase - (rowsUsed - 1) * SLOT_DY;
            int castleH = castleHeightBase + (rowsUsed - 1) * SLOT_DY;
            DrawRect(125, castleY, 150, castleH, new Color(204, 201, 201));
        }

        private void DrawUI()
        {
            // Bars
            DrawRect(500, 10, 200, 25, Color.Black);
            DrawRect(295, 10, 200, 25, Color.Black);
            DrawRect(5, 5, 100, 50, Color.Black);
            DrawRect(110, 5, 100, 50, Color.Black);
            DrawRect(215, 10, 75, 25, Color.Black);

            if (waving)
            {
                DrawRect(295, 40, 405, 25, Color.Black);
                DrawCircle(25, 450, 20, Color.Black);
                if (_font != null)
                    DrawText(speed + "x", 14, 450, Color.White, FONT_SIZE_SPEED_BUTTON);
            }
            else
            {
                DrawRect(395, 55, 300, 100, Color.Black);
                DrawRect(395, 165, 300, 100, Color.Black);
                DrawRect(410, 65, 70, 80, new Color(204, 201, 201));
                DrawRect(430, 175, 30, 60, Color.White, new Color(128, 128, 128), 2);
            }

            // Mana Bar
            DrawRect(505, 15, (int)(190 * (mana / maxMana)), 15, new Color(74, 74, 255));

            // Health Bar
            DrawRect(300, 15, (int)(190 * (health / maxHealth)), 15, Color.Red);

            // XP Bar
            DrawRect(220, 15, (int)(65 * (xp / xpRequired)), 15, Color.Lime);

            // Wave Bar
            if (waving)
                DrawRect(300, 45, (int)(395 * (numLeftToSpawn / (float)numOfEnemiesInWave)), 15, Color.White);

            // Text
            if (_font != null)
            {
                DrawText("HP", 300, 18, Color.White, FONT_SIZE_HP_MP_LABELS);
                DrawText(((int)health).ToString(), 455, 18, Color.White, FONT_SIZE_HP_MP_VALUES);
                DrawText("MP", 505, 18, Color.White, FONT_SIZE_HP_MP_LABELS);
                DrawText(((int)mana).ToString(), 670, 18, Color.White, FONT_SIZE_HP_MP_VALUES);
                DrawText(level.ToString(), 220, 18, Color.White, FONT_SIZE_LEVEL);
                DrawText(gold.ToString(), 8, 25, Color.Yellow, FONT_SIZE_GOLD_CRYSTALS);
                DrawText(crystals.ToString(), 113, 25, Color.Cyan, FONT_SIZE_GOLD_CRYSTALS);

                if (waving)
                {
                    DrawText("WAVE", 300, 48, Color.White, FONT_SIZE_WAVE_LABEL);
                    DrawText(wave.ToString(), 670, 48, Color.White, FONT_SIZE_WAVE_NUMBER);
                }

                if (!waving)
                {
                    DrawRect(635, 415, 75, 50, new Color(196, 196, 196));
                    DrawText("BATTLE", 635, 463, Color.White, FONT_SIZE_BATTLE_BUTTON);
                    DrawText("Upgrade Castle", 500, 73, Color.White, FONT_SIZE_UPGRADE_LABELS);
                    DrawText(castlePrice.ToString(), 500, 113, Color.White, FONT_SIZE_UPGRADE_COSTS);
                    DrawText("Upgrade Town Archers", 488, 183, Color.White, FONT_SIZE_UPGRADE_LABELS);
                    DrawText(archerPrice.ToString(), 500, 223, Color.White, FONT_SIZE_UPGRADE_COSTS);
                    DrawText(archerDamage.ToString("F1"), 410, 243, Color.Lime, FONT_SIZE_ARCHER_DAMAGE);
                }
            }
        }

        private void DrawSlots()
        {
            for (int i = 0; i < activeSlots; i++)
            {
                var slot = slotRects[i];
                DrawRect(slot.X, slot.Y, slot.Width, slot.Height, new Color(200, 200, 200, 100), new Color(180, 180, 180), 2);

                if (slotAssignments[i] != null)
                {
                    var unit = unitsList[slotAssignments[i].Value];
                    Color unitColor = unit.Col;
                    
                    // Visual indicator for buffs
                    if (slotBuffs.ContainsKey(i) && slotBuffs[i].AttackSpeedBonusTimer > 0)
                    {
                        unitColor = Color.Lerp(unit.Col, Color.White, 0.3f);
                    }
                    
                    DrawRect(slot.X + 9, slot.Y + 8, slot.Width - 18, slot.Height - 16, unitColor, new Color(80, 80, 80), 1);
                }

                if (selectedSlot == i && menuOpen)
                {
                    DrawRect(slot.X, slot.Y, slot.Width, slot.Height, Color.Transparent, new Color(80, 160, 255), 4);
                }
            }
        }

        private void DrawAbilityBars()
        {
            for (int i = 0; i < activeSlots; i++)
            {
                var unitIdx = slotAssignments[i];
                if (unitIdx == null) continue;

                var unit = unitsList[unitIdx.Value];
                if (unit.Ability == "none") continue;

                int cdFrames = unit.AbilityCooldown * 60;
                float percentReady = cdFrames > 0 ? 1 - (unitAbilityCooldowns[i] / (float)cdFrames) : 1;
                percentReady = Math.Max(0, Math.Min(1, percentReady));

                var slot = slotRects[i];
                int barX = slot.X + slot.Width / 2 - ABILITY_BAR_WIDTH / 2;
                
                // Position based on whether there's also a passive bar
                int barY = unit.PassiveAbility != "none" && HasPassiveTimer(unit) ? slot.Y - 18 : slot.Y - 10;

                DrawRect(barX, barY, ABILITY_BAR_WIDTH, ABILITY_BAR_HEIGHT, Color.Black);
                DrawRect(barX, barY, (int)(ABILITY_BAR_WIDTH * percentReady), ABILITY_BAR_HEIGHT, new Color(50, 140, 255));
            }
        }

        private void DrawPassiveBars()
        {
            for (int i = 0; i < activeSlots; i++)
            {
                var unitIdx = slotAssignments[i];
                if (unitIdx == null) continue;

                var unit = unitsList[unitIdx.Value];
                if (!HasPassiveTimer(unit)) continue;

                // 10 seconds = 600 frames
                int totalFrames = 600;
                float percentReady = passiveTimers[i] > 0 ? 1 - (passiveTimers[i] / (float)totalFrames) : 1;
                percentReady = Math.Max(0, Math.Min(1, percentReady));

                var slot = slotRects[i];
                int barX = slot.X + slot.Width / 2 - ABILITY_BAR_WIDTH / 2;
                int barY = slot.Y - 10;

                DrawRect(barX, barY, ABILITY_BAR_WIDTH, ABILITY_BAR_HEIGHT, Color.Black);
                DrawRect(barX, barY, (int)(ABILITY_BAR_WIDTH * percentReady), ABILITY_BAR_HEIGHT, new Color(138, 43, 226)); // Purple
            }
        }

        private bool HasPassiveTimer(Unit unit)
        {
            return unit.PassiveAbility == "thunder god" || 
                   unit.PassiveAbility == "charm" ||
                   unit.PassiveAbility == "summon melee" ||
                   unit.PassiveAbility == "summon bow" ||
                   unit.PassiveAbility == "summon mage";
        }

        private void DrawArchers()
        {
            for (int i = 0; i < Math.Min(archerLevel, 20); i++)
            {
                int x = 5 + (i % 5) * 24;
                int y = 260 + (i / 5) * 35;
                DrawRect(x, y, 19, 30, Color.White, Color.Black, 1);
            }
        }

        private void DrawEnemies()
        {
            foreach (var enemy in enemies)
            {
                Color enemyColor = Color.White;
                int width = 20, height = 30;
                
                if (enemy.IsFrog)
                {
                    enemyColor = Color.LimeGreen;
                    width = 15;
                    height = 10;
                }
                else if (enemy.IsCharmed)
                {
                    enemyColor = Color.HotPink;
                }
                else if (enemy.Name == "zombie")
                {
                    enemyColor = Color.DarkGreen;
                }
                else if (enemy.Name == "slime")
                {
                    enemyColor = Color.Cyan;
                    height = 20;
                }
                else if (enemy.Name == "brute")
                {
                    enemyColor = Color.Pink;
                }
                else if (enemy.Name == "king slime")
                {
                    enemyColor = Color.Cyan;
                    width = 50;
                    height = 50;
                }
                else if (enemy.Name == "giant king")
                {
                    enemyColor = Color.Pink;
                    width = 50;
                    height = 80;
                }
                else if (enemy.Name == "zombie boss")
                {
                    enemyColor = Color.DarkGreen;
                    width = 50;
                    height = 80;
                }
                
                DrawRect((int)enemy.XPos, (int)enemy.YPos, width, height, enemyColor);
                
                // Health bar
                int barWidth = width + 10;
                int barHeight = 5;
                if (enemy.IsBoss)
                {
                    barWidth = width + 10;
                    barHeight = 10;
                }
                
                DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, barWidth, barHeight, Color.Black);
                DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, (int)(barWidth * (enemy.Health / enemy.MaxHealth)), barHeight, Color.Red);
                
                // Freeze indicator
                if (enemy.FreezeTimer > 0)
                {
                    DrawRect((int)enemy.XPos, (int)enemy.YPos - 20, width, 3, Color.LightBlue);
                }
            }
        }

        private void DrawSummons()
        {
            foreach (var summon in summons)
            {
                int size = 15;
                if (summon.Type == "giant" || summon.Type == "golem" || summon.Type == "ent")
                    size = 30;
                
                DrawRect((int)summon.XPos, (int)summon.YPos, size, size, summon.Col, Color.Black, 1);
            }
        }

        private void DrawProjectiles()
        {
            foreach (var arrow in arrows)
            {
                if (arrow.X <= arrow.GoingToX)
                    DrawRect((int)arrow.X, (int)arrow.Y, 10, 10, Color.White, Color.Black, 1);
            }

            foreach (var proj in projectiles)
            {
                if (proj.X <= proj.GoingToX)
                    DrawRect((int)proj.X, (int)proj.Y, 10, 10, proj.Tower.Col, Color.Black, 1);
            }
        }

        private void DrawEffects()
        {
            foreach (var eff in effects)
            {
                DrawRect((int)eff.X, (int)eff.Y, 5, 30, eff.Col);
            }
        }

        private void DrawMenuOverlay()
        {
            DrawRect(0, 0, 720, 480, new Color(0, 0, 0, 100));
        }

        private void DrawUnitsListMenu()
        {
            int menuX = 720 - 240;
            DrawRect(menuX, 0, 240, 480, new Color(230, 230, 230, 245), new Color(180, 180, 180), 1);

            if (_font == null) return;

            // Create a clipping region for the menu
            int visibleStartY = 30;
            int visibleEndY = 480;

            for (int i = 0; i < unitsList.Count; i++)
            {
                var unit = unitsList[i];
                int itemY = visibleStartY + i * MENU_ITEM_HEIGHT - menuScrollOffset;
                int itemHeight = 56;
                int itemX = menuX + 12;
                int itemWidth = 216;

                // Only draw if visible
                if (itemY + itemHeight < visibleStartY || itemY > visibleEndY)
                    continue;

                DrawRect(itemX, itemY, itemWidth, itemHeight, new Color(245, 245, 245, 240), new Color(160, 160, 160), 2);
                DrawRect(itemX + 15, itemY + 7, 24, 42, unit.Col, new Color(80, 80, 80), 2);
                DrawText(unit.Name, itemX + 55, itemY + 15, new Color(40, 40, 40), FONT_SIZE_UNIT_LIST_NAME);

                if (!unit.Unlocked)
                    DrawText(unit.UnlockCost.ToString(), itemX + 160, itemY + itemHeight / 2, Color.Gold, FONT_SIZE_UNIT_LIST_COST);
            }
            
            // Draw scroll indicator if needed
            int totalHeight = unitsList.Count * MENU_ITEM_HEIGHT;
            if (totalHeight > 450)
            {
                int scrollBarHeight = (int)((450f / totalHeight) * 450);
                int scrollBarY = (int)((menuScrollOffset / (float)(totalHeight - 450)) * (450 - scrollBarHeight));
                DrawRect(menuX + 235, scrollBarY, 3, scrollBarHeight, new Color(100, 100, 100));
            }
        }

        private void DrawUnitInfoMenu()
        {
            var unit = unitsList[selectedUnitIndex];
            int panelW = 500, panelH = 360;
            int panelX = 720 / 2 - panelW / 2, panelY = 480 / 2 - panelH / 2;

            DrawRect(panelX, panelY, panelW, panelH, new Color(255, 255, 255, 240), new Color(180, 180, 180), 3);

            // X button
            DrawRect(panelX + panelW - 38, panelY + 10, 28, 28, new Color(220, 80, 80));
            if (_font != null)
                DrawText("X", panelX + panelW - 28, panelY + 16, Color.White, FONT_SIZE_CLOSE_BUTTON);

            // Unit sprite
            DrawRect(panelX + 20, panelY + 35, 35, 70, unit.Col, new Color(100, 100, 100), 2);

            if (_font == null) return;

            // Hero name and level
            DrawText(unit.Name, panelX + 65, panelY + 40, new Color(40, 40, 40), 1.2f);
            DrawText($"Level {unit.Lvl}", panelX + 65, panelY + 62, new Color(80, 80, 80), 0.9f);
            DrawText($"Element: {unit.Element}", panelX + 65, panelY + 80, new Color(100, 100, 100), 0.8f);

            // Stats grid (left side)
            int statsX = panelX + 20;
            int statsY = panelY + 115;
            int statRowHeight = 18;
            
            DrawText("=== Current Stats ===", statsX, statsY, new Color(40, 40, 40), 0.85f);
            statsY += 20;
            
            if (unit.AttackSpeed > 0)
            {
                DrawText($"Damage: {GetUnitStat(unit, "damage"):F1}", statsX, statsY, new Color(60, 60, 60), 0.8f);
                statsY += statRowHeight;
                DrawText($"Attack Speed: {GetUnitStat(unit, "attackSpeed"):F2}/s", statsX, statsY, new Color(60, 60, 60), 0.8f);
                statsY += statRowHeight;
                DrawText($"Targets: {unit.Targets}", statsX, statsY, new Color(60, 60, 60), 0.8f);
                statsY += statRowHeight;
                DrawText($"Crit Chance: {(GetUnitCritChance(unit, -1) * 100):F1}%", statsX, statsY, new Color(60, 60, 60), 0.8f);
                statsY += statRowHeight;
                DrawText($"Crit Damage: {(GetUnitCritDamage(unit, -1) * 100):F0}%", statsX, statsY, new Color(60, 60, 60), 0.8f);
                statsY += statRowHeight;
            }
            else
            {
                DrawText("No Attack", statsX, statsY, new Color(150, 150, 150), 0.8f);
                statsY += statRowHeight;
            }
            
            if (unit.Ability != "none")
            {
                statsY += 5;
                DrawText($"Ability Cost: {GetUnitStat(unit, "abilityMpCost"):F0} MP", statsX, statsY, new Color(60, 60, 200), 0.8f);
                statsY += statRowHeight;
                DrawText($"Cooldown: {GetUnitStat(unit, "abilityCooldown"):F0}s", statsX, statsY, new Color(60, 60, 200), 0.8f);
                statsY += statRowHeight;
            }

            // Next level preview (if can upgrade)
            int upgradePrice = GetUnitUpgradePrice(unit);
            bool canUpgrade = unit.Unlocked && gold >= upgradePrice && upgradePrice < 999999;
            bool atMaxLevel = unit.MaxLevel > 0 && unit.Lvl >= unit.MaxLevel;
            
            if (unit.Unlocked && !atMaxLevel)
            {
                statsY += 10;
                DrawText("=== Next Level ===", statsX, statsY, new Color(40, 40, 40), 0.85f);
                statsY += 20;
                
                // Create a temporary unit at next level for preview
                var previewUnit = new Unit
                {
                    Damage = unit.Damage,
                    AttackSpeed = unit.AttackSpeed,
                    Lvl = unit.Lvl + 1,
                    Ability = unit.Ability,
                    AbilityMpCost = unit.AbilityMpCost,
                    AbilityCooldown = unit.AbilityCooldown,
                    PassiveAbility = unit.PassiveAbility
                };
                
                if (unit.AttackSpeed > 0)
                {
                    float dmgChange = GetUnitStat(previewUnit, "damage") - GetUnitStat(unit, "damage");
                    float atkChange = GetUnitStat(previewUnit, "attackSpeed") - GetUnitStat(unit, "attackSpeed");
                    
                    DrawText($"Damage: {GetUnitStat(previewUnit, "damage"):F1} (+{dmgChange:F1})", 
                             statsX, statsY, new Color(0, 150, 0), 0.75f);
                    statsY += statRowHeight;
                    DrawText($"Atk Speed: {GetUnitStat(previewUnit, "attackSpeed"):F2} (+{atkChange:F2})", 
                             statsX, statsY, new Color(0, 150, 0), 0.75f);
                    statsY += statRowHeight;
                }
                
                if (unit.Ability != "none" && unit.Lvl < 51)
                {
                    float mpChange = GetUnitStat(previewUnit, "abilityMpCost") - GetUnitStat(unit, "abilityMpCost");
                    DrawText($"Ability MP: {GetUnitStat(previewUnit, "abilityMpCost"):F0} (+{mpChange:F0})", 
                             statsX, statsY, new Color(0, 100, 200), 0.75f);
                    statsY += statRowHeight;
                }
                else if (unit.Lvl >= 51)
                {
                    DrawText("(Stats capped at 51)", statsX, statsY, new Color(150, 150, 150), 0.7f);
                }
            }

            // Tab system (right side)
            int tabAreaX = panelX + 240;
            int tabAreaY = panelY + 35;
            int tabWidth = 80;
            int tabHeight = 25;
            
            // Draw tabs
            string[] tabNames = { "General", "Ability", "Passive" };
            for (int i = 0; i < 3; i++)
            {
                Color tabColor = selectedTab == i ? new Color(100, 150, 255) : new Color(200, 200, 200);
                Color textColor = selectedTab == i ? Color.White : new Color(80, 80, 80);
                
                DrawRect(tabAreaX + i * (tabWidth + 5), tabAreaY, tabWidth, tabHeight, tabColor, new Color(120, 120, 120), 1);
                DrawText(tabNames[i], tabAreaX + i * (tabWidth + 5) + 10, tabAreaY + 6, textColor, 0.7f);
            }

            // Description area
            int descX = tabAreaX;
            int descY = tabAreaY + tabHeight + 10;
            int descW = panelW - (descX - panelX) - 20;
            int descH = 200;
            
            DrawRect(descX, descY, descW, descH, new Color(245, 245, 245), new Color(190, 190, 190), 1);

            // Draw content based on selected tab
            string content = "";
            if (selectedTab == 0) // General
            {
                content = GetGeneralDescription(unit);
            }
            else if (selectedTab == 1) // Ability
            {
                content = GetAbilityDescription(unit);
            }
            else if (selectedTab == 2) // Passive
            {
                content = GetPassiveDescription(unit);
            }
            
            DrawWrappedText(content, descX + 10, descY + 10, descW - 20, new Color(60, 60, 60), 0.75f);

            // Buttons
            int btnW = 100, btnH = 34, gap = 12;
            int btnY = descY + descH + 16;
            int rightBtnX = descX + descW - btnW;
            int leftBtnX = rightBtnX - btnW - gap;

            if (unit.Unlocked)
            {
                // Equip/Unequip
                DrawRect(leftBtnX, btnY, btnW, btnH, equipMode ? Color.Lime : Color.Red);
                DrawText(equipMode ? "Equip" : "Unequip", leftBtnX + 20, btnY + 10, new Color(25, 25, 25), FONT_SIZE_UNIT_INFO_BUTTONS);

                // Upgrade
                if (atMaxLevel)
                {
                    DrawRect(rightBtnX, btnY, btnW, btnH, new Color(100, 100, 100));
                    DrawText("MAX LVL", rightBtnX + 20, btnY + 10, new Color(200, 200, 200), FONT_SIZE_UNIT_INFO_BUTTONS);
                }
                else
                {
                    DrawRect(rightBtnX, btnY, btnW, btnH, canUpgrade ? new Color(103, 167, 255) : new Color(170, 170, 170));
                    DrawText("Upgrade", rightBtnX + 20, btnY + 10, canUpgrade ? new Color(34, 34, 34) : new Color(85, 85, 85), FONT_SIZE_UNIT_INFO_BUTTONS);
                    DrawText($"${upgradePrice}", rightBtnX + 30, btnY + btnH + 2, new Color(85, 85, 85), FONT_SIZE_UNIT_INFO_COSTS);
                }
            }
            else
            {
                // Unlock
                bool canUnlock = gold >= unit.UnlockCost;
                DrawRect(rightBtnX, btnY, btnW, btnH, canUnlock ? Color.Lime : new Color(170, 170, 170));
                DrawText("Unlock", rightBtnX + 30, btnY + 10, canUnlock ? new Color(34, 34, 34) : new Color(85, 85, 85), FONT_SIZE_UNIT_INFO_BUTTONS);
                DrawText(unit.UnlockCost.ToString(), rightBtnX + 30, btnY + btnH + 2, new Color(85, 85, 85), FONT_SIZE_UNIT_INFO_COSTS);
            }
        }
        
        private string GetGeneralDescription(Unit unit)
        {
            // Extract just the first sentence or general description
            string desc = unit.Desc;
            int abilityIndex = desc.IndexOf("\n\nAbility:");
            int passiveIndex = desc.IndexOf("\n\nPassive:");
            
            if (abilityIndex > 0)
                return desc.Substring(0, abilityIndex);
            if (passiveIndex > 0)
                return desc.Substring(0, passiveIndex);
            
            return desc;
        }
        
        private string GetAbilityDescription(Unit unit)
        {
            if (unit.Ability == "none")
                return "This hero has no active ability.";
            
            float damage = GetUnitStat(unit, "damage", -1);
            float mpCost = GetUnitStat(unit, "abilityMpCost");
            float cooldown = GetUnitStat(unit, "abilityCooldown");
            
            // Get scaling values
            float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
            float durationMultiplier = GetAbilityScaling(unit, "duration");
            float attackSpeedMultiplier = GetAbilityScaling(unit, "attackSpeedMultiplier");
            float critChanceMultiplier = GetAbilityScaling(unit, "critChanceBonus");
            
            string desc = "";
            int lvl = Math.Min(unit.Lvl, 51);
            
            switch (unit.Ability)
            {
                case "rapid fire":
                    desc = $"Rapid Fire\n\nIncreases this hero's attack speed by {attackSpeedMultiplier * 100:F0}% for {5 * durationMultiplier:F1} seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Attack speed bonus scales with level)";
                    break;
                case "rally archers":
                    desc = $"Rally Archers\n\nIncreases town archer attack speed by {attackSpeedMultiplier * 100:F0}% for {3 * durationMultiplier:F1} seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Bonus and duration scale with level)";
                    break;
                case "death mark":
                    desc = $"Death Mark\n\nIncreases town archer critical hit chance by {0.1f * critChanceMultiplier * 100:F1}% for {3 * durationMultiplier:F1} seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Crit bonus and duration scale)";
                    break;
                case "deep freeze":
                    desc = $"Deep Freeze\n\nFreezes all monsters for {4 * durationMultiplier:F1} seconds and deals {damage * 0.5f * damageMultiplier:F1} damage to each.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Damage and duration scale)";
                    break;
                case "thunderstorm":
                    desc = $"Thunderstorm\n\nHits 8 random monsters dealing {damage * 2f * damageMultiplier:F1} damage each.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "meteor":
                    desc = $"Meteor\n\nHits 5 random monsters dealing {damage * 5f * damageMultiplier:F1} damage each.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "curse":
                    desc = $"Curse\n\nCauses all monsters to take {0.5f * damageMultiplier * 100:F0}% more damage for {3 * durationMultiplier:F1} seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Bonus and duration scale)";
                    break;
                case "bless summons":
                    desc = $"Bless Summons\n\nIncreases all summoned unit damage by {0.3f * damageMultiplier * 100:F0}% for {3 * durationMultiplier:F1} seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Bonus and duration scale)";
                    break;
                case "summon giant":
                    desc = $"Summon Giant\n\nSummons 1 giant that deals {damage * 4f * damageMultiplier:F1} damage per hit for 15 seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Summon damage scales)";
                    break;
                case "summon slingers":
                    desc = $"Summon Slingers\n\nSummons 5 stationary slingers that deal {damage * damageMultiplier:F1} damage each for 15 seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Summon damage scales)";
                    break;
                case "summon knights":
                    desc = $"Summon Knights\n\nSummons 5 knights that deal {damage * damageMultiplier:F1} damage each for 15 seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Summon damage scales)";
                    break;
                case "shadow strike":
                    desc = $"Shadow Strike\n\nHits ALL monsters on the field for {damage * 0.8f * damageMultiplier:F1} damage each.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "summon angels":
                    desc = $"Summon Angels\n\nSummons 5 flying angels that deal {damage * damageMultiplier:F1} damage each for 15 seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Summon damage scales)";
                    break;
                case "demonic speed":
                    desc = $"Demonic Speed\n\nIncreases this hero's attack speed by {2f * attackSpeedMultiplier * 100:F0}% for {5 * durationMultiplier:F1} seconds.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Bonus and duration scale)";
                    break;
                case "shadow clone":
                    desc = $"Shadow Clone\n\nCreates 4 clones that attack 4 random enemies once for {damage * 2f * damageMultiplier:F1} damage each.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "blizzard":
                    desc = $"Blizzard\n\nHits 8 random enemies with ice shards for {damage * damageMultiplier:F1} damage each.\n\nCost: {mpCost:F0} MP\nCooldown: {cooldown:F0}s\n\n(Level {lvl}: Damage scales with level)";
                    break;
                default:
                    desc = unit.Desc;
                    break;
            }
            
            return desc;
        }
        
        private string GetPassiveDescription(Unit unit)
        {
            if (unit.PassiveAbility == "none")
                return "This hero has no passive ability.";
            
            float damage = GetUnitStat(unit, "damage", -1);
            int lvl = Math.Min(unit.Lvl, 51);
            
            // Get scaling
            float damageMultiplier = GetAbilityScaling(unit, "damageMultiplier");
            float durationMultiplier = GetAbilityScaling(unit, "duration");
            
            string desc = "";
            
            switch (unit.PassiveAbility)
            {
                case "boss slayer":
                    desc = $"Boss Slayer\n\nThis hero deals {damageMultiplier * 100:F0}% bonus damage to boss enemies.\n\n(Level {lvl}: Scales with level, currently {damageMultiplier:F2}x)";
                    break;
                case "prosperity":
                    float goldBonus = 0.02f + (lvl - 1) * 0.02f;
                    desc = $"Prosperity\n\nIncreases gold and XP gained from enemies by {goldBonus * 100:F0}%.\n\nScales with level (currently level {unit.Lvl}).";
                    break;
                case "fortify":
                    float defenseBonus = 0.10f + (lvl - 1) * 0.001f;
                    desc = $"Fortify\n\nIncreases castle defense by {defenseBonus * 100:F1}%, reducing incoming damage.\n\nScales with level (currently level {unit.Lvl}).";
                    break;
                case "summon ent":
                    desc = $"Summon Ent\n\nAt the start of each wave, summons 1 immortal ent that deals {damage * 2f * damageMultiplier:F1} damage and increases castle defense by 5%.\n\nThe ent cannot die and lasts the entire wave.\n\n(Level {lvl}: Damage scales)";
                    break;
                case "summon golem":
                    desc = $"Summon Golem\n\nAt the start of each wave, summons 1 immortal stone golem that deals {damage * 4f * damageMultiplier:F1} damage.\n\nThe golem cannot die and lasts the entire wave.\n\n(Level {lvl}: Damage scales)";
                    break;
                case "summon melee":
                    desc = $"Summon Melee Skeletons\n\nEvery 10 seconds, automatically summons 2 melee skeletons that deal {damage * damageMultiplier:F1} damage each for 15 seconds.\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "summon bow":
                    desc = $"Summon Bow Skeletons\n\nEvery 10 seconds, automatically summons 2 bow skeletons that deal {damage * damageMultiplier:F1} damage each for 15 seconds.\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "summon mage":
                    desc = $"Summon Mage Skeletons\n\nEvery 10 seconds, automatically summons 2 mage skeletons that deal {damage * damageMultiplier:F1} damage each for 15 seconds.\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "thunder god":
                    desc = $"Thunder God\n\nEvery 10 seconds, automatically hits 5 random enemies for {damage * 1.5f * damageMultiplier:F1} damage each.\n\n(Level {lvl}: Damage scales with level)";
                    break;
                case "charm":
                    desc = $"Charm\n\nEvery 10 seconds, converts 4 random enemies to fight for your side for {3 * durationMultiplier:F1} seconds before reverting.\n\n(Level {lvl}: Duration scales with level)";
                    break;
                case "deadly precision":
                    float totalCritDamage = GetUnitCritDamage(unit, -1);
                    desc = $"Deadly Precision\n\nIncreases critical hit damage by 200% (total {totalCritDamage * 100:F0}% crit damage).";
                    break;
                case "frostbite":
                    desc = "Frostbite\n\nHas a 20% chance to freeze enemies hit for 2 seconds.";
                    break;
                default:
                    desc = unit.Desc;
                    break;
            }
            
            return desc;
        }

        // Helper drawing methods
        private void DrawRect(int x, int y, int width, int height, Color color, Color? borderColor = null, int borderWidth = 0)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), color);
            
            if (borderColor.HasValue && borderWidth > 0)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, width, borderWidth), borderColor.Value);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y + height - borderWidth, width, borderWidth), borderColor.Value);
                _spriteBatch.Draw(_pixel, new Rectangle(x, y, borderWidth, height), borderColor.Value);
                _spriteBatch.Draw(_pixel, new Rectangle(x + width - borderWidth, y, borderWidth, height), borderColor.Value);
            }
        }

        private void DrawCircle(int centerX, int centerY, int radius, Color color)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                        _spriteBatch.Draw(_pixel, new Rectangle(centerX + x, centerY + y, 1, 1), color);
                }
            }
        }

        private void DrawText(string text, int x, int y, Color color, float scale = 1.0f)
        {
            if (_font != null)
            {
                Vector2 position = new Vector2(x, y - 4);
                
                if (TEXT_OUTLINE_THICKNESS > 0)
                {
                    Vector2[] offsets = new Vector2[]
                    {
                        new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
                        new Vector2(-1, 0),                      new Vector2(1, 0),
                        new Vector2(-1, 1),  new Vector2(0, 1),  new Vector2(1, 1)
                    };
                    
                    foreach (var offset in offsets)
                    {
                        _spriteBatch.DrawString(_font, text, position + offset * TEXT_OUTLINE_THICKNESS, Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    }
                }
                
                _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private void DrawWrappedText(string text, int x, int y, int maxWidth, Color color, float scale)
        {
            if (_font == null) return;

            string[] words = text.Split(' ');
            List<string> lines = new List<string>();
            string currentLine = "";

            foreach (string word in words)
            {
                if (word.Contains("\n"))
                {
                    string[] parts = word.Split('\n');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string testLine = currentLine + (currentLine.Length > 0 ? " " : "") + parts[i];
                        Vector2 size = _font.MeasureString(testLine) * scale;
                        
                        if (size.X > maxWidth && currentLine.Length > 0)
                        {
                            lines.Add(currentLine);
                            currentLine = parts[i];
                        }
                        else
                        {
                            currentLine = testLine;
                        }
                        
                        if (i < parts.Length - 1)
                        {
                            lines.Add(currentLine);
                            currentLine = "";
                        }
                    }
                }
                else
                {
                    string testLine = currentLine + (currentLine.Length > 0 ? " " : "") + word;
                    Vector2 size = _font.MeasureString(testLine) * scale;
                    
                    if (size.X > maxWidth && currentLine.Length > 0)
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        currentLine = testLine;
                    }
                }
            }
            
            if (currentLine.Length > 0)
                lines.Add(currentLine);

            int lineHeight = (int)(_font.MeasureString("A").Y * scale);
            for (int i = 0; i < lines.Count; i++)
            {
                DrawText(lines[i], x, y + i * lineHeight, color, scale);
            }
        }
    }

    // Data classes
    public class Enemy
    {
        public string Name { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Damage { get; set; }
        public float AttackSpeed { get; set; }
        public float Speed { get; set; }
        public int GoldGiven { get; set; }
        public float XpGiven { get; set; }
        public float XPos { get; set; }
        public float YPos { get; set; }
        public bool IsBoss { get; set; }
        public int FreezeTimer { get; set; }
        public bool IsFrog { get; set; }
        public int FrogTimer { get; set; }
        public bool IsCharmed { get; set; }
        public int CharmTimer { get; set; }
        public Summon AttackingTarget { get; set; } // For when regular enemies attack summons
        public Enemy CharmTarget { get; set; } // For when charmed enemies attack other enemies
    }

    public class Unit
    {
        public string Name { get; set; }
        public float Damage { get; set; }
        public float AttackSpeed { get; set; }
        public int Targets { get; set; }
        public Color Col { get; set; }
        public string Ability { get; set; }
        public int AbilityMpCost { get; set; }
        public int AbilityCooldown { get; set; }
        public string PassiveAbility { get; set; }
        public bool Proj { get; set; }
        public string Element { get; set; }
        public string Desc { get; set; }
        public int Lvl { get; set; }
        public int UnlockCost { get; set; }
        public bool Unlocked { get; set; }
        public int MaxLevel { get; set; }
    }

    public class Arrow
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float FromX { get; set; }
        public float FromY { get; set; }
        public float GoingToX { get; set; }
        public float GoingToY { get; set; }
        public Enemy Enemy { get; set; }
    }

    public class Projectile
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float FromX { get; set; }
        public float FromY { get; set; }
        public float GoingToX { get; set; }
        public float GoingToY { get; set; }
        public Unit Tower { get; set; }
        public Enemy Enemy { get; set; }
        public int SlotIndex { get; set; }
    }

    public class Effect
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Timer { get; set; }
        public Color Col { get; set; }
    }

    public class Summon
    {
        public string Type { get; set; }
        public float XPos { get; set; }
        public float YPos { get; set; }
        public float Speed { get; set; }
        public float Damage { get; set; }
        public float AttackSpeed { get; set; }
        public int TimeLeft { get; set; }
        public Color Col { get; set; }
        public bool IsStationary { get; set; }
        public bool Immortal { get; set; }
        public Enemy Target { get; set; }
    }

    public class BuffState
    {
        public float AttackSpeedBonus { get; set; }
        public float DamageBonus { get; set; }
        public int AttackSpeedBonusTimer { get; set; }
        public float CritChanceBonus { get; set; }
        public int CritChanceBonusTimer { get; set; }
    }
}