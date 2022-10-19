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
using EnsoulSharp.SDK.Core;

namespace AIO7UP.Champions
{
    internal class Taric
    {
        public static Menu Menu, ComboMenu, HealMenu, Misc, draw;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q, W, E, R;
        public static Spell Ignite, Flash;

        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Taric")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 750f);
            W = new Spell(SpellSlot.W, 800);
            E = new Spell(SpellSlot.E, 575);
            R = new Spell(SpellSlot.R, 400);
            E.SetSkillshot(140, int.MaxValue, 85, false, SpellType.Line);
            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            Flash = new Spell(ObjectManager.Player.GetSpellSlot("summonerflash"));

            var MenuRyze = new Menu("Taric", "[7UP]Taric", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQCombo", "Use Q").SetValue(true));
            ComboMenu.Add(new MenuBool("UseWCombo", "Use W").SetValue(true));
            ComboMenu.Add(new MenuBool("UseECombo", "Use E").SetValue(true));
            ComboMenu.Add(new MenuBool("UseRCombo", "Use R").SetValue(false));
            ComboMenu.Add(new MenuSlider("Rmin", "Min. Enemies in range for R", 3, 1, 5));
            MenuRyze.Add(ComboMenu);
            HealMenu = new Menu("Heal Settings", "Heal Settings");
            HealMenu.Add(new MenuBool("ElEasy.Taric.Heal.Activated", "Heal").SetValue(true));
            HealMenu.Add(new MenuSlider("ElEasy.Taric.Heal.Player.HP", "Player HP", 55));
            HealMenu.Add(new MenuSlider("ElEasy.Taric.Heal.Ally.HP", "Ally HP", 55));
            HealMenu.Add(new MenuSlider("ElEasy.Taric.Heal.Player.Mana", "Minimum Mana", 20));
            MenuRyze.Add(HealMenu);
            Misc = new Menu("Misc Settings", "Misc");
            //Misc.Add(new MenuBool("tower", "Auto Q Under Tower"));
            Misc.Add(new MenuBool("EAntiGapcloser", "E AntiGapcloser"));
            Misc.Add(new MenuBool("EInterrupetSpell", "E InterruptSpell"));
            Misc.Add(new MenuBool("UseQCC", "Use Q on CC"));
            MenuRyze.Add(Misc);
            draw = new Menu("draw", "Drawing");
            draw.Add(new MenuBool("drawQ", "Draw Q"));
            draw.Add(new MenuBool("drawW", "Draw W"));
            draw.Add(new MenuBool("drawE", "Draw E"));
            draw.Add(new MenuBool("drawR", "Draw R"));
            MenuRyze.Add(draw);
            MenuRyze.Attach();


            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            Orbwalker.OnBeforeAttack += Orbwalker_OnBeforeAttack;
            Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;

        }


        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (args.DangerLevel != Interrupter.DangerLevel.High
                || sender.Distance(Player) > E.Range)
            {
                return;
            }

            if (sender.IsValidTarget(E.Range) && args.DangerLevel == Interrupter.DangerLevel.High
                && E.IsReady())
            {
                E.Cast(sender);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (E.IsReady() && Misc["EAntiGapcloser"].GetValue<MenuBool>().Enabled && sender.Distance(Player) < E.Range)
            {
                E.Cast(sender);
            }
        }
        private static void Orbwalker_OnAfterAttack(object sender, AfterAttackEventArgs args)
        {
            var t = args.Target as AIHeroClient;

            if (t != null)
            {
                if (Q.IsReady() && ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled)
                {
                    Q.Cast(t);
                }

            }
        }
        private static void Orbwalker_OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            var t = args.Target as AIHeroClient;

            if (t != null)
            {
                if (Q.IsReady() && ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled)
                {
                    Q.Cast(t);
                }

            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
            }

            HealManager();
        }

        public static void HealManager()
        {
            if (Player.IsRecalling() || Player.InFountain()
                || !HealMenu["ElEasy.Taric.Heal.Activated"].GetValue<MenuBool>().Enabled
                || Player.ManaPercent < HealMenu["ElEasy.Taric.Heal.Player.Mana"].GetValue<MenuSlider>().Value
                || !Q.IsReady())
            {
                return;
            }

            if ((Player.Health / Player.MaxHealth) * 100
                <= HealMenu["ElEasy.Taric.Heal.Player.HP"].GetValue<MenuSlider>().Value)
            {
                Q.CastOnUnit(Player);
            }

            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(h => h.IsAlly && !h.IsMe))
            {
                if ((hero.Health / hero.MaxHealth) * 100
                    <= HealMenu["ElEasy.Taric.Heal.Ally.HP"].GetValue<MenuSlider>().Value
                    && Q.IsInRange(hero))
                {
                    Q.Cast(hero);
                }
            }
        }
        public static void Combo()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled && E.IsReady()
                && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }

            if (ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled && W.IsReady()
                && target.IsValidTarget(W.Range))
            {
                W.CastOnUnit(Player);
            }

            if (ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled && R.IsReady()
                && target.IsValidTarget(R.Range)
                && Player.CountEnemyHeroesInRange(R.Range)
                >= ComboMenu["Rmin"].GetValue<MenuSlider>().Value)
            {
                R.CastOnUnit(Player);
            }

            /*if (this.Menu.Item("ElEasy.Taric.Combo.Ignite").IsActive() && target.IsValidTarget(600)
                && this.IgniteDamage(target) >= target.Health)
            {
                this.Player.Spellbook.CastSpell(Ignite, target);
            }*/
        }
    }
}

