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
    internal class Nautilus
    {
        public static Menu Menu, ComboMenu, Misc;
        public static int Stage = 0;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q, W, E, R;
        public static Spell Ignite;

        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Nautilus")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 1100f);
            Q.SetSkillshot(0.25f, 90f, 2000f, true, SpellType.Line);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 500f);
            R = new Spell(SpellSlot.R, 825f);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);

            var MenuRyze = new Menu("Nautilus", "[7UP]Nautilus", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQ", "Use Q").SetValue(true));
            ComboMenu.Add(new MenuBool("UseW", "Use W").SetValue(true));
            ComboMenu.Add(new MenuBool("UseE", "Use E").SetValue(true));
            ComboMenu.Add(new MenuBool("UseR", "Use R").SetValue(true));
            foreach (AIHeroClient enemy in GameObjects.EnemyHeroes)
                ComboMenu.Add(new MenuBool("nor" + enemy.CharacterName, String.Format("Don't Use R on {0}", enemy.CharacterName, false)));
            MenuRyze.Add(ComboMenu);
            Misc = new Menu("Misc Settings", "Misc");
            //Misc.Add(new MenuBool("tower", "Auto Q Under Tower"));
            Misc.Add(new MenuBool("interrupt", "Interrupt Spells"));
            Misc.Add(new MenuBool("antigapW", "AntiGapCloser with W"));
            Misc.Add(new MenuBool("antigapE", "AntiGapCloser with E"));
            MenuRyze.Add(Misc);
            MenuRyze.Attach();

            Game.OnUpdate += Game_OnUpdate;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Q.IsReady() && sender.IsValidTarget(Q.Range) && Misc["interrupt"].GetValue<MenuBool>().Enabled)
                Q.CastIfHitchanceEquals(sender, HitChance.High);
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (W.IsReady() && sender.IsValidTarget(Q.Range) && Misc["antigapW"].GetValue<MenuBool>().Enabled)
                W.Cast();

            if (E.IsReady() && sender.IsValidTarget(E.Range) && Misc["antigapE"].GetValue<MenuBool>().Enabled)
                E.Cast();
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
            }
        }

        public static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null || !target.IsValidTarget())
                return;

            if (R.IsReady() && ComboMenu["UseR"].GetValue<MenuBool>().Enabled && target.IsValidTarget(R.Range) && !ComboMenu["nor" + target.CharacterName].GetValue<MenuBool>().Enabled)
                R.CastOnUnit(target);


            if (Q.IsReady() && ComboMenu["UseQ"].GetValue<MenuBool>().Enabled && target.IsValidTarget(Q.Range))
            {
                Q.CastIfHitchanceEquals(target, HitChance.High);
            }

            if (W.IsReady() && target.IsValidTarget(W.Range) && ComboMenu["UseW"].GetValue<MenuBool>().Enabled)
                W.Cast();

            if (E.IsReady() && target.IsValidTarget(E.Range) && ComboMenu["UseE"].GetValue<MenuBool>().Enabled)
                E.Cast();
        }


        /*private static void UnderTower()
        {
            var Target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);

            if (LeagueSharp.Common.Utility.UnderTurret(Target, false) && Q.IsReady() && Config.Item("tower").GetValue<bool>() && Target.IsValidTarget(Q.Range))
            {
                var qpred = Q.GetPrediction(Target);
                if (qpred.Hitchance >= HitChance.High && qpred.CollisionObjects.Count(h => h.IsEnemy && !h.IsDead && h is Obj_AI_Minion) < 2)
                    Q.Cast(qpred.CastPosition);
            }
        }*/





        /*public static double QDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 80, 120, 140, 160, 180 }[Q.Level] + 1.1f * Player.FlatPhysicalDamageMod));

        }

        public static double WDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 60, 90, 120, 150, 180 }[W.Level] + 0.6f * Player.FlatPhysicalDamageMod));
        }

        public static double RDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 80, 120, 160 }[R.Level] + 0.8f * Player.FlatPhysicalDamageMod));
        }*/

    }
}

