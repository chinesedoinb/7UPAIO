using System;
using System.ComponentModel;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

namespace AIO7UP.Champions
{
    internal class Sejuani
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, Items;
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
            if (!Player.CharacterName.Contains("Sejuani")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 650);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 1300);

            Q.SetSkillshot(0, 70, 999, false, SpellType.Line);
            R.SetSkillshot(250, 220, 1600, false, SpellType.Line);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Sett", "[7UP]Sett", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("useQ", "Use Q"));
            ComboMenu.Add(new MenuBool("useW", "Use W"));
            ComboMenu.Add(new MenuBool("useE", "Use E"));
            ComboMenu.Add(new MenuBool("useR", "Use R"));
            ComboMenu.Add(new MenuSlider("RCount", "R Count >=", 2, 1, 5));
            ComboMenu.Add(new MenuKeyBind("SemiR", "Semi R", Keys.R, KeyBindType.Press));
            //ComboMenu.Add(new MenuBool("useorb", "Use Orb fixed by ProDragon"));
            MenuRyze.Add(ComboMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("interrupterQ", "Interrupter Q"));
            Misc.Add(new MenuBool("interrupterR", "Interrupter R", false));
            Misc.Add(new MenuBool("gapQ", "AntiGap Q"));
            Misc.Add(new MenuBool("gapR", "AntiGap R", false));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuSeparator("KillSteal Settings", "KillSteal Settings"));
            KillStealMenu.Add(new MenuBool("killstealQ", "Use Q"));
            KillStealMenu.Add(new MenuBool("killstealW", "Use W"));
            KillStealMenu.Add(new MenuBool("killstealE", "Use E"));
            KillStealMenu.Add(new MenuBool("killstealR", "Use R"));
            KillStealMenu.Add(new MenuBool("ign", "Use [Ignite] KillSteal"));
            MenuRyze.Add(KillStealMenu);
            MenuRyze.Attach();
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2OnOnInterruptableTarget;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            /*if (ComboMenu["useorb"].GetValue<MenuBool>().Enabled)
            {
                orbfixed();
                //orbfixed2();
            }*/
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    return;
                case OrbwalkerMode.Harass:
                    //Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    //LaneClear();
                    //JungleClear();
                    break;
            }
            KillSteal();
            SemiKey();

        }

        /*private static void orbfixed()
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                var target = TargetSelector.GetTarget(Player.GetRealAutoAttackRange(Player), DamageType.Physical);
                if (target != null && Player.GetRealAutoAttackRange() > 160)
                {
                    Orbwalker.ResetAutoAttackTimer();
                    Orbwalker.Attack(target);
                    return;
                }
            }
        }
        private static void orbfixed2()
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass || Orbwalker.ActiveMode == OrbwalkerMode.LaneClear || Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                if (Player.GetRealAutoAttackRange() > 160)
                {
                    Orbwalker.ResetAutoAttackTimer();
                    return;
                }
            }
        }*/
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (!sender.IsValidTarget(Q.Range))
            {
                return;
            }

            if (sender.Distance(Player) > Q.Range)
            {
                return;
            }

            var useQ = Misc["gapQ"].GetValue<MenuBool>().Enabled;
            var useR = Misc["gapR"].GetValue<MenuBool>().Enabled;

            if (sender.IsValidTarget(Q.Range))
            {
                if (useQ && Q.IsReady())
                {
                    Q.Cast(sender);
                }

                if (useR && Q.IsReady() && R.IsReady())
                {
                    R.Cast(sender);
                }
            }
        }
        private static void Interrupter2OnOnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (args.DangerLevel != Interrupter.DangerLevel.High || sender.Distance(Player) > Q.Range)
            {
                return;
            }

            if (sender.IsValidTarget(Q.Range) && args.DangerLevel == Interrupter.DangerLevel.High
                && Q.IsReady())
            {
                Q.Cast(sender);
            }
        }
        private static bool IsFrozen(AIBaseClient target)
        {
            return target.HasBuff("SejuaniFrost");
        }
        public static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            var comboQ = ComboMenu["useQ"].GetValue<MenuBool>().Enabled;
            var comboE = ComboMenu["useE"].GetValue<MenuBool>().Enabled;
            var comboW = ComboMenu["useW"].GetValue<MenuBool>().Enabled;
            var comboR = ComboMenu["useR"].GetValue<MenuBool>().Enabled;
            var countEnemyR = ComboMenu["RCount"].GetValue<MenuSlider>().Value;
            //var countEnemyE = ElSejuaniMenu.Menu.Item("ElSejuani.Combo.E.Count").GetValue<Slider>().Value;

            if (comboQ && Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }

            if (comboW && W.IsReady() && target.IsValidTarget(W.Range))
            {
                W.Cast();
            }

            if (comboE && E.IsReady() && IsFrozen(target) && target.IsValidTarget(E.Range))
            {
                if (IsFrozen(target))
                {
                    E.Cast();
                }

                if (IsFrozen(target)
                    && target.ServerPosition.Distance(Player.ServerPosition) <= E.Range)
                {
                    E.Cast();
                }
            }
            if (countEnemyR == 1)
            {
                if (comboR && R.IsReady())
                {
                    var targ = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                    var predA = R.GetPrediction(targ);
                    if (targ != null && targ.IsValidTarget())
                    {
                        R.Cast(predA.CastPosition);
                    }
                }
            }
            else
            {
                if (comboR && R.IsReady())
                {
                    foreach (
                        var x in
                            GameObjects.EnemyHeroes.Where((hero => !hero.IsDead && hero.IsValidTarget(R.Range))))
                    {
                        var pred = R.GetPrediction(x);
                        if (pred.AoeTargetsHitCount >= countEnemyR)
                        {
                            R.Cast(pred.CastPosition);
                        }
                    }
                }
            }
        }

        public static void SemiKey()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (!R.IsReady() || !target.IsValidTarget(R.Range))
            {
                return;
            }

            R.Cast(target);

        }

        /*private static void KillSteal()
        {
            var killstealQ = KillStealMenu["killstealQ"].GetValue<MenuBool>().Enabled;
            var killstealW = KillStealMenu["killstealW"].GetValue<MenuBool>().Enabled;
            var killstealE = KillStealMenu["killstealE"].GetValue<MenuBool>().Enabled;
            var killstealR = KillStealMenu["killstealR"].GetValue<MenuBool>().Enabled;
            var killstealIgnite = KillStealMenu["killstealIgnite"].GetValue<MenuBool>().Enabled;

            foreach (var target in GameObjects.EnemyHeroes.Where(target => target.IsValidTarget(W.Range)))
            {
                if (target == null)
                    return;
                if (target.HasBuff("SionPassiveZombie"))
                    return;
                if (target.HasBuffOfType(BuffType.Invulnerability) && target.HasBuffOfType(BuffType.SpellImmunity))
                    return;

                if (killstealQ && Q.IsReady() && target.Health + target.MagicalShield < Player.GetAutoAttackDamage(target) + Player.GetAutoAttackDamage(target) * 2 && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target, true);
                    return;
                }
                if (killstealW && W.IsReady() && target.Health + target.MagicalShield < W.GetDamage(target) && target.IsValidTarget(W.Range))
                {
                    W.Cast(target, true);
                    return;
                }
                if (killstealE && E.IsReady() && target.Health + target.MagicalShield < W.GetDamage(target) && target.IsValidTarget(E.Range))
                {
                    E.Cast(target, true);
                    return;
                }
                if (killstealR && R.IsReady() && target.Health + target.MagicalShield < R.GetDamage(target) && target.IsValidTarget(R.Range))
                {
                    R.Cast(target, true);
                    return;
                }
                if (killstealIgnite && Ignite.IsReady() && target.Health < Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite) && target.IsValidTarget(600))
                {
                    Ignite.Cast(target);
                    return;
                }
            }
        }*/


        private static float getDamage(AIBaseClient target, bool q = false, bool w = false, bool e = false, bool r = false, bool ignite = false)
        {
            float damage = 0;

            if (target == null || target.IsDead)
                return 0;

            if (target.HasBuffOfType(BuffType.Invulnerability))
                return 0;
            if (target.HasBuff("KingredRNoDeathBuff") || target.HasBuff("FioraW") || target.HasBuff("UndyingRage"))
                return 0;

            if (q && Q.IsReady())
                damage += (float)Player.GetAutoAttackDamage(target) * 2;
            if (w && W.IsReady())
                damage += (float)Damage.GetSpellDamage(Player, target, SpellSlot.W);
            if (e && E.IsReady())
                damage += (float)Damage.GetSpellDamage(Player, target, SpellSlot.E);

            if (r && R.IsReady())
                damage += (float)Damage.GetSpellDamage(Player, target, SpellSlot.R);

            if (ignite && Ignite.IsReady())
                damage += (float)Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);

            if (Player.GetBuffCount("itemmagicshankcharge") == 100) // oktw
                damage += (float)Player.CalculateMagicDamage(target, 100 + 0.1 * Player.TotalMagicalDamage);

            if (Player.HasBuff("SummonerExhaust"))
                damage = damage * 0.6f;

            if (target.HasBuff("ManaBarrier") && target.HasBuff("BlitzcrankManaBarrierCO"))
                damage += target.Mana / 2f;
            if (target.HasBuff("GarenW"))
                damage = damage * 0.7f;

            return damage;
        }

        public static double QDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Magical,
                    (float)(new[] { 0, 90, 140, 190, 240, 290 }[Q.Level] + 0.6f * Player.FlatMagicDamageMod));

        }

        public static double WDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 20, 25, 30, 35, 40 }[W.Level] + 0.2f * Player.FlatPhysicalDamageMod));
        }

        public static double EDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Magical,
                (float)(new[] { 0, 55, 105, 155, 205, 255 }[W.Level] + 0.6f * Player.FlatMagicDamageMod));
        }

        public static double RDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 80, 120, 160 }[R.Level] + 0.8f * Player.FlatPhysicalDamageMod));
        }


        public static void KillSteal()
        {
            //if (Player.HasBuff("TalonEHop"))
            //{

            //}
            var KsQ = KillStealMenu["killstealQ"].GetValue<MenuBool>().Enabled;
            var KsW = KillStealMenu["killstealW"].GetValue<MenuBool>().Enabled;
            var KsE = KillStealMenu["killstealE"].GetValue<MenuBool>().Enabled;
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
                if (KsW && W.IsReady())
                {
                    if (target != null)
                    {
                        if (target.Health + target.AllShield <= WDamage(target))/*try*/
                        {
                            W.Cast(target);
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
                    if (target.Health + target.AllShield < Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite))
                    {
                        Ignite.Cast(target);
                    }
                }
            }
        }
    }
}
