using System;
using System.Collections.Generic;
using System.Linq;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;

namespace AIO7UP.Champions
{
    internal class Viktor
    {
        public static List<Spell> SpellList = new List<Spell>();
        private static Spell Q, W, E, R;
        private static Menu Config;
        
        public static Dictionary<string, MenuBool> boolLinks = new Dictionary<string, MenuBool>();
        public static Dictionary<string, MenuColor> circleLinks = new Dictionary<string, MenuColor>();
        public static Dictionary<string, MenuKeyBind> keyLinks = new Dictionary<string, MenuKeyBind>();
        public static Dictionary<string, MenuSlider> sliderLinks = new Dictionary<string, MenuSlider>();
        public static Dictionary<string, MenuList> stringLinks = new Dictionary<string, MenuList>();
        
        private static readonly int maxRangeE = 1225;
        private static readonly int lengthE = 700;
        private static readonly int speedE = 1050;
        private static readonly int rangeE = 525;
        private static int lasttick = 0;
        private static Vector3 GapCloserPos;
        
        private static bool AttacksEnabled
        {
            get
            {
                if (keyLinks["comboActive"].GetValue<MenuKeyBind>().Active)
                {
                    return ((!Q.IsReady() || ObjectManager.Player.Mana < Q.Instance.ManaCost) && (!E.IsReady() || ObjectManager.Player.Mana < E.Instance.ManaCost) && (!boolLinks["qAuto"].GetValue<MenuBool>().Enabled || ObjectManager.Player.HasBuff("viktorpowertransferreturn")));
                }
                else if (keyLinks["harassActive"].GetValue<MenuKeyBind>().Active)
                {
                    return ((!Q.IsReady() || ObjectManager.Player.Mana < Q.Instance.ManaCost) && (!E.IsReady() || ObjectManager.Player.Mana < E.Instance.ManaCost));
                }
                return true;
            }
        }

        
        private static void ProcessLink(string key, object value)
        {
            if (value is MenuList)
            {
                stringLinks.Add(key, (MenuList)value);
            }
            else if (value is MenuSlider)
            {
                sliderLinks.Add(key, (MenuSlider)value);
            }
            else if (value is MenuKeyBind)
            {
                keyLinks.Add(key, (MenuKeyBind)value);
            }
            else
            {
                boolLinks.Add(key, (MenuBool)value);
            }
        }
        
        private static MenuBool AddSpellDraw(Menu menu, SpellSlot slot)
        {
            MenuBool a;
            switch (slot)
            {
                case SpellSlot.Q:
                    a = new MenuBool("DrawQRange", "Draw Q Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawQColor", "^ Q Color", Color.Indigo));
                    return a;
                case SpellSlot.W:
                    a = new MenuBool("DrawWRange", "Draw W Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawWColor", "^ W Color", Color.Yellow));
                    return a;
                case SpellSlot.E:
                    a = new MenuBool("DrawERange", "Draw E Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawEColor", "^ E Color", Color.Green));
                    return a;
                case SpellSlot.Item1:
                    a = new MenuBool("DrawEMaxRange", "Draw E Max Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawEMaxColor", "^ E Max Color", Color.Green));
                    return a;
                case SpellSlot.R:
                    a = new MenuBool("DrawRRange", "Draw R Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawRColor", "^ R Color", Color.Gold));
                    return a;
            }

            return null;
        }

        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Viktor")
            {
                return;
            }
            
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, rangeE);
            R = new Spell(SpellSlot.R, 700);
            var Emax = new Spell(SpellSlot.E, 1025);
            
            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.Add(Emax);
            
            Q.SetTargetted(0.25f, 2000);
            W.SetSkillshot(0.25f, 300, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(0f, 80, speedE, false, SpellType.Line);
            R.SetSkillshot(0.25f, 300f, float.MaxValue, false, SpellType.Circle);

            Config = new Menu("Viktor", "[7UP]: Viktor", true);
            var subMenu = Config.Add(new Menu("Combo", "Combo"));
            
            ProcessLink("comboUseQ", subMenu.Add(new MenuBool("comboUseQ", "Use Q")));
            ProcessLink("comboUseW", subMenu.Add(new MenuBool("comboUseW", "Use W")));
            ProcessLink("comboUseE", subMenu.Add(new MenuBool("comboUseE", "Use E")));
            ProcessLink("comboUseR", subMenu.Add(new MenuBool("comboUseR", "Use R")));
            ProcessLink("qAuto", subMenu.Add(new MenuBool("qAuto", "Dont autoattack without passive")));
            ProcessLink("comboActive", subMenu.Add(new MenuKeyBind("comboActive", "Combo active", Keys.Space, KeyBindType.Press)));

            subMenu = Config.Add(new Menu("Rconfig", "R config"));
            ProcessLink("HitR", subMenu.Add(new MenuList("HitR", "Auto R if: ", new string[] { "1 target", "2 targets", "3 targets", "4 targets", "5 targets" }, 3)));
            ProcessLink("AutoFollowR", subMenu.Add(new MenuBool("AutoFollowR", "Auto Follow R")));
            ProcessLink("rTicks", subMenu.Add(new MenuSlider("rTicks", "Ultimate ticks to count", 2, 1, 14)));
            
            subMenu = subMenu.Add(new Menu("Ronetarget", "R one target"));
            ProcessLink("forceR", subMenu.Add(new MenuKeyBind("forceR", "Force R on target", Keys.T, KeyBindType.Press)));
            ProcessLink("rLastHit", subMenu.Add(new MenuBool("rLastHit", "1 target ulti")));
            foreach (var hero in GameObjects.EnemyHeroes)
            {
                ProcessLink("RU" + hero.CharacterName, subMenu.Add(new MenuBool("RU" + hero.CharacterName, "Use R on: " + hero.CharacterName)));
            }

            subMenu = Config.Add(new Menu("Testfeatures", "Test features"));
            ProcessLink("spPriority", subMenu.Add(new MenuBool("spPriority", "Prioritize kill over dmg")));
            
            subMenu = Config.Add(new Menu("Harass", "Harass"));
            ProcessLink("harassUseQ", subMenu.Add(new MenuBool("harassUseQ", "Use Q")));
            ProcessLink("harassUseE", subMenu.Add(new MenuBool("harassUseE", "Use E")));
            ProcessLink("harassMana", subMenu.Add(new MenuSlider("harassMana", "Mana usage in percent (%)", 30)));
            ProcessLink("eDistance", subMenu.Add(new MenuSlider("eDistance", "Harass range with E", maxRangeE, rangeE,maxRangeE)));
            ProcessLink("harassActive", subMenu.Add(new MenuKeyBind("harassActive", "Harass active", Keys.C, KeyBindType.Press)));
            
            subMenu = Config.Add(new Menu("WaveClear", "WaveClear"));
            ProcessLink("waveUseQ", subMenu.Add(new MenuBool("waveUseQ", "Use Q")));
            ProcessLink("waveUseE", subMenu.Add(new MenuBool("waveUseE", "Use E")));
            ProcessLink("waveNumE", subMenu.Add(new MenuSlider("waveNumE", "Minions to hit with E", 2, 1, 10)));
            ProcessLink("waveMana", subMenu.Add(new MenuSlider("waveMana", "Mana usage in percent (%)", 30)));
            ProcessLink("waveActive", subMenu.Add(new MenuKeyBind("waveActive", "WaveClear active", Keys.V, KeyBindType.Press)));
            ProcessLink("jungleActive", subMenu.Add(new MenuKeyBind("jungleActive", "JungleClear active", Keys.G, KeyBindType.Press)));

            subMenu = Config.Add(new Menu("LastHit", "LastHit"));
            ProcessLink("waveUseQLH", subMenu.Add(new MenuKeyBind("waveUseQLH", "Use Q", Keys.A, KeyBindType.Press)));
            
            subMenu = Config.Add(new Menu("Flee", "Flee"));
            ProcessLink("FleeActive", subMenu.Add(new MenuKeyBind("FleeActive", "Flee mode", Keys.Z, KeyBindType.Press)));
            
            subMenu = Config.Add(new Menu("Misc", "Misc"));
            ProcessLink("rInterrupt", subMenu.Add(new MenuBool("rInterrupt", "Use R to interrupt dangerous spells")));
            ProcessLink("wInterrupt", subMenu.Add(new MenuBool("wInterrupt", "Use W to interrupt dangerous spells")));
            ProcessLink("autoW", subMenu.Add(new MenuBool("autoW", "Use W to continue CC")));
            ProcessLink("miscGapcloser", subMenu.Add(new MenuBool("miscGapcloser", "Use W against gapclosers")));
            
            subMenu = Config.Add(new Menu("Drawings", "Drawings"));
            ProcessLink("drawRangeQ", AddSpellDraw(subMenu, SpellSlot.Q));
            ProcessLink("drawRangeW", AddSpellDraw(subMenu, SpellSlot.W));
            ProcessLink("drawRangeE", AddSpellDraw(subMenu, SpellSlot.E));
            ProcessLink("drawRangeR", AddSpellDraw(subMenu, SpellSlot.R));
            
            Config.Attach();
            
            GameEvent.OnGameTick += OnGameUpdate;
            //Drawing.OnDraw += OnDraw;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
            AntiGapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
            Orbwalker.OnNonKillableMinion += OnNonKillableMinion;
        }

        private static void OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            if (args.Target is AIHeroClient)
            {
                args.Process = AttacksEnabled;
            }
            else
            {
                args.Process = true;
            }
        }

        private static void OnNonKillableMinion(object sender, NonKillableMinionEventArgs args)
        {
            var target = args.Target as AIHeroClient;

            if (target != null && target.IsValidTarget())
            {
                QLastHit(target);
            }
        }
        
        private static void QLastHit(AIBaseClient minion)
        {
            bool castQ = ((keyLinks["waveUseQLH"].GetValue<MenuKeyBind>().Active) || boolLinks["waveUseQ"].GetValue<MenuBool>().Enabled && keyLinks["waveActive"].GetValue<MenuKeyBind>().Active);
            if (castQ)
            {
                var distance = ObjectManager.Player.Distance(minion);
                var t = 250 + (int)distance / 2;
                var preRyusungealth = HealthPrediction.GetPrediction(minion, t, 0);
                
                if (preRyusungealth > 0 && Q.GetDamage(minion) > minion.Health)
                {
                    Q.Cast(minion);
                }
            }
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            if (keyLinks["comboActive"].GetValue<MenuKeyBind>().Active)
                OnCombo();
            
            if (keyLinks["harassActive"].GetValue<MenuKeyBind>().Active)
                OnHarass();
            
            if (keyLinks["waveActive"].GetValue<MenuKeyBind>().Active)
                OnWaveClear();

            if (keyLinks["jungleActive"].GetValue<MenuKeyBind>().Active)
                OnJungleClear();

            if (keyLinks["FleeActive"].GetValue<MenuKeyBind>().Active)
                Flee();

            if (keyLinks["forceR"].GetValue<MenuKeyBind>().Active)
            {
                if (R.IsReady())
                {
                    List<AIHeroClient> ignoredchamps = new List<AIHeroClient>();

                    foreach (var hero in GameObjects.EnemyHeroes)
                    {
                        if (!boolLinks["RU" + hero.CharacterName].GetValue<MenuBool>().Enabled)
                        {
                            ignoredchamps.Add(hero);
                        }
                    }
                    
                    var RTarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                    if (RTarget.IsValidTarget())
                    {
                        R.Cast(RTarget);
                    }
                }

            }
            // Ultimate follow
            if (R.Instance.Name != "ViktorChaosStorm" && boolLinks["AutoFollowR"].GetValue<MenuBool>().Enabled && Variables.TickCount - lasttick > 0)
            {
                var stormT = TargetSelector.GetTarget(1100, DamageType.Magical);
                if (stormT != null)
                {
                    R.Cast(stormT.Position);
                    lasttick = Variables.TickCount + 500;
                }
            }
        }
        
        private static void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            var unit = args.Sender;
            if (args.DangerLevel >= Interrupter.DangerLevel.High && unit.IsEnemy)
            {
                var useW = boolLinks["wInterrupt"].GetValue<MenuBool>().Enabled;
                var useR = boolLinks["rInterrupt"].GetValue<MenuBool>().Enabled;

                if (useW && W.IsReady() && unit.IsValidTarget(W.Range) &&
                    (Game.Time + 1.5 + W.Delay) >= args.EndTime)
                {
                    if (W.Cast(unit) == CastStates.SuccessfullyCasted)
                        return;
                }
                else if (useR && unit.IsValidTarget(R.Range) && R.Instance.Name == "ViktorChaosStorm")
                {
                    R.Cast(unit);
                }
            }
        }
        
        private static void OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsAlly)
            {
                return;
            }
            
            if (boolLinks["miscGapcloser"].GetValue<MenuBool>().Enabled && W.IsInRange(args.EndPosition) && sender.IsEnemy && args.EndPosition.DistanceToPlayer() < 200)
            {
                GapCloserPos = args.EndPosition;
                if (args.StartPosition.Distance(args.EndPosition) > sender.Spellbook.GetSpell(args.Slot).SData.CastRangeDisplayOverride && sender.Spellbook.GetSpell(args.Slot).SData.CastRangeDisplayOverride > 100)
                {
                    GapCloserPos = args.StartPosition.Extend(args.EndPosition, sender.Spellbook.GetSpell(args.Slot).SData.CastRangeDisplayOverride);
                }
                
                W.Cast(GapCloserPos.ToVector2());
            }
        }
        
        private static void Flee()
        {
            Orbwalker.Move(Game.CursorPos);
            
            if (!Q.IsReady() || !(ObjectManager.Player.HasBuff("viktorqaug") || ObjectManager.Player.HasBuff("viktorqeaug") || ObjectManager.Player.HasBuff("viktorqwaug") || ObjectManager.Player.HasBuff("viktorqweaug")))
            {
                return;
            }
            
            var closestminion = GameObjects.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy).MinOrDefault(m => ObjectManager.Player.Distance(m));
            var closesthero = GameObjects.EnemyHeroes.MinOrDefault(m => ObjectManager.Player.Distance(m) < Q.Range);
            
            if (closestminion.IsValidTarget(Q.Range))
            {
                Q.Cast(closestminion);
            }
            else if (closesthero.IsValidTarget(Q.Range))
            {
                Q.Cast(closesthero);
            }
        }
        
        private static void AutoW()
        {
            if (!W.IsReady() || !boolLinks["autoW"].GetValue<MenuBool>().Enabled)
                return;

            var tPanth = GameObjects.EnemyHeroes.Find(h => h.IsValidTarget(W.Range) && h.HasBuff("Pantheon_GrandSkyfall_Jump"));
            if (tPanth != null)
            {
                if (W.Cast(tPanth) == CastStates.SuccessfullyCasted)
                    return;
            }

            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(W.Range)))
            {
                if (enemy.HasBuff("rocketgrab2"))
                {
                    var t = ObjectManager.Get<AIHeroClient>().Where(i => i.IsAlly).ToList().Find(h => h.CharacterName.ToLower() == "blitzcrank" && h.Distance((AttackableUnit)ObjectManager.Player) < W.Range);
                    if (t != null)
                    {
                        if (W.Cast(t) == CastStates.SuccessfullyCasted)
                            return;
                    }
                }
                if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                         enemy.IsStunned || enemy.IsRecalling())
                {
                    if (W.Cast(enemy) == CastStates.SuccessfullyCasted)
                        return;
                }
                if (W.GetPrediction(enemy).Hitchance == HitChance.Immobile)
                {
                    if (W.Cast(enemy) == CastStates.SuccessfullyCasted)
                        return;
                }
            }
        }
        
        /*private static void OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;
            
            foreach (var spell in SpellList)
            {
                var menuBool = Config["Draw" + spell.Slot + "Range"].GetValue<MenuBool>().Enabled;
                var menuColor = Config["Draw" + spell.Slot + "Color"].GetValue<MenuColor>();
                
                if (menuBool)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuColor.Color.ToSystemColor());
                }
            }
        }*/

        private static void OnCombo()
        {
            var useQ = boolLinks["comboUseQ"].GetValue<MenuBool>().Enabled && Q.IsReady();
            var useW = boolLinks["comboUseW"].GetValue<MenuBool>().Enabled && W.IsReady();
            var useE = boolLinks["comboUseE"].GetValue<MenuBool>().Enabled && E.IsReady();
            var useR = boolLinks["comboUseR"].GetValue<MenuBool>().Enabled && R.IsReady();

            var killpriority = boolLinks["spPriority"].GetValue<MenuBool>().Enabled && R.IsReady();
            var rKillSteal = boolLinks["rLastHit"].GetValue<MenuBool>().Enabled;
            var Etarget = TargetSelector.GetTarget(maxRangeE, DamageType.Magical);
            var Qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var RTarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            
            if (killpriority && Qtarget != null & Etarget != null && Etarget != Qtarget &&
                ((Etarget.Health > TotalDmg(Etarget, false, true, false, false)) ||
                 (Etarget.Health > TotalDmg(Etarget, false, true, true, false) && Etarget == RTarget)) &&
                Qtarget.Health < TotalDmg(Qtarget, true, true, false, false))
            {
                Etarget = Qtarget;
            }

            if (RTarget != null && rKillSteal && useR && boolLinks["RU" + RTarget.CharacterName].GetValue<MenuBool>().Enabled)
            {
                if (TotalDmg(RTarget, true, true, false, false) < RTarget.Health &&
                    TotalDmg(RTarget, true, true, true, true) > RTarget.Health)
                {
                    R.Cast(RTarget.Position);
                }
            }
            
            if (useE)
            {
                if (Etarget != null)
                    PredictCastE(Etarget);
            }

            if (useQ)
            {
                if (Qtarget != null)
                    Q.Cast(Qtarget);
            }

            if (useW)
            {
                var t = TargetSelector.GetTarget(W.Range, DamageType.Magical);

                if (t != null)
                {
                    if (t.Path.Count() < 2)
                    {
                        if (t.HasBuffOfType(BuffType.Slow))
                        {
                            if (W.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                                if (W.Cast(t) == CastStates.SuccessfullyCasted)
                                    return;
                        }

                        if (t.CountEnemyHeroesInRange(250) > 2)
                        {
                            if (W.GetPrediction(t).Hitchance >= HitChance.VeryHigh)
                                if (W.Cast(t) == CastStates.SuccessfullyCasted)
                                    return;
                        }
                    }
                }
            }

            if (useR && R.Instance.Name == "ViktorChaosStorm" && ObjectManager.Player.CanCast && !ObjectManager.Player.Spellbook.IsCastingSpell)
            {
                foreach (var unit in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(R.Range)))
                {
                    R.CastIfWillHit(unit,
                        Array.IndexOf(stringLinks["HitR"].GetValue<MenuList>().Items,
                            stringLinks["HitR"].GetValue<MenuList>().SelectedValue) + 1);
                }
            }
        }

        private static void OnHarass()
        {
            if (ObjectManager.Player.ManaPercent < sliderLinks["harassMana"].GetValue<MenuSlider>().Value)
                return;
            
            bool useE = boolLinks["harassUseE"].GetValue<MenuBool>().Enabled && E.IsReady();
            bool useQ = boolLinks["harassUseQ"].GetValue<MenuBool>().Enabled && Q.IsReady();
            
            if (useQ)
            {
                var qtarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (qtarget != null)
                    Q.Cast(qtarget);
            }
            if (useE)
            {
                var harassrange = sliderLinks["eDistance"].GetValue<MenuSlider>().Value;
                var target = TargetSelector.GetTarget(harassrange, DamageType.Magical);

                if (target != null)
                    PredictCastE(target);
            }
        }
        
        private static void OnWaveClear()
        {
            // Mana check
            if (ObjectManager.Player.ManaPercent < sliderLinks["waveMana"].GetValue<MenuSlider>().Value)
                return;

            bool useQ = boolLinks["waveUseQ"].GetValue<MenuBool>().Enabled && Q.IsReady();
            bool useE = boolLinks["waveUseE"].GetValue<MenuBool>().Enabled && E.IsReady();

            if (useQ)
            {
                foreach (var minion in GameObjects.GetMinions(ObjectManager.Player.Position, ObjectManager.Player.AttackRange))
                {
                    if (Q.GetDamage(minion) > minion.Health && minion.CharacterName.Contains("Siege"))
                    {
                        QLastHit(minion);
                        break;
                    }
                }
            }

            if (useE)
                PredictCastMinionE();
        }

        private static void OnJungleClear()
        {
            // Mana check
            if (ObjectManager.Player.ManaPercent < sliderLinks["waveMana"].GetValue<MenuSlider>().Value)
                return;

            bool useQ = boolLinks["waveUseQ"].GetValue<MenuBool>().Enabled && Q.IsReady();
            bool useE = boolLinks["waveUseE"].GetValue<MenuBool>().Enabled && E.IsReady();

            if (useQ)
            {
                foreach (var minion in GameObjects.Jungle.Where(x => x.IsValidTarget(ObjectManager.Player.AttackRange)).OrderBy(x => x.MaxHealth).ToList())
                {
                    Q.Cast(minion);
                }
            }

            if (useE)
                PredictCastMinionEJungle();
        }
        
        private static bool PredictCastMinionE()
        {
            var farmLoc = GetBestLaserFarmLocation(false);
            if (farmLoc.MinionsHit > 0)
            {
                CastE(farmLoc.Position1, farmLoc.Position2);
                return true;
            }

            return false;
        }
        
        private static bool PredictCastMinionEJungle()
        {
            var farmLocation = GetBestLaserFarmLocation(true);

            if (farmLocation.MinionsHit > 0)
            {
                CastE(farmLocation.Position1, farmLocation.Position2);
                return true;
            }

            return false;
        }
        
        public static FarmLocation GetBestLaserFarmLocation(bool jungle)
        {
            var bestendpos = new SharpDX.Vector2();
            var beststartpos = new SharpDX.Vector2();
            var minionCount = 0;
            List<AIBaseClient> allminions;
            var minimalhit = sliderLinks["waveNumE"].GetValue<MenuSlider>().Value;
            
            if (!jungle)
            {
                allminions = GameObjects.GetMinions(maxRangeE).ToList();
            }
            else
            {
                allminions = GameObjects.Jungle.Where(x => x.IsValidTarget(maxRangeE)).ToList<AIBaseClient>();
            }
            
            var minionslist = (from mnion in allminions select mnion.Position.ToVector2()).ToList<SharpDX.Vector2>();
            var posiblePositions = new List<SharpDX.Vector2>();
            posiblePositions.AddRange(minionslist);
            var max = posiblePositions.Count;
            for (var i = 0; i < max; i++)
            {
                for (var j = 0; j < max; j++)
                {
                    if (posiblePositions[j] != posiblePositions[i])
                    {
                        posiblePositions.Add((posiblePositions[j] + posiblePositions[i]) / 2);
                    }
                }
            }

            foreach (var startposminion in allminions.Where(m => ObjectManager.Player.Distance(m) < rangeE))
            {
                var startPos = startposminion.Position.ToVector2();

                foreach (var pos in posiblePositions)
                {
                    if (pos.Distance(startPos) <= lengthE * lengthE)
                    {
                        var endPos = startPos + lengthE * (pos - startPos).Normalized();

                        var count =
                            minionslist.Count(pos2 => pos2.Distance(startPos, endPos, true) <= 140 * 140);

                        if (count >= minionCount)
                        {
                            bestendpos = endPos;
                            minionCount = count;
                            beststartpos = startPos;
                        }

                    }
                }
            }
            if ((!jungle && minimalhit < minionCount) || (jungle && minionCount > 0))
            {
                return new FarmLocation(beststartpos, bestendpos, minionCount);
            }
            else
            {
                return new FarmLocation(beststartpos, bestendpos, 0);
            }
        }
        
        
        public struct FarmLocation
        {
            public int MinionsHit;
            public Vector2 Position1;
            public Vector2 Position2;
            public FarmLocation(Vector2 startpos, Vector2 endpos, int minionsHit)
            {
                Position1 = startpos;
                Position2 = endpos;
                MinionsHit = minionsHit;
            }
        }
        private static void PredictCastE(AIHeroClient target)
        {
            var inRange = SharpDX.Vector2.DistanceSquared(target.Position.ToVector2(), ObjectManager.Player.Position.ToVector2()) < E.Range * E.Range;
            PredictionOutput prediction;
            var spellCasted = false;
            Vector3 pos1, pos2;
            
            var nearChamps = (from champ in ObjectManager.Get<AIHeroClient>() where champ.IsValidTarget(maxRangeE) && target != champ select champ).ToList();
            var innerChamps = new List<AIHeroClient>();
            var outerChamps = new List<AIHeroClient>();
            
            foreach (var champ in nearChamps)
            {
                if (SharpDX.Vector2.DistanceSquared(champ.Position.ToVector2(), ObjectManager.Player.Position.ToVector2()) < E.Range * E.Range)
                    innerChamps.Add(champ);
                else
                    outerChamps.Add(champ);
            }
            
            var nearMinions = GameObjects.GetMinions(ObjectManager.Player.Position, maxRangeE);
            var innerMinions = new List<AIBaseClient>();
            var outerMinions = new List<AIBaseClient>();
            foreach (var minion in nearMinions)
            {
                if (SharpDX.Vector2.DistanceSquared(minion.Position.ToVector2(), ObjectManager.Player.Position.ToVector2()) < E.Range * E.Range)
                    innerMinions.Add(minion);
                else
                    outerMinions.Add(minion);
            }
            
            if (inRange)
            {
                E.Speed = speedE * 0.9f;
                E.From = target.Position + (SharpDX.Vector3.Normalize(ObjectManager.Player.Position - target.Position) * (lengthE * 0.1f));
                prediction = E.GetPrediction(target);
                E.From = ObjectManager.Player.Position;

                if (prediction.CastPosition.Distance(ObjectManager.Player.Position) < E.Range)
                    pos1 = prediction.CastPosition;
                else
                {
                    pos1 = target.Position;
                    E.Speed = speedE;
                }
                
                E.From = pos1;
                E.RangeCheckFrom = pos1;
                E.Range = lengthE;
                
                if (nearChamps.Count > 0)
                {
                    var closeToPrediction = new List<AIHeroClient>();
                    foreach (var enemy in nearChamps)
                    {
                        prediction = E.GetPrediction(enemy);
                        
                        if (prediction.Hitchance >= HitChance.High && SharpDX.Vector2.DistanceSquared(pos1.ToVector2(), prediction.CastPosition.ToVector2()) < (E.Range * E.Range) * 0.8)
                            closeToPrediction.Add(enemy);
                    }
                    
                    if (closeToPrediction.Count > 0)
                    {
                        if (closeToPrediction.Count > 1)
                            closeToPrediction.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));
                        
                        prediction = E.GetPrediction(closeToPrediction[0]);
                        pos2 = prediction.CastPosition;
                        
                        CastE(pos1, pos2);
                        spellCasted = true;
                    }
                }
                
                if (!spellCasted)
                {
                    CastE(pos1, E.GetPrediction(target).CastPosition);
                }
                
                E.Speed = speedE;
                E.Range = rangeE;
                E.From = ObjectManager.Player.Position;
                E.RangeCheckFrom = ObjectManager.Player.Position;
            }
            else
            {
                float startPointRadius = 150;
                
                SharpDX.Vector3 startPoint = ObjectManager.Player.Position + SharpDX.Vector3.Normalize(target.Position - ObjectManager.Player.Position) * rangeE;
                
                var targets = (from champ in nearChamps where SharpDX.Vector2.DistanceSquared(champ.Position.ToVector2(), startPoint.ToVector2()) < startPointRadius * startPointRadius && SharpDX.Vector2.DistanceSquared(ObjectManager.Player.Position.ToVector2(), champ.Position.ToVector2()) < rangeE * rangeE select champ).ToList();
                if (targets.Count > 0)
                {
                    if (targets.Count > 1)
                        targets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));
                    
                    pos1 = targets[0].Position;
                }
                else
                {
                    var minionTargets = (from minion in nearMinions where SharpDX.Vector2.DistanceSquared(minion.Position.ToVector2(), startPoint.ToVector2()) < startPointRadius * startPointRadius && SharpDX.Vector2.DistanceSquared(ObjectManager.Player.Position.ToVector2(), minion.Position.ToVector2()) < rangeE * rangeE select minion).ToList();
                    if (minionTargets.Count > 0)
                    {
                        if (minionTargets.Count > 1)
                            minionTargets.Sort((enemy1, enemy2) => enemy2.Health.CompareTo(enemy1.Health));
                        
                        pos1 = minionTargets[0].Position;
                    }
                    else
                        pos1 = startPoint;
                }
                
                E.From = pos1;
                E.Range = lengthE;
                E.RangeCheckFrom = pos1;
                prediction = E.GetPrediction(target);
                
                if (prediction.Hitchance >= HitChance.High)
                    CastE(pos1, prediction.CastPosition);
                
                E.Range = rangeE;
                E.From = ObjectManager.Player.Position;
                E.RangeCheckFrom = ObjectManager.Player.Position;
            }
        }
        
        private static void CastE(SharpDX.Vector3 source, SharpDX.Vector3 destination)
        {
            E.Cast(source, destination);
        }

        private static void CastE(SharpDX.Vector2 source, SharpDX.Vector2 destination)
        {
            E.Cast(source, destination);
        }
        
        private static float TotalDmg(AIBaseClient enemy, bool useQ, bool useE, bool useR, bool qRange)
        {
            var qaaDmg = new Double[] { 20, 40, 60, 80, 100 };
            var damage = 0d;
            var rTicks = sliderLinks["rTicks"].GetValue<MenuSlider>().Value;
            bool inQRange = ((qRange && enemy.InAutoAttackRange()) || qRange == false);
            
            if (useQ && Q.IsReady() && inQRange)
            {
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);
                damage += ObjectManager.Player.CalculateDamage(enemy, DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * ObjectManager.Player.TotalMagicalDamage + ObjectManager.Player.TotalAttackDamage);
            }
            
            if (useQ && !Q.IsReady() && ObjectManager.Player.HasBuff("viktorpowertransferreturn") && inQRange)
            {
                damage += ObjectManager.Player.CalculateDamage(enemy, DamageType.Magical, qaaDmg[Q.Level - 1] + 0.5 * ObjectManager.Player.TotalMagicalDamage + ObjectManager.Player.TotalAttackDamage);
            }
            
            if (useE && E.IsReady())
            {
                if (ObjectManager.Player.HasBuff("viktoreaug") || ObjectManager.Player.HasBuff("viktorqeaug") || ObjectManager.Player.HasBuff("viktorqweaug"))
                    damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E);
                else
                    damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E);
            }
            
            if (useR && R.Level > 0 && R.IsReady() && R.Instance.Name == "ViktorChaosStorm")
            {
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R) * rTicks;
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);
            }
            
            if (Items.HasItem(ObjectManager.Player, ItemId.Ludens_Tempest))
                damage += ObjectManager.Player.CalculateDamage(enemy, DamageType.Magical, 100 + ObjectManager.Player.FlatMagicDamageMod * 0.1);
            
            if (Items.HasItem(ObjectManager.Player, ItemId.Sheen))
                damage += ObjectManager.Player.CalculateDamage(enemy, DamageType.Physical, 0.5 * ObjectManager.Player.BaseAttackDamage);
            
            if (Items.HasItem(ObjectManager.Player, ItemId.Lich_Bane))
                damage += ObjectManager.Player.CalculateDamage(enemy, DamageType.Magical, 0.5 * ObjectManager.Player.FlatMagicDamageMod + 0.75 * ObjectManager.Player.BaseAttackDamage);

            return (float)damage;
        }
    }
}