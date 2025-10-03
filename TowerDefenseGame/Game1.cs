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

        private int castleLevel = 1;
        private int castlePrice = 100;

        private int gold = 0;
        private int crystals = 0;

        private const int ABILITY_BAR_WIDTH = 34;
        private const int ABILITY_BAR_HEIGHT = 7;

        // ===== TEXT OUTLINE CONFIGURATION =====
        private const float TEXT_OUTLINE_THICKNESS = 0.25f;

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
        private const float FONT_SIZE_UNIT_INFO_STATS = 0.8f;
        private const float FONT_SIZE_UNIT_INFO_DESC = 1.0f;
        private const float FONT_SIZE_UNIT_INFO_MP = 0.65f;
        private const float FONT_SIZE_UNIT_INFO_BUTTONS = 0.75f;
        private const float FONT_SIZE_UNIT_INFO_COSTS = 0.65f;
        private const float FONT_SIZE_CLOSE_BUTTON = 1.0f;
        private const float FONT_SIZE_SPEED_BUTTON = 1.0f;

        // Lists
        private List<Enemy> enemies = new List<Enemy>();
        private List<Arrow> arrows = new List<Arrow>();
        private List<Projectile> projectiles = new List<Projectile>();
        private List<Effect> effects = new List<Effect>();
        private List<Unit> unitsList = new List<Unit>();

        private int speed = 1;
        private bool hasteActive = false;
        private int hasteFramesLeft = 0;
        private int hasteSlotIndex = -1; // Track which slot has haste active

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

        private List<int?> slotAssignments = new List<int?>();
        private List<List<Enemy>> unitTargets = new List<List<Enemy>>();
        private List<int> unitAbilityCooldowns = new List<int>();

        private List<Enemy> archerTargets = new List<Enemy>();

        private long frameCount = 0; // Changed to long to prevent overflow
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
            unitsList.Add(new Unit
            {
                Name = "lightning mage",
                Damage = 12,
                AttackSpeed = 0.85f,
                Targets = 1,
                Col = Color.Yellow,
                Ability = "lightning strike",
                AbilityMpCost = 20,
                AbilityCooldown = 7,
                PassiveAbility = "none",
                Proj = true,
                Desc = "A mage that strikes enemies with lightning.\n\nAbility: Hits 20 enemies dealing 500% damage.",
                Lvl = 1,
                UnlockCost = 500,
                Unlocked = false
            });

            unitsList.Add(new Unit
            {
                Name = "zeus",
                Damage = 45,
                AttackSpeed = 0.65f,
                Targets = 7,
                Col = Color.Gold,
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "none",
                Proj = false,
                Desc = "Zeus instantly zaps up to 7 enemies but uses 5 MP per bolt.",
                Lvl = 1,
                UnlockCost = 2500,
                Unlocked = false
            });

            unitsList.Add(new Unit
            {
                Name = "fire mage",
                Damage = 9,
                AttackSpeed = 0.85f,
                Targets = 1,
                Col = Color.Red,
                Ability = "meteor shower",
                AbilityMpCost = 15,
                AbilityCooldown = 6,
                PassiveAbility = "none",
                Proj = true,
                Desc = "A mage that fires fire balls at enemies.\n\nAbility: Does 250% damage to all enemies.",
                Lvl = 1,
                UnlockCost = 500,
                Unlocked = false
            });

            unitsList.Add(new Unit
            {
                Name = "ice mage",
                Damage = 7,
                AttackSpeed = 0.85f,
                Targets = 1,
                Col = Color.Blue,
                Ability = "ice spikes",
                AbilityMpCost = 12,
                AbilityCooldown = 5,
                PassiveAbility = "slow",
                Proj = true,
                Desc = "A mage that shoots slowing bullets at targets, slowing them by 10%.\n\nAbility: Does 150% damage and slows all enemies by 40%.",
                Lvl = 1,
                UnlockCost = 1500,
                Unlocked = false
            });

            unitsList.Add(new Unit
            {
                Name = "Archer",
                Damage = 9,
                AttackSpeed = 1.5f,
                Targets = 1,
                Col = new Color(255, 255, 185),
                Ability = "haste",
                AbilityMpCost = 40,
                AbilityCooldown = 10,
                PassiveAbility = "none",
                Proj = true,
                Desc = "A town archer that decided to do some training and become a unit.\n\nAbility: Increases attack speed and damage by 100% for 5 seconds.",
                Lvl = 1,
                UnlockCost = 0,
                Unlocked = true
            });

            unitsList.Add(new Unit
            {
                Name = "Druid",
                Damage = 5,
                AttackSpeed = 1f,
                Targets = 3,
                Col = Color.Green,
                Ability = "none",
                AbilityMpCost = 0,
                AbilityCooldown = 0,
                PassiveAbility = "lifesteal",
                Proj = true,
                Desc = "A druid that throws 3 thorns at enemies, stealing 10% of damage dealt as hp.",
                Lvl = 1,
                UnlockCost = 2000,
                Unlocked = false
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
            
            for (int i = 0; i < activeSlots; i++)
            {
                unitTargets.Add(new List<Enemy>());
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
            
            // FIX 1: Corrected XP required formula
            xpRequired = 100 + (level - 1) * 25;

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
            previousMouseState = mouseState;

            // Combat tick
            if (waving)
            {
                HandleWave();
                if (mana < maxMana)
                    mana += (maxMana / 1000f) * speed;
            }

            // Arrows
            for (int i = arrows.Count - 1; i >= 0; i--)
            {
                var arrow = arrows[i];
                arrow.X += 20 * speed;
                arrow.Y = GetParabolaY(arrow.X, arrow.FromX, arrow.FromY, arrow.GoingToX, arrow.GoingToY, 0.001f);
                
                if (arrow.X > arrow.GoingToX)
                {
                    if (arrow.Enemy != null && enemies.Contains(arrow.Enemy))
                        arrow.Enemy.Health -= archerDamage;
                    arrows.RemoveAt(i);
                }
            }

            // Update projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var proj = projectiles[i];
                proj.X += 20 * speed;
                proj.Y = GetLineY(proj.X, proj.FromX, proj.FromY, proj.GoingToX, proj.GoingToY);
                
                if (proj.X > proj.GoingToX)
                {
                    if (proj.Enemy != null && enemies.Contains(proj.Enemy))
                    {
                        float damage = GetUnitStat(proj.Tower, "damage", proj.SlotIndex);
                        proj.Enemy.Health -= damage;
                        
                        if (proj.Tower.PassiveAbility == "slow")
                            proj.Enemy.Speed = Math.Max(0.5f, proj.Enemy.Speed * 0.9f);
                        else if (proj.Tower.PassiveAbility == "lifesteal")
                        {
                            if (health < maxHealth)
                                health += damage * 0.1f;
                        }
                    }
                    projectiles.RemoveAt(i);
                }
            }

            // Update effects
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                effects[i].Timer -= speed;
                if (effects[i].Timer <= 0)
                    effects.RemoveAt(i);
            }

            // Update ability cooldowns
            for (int i = 0; i < unitAbilityCooldowns.Count; i++)
            {
                if (unitAbilityCooldowns[i] > 0)
                    unitAbilityCooldowns[i] -= speed;
            }

            // Haste decay
            if (hasteActive)
            {
                hasteFramesLeft -= speed;
                if (hasteFramesLeft <= 0)
                {
                    hasteActive = false;
                    hasteSlotIndex = -1;
                }
            }

            base.Update(gameTime);
        }

        private void HandleWave()
        {
            // Enemy movement and combat
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                enemy.XPos -= enemy.Speed * speed;

                if (enemy.XPos < 275)
                {
                    enemy.Speed = 0;
                    health -= (enemy.Damage / (60f / enemy.AttackSpeed)) * speed;
                }

                if (enemy.Health <= 0)
                {
                    gold += enemy.GoldGiven;
                    xp += enemy.XpGiven;
                    enemies.RemoveAt(i);
                    numOfEnemies--;
                }
            }

            // Archer firing
            if (enemies.Count > 0 && frameCount % (60 / speed) == 0)
            {
                int numArchers = Math.Min(archerLevel, 20);
                while (archerTargets.Count < numArchers)
                    archerTargets.Add(null);
                while (archerTargets.Count > numArchers)
                    archerTargets.RemoveAt(archerTargets.Count - 1);

                for (int i = 0; i < numArchers; i++)
                {
                    var target = archerTargets[i];
                    if (target == null || !enemies.Contains(target) || target.Health <= 0)
                    {
                        if (enemies.Count > 0)
                        {
                            target = enemies[random.Next(enemies.Count)];
                            archerTargets[i] = target;
                        }
                        else
                        {
                            archerTargets[i] = null;
                            continue;
                        }
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

            // Unit firing
            if (enemies.Count > 0)
            {
                for (int j = 0; j < slotAssignments.Count; j++)
                {
                    var unitIdx = slotAssignments[j];
                    if (unitIdx == null) continue;
                    
                    var unit = unitsList[unitIdx.Value];
                    
                    // FIX 2: Safer attack ready calculation
                    float attacksPerSecond = GetUnitStat(unit, "attackSpeed", j) * speed;
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
                    else
                    {
                        if (attackReady)
                        {
                            foreach (var t in unitTargets[j])
                            {
                                if (unit.Name == "zeus")
                                {
                                    if (mana >= 5)
                                        mana -= 5;
                                    else
                                        break;
                                }

                                float damage = GetUnitStat(unit, "damage", j);
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

            // Enemy spawning
            spawnTimer += speed;
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

                // Reset haste when wave ends
                if (hasteActive)
                {
                    hasteActive = false;
                    hasteSlotIndex = -1;
                }
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

                // Reset haste on defeat
                if (hasteActive)
                {
                    hasteActive = false;
                    hasteSlotIndex = -1;
                }
            }
        }

        private void SelectRandomEnemy()
        {
            var enemyTypes = new[]
            {
                new { Name = "slime", Health = 10f, Damage = 5f, AttackSpeed = 1f, Speed = 1.5f, GoldGiven = 10, XpGiven = 5f },
                new { Name = "zombie", Health = 20f, Damage = 10f, AttackSpeed = 1f, Speed = 1.15f, GoldGiven = 15, XpGiven = 10f },
                new { Name = "brute", Health = 40f, Damage = 20f, AttackSpeed = 0.8f, Speed = 0.85f, GoldGiven = 30, XpGiven = 15f }
            };

            var enemyTemplate = enemyTypes[random.Next(enemyTypes.Length)];
            
            // FIX 3: Better enemy damage scaling
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
                YPos = 250 + (float)(random.NextDouble() * 100)
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
            
            // FIX 3: Better boss damage scaling
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
                YPos = 250 + (float)(random.NextDouble() * 50)
            });
        }

        private List<Enemy> CleanAndRefillTargets(List<Enemy> arr, int maxTargets)
        {
            arr = arr.Where(e => enemies.Contains(e) && e.Health > 0).ToList();
            
            int need = maxTargets - arr.Count;
            if (need <= 0) return arr;

            var pool = enemies.Where(e => !arr.Contains(e)).OrderBy(x => random.Next()).ToList();
            arr.AddRange(pool.Take(need));
            return arr;
        }

        // FIX 4: Added slotIndex parameter to properly handle haste
        private float GetUnitStat(Unit unit, string stat, int slotIndex = -1)
        {
            int lvl = unit.Lvl;
            
            if (stat == "damage")
            {
                float baseDamage = (float)Math.Round(unit.Damage * Math.Pow(1.075, lvl - 1));
                
                // Apply haste bonus if active for this slot
                if (hasteActive && slotIndex == hasteSlotIndex)
                    return baseDamage * 2;
                
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
                
                // Apply haste bonus if active for this slot
                if (hasteActive && slotIndex == hasteSlotIndex)
                    return baseSpeed * 2;
                
                return baseSpeed;
            }
            
            if (stat == "abilityMpCost")
                return (float)Math.Round(unit.AbilityMpCost * Math.Pow(1.025, lvl - 1));
            
            if (stat == "abilityCooldown")
                return unit.AbilityCooldown;
            
            return 0;
        }

        private int GetUnitUpgradePrice(Unit unit)
        {
            int lvl = unit.Lvl;
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
                int panelW = 450, panelH = 300;
                int panelX = 720 / 2 - panelW / 2, panelY = 480 / 2 - panelH / 2;
                
                if (mouseX >= panelX + panelW - 38 && mouseX <= panelX + panelW - 10 &&
                    mouseY >= panelY + 10 && mouseY <= panelY + 38)
                {
                    selectedUnitIndex = -1;
                    return;
                }

                // Buttons
                int descW = 250, descH = 200;
                int descX = panelX + panelW - descW - 24, descY = panelY + 32;
                int btnW = 92, btnH = 34, gap = 12;
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
                    return;
                }

                // Upgrade/Unlock button
                if (mouseX >= rightBtnX && mouseX <= rightBtnX + btnW &&
                    mouseY >= btnY && mouseY <= btnY + btnH)
                {
                    if (unit.Unlocked)
                    {
                        int upgradePrice = GetUnitUpgradePrice(unit);
                        if (gold >= upgradePrice)
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
                    int itemY = 30 + i * 64;
                    int itemHeight = 56;
                    int itemX = menuX + 12;
                    int itemWidth = 216;
                    
                    if (mouseX >= itemX && mouseX <= itemX + itemWidth &&
                        mouseY >= itemY && mouseY <= itemY + itemHeight)
                    {
                        selectedUnitIndex = i;
                        equipMode = slotAssignments[selectedSlot] != i;
                        return;
                    }
                }

                if (mouseX <= 480)
                {
                    menuOpen = false;
                    selectedSlot = -1;
                    selectedUnitIndex = -1;
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
            if (unit.Ability == "meteor shower")
            {
                float damage = GetUnitStat(unit, "damage", slotIndex);
                foreach (var enemy in enemies)
                {
                    enemy.Health -= 2.5f * damage;
                    effects.Add(new Effect
                    {
                        X = enemy.XPos,
                        Y = enemy.YPos - 20,
                        Timer = 18,
                        Col = Color.Red
                    });
                }
            }
            else if (unit.Ability == "lightning strike")
            {
                // FIX 5: Improved lightning strike target selection
                var targets = new List<Enemy>();
                int targetCount = Math.Min(20, enemies.Count);
                
                // Create a shuffled list of all enemies
                var shuffledEnemies = enemies.OrderBy(x => random.Next()).ToList();
                
                // Take the first targetCount enemies
                for (int i = 0; i < targetCount; i++)
                {
                    targets.Add(shuffledEnemies[i]);
                }
                
                float damage = GetUnitStat(unit, "damage", slotIndex);
                foreach (var t in targets)
                {
                    t.Health -= 5 * damage;
                    effects.Add(new Effect
                    {
                        X = t.XPos,
                        Y = t.YPos - 20,
                        Timer = 18,
                        Col = Color.Yellow
                    });
                }
            }
            else if (unit.Ability == "ice spikes")
            {
                float damage = GetUnitStat(unit, "damage", slotIndex);
                foreach (var enemy in enemies)
                {
                    enemy.Health -= 1.5f * damage;
                    enemy.Speed = Math.Max(0.5f, enemy.Speed * 0.6f);
                    effects.Add(new Effect
                    {
                        X = enemy.XPos,
                        Y = enemy.YPos - 20,
                        Timer = 18,
                        Col = Color.Blue
                    });
                }
            }
            else if (unit.Ability == "haste" && !hasteActive)
            {
                // FIX 6: Properly handle haste with level scaling
                hasteActive = true;
                hasteSlotIndex = slotIndex;
                hasteFramesLeft = 300; // 5 seconds at 60 FPS
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
            // FIX 7: Handle vertical lines
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
            DrawArchers();
            DrawEnemies();
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
                    
                    // FIX 8: Visual indicator for haste
                    if (hasteActive && hasteSlotIndex == i)
                    {
                        // Make the unit glow when haste is active
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
                int barY = slot.Y - 10;

                DrawRect(barX, barY, ABILITY_BAR_WIDTH, ABILITY_BAR_HEIGHT, Color.Black);
                DrawRect(barX, barY, (int)(ABILITY_BAR_WIDTH * percentReady), ABILITY_BAR_HEIGHT, new Color(50, 140, 255));
            }
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
                if (enemy.Name == "zombie")
                {
                    DrawRect((int)enemy.XPos, (int)enemy.YPos, 20, 30, Color.DarkGreen);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, 30, 5, Color.Black);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, (int)(30 * (enemy.Health / enemy.MaxHealth)), 5, Color.Red);
                }
                else if (enemy.Name == "slime")
                {
                    DrawRect((int)enemy.XPos, (int)enemy.YPos, 20, 20, Color.Cyan);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, 30, 5, Color.Black);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, (int)(30 * (enemy.Health / enemy.MaxHealth)), 5, Color.Red);
                }
                else if (enemy.Name == "brute")
                {
                    DrawRect((int)enemy.XPos, (int)enemy.YPos, 20, 30, Color.Pink);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, 30, 5, Color.Black);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 10, (int)(30 * (enemy.Health / enemy.MaxHealth)), 5, Color.Red);
                }
                else if (enemy.Name == "king slime")
                {
                    DrawRect((int)enemy.XPos, (int)enemy.YPos, 50, 50, Color.Cyan);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 15, 60, 10, Color.Black);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 15, (int)(60 * (enemy.Health / enemy.MaxHealth)), 10, Color.Red);
                }
                else if (enemy.Name == "giant king")
                {
                    DrawRect((int)enemy.XPos, (int)enemy.YPos, 50, 80, Color.Pink);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 15, 60, 10, Color.Black);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 15, (int)(60 * (enemy.Health / enemy.MaxHealth)), 10, Color.Red);
                }
                else if (enemy.Name == "zombie boss")
                {
                    DrawRect((int)enemy.XPos, (int)enemy.YPos, 50, 80, Color.DarkGreen);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 15, 60, 10, Color.Black);
                    DrawRect((int)enemy.XPos - 5, (int)enemy.YPos - 15, (int)(60 * (enemy.Health / enemy.MaxHealth)), 10, Color.Red);
                }
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

            for (int i = 0; i < unitsList.Count; i++)
            {
                var unit = unitsList[i];
                int itemY = 30 + i * 64;
                int itemHeight = 56;
                int itemX = menuX + 12;
                int itemWidth = 216;

                DrawRect(itemX, itemY, itemWidth, itemHeight, new Color(245, 245, 245, 240), new Color(160, 160, 160), 2);
                DrawRect(itemX + 15, itemY + 7, 24, 42, unit.Col, new Color(80, 80, 80), 2);
                DrawText(unit.Name, itemX + 55, itemY + itemHeight / 2 - 5, new Color(40, 40, 40), FONT_SIZE_UNIT_LIST_NAME);

                if (!unit.Unlocked)
                    DrawText(unit.UnlockCost.ToString(), itemX + 160, itemY + itemHeight / 2 - 5, Color.Gold, FONT_SIZE_UNIT_LIST_COST);
            }
        }

        private void DrawUnitInfoMenu()
        {
            var unit = unitsList[selectedUnitIndex];
            int panelW = 450, panelH = 300;
            int panelX = 720 / 2 - panelW / 2, panelY = 480 / 2 - panelH / 2;

            DrawRect(panelX, panelY, panelW, panelH, new Color(255, 255, 255, 240), new Color(180, 180, 180), 3);

            // X button
            DrawRect(panelX + panelW - 38, panelY + 10, 28, 28, new Color(220, 80, 80));
            if (_font != null)
                DrawText("X", panelX + panelW - 28, panelY + 16, Color.White, FONT_SIZE_CLOSE_BUTTON);

            // Unit sprite
            DrawRect(panelX + 28, panelY + 40, 28, 64, unit.Col, new Color(100, 100, 100), 2);

            if (_font == null) return;

            // Stats
            DrawText($"Damage: {GetUnitStat(unit, "damage")}", panelX + 28, panelY + 112, new Color(40, 40, 40), FONT_SIZE_UNIT_INFO_STATS);
            DrawText($"Attacks/sec: {GetUnitStat(unit, "attackSpeed"):F2}", panelX + 28, panelY + 136, new Color(40, 40, 40), FONT_SIZE_UNIT_INFO_STATS);
            DrawText($"Level: {unit.Lvl}", panelX + 28, panelY + 160, new Color(40, 40, 40), FONT_SIZE_UNIT_INFO_STATS);

            // Description box
            int descW = 250, descH = 200;
            int descX = panelX + panelW - descW - 24, descY = panelY + 32;
            DrawRect(descX, descY, descW, descH, new Color(245, 245, 245), new Color(190, 190, 190), 1);

            if (unit.Ability != "none")
            {
                string mpText = $"MP: {GetUnitStat(unit, "abilityMpCost")}   Cooldown: {GetUnitStat(unit, "abilityCooldown")}";
                DrawText(mpText, descX + 10, descY + 10, new Color(60, 60, 60), FONT_SIZE_UNIT_INFO_MP);
                DrawWrappedText(unit.Desc, descX + 10, descY + 32, descW - 20, new Color(60, 60, 60), FONT_SIZE_UNIT_INFO_DESC);
            }
            else
            {
                DrawWrappedText(unit.Desc, descX + 10, descY + 10, descW - 20, new Color(60, 60, 60), FONT_SIZE_UNIT_INFO_DESC);
            }

            // Buttons
            int btnW = 92, btnH = 34, gap = 12;
            int btnY = descY + descH + 16;
            int rightBtnX = descX + descW - btnW;
            int leftBtnX = rightBtnX - btnW - gap;

            int upgradePrice = GetUnitUpgradePrice(unit);
            bool canUpgrade = gold >= upgradePrice;

            if (unit.Unlocked)
            {
                // Equip/Unequip
                DrawRect(leftBtnX, btnY, btnW, btnH, equipMode ? Color.Lime : Color.Red);
                DrawText(equipMode ? "Equip" : "Unequip", leftBtnX + 18, btnY + 10, new Color(25, 25, 25), FONT_SIZE_UNIT_INFO_BUTTONS);

                // Upgrade
                DrawRect(rightBtnX, btnY, btnW, btnH, canUpgrade ? new Color(103, 167, 255) : new Color(170, 170, 170));
                DrawText("Upgrade", rightBtnX + 16, btnY + 10, canUpgrade ? new Color(34, 34, 34) : new Color(85, 85, 85), FONT_SIZE_UNIT_INFO_BUTTONS);
                DrawText($"${upgradePrice}", rightBtnX + 26, btnY + btnH + 2, new Color(85, 85, 85), FONT_SIZE_UNIT_INFO_COSTS);
            }
            else
            {
                // Unlock
                DrawRect(rightBtnX, btnY, btnW, btnH, Color.Lime);
                DrawText("Unlock", rightBtnX + 22, btnY + 10, new Color(34, 34, 34), FONT_SIZE_UNIT_INFO_BUTTONS);
                DrawText(unit.UnlockCost.ToString(), rightBtnX + 26, btnY + btnH + 2, new Color(85, 85, 85), FONT_SIZE_UNIT_INFO_COSTS);
            }
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
        public string Desc { get; set; }
        public int Lvl { get; set; }
        public int UnlockCost { get; set; }
        public bool Unlocked { get; set; }
        public float BaseAttackSpeed { get; set; }
        public float BaseDamage { get; set; }
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
        public int SlotIndex { get; set; } // FIX 9: Track which slot fired this projectile
    }

    public class Effect
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Timer { get; set; }
        public Color Col { get; set; }
    }
}