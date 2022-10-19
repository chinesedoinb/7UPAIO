using System;
using System.Collections.Generic;
using System.Linq;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;
using SharpDX.Direct3D9;

namespace AIO7UP.Champions
{
    internal class Olaf
    {
        internal class OlafAxe
        {
            public GameObject Object { get; set; }
            public float NetworkId { get; set; }
            public Vector3 AxePos { get; set; }
            public double ExpireTime { get; set; }
        }
        
        private struct Tuple<TA, TB, TC> : IEquatable<Tuple<TA, TB, TC>>
        {
            private readonly TA item;
            private readonly TB itemType;
            private readonly TC targetingType;

            public Tuple(TA pItem, TB pItemType, TC pTargetingType)
            {
                this.item = pItem;
                this.itemType = pItemType;
                this.targetingType = pTargetingType;
            }

            public TA Item => this.item;

            public TB ItemType => this.itemType;

            public TC TargetingType => this.targetingType;

            public override int GetHashCode()
            {
                return this.item.GetHashCode() ^ this.itemType.GetHashCode() ^ this.targetingType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }
                return this.Equals((Tuple<TA, TB, TC>)obj);
            }

            public bool Equals(Tuple<TA, TB, TC> other)
            {
                return other.item.Equals(item) && other.itemType.Equals(this.itemType)
                                               && other.targetingType.Equals(this.targetingType);
            }
        }

        private enum EnumItemType
        {
            OnTarget,
            Targeted,
            AoE
        }

        private enum EnumItemTargettingType
        {
            Ally,
            EnemyHero,
            EnemyObjects
        }

        public static AIHeroClient Player => ObjectManager.Player;

        private static string Tab => "       ";
        
        private static readonly OlafAxe olafAxe = new OlafAxe();
        public static Font TextAxe, TextLittle;
        public static int LastTickTime;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        
        private static Items.Item itemYoumuu;
        private static Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>> ItemDb;
        
        private static Menu Config, MenuMisc, MenuCombo, drawingMenu, harassMenu, laneclearMenu, jungleMenu, fleeMenu;
        

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Olaf")
            {
                return;
            }
            
            Q = new Spell(SpellSlot.Q, 1000);
            Q2 = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 75f, 1500f, false, SpellType.Line);
            Q2.SetSkillshot(0.25f, 75f, 1600f, false, SpellType.Line);

            SpellList.Add(Q);
            SpellList.Add(E);
            
            itemYoumuu = new Items.Item(3142, 225f);

            ItemDb =
                new Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>>
                    {
                         {
                            "Tiamat",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3077, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                        },
                        {
                            "Bilge",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3144, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Blade",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3153, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Hydra",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3074, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                        },
                        {
                            "Titanic Hydra Cleave",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3748, Player.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.OnTarget,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Randiun",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3143, 490f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Hextech",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3146, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Entropy",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3184, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Youmuu's Ghostblade",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3142, Player.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Sword of the Divine",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3131, Player.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                        }
                    };
            
            Config = new Menu(Player.CharacterName, "[7UP]: Olaf", true);
            
            MenuCombo = new Menu("Combo", "Combo");
            Config.Add(MenuCombo);
            {
                MenuCombo.Add(new MenuBool("UseQCombo", "Use Q"));
            }
            Config.Add(
                new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press));

            harassMenu = new Menu("Harass", "Harass");
            {
                harassMenu.Add(new MenuBool("Spell Settings", "Spell Settings:"));
                harassMenu.Add(new MenuBool("UseQHarass", Tab + "Use Q", false));
                harassMenu.Add(new MenuBool("UseQ2Harass", Tab + "Use Q (Short-Range)"));
                harassMenu.Add(new MenuBool("UseEHarass", Tab + "Use E"));
                harassMenu.Add(new MenuBool("Mana Settings", "Mana Settings:"));
                harassMenu.Add(new MenuSlider("Harass.UseQ.MinMana", Tab + "Q Harass Min. Mana", 30));

                harassMenu.Add(new MenuBool("Toggle Settings", "Toggle Settings:"));
                {
                    harassMenu.Add(new MenuKeyBind("Harass.UseQ.Toggle", Tab + "Auto-Use Q", Keys.T, KeyBindType.Toggle));
                }
                harassMenu.Add(new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press));
            }

            Config.Add(harassMenu);

            laneclearMenu = new Menu("LaneClear", "Lane Clear");
            {
                laneclearMenu.Add(new MenuBool("UseQFarm", "Use Q"));
                laneclearMenu.Add(new MenuSlider("UseQFarmMinCount", Tab + "Min. Minion to Use Q", 2, 1, 5));
                laneclearMenu.Add(new MenuSlider("UseQFarmMinMana", Tab + "Min. Mana to Use Q", 30));

                laneclearMenu.Add(new MenuBool("UseEFarm", "Use E"));
                    
                laneclearMenu.Add(new MenuList("UseEFarmSet", Tab + "Use E:", new[] { "Last Hit", "Always" }, 0));
                laneclearMenu.Add(new MenuSlider("UseEFarmMinHealth", Tab + "Min. Health to Use E", 10));

                laneclearMenu.Add(new MenuBool("LaneClearUseItems", "Use Items "));
                laneclearMenu.Add(new MenuKeyBind("LaneClearActive", "Lane Clear!", Keys.S, KeyBindType.Press));
            }

            Config.Add(laneclearMenu);

            jungleMenu = new Menu("JungleFarm", "Jungle Clear");
            {
                jungleMenu.Add(new MenuBool("UseQJFarm", "Use Q"));
                jungleMenu.Add(new MenuSlider("UseQJFarmMinMana", Tab + "Min. Mana to Use Q", 30));

                jungleMenu.Add(new MenuBool("UseWJFarm", "Use W").SetValue(true));
                jungleMenu.Add(new MenuSlider("UseWJFarmMinMana", Tab + "Min. Man to Use W", 30));

                jungleMenu.Add(new MenuBool("UseEJFarm", "Use E").SetValue(true));
                jungleMenu.Add(new MenuList("UseEJFarmSet", Tab + "Use E:",new[] { "Last Hit", "Always" }, 1));
                jungleMenu.Add(new MenuSlider("UseEJFarmMinHealth", Tab + "Min. Health to Use E", 10));
                
                jungleMenu.Add(new MenuBool("JungleFarmUseItems", "Use Items "));
                jungleMenu.Add(new MenuList("UseJFarmYoumuuForDragon", Tab + "Baron/Dragon:",new[] { "Off", "Dragon", "Baron", "Both" }, 3));
                jungleMenu.Add(new MenuList("UseJFarmYoumuuForBlueRed", Tab + "Blue/Red:",new[] { "Off", "Blue", "Red", "Both" }, 3));

                jungleMenu.Add(new MenuKeyBind("JungleFarmActive", "Jungle Farm!", Keys.V, KeyBindType.Press));
            }

            Config.Add(jungleMenu);
            
            fleeMenu = new Menu("Flee", "Flee");
            {
                fleeMenu.Add(new MenuBool("Flee.UseQ", "Use Q", false));
                fleeMenu.Add(new MenuBool("Flee.UseYou", "Use Youmuu's Ghostblade", false));
                fleeMenu.Add(
                    new MenuKeyBind("Flee.Active", "Flee!", Keys.Z, KeyBindType.Press));
            }
            
            Config.Add(fleeMenu);
            
            MenuMisc = new Menu("Misc", "Misc");
            {
                MenuMisc.Add(new MenuBool("Misc.AutoE", "Auto-Use E (If Enemy Hit)").SetValue(false));
                string[] strE = new string[1000 / 250];
                for (var i = 250; i <= 1000; i += 250)
                {
                    strE[i / 250 - 1] = "Add " + i + " ms. delay for who visible instantly (Shaco/Rengar etc.)";
                }
                MenuMisc.Add(new MenuList("Misc.AutoE.Delay", "E:",strE, 0));

                MenuMisc.Add(new MenuBool("Misc.AutoR", "Auto-Use R on Crowd-Control").SetValue(false));
                Config.Add(MenuMisc);
            }
            
            drawingMenu = Config.Add(new Menu("Drawings", "Drawings"));

            drawingMenu.Add(new MenuBool("Draw.SpellDrawing", "Spell Drawing:"));
            drawingMenu.Add(
        new MenuBool("Draw.QRange", Tab + "Q range"));
            drawingMenu.Add(
                    new MenuBool("Draw.Q2Range", Tab + "Short Q range"));
            drawingMenu.Add(
                    new MenuBool("Draw.ERange", Tab + "E range"));

            drawingMenu.Add(new MenuBool("Draw.AxeDrawing", "Axe Drawing:"));
            drawingMenu.Add(
                    new MenuList("Draw.AxePosition", Tab + "Axe Position", new[] { "Off", "Circle", "Line", "Both" }, 3));
            drawingMenu.Add(new MenuBool("Draw.AxeTime", Tab + "Axe Time Remaining"));
            
            Config.Attach();

            TextAxe = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 39,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });
            TextLittle = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });

            new Helper();
            
            GameEvent.OnGameTick += OnGameUpdate;
            //Drawing.OnDraw += OnDraw;
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        internal class EnemyHeros
        {
            public AIHeroClient Player;
            public int LastSeen;

            public EnemyHeros(AIHeroClient player)
            {
                Player = player;
            }
        }

        internal class Helper
        {
            public static List<EnemyHeros> EnemyInfo = new List<EnemyHeros>();

            public Helper()
            {
                var champions = ObjectManager.Get<AIHeroClient>().ToList();

                EnemyInfo = GameObjects.EnemyHeroes.Select(e => new EnemyHeros(e)).ToList();

                EnsoulSharp.SDK.GameEvent.OnGameTick += Game_OnGameUpdate;
            }

            private void Game_OnGameUpdate(EventArgs args)
            {
                foreach (var enemyInfo in EnemyInfo)
                {
                    if (!enemyInfo.Player.IsVisible)
                        enemyInfo.LastSeen = Variables.TickCount;
                }
            }
        }

        private static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {

            //if (obj.Name == "olaf_axe_totem_team_id_green.troy")
            if (obj.Name.ToLower().Contains("_q_axe") && obj.Name.ToLower().Contains("ally"))
            {
                olafAxe.Object = obj;
                olafAxe.ExpireTime = Game.Time + 8;
                olafAxe.NetworkId = obj.NetworkId;
                olafAxe.AxePos = obj.Position;
                //_axeObj = obj;
                //LastTickTime = Environment.TickCount;
            }
        }

        private static void GameObject_OnDelete(GameObject obj, EventArgs args)
        {
            //if (obj.Name == "olaf_axe_totem_team_id_green.troy")
            if (obj.Name.ToLower().Contains("_q_axe") && obj.Name.ToLower().Contains("ally"))
            {
                olafAxe.Object = null;
                //_axeObj = null;
                LastTickTime = 0;
            }
        }

        private static void OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            if (!(args.Target is AIHeroClient target))
            {
                return;
            }
            
            foreach (var item in
                ItemDb.Where(
                    i =>
                        i.Value.ItemType == EnumItemType.OnTarget
                        && i.Value.TargetingType == EnumItemTargettingType.EnemyHero && i.Value.Item.IsReady))
            {
                item.Value.Item.Cast();
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && W.IsReady()
                                                            && args.Target.Health > Player.TotalAttackDamage * 2)
            {
                W.Cast();
            }
        }

        private static void CountAa()
        {
            int result = 0;

            foreach (var e in GameObjects.EnemyHeroes.Where(e => e.Distance(Player.Position) < Q.Range * 3 && !e.IsDead && e.IsVisible))
            {
                var getComboDamage = GetComboDamage(e);
                var str = " ";

                if (e.Health < getComboDamage + Player.TotalAttackDamage * 5)
                {
                    result = (int)Math.Ceiling((e.Health - getComboDamage) / Player.TotalAttackDamage) + 1;
                    if (e.Health < getComboDamage)
                    {
                        str = "Combo = Kill";
                    }
                    else
                    {
                        str = (getComboDamage > 0 ? "Combo " : "") + (result > 0 ? result + " x AA Damage = Kill" : "");
                    }
                }

                DrawText(
                    TextLittle,
                    str,
                    (int)e.HPBarPosition.X + 145,
                    (int)e.HPBarPosition.Y + 5,
                    result <= 4 ? SharpDX.Color.GreenYellow : SharpDX.Color.White);
            }
        }

        /*private static void OnDraw(EventArgs args)
        {
            CountAa();

            var drawAxePosition = Array.IndexOf(drawingMenu["Draw.AxePosition"].GetValue<MenuList>().Items, drawingMenu["Draw.AxePosition"].GetValue<MenuList>().SelectedValue);
            if (olafAxe.Object != null)
            {
                var exTime = TimeSpan.FromSeconds(olafAxe.ExpireTime - Game.Time).TotalSeconds;
                var color = exTime > 4 ? System.Drawing.Color.Yellow : System.Drawing.Color.Red;
                switch (drawAxePosition)
                {
                    case 1:
                        Render.Circle.DrawCircle(olafAxe.Object.Position, 150, color, 6);
                        break;
                    case 2:
                        {
                            var line = new Geometry.Line(
                                Player.Position,
                                olafAxe.AxePos,
                                Player.Distance(olafAxe.AxePos));
                            line.Draw(color, 2);
                        }
                        break;
                    case 3:
                        {
                            Render.Circle.DrawCircle(olafAxe.Object.Position, 150, color, 6);

                            var line = new Geometry.Line(
                                Player.Position,
                                olafAxe.AxePos,
                                Player.Distance(olafAxe.AxePos));
                            line.Draw(color, 2);
                        }
                        break;


                }
            }

            if (drawingMenu["Draw.AxeTime"].GetValue<MenuBool>().Enabled && olafAxe.Object != null)
            {
                var time = TimeSpan.FromSeconds(olafAxe.ExpireTime - Game.Time);
                var pos = Drawing.WorldToScreen(olafAxe.AxePos);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);

                SharpDX.Color vTimeColor = time.TotalSeconds > 4 ? SharpDX.Color.White : SharpDX.Color.Red;
                DrawText(TextAxe, display, (int)pos.X - display.Length * 3, (int)pos.Y - 65, vTimeColor);
            }
            
            foreach (var spell in SpellList)
            {
                var menuItem = drawingMenu["Draw." + spell.Slot + "Range"].GetValue<MenuBool>();
                if (menuItem.Enabled)
                {
                    Render.Circle.DrawCircle(Player.Position, spell.Range, System.Drawing.Color.Gray, 1);
                }
            }
            var Q2Range = drawingMenu["Draw.Q2Range"].GetValue<MenuBool>();
            if (Q2Range.Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, Q2.Range, System.Drawing.Color.Gray, 1);
            }
        }*/

        private static void OnGameUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode != OrbwalkerMode.Combo || !Player.HasBuff("Recall"))
            {
                if (harassMenu["Harass.UseQ.Toggle"].GetValue<MenuKeyBind>().Active)
                {
                    CastQ();
                }
            }

            if (E.IsReady() && MenuMisc["Misc.AutoE"].GetValue<MenuBool>().Enabled)
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (t.IsValidTarget())
                    CastE(t);
                //E.CastOnUnit(t);
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                Combo();
            }

            if (laneclearMenu["LaneClearActive"].GetValue<MenuKeyBind>().Active)
            {
                LaneClear();
            }

            if (jungleMenu["JungleFarmActive"].GetValue<MenuKeyBind>().Active)
            {
                JungleFarm();
            }

            if (harassMenu["HarassActive"].GetValue<MenuKeyBind>().Active)
            {
                Harass();
            }

            if (fleeMenu["Flee.Active"].GetValue<MenuKeyBind>().Active)
                Flee();

            if (R.IsReady() && MenuMisc["Misc.AutoR"].GetValue<MenuBool>().Enabled)
            {
                CastR();
            }
        }

        private static void Combo()
        {
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            if (MenuCombo["UseQCombo"].GetValue<MenuBool>().Enabled && Q.IsReady() &&
                Player.Distance(t.Position) <= Q.Range)
            {
                var qPredictionOutput = Q.GetPrediction(t);
                var castPosition = qPredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -100);

                if (Player.Distance(t.Position) >= 300)
                {
                    Q.Cast(castPosition);
                }
                else
                {
                    Q.Cast(qPredictionOutput.CastPosition);
                }
            }

            if (E.IsReady() && Player.Distance(t.Position) <= E.Range)
            {
                CastE(t);
                //E.CastOnUnit(t);
            }

            if (W.IsReady() && Player.Distance(t.Position) <= 225f)
            {
                W.Cast();
            }

            CastItems(t);
        }

        private static void CastE(AttackableUnit t)
        {
            if (!E.IsReady() && !t.IsValidTarget(E.Range))
            {
                return;
            }

            foreach (var enemy in Helper.EnemyInfo.Where(
                x =>
                    !x.Player.IsDead &&
                    Environment.TickCount - x.LastSeen >=
                    (Array.IndexOf(MenuMisc["Misc.AutoE.Delay"].GetValue<MenuList>().Items, MenuMisc["Misc.AutoE.Delay"].GetValue<MenuList>().SelectedValue) + 1) * 250 &&
                    x.Player.NetworkId == t.NetworkId).Select(x => x.Player).Where(enemy => enemy != null))
            {
                E.CastOnUnit(enemy);
            }

        }
        private static void CastQ()
        {
            if (!Q.IsReady())
                return;

            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (t.IsValidTarget())
            {
                Vector3 castPosition;
                var qPredictionOutput = Q.GetPrediction(t);

                if (!t.IsFacing(Player) && t.Path.Count() >= 1) // target is running
                {
                    castPosition = Q.GetPrediction(t).CastPosition
                                   + Vector3.Normalize(t.Position - Player.Position) * t.MoveSpeed / 2;
                }
                else
                {
                    castPosition = qPredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -100);
                }

                Q.Cast(Player.Distance(t.Position) >= 350 ? castPosition : qPredictionOutput.CastPosition);
            }
        }

        private static void CastShortQ()
        {
            if (!Q.IsReady())
                return;

            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (t.IsValidTarget() && Q.IsReady()
                && Player.Mana > Player.MaxMana / 100 * harassMenu["Harass.UseQ.MinMana"].GetValue<MenuSlider>().Value
                && Player.Distance(t.Position) <= Q2.Range)
            {
                var q2PredictionOutput = Q2.GetPrediction(t);
                var castPosition = q2PredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -140);
                if (q2PredictionOutput.Hitchance >= HitChance.High) Q2.Cast(castPosition);
            }
        }

        private static void CastR()
        {
            BuffType[] buffList =
            {
                BuffType.Blind,
                BuffType.Charm,
                BuffType.Fear,
                BuffType.Knockback,
                BuffType.Knockup,
                BuffType.Taunt,
                BuffType.Slow,
                BuffType.Silence,
                BuffType.Disarm,
                BuffType.Snare
            };

            foreach (var b in buffList.Where(b => Player.HasBuffOfType(b)))
            {
                R.Cast();
            }
        }

        private static void Harass()
        {
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            if (harassMenu["UseQHarass"].GetValue<MenuBool>().Enabled)
            {
                CastQ();
            }

            if (harassMenu["UseQ2Harass"].GetValue<MenuBool>().Enabled)
            {
                CastShortQ();
            }

            if (E.IsReady() && harassMenu["UseEHarass"].GetValue<MenuBool>().Enabled && Player.Distance(t.Position) <= E.Range)
            {
                CastE(t);
                //E.CastOnUnit(t);
            }
        }

        private static void LaneClear()
        {
            var allMinions = GameObjects.GetMinions(
                Player.Position,
                Q.Range,
                MinionTypes.All,
                MinionTeam.Enemy,
                MinionOrderTypes.MaxHealth);

            if (allMinions.Count <= 0) return;

            if (laneclearMenu["LaneClearUseItems"].GetValue<MenuBool>().Enabled)
            {
                foreach (var item in from item in ItemDb
                                     where
                                         item.Value.ItemType == EnumItemType.AoE
                                         && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                                     let iMinions = allMinions
                                     where
                                         item.Value.Item.IsReady
                                         && iMinions.FirstOrDefault().Distance(Player.Position) < item.Value.Item.Range
                                     select item)
                {
                    item.Value.Item.Cast();
                }
            }

            if (laneclearMenu["UseQFarm"].GetValue<MenuBool>().Enabled && Q.IsReady()
                && Player.HealthPercent > laneclearMenu["UseQFarmMinMana"].GetValue<MenuSlider>().Value)
            {
                var vParamQMinionCount = laneclearMenu["UseQFarmMinCount"].GetValue<MenuSlider>().Value;

                var objAiHero = from x1 in ObjectManager.Get<AIMinionClient>()
                                where x1.IsValidTarget() && x1.IsEnemy
                                select x1
                                    into h
                                orderby h.Distance(Player) descending
                                select h
                                        into x2
                                where x2.Distance(Player) < Q.Range - 20 && !x2.IsDead
                                select x2;

                var aiMinions = objAiHero as AIMinionClient[] ?? objAiHero.ToArray();

                var lastMinion = aiMinions.First();

                var qMinions = GameObjects.GetMinions(
                    ObjectManager.Player.Position,
                    Player.Distance(lastMinion.Position));

                if (qMinions.Count > 0)
                {
                    var locQ = Q.GetLineFarmLocation(qMinions.ToList(), Q.Width);

                    if (qMinions.Count == qMinions.Count(m => Player.Distance(m) < Q.Range)
                        && locQ.MinionsHit >= vParamQMinionCount && locQ.Position.IsValid())
                    {
                        Q.Cast(lastMinion.Position);
                    }
                }
            }

            if (laneclearMenu["UseEFarm"].GetValue<MenuBool>().Enabled && E.IsReady()
                && Player.HealthPercent > laneclearMenu["UseEFarmMinHealth"].GetValue<MenuSlider>().Value)
            {
                var eMinions = GameObjects.GetMinions(Player.Position, E.Range);
                if (eMinions.Count > 0)
                {
                    var eFarmSet = Array.IndexOf(laneclearMenu["UseEFarmSet"].GetValue<MenuList>().Items, laneclearMenu["UseEFarmSet"].GetValue<MenuList>().SelectedValue);
                    switch (eFarmSet)
                    {
                        case 0:
                            {
                                if (eMinions.FirstOrDefault().Health <= E.GetDamage(eMinions.FirstOrDefault()))
                                {
                                    E.CastOnUnit(eMinions.FirstOrDefault());
                                }
                                break;
                            }
                        case 1:
                            {
                                E.CastOnUnit(eMinions.FirstOrDefault());
                                break;
                            }
                    }
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).ToList();

            if (mobs.Count <= 0)
            {
                ;
                return;
            }

            var mob = mobs[0];

            if (jungleMenu["JungleFarmUseItems"].GetValue<MenuBool>().Enabled)
            {
                foreach (var item in from item in ItemDb
                                     where
                                         item.Value.ItemType == EnumItemType.AoE
                                         && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                                     let iMinions = mobs
                                     where item.Value.Item.IsReady && iMinions[0].IsValidTarget(item.Value.Item.Range)
                                     select item)
                {
                    item.Value.Item.Cast();
                }

                if (itemYoumuu.IsReady && Player.Distance(mob) < 400)
                {
                    var youmuuBaron = Array.IndexOf(jungleMenu["UseJFarmYoumuuForDragon"].GetValue<MenuList>().Items, jungleMenu["UseJFarmYoumuuForDragon"].GetValue<MenuList>().SelectedValue);
                    var youmuuRed = Array.IndexOf(jungleMenu["UseJFarmYoumuuForBlueRed"].GetValue<MenuList>().Items, jungleMenu["UseJFarmYoumuuForBlueRed"].GetValue<MenuList>().SelectedValue);

                    /*if (mob.Name.Contains("Dragon") && (youmuuBaron == (int)jungl.Dragon || youmuuBaron == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Baron") && (youmuuBaron == (int)Mobs.Baron || youmuuBaron == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Blue") && (youmuuRed == (int)Mobs.Blue || youmuuRed == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Red") && (youmuuRed == (int)Mobs.Red || youmuuRed == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }*/
                }
            }
            if (jungleMenu["UseQJFarm"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                if (Player.Mana < Player.MaxMana / 100 * jungleMenu["UseQJFarmMinMana"].GetValue<MenuSlider>().Value) return;

                if (Q.IsReady()) Q.Cast(mob.Position - 20);
            }

            if (jungleMenu["UseWJFarm"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                if (Player.Mana < Player.MaxMana / 100 * jungleMenu["UseWJFarmMinMana"].GetValue<MenuSlider>().Value) return;

                if (mobs.Count >= 2 || mob.Health > Player.TotalAttackDamage * 2.5) W.Cast();
            }

            if (jungleMenu["UseEJFarm"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                if (Player.Health < Player.MaxHealth / 100 * jungleMenu["UseEJFarmMinHealth"].GetValue<MenuSlider>().Value) return;

                var vParamESettings = Array.IndexOf(jungleMenu["UseEJFarmSet"].GetValue<MenuList>().Items, jungleMenu["UseEJFarmSet"].GetValue<MenuList>().SelectedValue);
                switch (vParamESettings)
                {
                    case 0:
                        {
                            if (mob.Health <= Player.GetSpellDamage(mob, SpellSlot.E)) E.CastOnUnit(mob);
                            break;
                        }
                    case 1:
                        {
                            E.CastOnUnit(mob);
                            break;
                        }
                }
            }
        }

        private static void Flee()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (fleeMenu["Flee.UseQ"].GetValue<MenuBool>().Enabled)
                if (Q.IsReady())
                {
                    CastQ();
                }
            if (fleeMenu["Flee.UseYou"].GetValue<MenuBool>().Enabled)
            {
                if (itemYoumuu.IsReady)
                    itemYoumuu.Cast();
            }
        }

        private static void CastItems(AIHeroClient t)
        {
            foreach (var item in ItemDb)
            {
                if (item.Value.ItemType == EnumItemType.AoE
                    && item.Value.TargetingType == EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady) item.Value.Item.Cast();
                }
                if (item.Value.ItemType == EnumItemType.Targeted
                    && item.Value.TargetingType == EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady) item.Value.Item.Cast(t);
                }
            }
        }

        private static float GetComboDamage(AIBaseClient t)
        {
            var fComboDamage = 0d;

            if (Q.IsReady()) fComboDamage += Q.GetDamage(t);

            if (E.IsReady()) fComboDamage += E.GetDamage(t);

            return (float)fComboDamage;
        }

        public static void DrawText(Font aFont, String aText, int aPosX, int aPosY, ColorBGRA aColor)
        {
            aFont.DrawText(null, aText, aPosX + 2, aPosY + 2, aColor);
            aFont.DrawText(null, aText, aPosX, aPosY, aColor);

            //vFont.DrawText(null, vText, (int)vPosX, (int)vPosY, vColor);
        }
    }
}