using System;
using System.ComponentModel;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

namespace AIO7UP.Champions
{
    internal class Sett
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
            if (!Player.CharacterName.Contains("Sett")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 300f);

            W = new Spell(SpellSlot.W, 725f);
            W.SetSkillshot(0.75f, 180f, 0, false, SpellType.Line);

            E = new Spell(SpellSlot.E, 450f);
            E.SetSkillshot(0.25f, 270f, 0, false, SpellType.Line);

            R = new Spell(SpellSlot.R, 400f);
            R.SetTargetted(0.25f, float.MaxValue);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Sett", "[7UP]Sett", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("useQ", "Use Q"));
            ComboMenu.Add(new MenuBool("useW", "Use W"));
            ComboMenu.Add(new MenuSlider("MiscAutoW", "Use W when mana higher than", 75));
            ComboMenu.Add(new MenuKeyBind("SemiW", "Semi W", Keys.W, KeyBindType.Press));
            ComboMenu.Add(new MenuBool("useE", "Use E"));
            ComboMenu.Add(new MenuKeyBind("SemiE", "Semi E", Keys.E, KeyBindType.Press));
            ComboMenu.Add(new MenuBool("useR", "Use R"));
            ComboMenu.Add(new MenuKeyBind("SemiR", "Semi R", Keys.R, KeyBindType.Press));
            //ComboMenu.Add(new MenuBool("useorb", "Use Orb fixed by ProDragon"));
            MenuRyze.Add(ComboMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("interrupter", "Interrupter"));
            Misc.Add(new MenuBool("gapcloser", "Gapcloser"));
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
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
            Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;
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
            if (Misc["gapcloser"].GetValue<MenuBool>().Enabled)
            {
                if (args != null && args.EndPosition.DistanceToPlayer() < E.Range && E.IsReady())
                    E.Cast(args.EndPosition);
                return;
            }
        }
        private static void Interrupter2OnOnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (E.IsReady() && Misc["interrupter"].GetValue<MenuBool>().Enabled)
            {
                if (sender.IsEnemy && sender.DistanceToPlayer() < E.Range)
                {
                    E.CastIfHitchanceMinimum(sender, HitChance.Medium);
                    return;
                }
            }
        }
        public static void Combo()
        {
            var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var useQ = ComboMenu["useQ"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["useW"].GetValue<MenuBool>().Enabled;
            var MiscAutoW = ComboMenu["MiscAutoW"].GetValue<MenuSlider>().Value;
            var useE = ComboMenu["useE"].GetValue<MenuBool>().Enabled;
            var useR = ComboMenu["useR"].GetValue<MenuBool>().Enabled;

            if (target == null)
            {
                return;
            }
            if (useQ && Q.IsReady())
            {
                if (target.DistanceToPlayer() > Q.Range && target.IsValidTarget(Q.Range))
                {
                    Q.Cast();
                }
            }
            if (useW && W.IsReady())
            {
                if (Player.ManaPercent > MiscAutoW && target.IsValidTarget(W.Range) && E.IsReady() && target.IsValidTarget(E.Range))
                {
                    E.CastIfHitchanceMinimum(target, HitChance.Medium);
                    W.CastIfHitchanceMinimum(target, HitChance.Medium);
                }
                else if (Player.ManaPercent > MiscAutoW && target.IsValidTarget(W.Range))
                {
                    W.CastIfHitchanceMinimum(target, HitChance.Medium);
                }
            }
            if (useE && E.IsReady())
            {
                if (target.DistanceToPlayer() > Player.GetRealAutoAttackRange() && target.IsValidTarget(E.Range))
                {
                    E.CastIfHitchanceMinimum(target, HitChance.High);
                }
            }
            if (target.Health <= getDamage(target, true, false, true, true, false) && R.IsReady() && useR)
            {
                R.CastOnUnit(target);
                W.CastIfHitchanceMinimum(target, HitChance.Medium);
            }
        }

        public static void OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var useQ = ComboMenu["useQ"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["useE"].GetValue<MenuBool>().Enabled;
            if (target != null)
            {
                    if (useQ && Q.IsReady() && Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                    {
                        Q.Cast();
                        Orbwalker.ResetAutoAttackTimer();
                    }
                    if (useE && E.IsReady() && E.GetPrediction(target).Hitchance >= HitChance.High && Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                    {
                        E.Cast(target);
                        Orbwalker.ResetAutoAttackTimer();
                    }
                    /*if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                    {
                        foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                        {
                            if (minion != null && minion.DistanceToPlayer() <= objPlayer.GetRealAutoAttackRange() && minion.IsValidTarget(300))
                            {
                                if (Q.IsReady())
                                {
                                    Q.Cast();
                                    Orbwalker.ResetAutoAttackTimer();
                                    return;
                                }
                            }
                        }
                    }*/
            }
            else
            {
                return;
            }
        }



        public static void Orbwalker_OnAfterAttack(object e, AfterAttackEventArgs args)
        {
            var useQ = ComboMenu["useQ"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["useE"].GetValue<MenuBool>().Enabled;
            if (e != null)
            {

                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                    var selectedTarget = TargetSelector.SelectedTarget;
                    if (selectedTarget != null && selectedTarget.IsValidTarget(Player.GetRealAutoAttackRange()))
                    {
                        target = selectedTarget;
                    }
                    if (target == null)
                    {
                        return;
                    }
                    if (E.IsReady() && useE && target.IsValidTarget(E.Range))
                    {
                        E.CastIfHitchanceMinimum(target, HitChance.Medium);
                    }
                    if (Q.IsReady() && useQ && target.IsValidTarget(Q.Range))
                    {
                        Q.Cast();
                        Orbwalker.ResetAutoAttackTimer();
                    }
                }
            }

        }

        public static void SemiKey()
        {
            var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
            var useW = ComboMenu["SemiW"].GetValue<MenuKeyBind>().Active;
            var useE = ComboMenu["SemiE"].GetValue<MenuKeyBind>().Active;
            var useR = ComboMenu["SemiR"].GetValue<MenuKeyBind>().Active;

            if (target == null)
            {
                return;
            }

            if(useR && R.IsReady())
            {
                    R.Cast(target);
            }

            if(useW && W.IsReady())
            {
                    W.Cast(target);
                
            }

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
            return Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 10, 20, 30, 40, 50 }[Q.Level] + 0.01f * Player.FlatPhysicalDamageMod));

        }

        public static double WDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 80, 100, 120, 140, 160 }[W.Level] + 0.25f * Player.FlatPhysicalDamageMod));
        }

        public static double EDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 50, 70, 90, 110, 130 }[W.Level] + 0.6f * Player.FlatPhysicalDamageMod));
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
            var KsR = KillStealMenu["killstealR"].GetValue<MenuBool>().Enabled;
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
                    if (target.Health + target.AllShield < Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite))
                    {
                        Ignite.Cast(target);
                    }
                }
            }
        }
    }
}
