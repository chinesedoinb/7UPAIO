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
    internal class Shyvana
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, Items;

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
            if (!_Player.CharacterName.Contains("Shyvana")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, _Player.AttackRange);
            W = new Spell(SpellSlot.W, 350f);
            E = new Spell(SpellSlot.E, 925f);
            E.SetSkillshot(0.25f, 60f, 1500, false, SpellType.Line);
            R = new Spell(SpellSlot.R, 1000f);
            R.SetSkillshot(0.25f, 150f, 1500, false, SpellType.Line);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Shyvana", "[7UP]Shyvana", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQ", "Use Q"));
            ComboMenu.Add(new MenuBool("UseW", "Use W"));
            ComboMenu.Add(new MenuBool("UseE", "Use E"));
            ComboMenu.Add(new MenuBool("UseR", "Use R"));
            ComboMenu.Add(new MenuSlider("Rene", "Min Enemies for R", 2, 1, 5));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuSeparator("Harass Settings", "Harass Settings"));
            HarassMenu.Add(new MenuBool("hQ", "Use Q"));
            HarassMenu.Add(new MenuBool("hW", "Use W"));
            HarassMenu.Add(new MenuBool("hE", "Use E"));
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("lQ", "Use Q"));
            LaneClearMenu.Add(new MenuBool("lW", "Use W"));
            LaneClearMenu.Add(new MenuBool("lE", "Use E"));
            MenuRyze.Add(LaneClearMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("combodamage", "Damage Indicator"));
            Misc.Add(new MenuBool("interrupt", "Interrupt Spells"));
            Misc.Add(new MenuBool("antigap", "AntiGapCloser"));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuBool("KsQ", "Use Q KillSteal"));
            KillStealMenu.Add(new MenuBool("KsE", "Use E KillSteal"));
            KillStealMenu.Add(new MenuBool("ign", "Use [Ignite] KillSteal"));
            MenuRyze.Add(KillStealMenu);
            MenuRyze.Attach();

            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            
        }


        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (R.IsReady() && sender.IsValidTarget(R.Range) && Misc["interrupt"].GetValue<MenuBool>().Enabled)
                R.CastIfHitchanceEquals(sender, HitChance.High);
        }
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (W.IsReady() && sender.IsValidTarget(Q.Range) && Misc["antigap"].GetValue<MenuBool>().Enabled)
                W.Cast();
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
                    LaneClear();
                    JungleClear();
                    break;
            }
            KillSteal();
        }


        private static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var enemys = target.CountEnemyHeroesInRange(R.Range);
            if (target == null || !target.IsValidTarget())
                return;

            if (R.IsReady() && ComboMenu["UseR"].GetValue<MenuBool>().Enabled && target.IsValidTarget(R.Range))
                if (!target.HasBuff("JudicatorIntervention") && !target.HasBuff("Undying Rage") &&
                (ComboMenu["Rene"].GetValue<MenuSlider>().Value <= enemys))
                    R.CastIfHitchanceEquals(target, HitChance.High);

            if (W.IsReady() && target.IsValidTarget(W.Range) && ComboMenu["UseW"].GetValue<MenuBool>().Enabled)
                W.Cast();

            if (E.IsReady() && target.IsValidTarget(E.Range) && ComboMenu["UseE"].GetValue<MenuBool>().Enabled)
                E.CastIfHitchanceEquals(target, HitChance.VeryHigh);

            if (Q.IsReady() && ComboMenu["UseQ"].GetValue<MenuBool>().Enabled & target.IsValidTarget(_Player.AttackRange))
                Q.Cast();

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo) ;
        }
        public static void JungleClear()
        {
            var minionCount = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).ToList();

            {
                foreach (var minion in minionCount)
                {
                    if (LaneClearMenu["lQ"].GetValue<MenuBool>().Enabled
                        && Q.IsReady()
                        && minion.IsValidTarget(125)
                        )
                    {
                        Q.Cast();
                    }

                    if (LaneClearMenu["lQ"].GetValue<MenuBool>().Enabled
                        && W.IsReady()
                        && minion.IsValidTarget(W.Range)
                       )
                    {
                        W.Cast();
                    }

                    if (LaneClearMenu["lQ"].GetValue<MenuBool>().Enabled
                        && E.IsReady()
                        && minion.IsValidTarget(E.Range)
                        )
                    {
                        E.Cast(minion);
                    }

                }
            }
        }
        public static void LaneClear()
        {
            var minionCount = GameObjects.GetMinions(_Player.Position, E.Range, MinionTypes.All, MinionTeam.Ally);

            {
                foreach (var minion in minionCount)
                {
                    if (LaneClearMenu["lQ"].GetValue<MenuBool>().Enabled
                        && Q.IsReady()
                        && minion.IsValidTarget(125)
                        )
                    {
                        Q.Cast();
                    }

                    if (LaneClearMenu["lQ"].GetValue<MenuBool>().Enabled
                        && W.IsReady()
                        && minion.IsValidTarget(W.Range)
                       )
                    {
                        W.Cast();
                    }

                    if (LaneClearMenu["lQ"].GetValue<MenuBool>().Enabled
                        && E.IsReady()
                        && minion.IsValidTarget(E.Range)
                        )
                    {
                        E.Cast(minion);
                    }

                }
            }
        }


        public static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            if (target == null || !target.IsValidTarget())
                return;

            if (W.IsReady() && HarassMenu["hW"].GetValue<MenuBool>().Enabled && target.IsValidTarget(W.Range))
                W.Cast();

            if (Q.IsReady() && HarassMenu["hQ"].GetValue<MenuBool>().Enabled && target.IsValidTarget(_Player.AttackRange))
                Q.Cast();

            if (E.IsReady() && HarassMenu["hE"].GetValue<MenuBool>().Enabled && target.IsValidTarget(E.Range))
                E.CastIfHitchanceEquals(target, HitChance.VeryHigh);
        }

        public static void Orbwalker_OnAfterAttack(object e, AfterAttackEventArgs args)
        {
            if (!(e is AIHeroClient)) return;
            var target = TargetSelector.GetTarget(250, DamageType.Physical);
            var champ = (AIHeroClient)e;
            var useQ = ComboMenu["UseQ"].GetValue<MenuBool>().Enabled;
            var HasQ = HarassMenu["hQ"].GetValue<MenuBool>().Enabled;
            if (champ == null || champ.Type != GameObjectType.AIHeroClient || !champ.IsValid) return;
            if (target != null)
            {
                if (useQ && Q.IsReady() && Orbwalker.ActiveMode.HasFlag(OrbwalkerMode.Combo) && target.IsValidTarget(150))
                {
                    Q.Cast(target);
                    Orbwalker.ResetAutoAttackTimer();
                    _Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
                if (HasQ && Q.IsReady() && Orbwalker.ActiveMode.HasFlag(OrbwalkerMode.Harass) && target.IsValidTarget(150))
                {
                    Q.Cast(target);
                    Orbwalker.ResetAutoAttackTimer();
                    _Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }

            }
        }

        public static double QDamage(AIBaseClient target)
        {
            return _Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 20, 35, 50, 65, 80 }[Q.Level] + 0.25f * _Player.FlatMagicDamageMod));

        }

        public static double EDamage(AIBaseClient target)
        {
            return _Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 60, 100, 140, 180, 220 }[W.Level] + 0.7f * _Player.FlatMagicDamageMod));
        }


        public static void KillSteal()
        {
            if (_Player.HasBuff("ShyvanaDragon"))
            {

            }
            var KsQ = KillStealMenu["KsQ"].GetValue<MenuBool>().Enabled;
            var KsE = KillStealMenu["KsE"].GetValue<MenuBool>().Enabled;
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
                if (KsE && E.IsReady())
                {
                    if (target != null)
                    {
                        if (target.Health + target.AllShield <= EDamage(target))/*try*/
                        {
                            E.Cast(target);
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
