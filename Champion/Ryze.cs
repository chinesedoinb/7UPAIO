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
    internal class Ryze
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, Drawings;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell Ignite;


        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Ryze")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 1000f);
            W = new Spell(SpellSlot.W, 615f);
            E = new Spell(SpellSlot.E, 615f);
            R = new Spell(SpellSlot.R, 1750f);
            Q.SetSkillshot(0.25f, 50, 1700f, true, SpellType.Line, HitChance.High);
            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Ryze", "[7UP]Ryze", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQOutRangeEW", "Use Q out range EW", true)).Permashow();
            ComboMenu.Add(new MenuBool("AACombo", "AA in Combo (On/Off: T)", false)).Permashow();
            ComboMenu.Add(new MenuBool("UseQCombo", "Use Q", true));
            ComboMenu.Add(new MenuBool("UseWCombo", "Use W"));
            ComboMenu.Add(new MenuBool("UseECombo", "Use E"));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("UseQHarass", "Use Q"));
            HarassMenu.Add(new MenuBool("UseWHarass", "Use W", false));
            HarassMenu.Add(new MenuBool("UseEHarass", "Use E", false));
            HarassMenu.Add(new MenuSlider("HarassManaCheck", "Don't harass if mana < %", 0, 0, 100));
            MenuRyze.Add(HarassMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("AutoW", "Auto W AntiGrapcloser"));
            MenuRyze.Add(Misc);
            Drawings = new Menu("KillSteal Settings", "KillSteal");
            Drawings.Add(new MenuBool("QRange", "Q range"));
            Drawings.Add(new MenuBool("WRange", "W range"));
            Drawings.Add(new MenuBool("ERange", "E range"));
            MenuRyze.Add(Drawings);
            MenuRyze.Attach();
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Orbwalker.OnBeforeAttack += Orbwalker_OnBeforeAttack;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Drawings["QRange"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.Orange, 1);
            }
            if (Drawings["WRange"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.Orange, 1);
            }
            if (Drawings["ERange"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, Color.Orange, 1);
            }
        }
        private static void Orbwalker_OnBeforeAttack(object e, BeforeAttackEventArgs args)
        {
            if (ComboMenu["AACombo"].GetValue<MenuBool>().Enabled)
            {
                args.Process = false;
            }
        }
        private static void Game_OnWndProc(GameWndEventArgs args)
        {
            if (args.Msg == (uint)Keys.T)
            {
                ComboMenu["AACombo"].GetValue<MenuBool>().Enabled = !ComboMenu["AACombo"].GetValue<MenuBool>().Enabled;
            }
        }
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {

            if (Misc["AutoW"].GetValue<MenuBool>().Enabled)
                return;
            var attacker = sender;
            if (attacker.IsValidTarget(W.Range))
            {
                if (attacker.HasBuff("ryzee"))
                {
                    W.Cast(attacker);
                }
                else
                {
                    E.Cast(attacker);
                    W.Cast(attacker);
                }
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
                    Combo();
                    return;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    //LaneClear();
                    //JungleClear();
                    break;
            }
        }


        static void Harass()
        {
            if (Player.ManaPercent < HarassMenu["HarassManaCheck"].GetValue<MenuSlider>().Value)
                return;

            var targetQ = TargetSelector.GetTargets(Q.Range, DamageType.Magical).Where(t => t.IsValidTarget(Q.Range)).OrderBy(x => 1 / x.Health).FirstOrDefault();
            var targetE = TargetSelector.GetTarget(E.Range + 150, DamageType.Magical);
            if (targetQ != null || targetE != null)
            {
                if (HarassMenu["UseQHarass"].GetValue<MenuBool>().Enabled && Q.IsReady() && targetQ != null)
                {
                    CastQ(targetQ);
                }
                if (HarassMenu["UseEHarass"].GetValue<MenuBool>().Enabled && E.IsReady() && targetE != null)
                {
                    CastE();
                }
                if (HarassMenu["UseWHarass"].GetValue<MenuBool>().Enabled && W.IsReady() && targetE != null)
                {
                    W.Cast(targetE);
                }
            }
        }



        private static void CastQ(AIBaseClient target)
        {
            if (target == null)
            {
                return;
            }
            var predQ = Q.GetPrediction(target);
            if (predQ.Hitchance == HitChance.Collision)
            {
                var colObj = predQ.CollisionObjects.Where(obj => obj.HasBuff("ryzee") && obj.Distance(target) <= 350 && obj.IsValidTarget(Q.Range)).OrderBy(obj => obj.DistanceToPlayer()).FirstOrDefault();
                if (colObj != null)
                {
                    Q.Cast(colObj);
                }
                else if (E.IsReady())
                {
                    CastE();
                }
                else if (predQ.Hitchance >= HitChance.Low)
                {
                    Q.Cast(predQ.UnitPosition);
                }

            }
            else if (predQ.Hitchance >= HitChance.Low)
            {
                Q.Cast(predQ.UnitPosition);
            }
        }
        private static void CastE()
        {
            var target = TargetSelector.GetTarget(E.Range + 350, DamageType.Magical);

            if (target == null)
            {
                return;
            }
            if (target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }
            else
            {
                var colNear = GameObjects.AttackableUnits.Where(obj => obj is AIBaseClient && obj.Distance(target) <= 300 && obj.IsValidTarget(E.Range)).OrderBy(obj => obj.DistanceToPlayer()).FirstOrDefault();
                if (colNear != null)
                {
                    E.Cast(colNear as AIBaseClient);
                }
            }
        }

        static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (ComboMenu["UseQOutRangeEW"].GetValue<MenuBool>().Enabled)
            {
                target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            }
            if (target == null)
            {
                return;
            }

            var useQ = ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled;

            if (Q.IsReady() && useQ)
            {
                CastQ(target);
            }
            else if (E.IsReady() && useE)
            {
                CastE();
            }
            else if (W.IsReady() && useW)
            {
                W.Cast(target);
            }
        }


    }
}

