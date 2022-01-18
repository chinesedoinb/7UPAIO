using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using Color = System.Drawing.Color;
using static EnsoulSharp.SDK.Items;
using SharpDX.Direct3D9;

namespace AIO7UP.Champions
{
    internal class Vayne
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, condemnMenu, drawMenu;

        public static Item Ward;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell Ignite;


        public static void OnGameLoad()
        {
            if (GameObjects.Player.CharacterName != "Vayne") return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R);

            E.SetTargetted(0.25f, 2200f);

            var MenuRyze = new Menu("Talon", "[7UP]Vayne", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQCombo", "Use Q"));
            ComboMenu.Add(new MenuBool("UseECombo", "Use E"));
            ComboMenu.Add(new MenuBool("UseRCombo", "Use R"));
            ComboMenu.Add(new MenuSlider("RComboEnemies", "Enemies to use R", 3, 1, 5));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("UseQHarass", "Use Q"));
            HarassMenu.Add(new MenuBool("UseEHarass", "Use E", false));
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("UseQWaveClear", "Use Q").SetValue(false));
            MenuRyze.Add(LaneClearMenu);
            condemnMenu = new Menu("Condemn Settings", "ConDAMM_Settings");
            condemnMenu.Add(new MenuSlider("EDistance", "E Push Distance", 400, 300, 600));
            condemnMenu.Add(new MenuKeyBind("Qout", "Q out AA", Keys.T, KeyBindType.Toggle, false));
            condemnMenu.Add(new MenuBool("QIntoE", "Q to E target").SetValue(true));
            condemnMenu.Add(new MenuBool("EPeel", "Peel with E").SetValue(false));
            condemnMenu.Add(new MenuBool("EKS", "Finish with E").SetValue(true));
            condemnMenu.Add(new MenuBool("GapcloseE", "E on Gapcloser").SetValue(true));
            condemnMenu.Add(new MenuBool("InterruptE", "E to Interrupt").SetValue(true));
            condemnMenu.Add(new MenuBool("Wardbush", "Ward bush on E").SetValue(true));
            MenuRyze.Add(condemnMenu);
            drawMenu = new Menu("Drawing", "Drawing");
            drawMenu.Add(new MenuBool("DrawQ", "Draw Q").SetValue(true));
            drawMenu.Add(new MenuBool("DrawE", "Draw E").SetValue(true));
            MenuRyze.Add(drawMenu);
            MenuRyze.Attach();
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;
            Orbwalker.OnBeforeAttack += Orbwalker_OnBeforeAttack;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2OnOnInterruptableTarget;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var PlayerPos = GameObjects.Player.Position;
            if (drawMenu["DrawQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                Render.Circle.DrawCircle(PlayerPos, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);
            }
            if (drawMenu["DrawE"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                Render.Circle.DrawCircle(PlayerPos, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    DoCombo();
                    return;
                case OrbwalkerMode.Harass:
                    DoHarass();                   
                    break;
            }
        }

        private static void Orbwalker_OnAfterAttack(object sender, AfterAttackEventArgs args)
        {
            var t = args.Target as AIHeroClient;
            var t2 = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (t != null)
            {
                if (E.IsReady() && condemnMenu["EKS"].GetValue<MenuBool>().Enabled)
                {
                    var dmgE = E.GetDamage(t);

                    if (GetWStacks(t) == 1)
                    {
                        dmgE += W.GetDamage(t);
                    }

                    if (dmgE > t.Health)
                    {
                        E.Cast(t2);
                    }
                }

            }

            var m = args.Target as AIMinionClient;

            if (m != null && Q.IsReady() && LaneClearMenu["UseQWaveClear"].GetValue<MenuBool>().Enabled)
            {
                var dashPosition = GameObjects.Player.Position.Extend(Game.CursorPos, Q.Range);


                if (LaneClearMenu["UseQWaveClear"].GetValue<MenuBool>().Enabled && m.Team == GameObjectTeam.Neutral)
                {
                    Q.Cast(dashPosition);
                }

                if (LaneClearMenu["UseQWaveClear"].GetValue<MenuBool>().Enabled)
                {
                    foreach (var minion in GameObjects.EnemyMinions.Where(e => e.InAutoAttackRange() && e.NetworkId != m.NetworkId))
                    {
                        var time = (int)(GameObjects.Player.AttackCastDelay * 1000) + Game.Ping / 2 + 1000 * (int)Math.Max(0, GameObjects.Player.Distance(minion) - GameObjects.Player.BoundingRadius) / (int)GameObjects.Player.BasicAttack.MissileSpeed;
                        var predHealth = HealthPrediction.GetPrediction(minion, time);

                        if (predHealth > 0 && predHealth < GameObjects.Player.GetAutoAttackDamage(minion) + Q.GetDamage(minion))
                        {
                            Q.Cast(dashPosition);
                        }
                    }
                }
            }
        }

        private static void Orbwalker_OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            if (ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled && GameObjects.Player.HasBuff("vaynetumblefade") && GameObjects.Player.CountEnemyHeroesInRange(800) > 1)
            {
                args.Process = false;
            }

            if (args.Target.Type != GameObjectType.AIHeroClient)
            {
                return;
            }

            var t = args.Target as AIHeroClient;

            if (GetWStacks(t) < 2 && args.Target.Health > 5 * GameObjects.Player.GetAutoAttackDamage(t))
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(800) && GetWStacks(e) == 2))
                {
                    if (target.InAutoAttackRange() && args.Target.Health > 3 * GameObjects.Player.GetAutoAttackDamage(target))
                    {
                        args.Process = false;
                        Orbwalker.ForceTarget = target;
                    }
                }
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (sender.IsValidTarget(E.Range) || !condemnMenu["GapcloseE"].GetValue<MenuBool>().Enabled)
            {
                return;
            }
            if (sender.IsAlly)
                return;

            if (args.SpellName == "ZedR")
                return;
            if (args.EndPosition.DistanceToPlayer() < args.StartPosition.DistanceToPlayer())
            {
                if (args.EndPosition.DistanceToPlayer() <= 300 && sender.IsValidTarget(550))
                {
                    if (E.Cast(sender) == CastStates.SuccessfullyCasted)
                        return;
                }
                else
                {
                    return;
                }
            }

            E.Cast(sender);
        }
        private static void Interrupter2OnOnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range) || args.DangerLevel == Interrupter.DangerLevel.Low)
            {
                return;
            }

            E.Cast(sender);
        }


        private static bool CanCondemnStun(AIBaseClient target, Vector3 startPos = default(Vector3), bool casting = true)
        {
            if (startPos == default(Vector3))
            {
                startPos = GameObjects.Player.Position;
            }

            var knockbackPos = startPos.Extend(
                target.Position,
                startPos.Distance(target.Position) + condemnMenu["EDistance"].GetValue<MenuSlider>().Value);

            var flags = NavMesh.GetCollisionFlags(knockbackPos);
            var collision = flags.HasFlag(CollisionFlags.Building) || flags.HasFlag(CollisionFlags.Wall);


            if (!casting || !condemnMenu["Wardbush"].GetValue<MenuBool>().Enabled)
            {
                return collision;
            }

            if (!condemnMenu["Wardbush"].GetValue<MenuBool>().Enabled)
            {
                return collision;
            }
            return collision;
        }
        private static void DoCombo()
        {
            var useQ = ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled;
            var useQout = condemnMenu["Qout"].GetValue<MenuKeyBind>().Active;
            var useE = ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled;
            var useEPeel = condemnMenu["EPeel"].GetValue<MenuBool>().Enabled;
            var qIntoE = condemnMenu["QIntoE"].GetValue<MenuBool>().Enabled;
            var useR = ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled;
            var useREnemies = ComboMenu["RComboEnemies"].GetValue<MenuSlider>().Value;
            var useEFinisher = condemnMenu["EKS"].GetValue<MenuBool>().Enabled;

            var target = TargetSelector.GetTarget(
                GameObjects.Player.GetRealAutoAttackRange(GameObjects.Player) + 300,
                DamageType.Physical);
            var target2 = TargetSelector.GetTarget(E.Range, DamageType.Physical);
            if (!target.IsValidTarget())
            {
                return;
            }

            if (useQ && Q.IsReady() && !CanCondemnStun(target, default(Vector3), false) && GetWStacks(target) == 2)
            {
                var dashPosition = GameObjects.Player.Position.Extend(Game.CursorPos, Q.Range);
                Q.Cast(dashPosition);
            }

            if (qIntoE && Q.IsReady() && E.IsReady() && !CanCondemnStun(target, default(Vector3), false))
            {
                var predictedPosition = GameObjects.Player.Position.Extend(Game.CursorPos, Q.Range);

                if (predictedPosition.Distance(target.Position) < E.Range
                    && CanCondemnStun(target, predictedPosition))
                {
                    Q.Cast(predictedPosition);
                    DelayAction.Add((int)(Q.Delay * 1000 + Game.Ping / 2f), () => E.Cast(target));
                }
            }

            if (Q.IsReady() && useQout && !Orbwalker.CanAttack()
                && GameObjects.Player.Distance(target) > GameObjects.Player.GetRealAutoAttackRange(GameObjects.Player))
            {
                Q.Cast(target.Position);
            }

            if (useE && E.IsReady() && CanCondemnStun(target))
            {
                E.Cast(target);
            }

            if (useEPeel && E.IsReady() && !GameObjects.Player.IsFacing(target))
            {
                E.Cast(target);
            }

            if (useR && R.IsReady() && GameObjects.Player.CountEnemyHeroesInRange(1000) >= useREnemies)
            {
                R.Cast();
            }
            if (useEFinisher && E.IsReady() && GameObjects.Player.GetSpellDamage(target, SpellSlot.E) > target.Health)
            {
                E.Cast(target2);
            }

        }
        private static void DoHarass()
        {
            var useQ = HarassMenu["UseQHarass"].GetValue<MenuBool>().Enabled;
            var useE = HarassMenu["UseEHarass"].GetValue<MenuBool>().Enabled;

            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (useQ && Q.IsReady() && GetWStacks(t) == 2)
            {
                var dashPosition = GameObjects.Player.Position.Extend(Game.CursorPos, Q.Range);
                Q.Cast(dashPosition);
            }

            if (useE && E.IsReady() && CanCondemnStun(target))
            {
                E.Cast(target);
            }
        }

        private static int GetWStacks(AIBaseClient target)
        {
            return target.GetBuffCount("VayneSilveredDebuff");
        }


    }
}