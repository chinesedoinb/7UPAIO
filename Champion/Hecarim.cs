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
    internal class Hecarim
    {
        public static Menu Menu, ComboMenu, HarassMenu, ClearMenu, Misc, KillStealMenu, Items;
        public static int[] abilitySequence;
        public static int qOff = 0, wOff = 0, eOff = 0, rOff = 0;
        public static AIHeroClient _Player
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
            if (!_Player.CharacterName.Contains("Hecarim")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 350);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 1000);
            R.SetSkillshot(0.25f, 300f, float.MaxValue, false, SpellType.Circle);


            abilitySequence = new int[] { 1, 3, 1, 2, 1, 4, 1, 2, 1, 2, 4, 2, 3, 2, 3, 4, 3, 3 };
            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Hecarim", "[7UP]Hecarim", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQ", "Use Q"));
            ComboMenu.Add(new MenuBool("UseW", "Use W"));
            ComboMenu.Add(new MenuBool("UseE", "Use E"));
            ComboMenu.Add(new MenuBool("UseR", "Use R"));
            ComboMenu.Add(new MenuSlider("Rene", "Min Enemies for R", 2, 1, 5));
            ComboMenu.Add(new MenuBool("AutoR", "Auto R", false));
            ComboMenu.Add(new MenuSlider("Renem", "Min Enemies for Auto R", 2, 1, 5));
            MenuRyze.Add(ComboMenu);
            ClearMenu = new Menu("JungleClear Settings", "JungleClear");
            ClearMenu.Add(new MenuBool("laneQ", "Use Q"));
            ClearMenu.Add(new MenuBool("fQ", "Farm with Q ( While pressing last hit )"));
            ClearMenu.Add(new MenuBool("laneW", "Use W"));
            ClearMenu.Add(new MenuSlider("wmin", "Min Minion for W", 3, 1, 5));
            ClearMenu.Add(new MenuSlider("lanemana", "Mana Percentage", 30, 0, 100));
            MenuRyze.Add(ClearMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("antigap", "AntiGapCloser with E", false));
            Misc.Add(new MenuBool("interrupte", "Interrupt with E"));
            Misc.Add(new MenuBool("interruptr", "Interrupt with R"));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuSeparator("KillSteal Settings", "KillSteal Settings"));
            KillStealMenu.Add(new MenuBool("KsQ", "Use Q KS"));
            KillStealMenu.Add(new MenuBool("KsR", "Use R KS", false));
            KillStealMenu.Add(new MenuBool("ign", "Use Ignite KS"));
            MenuRyze.Add(KillStealMenu);
            MenuRyze.Attach();
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            Orbwalker.OnAfterAttack += Orbwalking_AfterAttack;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (E.IsReady() && sender.IsValidTarget(E.Range) && Misc["interrupte"].GetValue<MenuBool>().Enabled)
            {
                E.Cast();
            }

            if (R.IsReady() && sender.IsValidTarget(R.Range) && Misc["interruptr"].GetValue<MenuBool>().Enabled)
            {
                var pred = R.GetPrediction(sender).Hitchance;
                if (pred >= HitChance.High)
                    R.Cast(sender);
            }

        }
        private static void Gapcloser_OnGapcloser(AIBaseClient gapcloser, AntiGapcloser.GapcloserArgs args)
        {
            if (E.IsReady() && gapcloser.IsValidTarget(_Player.AttackRange) && Misc["antigap"].GetValue<MenuBool>().Enabled)
                E.Cast();
        }
        static void Spellbook_OnCastSpell(Spellbook s, SpellbookCastSpellEventArgs args)
        {
            if (args.Slot == SpellSlot.E)
            {
                Orbwalker.ResetAutoAttackTimer();
            }
        }

        static void Orbwalking_AfterAttack(object unit, AfterAttackEventArgs target)
        {
            if (target.Target == null || !target.Target.IsValidTarget())
            {
                return;
            }
            {
                if (target is AIHeroClient)
                {
                    if (ComboMenu["UseQ"].GetValue<MenuBool>().Enabled)
                    {
                        if (Q.IsReady())
                        {
                            var starget = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                            if (starget != null && _Player.Position.Distance(starget.Position) <= Q.Range)
                            {
                                Q.Cast();
                            }
                        }
                    }
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
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }
            KillSteal();
            AutoR();
        }
        private static void AutoR()
        {
            if (!R.IsReady() || !ComboMenu["AutoR"].GetValue<MenuBool>().Enabled)
                return;

            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            var enemys = target.CountEnemyHeroesInRange(R.Range);
            if (R.IsReady() && ComboMenu["UseR"].GetValue<MenuBool>().Enabled && target.IsValidTarget(R.Range))
            {
                var pred = R.GetPrediction(target).Hitchance;
                if (pred >= HitChance.High)
                    R.CastIfWillHit(target, enemys);
            }

        }
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            var enemys = target.CountEnemyHeroesInRange(R.Range);
            if (target == null || !target.IsValidTarget())
                return;

            if (E.IsReady() && target.IsValidTarget(2500) && ComboMenu["UseE"].GetValue<MenuBool>().Enabled)
                E.Cast();

            if (W.IsReady() && target.IsValidTarget(W.Range) && ComboMenu["UseW"].GetValue<MenuBool>().Enabled)
                W.Cast();

            if (Q.IsReady() && ComboMenu["UseQ"].GetValue<MenuBool>().Enabled && target.IsValidTarget(Q.Range))
            {
                Q.Cast();
            }

            if (R.IsReady() && ComboMenu["UseR"].GetValue<MenuBool>().Enabled && target.IsValidTarget(R.Range))
                if (ComboMenu["Rene"].GetValue<MenuSlider>().Value <= enemys)
                    R.CastIfHitchanceEquals(target, HitChance.High);

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo) ;
        }
        private static void LaneClear()
        {
            var minionObj = GameObjects.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Ally,
                MinionOrderTypes.MaxHealth);
            var lanemana = ClearMenu["lanemana"].GetValue<MenuSlider>().Value;
            var minions = GameObjects.GetMinions(_Player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy,
                   MinionOrderTypes.MaxHealth);

            if (!minionObj.Any())
            {
                return;
            }

            if (_Player.ManaPercent >= lanemana)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && ClearMenu["laneQ"].GetValue<MenuBool>().Enabled)
                {
                    Q.Cast();
                }
            }


            if (minionObj.Count > ClearMenu["wmin"].GetValue<MenuSlider>().Value && _Player.ManaPercent >= lanemana)
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && ClearMenu["laneW"].GetValue<MenuBool>().Enabled)
                {
                    {
                        W.Cast();
                    }
                }

        }
        private static void JungleClear()
        {
            var minionObj = GameObjects.Jungle.Where(j => j.IsValidTarget(Q.Range)).FirstOrDefault(j => j.IsValidTarget(Q.Range));
            var lanemana = ClearMenu["lanemana"].GetValue<MenuSlider>().Value;

            if (!minionObj.IsJungle())
            {
                return;
            }

            if (_Player.ManaPercent >= lanemana)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && ClearMenu["laneQ"].GetValue<MenuBool>().Enabled)
                {
                    Q.Cast();
                }
            }
            if (_Player.ManaPercent >= lanemana)
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && ClearMenu["laneW"].GetValue<MenuBool>().Enabled)
                {
                    {
                        W.Cast();
                    }
                }
        }



        public static double QDamage(AIBaseClient target)
        {
            return _Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 60, 90, 120, 150, 180 }[Q.Level] + 0.85f * _Player.FlatPhysicalDamageMod));

        }


        public static double RDamage(AIBaseClient target)
        {
            return _Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 150, 250, 350 }[R.Level] + 1.0f * _Player.FlatPhysicalDamageMod));
        }

        public static void KillSteal()
        {
            if (_Player.HasBuff("JustHecarim"))
            {

            }
            var KsQ = KillStealMenu["KsQ"].GetValue<MenuBool>().Enabled;
            var KsW = KillStealMenu["KsW"].GetValue<MenuBool>().Enabled;
            var KsR = KillStealMenu["KsR"].GetValue<MenuBool>().Enabled;
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
            {
                if (KsQ && Q.IsReady())
                {
                    if (target != null)
                    {
                        if (_Player.Distance(target) > 150)
                        {
                            if (target.Health + target.AllShield <= QDamage(target))
                            {
                                Q.Cast(target);
                            }
                        }
                        else
                        {
                            if (target.Health + target.AllShield <= QDamage(target) * 1.5f)
                            {
                                Q.Cast(target);
                            }
                        }
                    }
                }
                if (KsR && R.IsReady() && target.IsValidTarget(500))
                {
                    if (target != null)
                    {
                        if (target.Health + target.AllShield <= RDamage(target))
                        {
                            R.Cast();
                        }
                    }
                }
                if (Ignite != null && KillStealMenu["ign"].GetValue<MenuBool>().Enabled && Ignite.IsReady())
                {
                    if (target.Health + target.AllShield < _Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite))
                    {
                        Ignite.Cast(target);
                    }
                }
            }
        }
    }
}
