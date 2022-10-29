using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX.Direct3D9;

namespace AIO7UP.Champions
{
    internal class Ezreal
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, RMenu;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        //public static Dash dash;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell EQ;
        public static Spell Ignite;

        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Ezreal")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 55f, 2000f, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 1200f);
            W.SetSkillshot(0.25f, 55f, 1700f, false, SpellType.Line);
            E = new Spell(SpellSlot.E, 475f) { Delay = 0.65f };
            R = new Spell(SpellSlot.R, 5000f);
            R.SetSkillshot(1f, 160f, 2200f, false, SpellType.Line);

            EQ = new Spell(SpellSlot.Q, 1625f);
            EQ.SetSkillshot(0.90f, 60f, 1350f, true, SpellType.Line);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Ezreal", "[7UP]Ezreal", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("useQ", "Use Q"));
            ComboMenu.Add(new MenuBool("useW", "Use W"));
            ComboMenu.Add(new MenuBool("useE", "Use E"));
            ComboMenu.Add(new MenuBool("ComboECheck", "Use E |Safe Check"));
            ComboMenu.Add(new MenuBool("ComboEWall", "Use E |Wall Check"));
            ComboMenu.Add(new MenuBool("useR", "Use R"));
            ComboMenu.Add(new MenuKeyBind("SemiR", "Semi R", Keys.R, KeyBindType.Press));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("useQ", "Use Q"));
            HarassMenu.Add(new MenuBool("useW", "Use W"));
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "Lane Clear");
            LaneClearMenu.Add(new MenuBool("useQ", "Use Q"));
            LaneClearMenu.Add(new MenuBool("QLH", "Use Q Last Hit", false));
            LaneClearMenu.Add(new MenuSlider("ManaCL", "Mana Clear", 15));
            MenuRyze.Add(LaneClearMenu);
            JungleClearMenu = new Menu("Jungle Settings", "Jungle Clear");
            JungleClearMenu.Add(new MenuBool("useQ", "Use Q"));
            JungleClearMenu.Add(new MenuBool("useW", "Use W"));
            JungleClearMenu.Add(new MenuSlider("ManaCL", "Mana Clear", 15));
            MenuRyze.Add(JungleClearMenu);
            RMenu = new Menu("R Settings", "RMenu");
            RMenu.Add(new MenuBool("AutoR", "Auto R"));
            RMenu.Add(new MenuSlider("RRange", "Auto R |Min Cast Range >= x", 900, 0, 1500));
            RMenu.Add(new MenuSlider("RMaxRange", "Auto R |Max Cast Range >= x", 3000, 1500, 5000));
            MenuRyze.Add(RMenu);
            Misc = new Menu("Misc Settings", "Misc");
            //Misc.Add(new MenuBool("interrupter", "Interrupter"));
            Misc.Add(new MenuBool("gapcloser", "Gapcloser"));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuBool("killstealQ", "Use Q"));
            MenuRyze.Add(KillStealMenu);
            MenuRyze.Attach();
            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
            Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            //AIBaseClient.OnBuffAdd += OnBuffAdd;

        }

        public static void OnBeforeAttack(object sender, BeforeAttackEventArgs Args)
        {
            //var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var useQ = ComboMenu["useQ"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["useW"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["useE"].GetValue<MenuBool>().Enabled;
            var lanemana = LaneClearMenu["ManaCL"].GetValue<MenuSlider>().Value;
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.AIHeroClient:
                    {
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                        {
                            var target = (AIHeroClient)Args.Target;
                            if (target != null && target.IsValidTarget(W.Range))
                            {
                                if (useW && W.IsReady())
                                {
                                    var pred = W.GetPrediction(target);
                                    if (pred.Hitchance >= HitChance.High)
                                    {
                                        W.Cast(pred.CastPosition);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.AITurretClient:
                case GameObjectType.HQClient:
                case GameObjectType.Barracks:
                case GameObjectType.BarracksDampenerClient:
                case GameObjectType.BuildingClient:
                    break;
            }

        }

        public static void Orbwalker_OnAfterAttack(object e, AfterAttackEventArgs Args)
        {
            var useQ = ComboMenu["useQ"].GetValue<MenuBool>().Enabled;
            var QHarass = HarassMenu["useQ"].GetValue<MenuBool>().Enabled;

            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.AIHeroClient:
                    {
                        var target = (AIHeroClient)Args.Target;
                        if (target != null && target.IsValidTarget())
                        {
                            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                            {
                                if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(target);
                                    if (qPred.Hitchance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                            }
                            else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass ||
                                     Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                            {

                                if (QHarass && Q.IsReady() && target.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(target);
                                    if (qPred.Hitchance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                            }
                        }
                    }
                    break;
            }

        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs Args)
        {
            if (!Menu["Misc"].GetValue<MenuBool>("gapcloser").Enabled) return;

            if (!E.IsReady() || sender == null || !sender.IsValidTarget(E.Range)) return;
            if (sender.IsMelee)
                if (sender.IsValidTarget(sender.AttackRange + sender.BoundingRadius + 100))
                    E.Cast(Player.PreviousPosition.Extend(sender.PreviousPosition, -E.Range));

            if (sender.IsDashing())
                if (Args.EndPosition.DistanceToPlayer() <= 250 ||
                    sender.PreviousPosition.DistanceToPlayer() <= 300)
                    E.Cast(Player.PreviousPosition.Extend(sender.PreviousPosition, -E.Range));

            if (!sender.IsCastingImporantSpell()) return;
            if (sender.PreviousPosition.DistanceToPlayer() <= 300)
                E.Cast(Player.PreviousPosition.Extend(sender.PreviousPosition, -E.Range));
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            if (R.Level > 0)
            {
                R.Range = RMenu["RMaxRange"].GetValue<MenuSlider>().Value;
            }

            if (ComboMenu["SemiR"].GetValue<MenuKeyBind>().Active)
            {
                OneKeyCastR();
            }
            if (RMenu["AutoR"].GetValue<MenuBool>().Enabled && R.IsReady() && Player.CountEnemyHeroesInRange(1000) == 0)
            {
                AutoRLogic();
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    return;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }
            KillSteal();
            //SemiKey();

        }

        private static void OneKeyCastR()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (!R.IsReady())
            {
                return;
            }

            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            if (target.IsValidTarget(R.Range) && !target.IsValidTarget(RMenu["RRange"].GetValue<MenuSlider>().Value))
            {
                var rPred = R.GetPrediction(target);

                if (rPred.Hitchance >= HitChance.High)
                {
                    R.Cast(rPred.CastPosition);
                }
            }
        }

        private static void AutoRLogic()
        {
            foreach (
                var target in
                GameObjects.EnemyHeroes.Where(
                    x =>
                        x.IsValidTarget(R.Range) && x.DistanceToPlayer() >= RMenu["RRange"].GetValue<MenuSlider>().Value))
            {
                if (!target.CanMove && target.IsValidTarget(EQ.Range) &&
                    Player.GetSpellDamage(target, SpellSlot.R) + Player.GetSpellDamage(target, SpellSlot.Q) * 3 >=
                    target.Health + target.HPRegenRate * 2)
                {
                    R.Cast(target);
                }

                if (Player.GetSpellDamage(target, SpellSlot.R) > target.Health + target.HPRegenRate * 2 &&
                    target.Path.Length < 2 &&
                    R.GetPrediction(target).Hitchance >= HitChance.High)
                {
                    R.Cast(target);
                }
            }
        }


        /*private void OnBuffAdd(AIBaseClient sender, AIBaseClientBuffAddEventArgs args)
        {
            if (sender.IsMe && E.IsReady())
            {
                if (args.Buff.Name == "ThreshQ" || args.Buff.Name == "rocketgrab2" || args.Buff.Name == "PykeQ")
                {
                    var dashPos = dash.CastDash(true);
                    if (dashPos.IsValid())
                    {
                        E.Cast(dashPos);
                    }
                }
            }
        }*/
        public static void Combo()
        {
            var target = TargetSelector.GetTarget(EQ.Range, DamageType.Physical);
            var useQ = ComboMenu["useQ"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["useW"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["useE"].GetValue<MenuBool>().Enabled;
            var useR = ComboMenu["useR"].GetValue<MenuBool>().Enabled;

            if (target.IsValidTarget(EQ.Range))
            {
                if (useE && E.IsReady() && target.IsValidTarget(EQ.Range))
                {
                    ComboELogic(target);
                }

                if (useW && W.IsReady() && target.IsValidTarget(W.Range))
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.Hitchance >= HitChance.High)
                    {
                        if (Q.IsReady())
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.Hitchance >= HitChance.High)
                            {
                                W.Cast(qPred.CastPosition);
                            }
                        }

                        if (Orbwalker.CanAttack() && target.InAutoAttackRange())
                        {
                            W.Cast(wPred.CastPosition);
                        }
                    }
                }

                if (useQ && Q.IsReady() && target.IsValidTarget(Q.Range))
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }

                if (useR && R.IsReady())
                {
                    if (Player.IsUnderEnemyTurret() || Player.CountEnemyHeroesInRange(800) > 1)
                    {
                        return;
                    }

                    foreach (var rTarget in GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range) &&
                            x.DistanceToPlayer() >= RMenu["RRange"].GetValue<MenuSlider>().Value))
                    {
                        if (rTarget.Health < Player.GetSpellDamage(rTarget, SpellSlot.R) &&
                            R.GetPrediction(rTarget).Hitchance >= HitChance.High &&
                            rTarget.DistanceToPlayer() > Q.Range + E.Range / 2)
                        {
                            R.Cast(target);
                        }

                        if (rTarget.IsValidTarget(Q.Range + E.Range) &&
                            Player.GetSpellDamage(rTarget, SpellSlot.R) +
                            (Q.IsReady() ? Player.GetSpellDamage(rTarget, SpellSlot.Q) : 0) +
                            (W.IsReady() ? Player.GetSpellDamage(rTarget, SpellSlot.W) : 0) >
                            rTarget.Health + rTarget.HPRegenRate * 2)
                        {
                            R.Cast(rTarget);
                        }
                    }
                }
            }

        }

        private static void ComboELogic(AIHeroClient target)
        {
            var ECheck = ComboMenu["ComboECheck"].GetValue<MenuBool>().Enabled;
            var EWall = ComboMenu["ComboEWall"].GetValue<MenuBool>().Enabled;

            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (ECheck && !Player.IsUnderEnemyTurret() && Player.CountEnemyHeroesInRange(1200f) <= 2)
            {
                if (target.DistanceToPlayer() > Player.GetRealAutoAttackRange(target) && target.IsValidTarget())
                {
                    if (target.Health < Player.GetSpellDamage(target, SpellSlot.E) + Player.GetAutoAttackDamage(target) &&
                        target.PreviousPosition.Distance(Game.CursorPos) < Player.PreviousPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Player.PreviousPosition.Extend(target.PreviousPosition, 475f);

                        if (EWall)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Player.PreviousPosition.Extend(target.PreviousPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Player.PreviousPosition.Extend(target.PreviousPosition, 475f));
                        }
                        return;
                    }

                    if (target.Health <
                        Player.GetSpellDamage(target, SpellSlot.E) + Player.GetSpellDamage(target, SpellSlot.W) &&
                        W.IsReady() &&
                        target.PreviousPosition.Distance(Game.CursorPos) + 350 < Player.PreviousPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Player.PreviousPosition.Extend(target.PreviousPosition, 475f);

                        if (EWall)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Player.PreviousPosition.Extend(target.PreviousPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Player.PreviousPosition.Extend(target.PreviousPosition, 475f));
                        }
                        return;
                    }

                    if (target.Health <
                        Player.GetSpellDamage(target, SpellSlot.E) + Player.GetSpellDamage(target, SpellSlot.Q) &&
                        Q.IsReady() &&
                        target.PreviousPosition.Distance(Game.CursorPos) + 300 < Player.PreviousPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Player.PreviousPosition.Extend(target.PreviousPosition, 475f);

                        if (EWall)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Player.PreviousPosition.Extend(target.PreviousPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Player.PreviousPosition.Extend(target.PreviousPosition, 475f));
                        }
                    }
                }
            }

        }

        public static void Harass()
        {
            if (HarassMenu["useQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    var qPred = Q.GetPrediction(target);
                    if (qPred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }
            }
        }

        public static void LaneClear()
        {
            if (!LaneClearMenu["useQ"].GetValue<MenuBool>().Enabled || Player.ManaPercent < LaneClearMenu["ManaCL"].GetValue<MenuSlider>().Value)
                return;

            if (Q.IsReady())
            {
                if (LaneClearMenu["useQ"].GetValue<MenuBool>().Enabled)
                {
                    var preds = GameObjects.GetMinions(Player.Position, Q.Range).Where(i => Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= Q.GetDamage(i) && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                || i.Health > Player.GetAutoAttackDamage(i))).Select(y => Q.GetPrediction(y, false, -1, new CollisionObjects[] { CollisionObjects.Minions })).Where(i => i.Hitchance >= HitChance.High && i.CastPosition.DistanceToPlayer() <= Q.Range).ToList();
                    if (preds.Count > 0)
                    {
                        Q.Cast(preds.FirstOrDefault().CastPosition);
                    }
                }
            }
        }


        public static void JungleClear()
        {
            var useQ = JungleClearMenu["useQ"].GetValue<MenuBool>().Enabled;
            var useW = JungleClearMenu["useW"].GetValue<MenuBool>().Enabled;
            var mana = JungleClearMenu["ManaCL"].GetValue<MenuSlider>().Value;

            //var JcQq = Config["Clear"].GetValue<MenuBool>("JcQ");
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            var target = GameObjects.GetJungles(Player.ServerPosition, Q.Range);
            foreach (var obj in target)
            {
                if (useW && W.IsReady() && obj.IsValidTarget(W.Range))
                {
                    if (obj.GetJungleType() >= JungleType.Legendary)
                    {
                        var predpos = W.GetPrediction(obj, false, -1, new CollisionObjects[] { CollisionObjects.YasuoWall, CollisionObjects.Heroes });
                        if (predpos.Hitchance >= HitChance.High)
                        {
                            W.Cast(predpos.CastPosition);
                        }
                    }
                }

                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (useQ && Q.IsReady() && Player.Distance(mob.Position) < Q.Range) Q.Cast(mob.Position);
                }
            }
        }

        public static double QDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 20, 45, 70, 95, 120 }[Q.Level] + 1.3f * Player.FlatPhysicalDamageMod));

        }

        public static void KillSteal()
        {
            //if (Player.HasBuff("TalonEHop"))
            //{

            //}
            var KsQ = KillStealMenu["killstealQ"].GetValue<MenuBool>().Enabled;
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage") && !hero.HasBuff("FioraW") && !hero.HasBuff("BlitzcrankManaBarrierCO")))
            {
                if (KsQ && Q.IsReady())
                {
                    if (target != null)
                    {
                        if (Player.Distance(target) > 150)
                        {
                            if (target.Health + target.AllShield <= QDamage(target))
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(qPred.CastPosition);
                                }
                            }
                        }
                        else
                        {
                            if (target.Health + target.AllShield <= QDamage(target) * 1.5f)
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.Hitchance >= HitChance.High)
                                {
                                    Q.Cast(qPred.CastPosition);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
