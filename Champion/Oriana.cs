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
    internal class Orianna
    {
        public static Menu Config;

        private static Spell Q, W, E, R;
        
        private static Dictionary<string, string> InitiatorsList = new Dictionary<string, string>
        {
            {"aatroxq", "Aatrox"},
            {"akalishadowdance", "Akali"},
            {"headbutt", "Alistar"},
            {"bandagetoss", "Amumu"},
            {"dianateleport", "Diana"},
            {"ekkoe", "ekko"},
            {"elisespidereinitial", "Elise"},
            {"crowstorm", "FiddleSticks"},
            {"fioraq", "Fiora"},
            {"gnare", "Gnar"},
            {"gnarbige", "Gnar"},
            {"gragase", "Gragas"},
            {"hecarimult", "Hecarim"},
            {"ireliagatotsu", "Irelia"},
            {"jarvanivdragonstrike", "JarvanIV"},
            {"jaxleapstrike", "Jax"},
            {"riftwalk", "Kassadin"},
            {"katarinae", "Katarina"},
            {"kennenlightningrush", "Kennen"},
            {"khazixe", "KhaZix"},
            {"khazixelong", "KhaZix"},
            {"blindmonkqtwo", "LeeSin"},
            {"leonazenithblademissle", "Leona"},
            {"lissandrae", "Lissandra"},
            {"ufslash", "Malphite"},
            {"maokaiunstablegrowth", "Maokai"},
            {"monkeykingnimbus", "MonkeyKing"},
            {"monkeykingspintowin", "MonkeyKing"},
            {"nocturneparanoia", "Nocturne"},
            {"olafragnarok", "Olaf"},
            {"poppyheroiccharge", "Poppy"},
            {"renektonsliceanddice", "Renekton"},
            {"rengarr", "Rengar"},
            {"reksaieburrowed", "RekSai"},
            {"sejuaniarcticassault", "Sejuani"},
            {"shenshadowdash", "Shen"},
            {"shyvanatransformcast", "Shyvana"},
            {"shyvanatransformleap", "Shyvana"},
            {"sionr", "Sion"},
            {"taloncutthroat", "Talon"},
            {"threshqleap", "Thresh"},
            {"slashcast", "Tryndamere"},
            {"udyrbearstance", "Udyr"},
            {"urgotswap2", "Urgot"},
            {"viq", "Vi"},
            {"vir", "Vi"},
            {"volibearq", "Volibear"},
            {"infiniteduress", "Warwick"},
            {"yasuorknockupcombow", "Yasuo"},
            {"zace", "Zac"}
        };
        
        public class OriannaBallManager
        {
            public static Vector3 BallPosition { get; private set; }
            private static int _sTick = Variables.GameTimeTickCount;

            static OriannaBallManager()
            {
                EnsoulSharp.SDK.GameEvent.OnGameTick += Game_OnGameUpdate;
                AIHeroClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
                BallPosition = ObjectManager.Player.Position;
            }

            static void AIBaseClientProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                if (sender.IsMe)
                {
                    var objs = GameObjects.AllGameObjects.Where(x => x.Name == "Orianna_Base_Z_ball_glow_green");
                    switch (args.SData.Name)
                    {
                        case "OrianaIzunaCommand":
                            BallPosition = args.To;
                            _sTick = Variables.GameTimeTickCount;
                            break;

                        case "OrianaRedactCommand":
                            BallPosition = Vector3.Zero;
                            _sTick = Variables.GameTimeTickCount;
                            break;
                    }
                }
            }

            static void Game_OnGameUpdate(EventArgs args)
            {
                if (Variables.GameTimeTickCount - _sTick > 300 && ObjectManager.Player.HasBuff("orianaghostself"))
                {
                    BallPosition = ObjectManager.Player.Position;
                }

                foreach (var ally in GameObjects.AllyHeroes)
                {
                    if (ally.HasBuff("orianaghost"))
                    {
                        BallPosition = ally.Position;
                    }
                }
            }
        }


        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Orianna")
            {
                return;
            }
            
            Q = new Spell(SpellSlot.Q, 825f);
            W = new Spell(SpellSlot.W, 245);
            E = new Spell(SpellSlot.E, 1095);
            R = new Spell(SpellSlot.R, 325);

            Q.SetSkillshot(0f, 130f, 1400f, false, SpellType.Circle);
            W.SetSkillshot(0f, 250f, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(0f, 80f, 1700f, false, SpellType.Line);
            R.SetSkillshot(0.5f, 375f, float.MaxValue, false, SpellType.Circle);
            
            Config = new Menu("Orianna", "[7UP] Orianna", true);
            
            var combo = new Menu("Combo", "Combo");
            combo.Add(new MenuBool("UseQCombo", "Use Q", true));
            combo.Add(new MenuBool("UseWCombo", "Use W"));
            combo.Add(new MenuBool("UseECombo", "Use E"));
            combo.Add(new MenuBool("UseRCombo", "Use R"));
            combo.Add(new MenuSlider("UseRNCombo", "Use R on at least", 3, 1, 5));
            combo.Add(new MenuSlider("UseRImportant", "-> Or if hero priority >=", 5, 1, 5)); // 5 for e.g adc's
            combo.Add(new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press)).AddPermashow();
            Config.Add(combo);
            
            Menu Misc = new Menu("Misc", "Misc");
            Misc.Add(new MenuSlider("AutoW", "Auto W if it'll hit", 2, 1, 5));
            Misc.Add(new MenuSlider("AutoR", "Auto R if it'll hit", 3, 1, 5));
            Misc.Add(new MenuBool("AutoEInitiators", "Auto E initiators"));

            var InitiatorsMenu = new Menu("InitiatorsMenu", "Initiator's List");
            GameObjects.AllyHeroes.ForEach(
                delegate (AIHeroClient hero)
                {
                    InitiatorsList.ToList().ForEach(
                        delegate (KeyValuePair<string, string> pair)
                        {
                            if (string.Equals(hero.CharacterName, pair.Value, StringComparison.InvariantCultureIgnoreCase))
                            {
                                InitiatorsMenu.Add(new MenuBool(pair.Key, pair.Value + " - " + pair.Key));
                            }
                        });
                });

            Misc.Add(InitiatorsMenu);

            Misc.Add(new MenuBool("InterruptSpells", "Interrupt spells using R"));
            Misc.Add(new MenuBool("BlockR", "Block R if it won't hit", false));
            Config.Add(Misc);

            var Harass = new Menu("Harass", "Harass");
            Harass.Add(new MenuBool("UseQHarass", "Use Q"));
            Harass.Add(new MenuBool("UseWHarass", "Use W", false));
            Harass.Add(new MenuSlider("HarassManaCheck", "Don't harass if mana < %", 0, 0, 100));
            Harass.Add(new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press)).AddPermashow();
            Harass.Add(new MenuKeyBind("HarassActiveT", "Harass (toggle)!", Keys.Y, KeyBindType.Toggle)).AddPermashow();
            Config.Add(Harass);
            
            var Farm = new Menu("Farm", "Farm");
            Farm.Add(new MenuBool("EnabledFarm", "Enable! (On/Off: Mouse Scroll)")).AddPermashow();
            Farm.Add(new MenuList("UseQFarm", "Use Q", new[] { "Freeze", "LaneClear", "Both", "No" }, 2));
            Farm.Add(new MenuList("UseWFarm", "Use W", new[] { "Freeze", "LaneClear", "Both", "No" }, 1));
            Farm.Add(new MenuList("UseEFarm", "Use E", new[] { "Freeze", "LaneClear", "Both", "No" }, 1));
            Farm.Add(new MenuSlider("LaneClearManaCheck", "Don't LaneClear if mana < %", 0, 0, 100));

            Farm.Add(new MenuKeyBind("FreezeActive", "Freeze!", Keys.X, KeyBindType.Press)).AddPermashow();
            Farm.Add(new MenuKeyBind("LaneClearActive", "LaneClear!", Keys.S, KeyBindType.Press)).AddPermashow();
            Config.Add(Farm);
            
            var JungleFarm = new Menu("JungleFarm", "JungleFarm");
            JungleFarm.Add(new MenuBool("UseQJFarm", "Use Q"));
            JungleFarm.Add(new MenuBool("UseWJFarm", "Use W"));
            JungleFarm.Add(new MenuBool("UseEJFarm", "Use E"));
            JungleFarm.Add(new MenuKeyBind("JungleFarmActive", "JungleFarm!", Keys.S, KeyBindType.Press)).AddPermashow();
            Config.Add(JungleFarm);
            
            var Drawings = new Menu("Drawings", "Drawings");
            Drawings.Add(new MenuBool("QRange", "Q range"));
            Drawings.Add(new MenuBool("WRange", "W range"));
            Drawings.Add(new MenuBool("ERange", "E range"));
            Drawings.Add(new MenuBool("RRange", "R range"));
            Drawings.Add(new MenuBool("QOnBallRange", "Draw ball position"));
            Config.Add(Drawings);

            Config.Attach();

            EnsoulSharp.SDK.GameEvent.OnGameTick += OnGameUpdate;
            Game.OnWndProc += OnWndProc;
            //Drawing.OnDraw += OnDraw;
            Spellbook.OnCastSpell += OnCastSpell;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
        }
        
        private static void OnWndProc(GameWndEventArgs args)
        {
            if (args.Msg != 520)
                return;

            Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled = !Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled;
        }
        
        static void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!Config["Misc"].GetValue<MenuBool>("InterruptSpells").Enabled)
            {
                return;
            }

            if (args.DangerLevel <= Interrupter.DangerLevel.Medium)
            {
                return;
            }

            if (sender.IsAlly)
            {
                return;
            }

            if (R.IsReady())
            {
                Q.Cast(sender, true);
                if (OriannaBallManager.BallPosition.Distance(sender.Position) < R.Range * R.Range)
                {
                    R.Cast(ObjectManager.Player.Position);
                }
            }
        }
        
        static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!(sender is AIHeroClient))
            {
                return;
            }
            if(sender.Position.DistanceToPlayer() > E.Range)
            {
                return;
            }

            if (!Config["Misc"].GetValue<MenuBool>("AutoEInitiators").Enabled)
            {
                return;
            }

            var spellName = args.SData.Name.ToLower();
            if (!InitiatorsList.ContainsKey(spellName))
            {
                return;
            }

            var item = Config["Misc"]["InitiatorsMenu"].GetValue<MenuBool>(spellName);
            if (item == null || !item.Enabled)
            {
                return;
            }

            if (!E.IsReady())
            {
                return;
            }

            if (sender.IsAlly && ObjectManager.Player.Distance(sender) < E.Range * E.Range)
            {
                E.CastOnUnit(sender);
            }
        }

        static void OnCastSpell(Spellbook s, SpellbookCastSpellEventArgs a)
        {
            if (!Config["Misc"].GetValue<MenuBool>("BlockR").Enabled)
            {
                return;
            }

            if (a.Slot == SpellSlot.R && GetHits(R).Item1 == 0)
            {
                a.Process = false;
            }
        }

        private static float GetComboDamage(AIHeroClient target)
        {
            var result = 0f;
            if (Q.IsReady())
            {
                result += 2 * Q.GetDamage(target);
            }

            if (W.IsReady())
            {
                result += W.GetDamage(target);
            }

            if (R.IsReady())
            {
                result += R.GetDamage(target);
            }

            result += 2 * (float)ObjectManager.Player.GetAutoAttackDamage(target);

            return result;
        }

        private static Tuple<int, List<AIHeroClient>> GetHits(Spell spell)
        {
            var hits = new List<AIHeroClient>();
            var range = spell.Range * spell.Range;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget() && OriannaBallManager.BallPosition.Distance(h.Position) < range))
            {
                if (spell.WillHit(enemy, OriannaBallManager.BallPosition) && OriannaBallManager.BallPosition.Distance(enemy.Position) < spell.Width * spell.Width)
                {
                    hits.Add(enemy);
                }
            }
            return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
        }

        private static Tuple<int, List<AIHeroClient>> GetEHits(Vector3 to)
        {
            var hits = new List<AIHeroClient>();
            var oldERange = E.Range;
            E.Range = 10000; //avoid the range check
            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(2000)))
            {
                if (E.WillHit(enemy, to))
                {
                    hits.Add(enemy);
                }
            }
            E.Range = oldERange;
            return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
        }

        private static bool CastQ(AIHeroClient target)
        {
            var qPrediction = Q.GetPrediction(target);

            if (qPrediction.Hitchance < HitChance.VeryHigh)
            {
                return false;
            }

            if (E.IsReady())
            {
                var directTravelTime = OriannaBallManager.BallPosition.Distance(qPrediction.CastPosition) / Q.Speed;
                var bestEQTravelTime = float.MaxValue;

                AIHeroClient eqTarget = null;

                foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                {
                    var t = OriannaBallManager.BallPosition.Distance(ally.Position) / E.Speed + ally.Distance(qPrediction.CastPosition) / Q.Speed;
                    if (t < bestEQTravelTime)
                    {
                        eqTarget = ally;
                        bestEQTravelTime = t;
                    }
                }

                if (eqTarget != null && bestEQTravelTime < directTravelTime * 1.3f && (OriannaBallManager.BallPosition.Distance(eqTarget.Position) > 10000))
                {
                    E.CastOnUnit(eqTarget);
                    return true;
                }
            }

            if (!target.IsFacing(ObjectManager.Player) && target.Path.Count() >= 1) // target is running
            {
                var targetBehind = Q.GetPrediction(target).CastPosition +
                                   Vector3.Normalize(target.Position - OriannaBallManager.BallPosition) * target.MoveSpeed / 2;
                Q.Cast(targetBehind);
                return true;
            }

            Q.Cast(qPrediction.CastPosition);
            return true;
        }

        private static bool CastW(int minTargets)
        {
            var hits = GetHits(W);
            if (hits.Item1 >= minTargets)
            {
                W.Cast(ObjectManager.Player.Position);
                return true;
            }
            return false;
        }

        private static bool CastE(AIHeroClient target, int minTargets)
        {
            if (GetEHits(target.Position).Item1 >= minTargets)
            {
                E.CastOnUnit(target);
                return true;
            }
            return false;
        }

        private static bool CastR(int minTargets, bool prioriy = false)
        {
            if (GetHits(R).Item1 >= minTargets || prioriy && GetHits(R)
                    .Item2.Any(
                        hero =>
                            (int)TargetSelector.GetPriority(hero) >= Config["Combo"].GetValue<MenuSlider>("UseRImportant").Value))
            {
                R.Cast(ObjectManager.Player.Position);
                return true;
            }

            return false;
        }
        
        private static void Farm(bool laneClear)
        {
            if (!Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled)
            {
                return;
            }
            var allMinions = GameObjects.Minions.Where(x => x.IsValidTarget(Q.Range + W.Width)).Cast<AIBaseClient>().ToList();
            var rangedMinions = GameObjects.Minions.Where(x => x.IsValidTarget(Q.Range + W.Width) && x.IsRanged).Cast<AIBaseClient>().ToList();

            var useQi = Config["Farm"].GetValue<MenuList>("UseQFarm").SelectedValue;
            var useWi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;
            var useEi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;

            var useQ = (laneClear && (useQi == "LaneClear" || useQi == "Both")) || (!laneClear && (useQi == "Freeze" || useQi == "Both"));
            var useW = (laneClear && (useWi == "LaneClear" || useWi == "Both")) || (!laneClear && (useWi == "Freeze" || useWi == "Both"));
            var useE = (laneClear && (useEi == "LaneClear" || useEi == "Both")) || (!laneClear && (useEi == "Freeze" || useEi == "Both"));

            if (useQ && Q.IsReady())
            {
                if (useW)
                {
                    var qLocation = Q.GetCircularFarmLocation(allMinions, W.Range);
                    var q2Location = Q.GetCircularFarmLocation(rangedMinions, W.Range);
                    var bestLocation = (qLocation.MinionsHit > q2Location.MinionsHit + 1) ? qLocation : q2Location;

                    if (bestLocation.MinionsHit > 0)
                    {
                        Q.Cast(bestLocation.Position);
                        return;
                    }
                }
                else
                {
                    foreach (var minion in allMinions.Where(m => !m.InAutoAttackRange()))
                    {
                        if (HealthPrediction.GetPrediction(minion, Math.Max((int)(minion.Position.Distance(OriannaBallManager.BallPosition) / Q.Speed * 1000) - 100, 0)) < 50)
                        {
                            Q.Cast(minion.Position);
                            return;
                        }
                    }
                }
            }

            if (useW && W.IsReady())
            {
                var n = 0;
                var d = 0;
                foreach (var m in allMinions)
                {
                    if (m.Distance(OriannaBallManager.BallPosition) <= W.Range)
                    {
                        n++;
                        if (W.GetDamage(m) > m.Health)
                        {
                            d++;
                        }
                    }
                }
                if (n >= 3 || d >= 2)
                {
                    W.Cast(ObjectManager.Player.Position);
                    return;
                }
            }

            if (useE && E.IsReady())
            {
                if (E.GetLineFarmLocation(allMinions).MinionsHit >= 3)
                {
                    E.CastOnUnit(ObjectManager.Player);
                    return;
                }
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config["JungleFarm"].GetValue<MenuBool>("UseQJFarm").Enabled;
            var useW = Config["JungleFarm"].GetValue<MenuBool>("UseWJFarm").Enabled;
            var useE = Config["JungleFarm"].GetValue<MenuBool>("UseEJFarm").Enabled;

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth).Cast<AIBaseClient>().ToList();

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                var conditionUseW = useW && W.IsReady() && W.WillHit(mob.Position, OriannaBallManager.BallPosition);

                if (conditionUseW)
                {
                    W.Cast(ObjectManager.Player.Position);
                }
                if (useQ && Q.IsReady())
                {
                    Q.Cast(mob, true);
                }
                if (useE && E.IsReady() && !conditionUseW)
                {
                    var closestAlly = GameObjects.AllyHeroes
                        .Where(h => h.IsValidTarget(E.Range, false))
                        .MinOrDefault(h => h.Distance(mob));
                    if (closestAlly != null)
                    {
                        E.CastOnUnit(closestAlly);
                    }
                    else
                    {
                        E.CastOnUnit(ObjectManager.Player);
                    }
                }
            }
        }
        
        private static Tuple<int, Vector3> GetBestQLocation(AIHeroClient mainTarget)
        {
            var points = new List<Vector2>();
            var qPrediction = Q.GetPrediction(mainTarget);
            if (qPrediction.Hitchance < HitChance.VeryHigh)
            {
                return new Tuple<int, Vector3>(1, Vector3.Zero);
            }
            points.Add(qPrediction.UnitPosition.ToVector2());

            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(Q.Range + R.Range)))
            {
                var prediction = Q.GetPrediction(enemy);
                if (prediction.Hitchance >= HitChance.High)
                {
                    points.Add(prediction.UnitPosition.ToVector2());
                }
            }

            for (int j = 0; j < 5; j++)
            {
                var mecResult = Mec.GetMec(points);

                if (mecResult.Radius < (R.Range - 75) && points.Count >= 3 && R.IsReady())
                {
                    return new Tuple<int, Vector3>(3, mecResult.Center.ToVector3());
                }

                if (mecResult.Radius < (W.Range - 75) && points.Count >= 2 && W.IsReady())
                {
                    return new Tuple<int, Vector3>(2, mecResult.Center.ToVector3());
                }

                if (points.Count == 1)
                {
                    return new Tuple<int, Vector3>(1, mecResult.Center.ToVector3());
                }

                if (mecResult.Radius < Q.Width && points.Count == 2)
                {
                    return new Tuple<int, Vector3>(2, mecResult.Center.ToVector3());
                }

                float maxdist = -1;
                var maxdistindex = 1;
                for (var i = 1; i < points.Count; i++)
                {
                    var distance = Vector2.DistanceSquared(points[i], points[0]);
                    if (distance > maxdist || maxdist.CompareTo(-1) == 0)
                    {
                        maxdistindex = i;
                        maxdist = distance;
                    }
                }
                points.RemoveAt(maxdistindex);
            }

            return new Tuple<int, Vector3>(1, points[0].ToVector3());
        }
        
        static void Combo()
        {

            var target = TargetSelector.GetTarget(Q.Range + Q.Width, DamageType.Magical);

            if (target == null)
            {
                return;
            }

            var useQ = Config["Combo"].GetValue<MenuBool>("UseQCombo").Enabled;
            var useW = Config["Combo"].GetValue<MenuBool>("UseWCombo").Enabled;
            var useE = Config["Combo"].GetValue<MenuBool>("UseECombo").Enabled;
            var useR = Config["Combo"].GetValue<MenuBool>("UseRCombo").Enabled;

            var minRTargets = Config["Combo"].GetValue<MenuSlider>("UseRNCombo").Value;
            var EnemiesInQR = ObjectManager.Player.CountEnemyHeroesInRange((int)(Q.Range + R.Width));
            if (useW && W.IsReady())
            {
                CastW(1);
            }

            if (EnemiesInQR <= 1)
            {
                if (useR && GetComboDamage(target) > target.Health && R.IsReady())
                {
                    CastR(minRTargets, true);
                }

                if (useQ && Q.IsReady())
                {
                    CastQ(target);
                }

                if (useE && E.IsReady())
                {
                    foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                    {
                        if (ally.Position.CountEnemyHeroesInRange(300) >= 1)
                        {
                            E.CastOnUnit(ally);
                        }

                        CastE(ally, 1);
                    }
                }
            }
            else
            {
                if (useR && R.IsReady())
                {
                    if (OriannaBallManager.BallPosition.CountEnemyHeroesInRange(800) > 1)
                    {
                        var rCheck = GetHits(R);
                        var pk = 0;
                        var k = 0;
                        if (rCheck.Item1 >= 2)
                        {
                            foreach (var hero in rCheck.Item2)
                            {
                                var comboDamage = GetComboDamage(hero);
                                if ((hero.Health - comboDamage) < 0.4 * hero.MaxHealth || comboDamage >= 0.4 * hero.MaxHealth)
                                {
                                    pk++;
                                }

                                if ((hero.Health - comboDamage) < 0)
                                {
                                    k++;
                                }
                            }

                            if (rCheck.Item1 >= OriannaBallManager.BallPosition.CountEnemyHeroesInRange(800) || pk >= 2 ||
                                k >= 1)
                            {
                                if (rCheck.Item1 >= minRTargets)
                                {
                                    R.Cast(ObjectManager.Player.Position);
                                }
                            }
                        }
                    }

                    else if (GetComboDamage(target) > target.Health)
                    {
                        CastR(minRTargets, true);
                    }
                }

                if (useQ && Q.IsReady())
                {
                    var qLoc = GetBestQLocation(target);
                    if (qLoc.Item1 > 1)
                    {
                        Q.Cast(qLoc.Item2);
                    }
                    else
                    {
                        CastQ(target);
                    }
                }

                if (useE && E.IsReady())
                {
                    if (OriannaBallManager.BallPosition.CountEnemyHeroesInRange(800) <= 2)
                    {
                        CastE(ObjectManager.Player, 1);
                    }
                    else
                    {
                        CastE(ObjectManager.Player, 2);
                    }

                    foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                    {
                        if (ally.Position.CountEnemyHeroesInRange(300) >= 2)
                        {
                            E.CastOnUnit(ally);
                        }
                    }
                }
            }
            if (!Q.IsReady() && !W.IsReady() && !R.IsReady() && E.IsReady() && ObjectManager.Player.HealthPercent < 15 && EnemiesInQR > 0)
            {
                CastE(ObjectManager.Player, 0);
            }
        }
        
        static void Harass()
        {
            if (ObjectManager.Player.ManaPercent < Config["Harass"].GetValue<MenuSlider>("HarassManaCheck").Value)
                return;

            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target != null)
            {
                if (Config["Harass"].GetValue<MenuBool>("UseQHarass").Enabled && Q.IsReady())
                {
                    CastQ(target);
                    return;
                }

                if (Config["Harass"].GetValue<MenuBool>("UseWHarass").Enabled && W.IsReady())
                {
                    CastW(1);
                }
            }
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if (OriannaBallManager.BallPosition == Vector3.Zero)
            {
                return;
            }

            Q.From = OriannaBallManager.BallPosition;
            Q.RangeCheckFrom = ObjectManager.Player.Position;
            W.From = OriannaBallManager.BallPosition;
            W.RangeCheckFrom = OriannaBallManager.BallPosition;
            E.From = OriannaBallManager.BallPosition;
            R.From = OriannaBallManager.BallPosition;
            R.RangeCheckFrom = OriannaBallManager.BallPosition;

            var autoWminTargets = Config["Misc"].GetValue<MenuSlider>("AutoW").Value;
            if (autoWminTargets > 0)
            {
                CastW(autoWminTargets);
            }

            var autoRminTargets = Config["Misc"].GetValue<MenuSlider>("AutoR").Value;
            if (autoRminTargets > 0)
            {
                CastR(autoRminTargets);
            }

            if (Config["Combo"].GetValue<MenuKeyBind>("ComboActive").Active)
            {
                Combo();
            }
            else
            {
                if (Config["Harass"].GetValue<MenuKeyBind>("HarassActive").Active ||
                    (Config["Harass"].GetValue<MenuKeyBind>("HarassActiveT").Active && !ObjectManager.Player.HasBuff("Recall")))
                {
                    Harass();
                }

                var lc = Config["Farm"].GetValue<MenuKeyBind>("LaneClearActive").Active;
                if (lc || Config["Farm"].GetValue<MenuKeyBind>("FreezeActive").Active)
                {
                    Farm(lc && (ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana >= Config["Farm"].GetValue<MenuSlider>("LaneClearManaCheck").Value));
                }

                if (Config["JungleFarm"].GetValue<MenuKeyBind>("JungleFarmActive").Active)
                {
                    JungleFarm();
                }
            }
        }
        
        /*private static void OnDraw(EventArgs args)
        {
            var qCircle = Config["Drawings"].GetValue<MenuBool>("QRange");
            if (Config["Drawings"].GetValue<MenuBool>("QRange").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.DodgerBlue);
            }

            if (Config["Drawings"].GetValue<MenuBool>("WRange").Enabled)
            {
                Render.Circle.DrawCircle(OriannaBallManager.BallPosition, W.Range, System.Drawing.Color.DodgerBlue);
            }

            if (Config["Drawings"].GetValue<MenuBool>("ERange").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.DodgerBlue);
            }

            if (Config["Drawings"].GetValue<MenuBool>("RRange").Enabled)
            {
                Render.Circle.DrawCircle(OriannaBallManager.BallPosition, R.Range, System.Drawing.Color.DodgerBlue);
            }

            if (Config["Drawings"].GetValue<MenuBool>("QOnBallRange").Enabled)
            {
                Render.Circle.DrawCircle(OriannaBallManager.BallPosition, Q.Width, System.Drawing.Color.DodgerBlue, 5, true);
            }
        }*/
    }
}