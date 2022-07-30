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
    internal class Nami
    {
        public static Menu Menu, ComboMenu, ESettings, HealMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, Items;
        public static int Stage = 0;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q, W, E, R;
        public static Spell Ignite;
        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Nami")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 848);
            Q.SetSkillshot(1.25f, 150, float.MaxValue, false, SpellType.Circle);
            W = new Spell(SpellSlot.W, 725);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 2700);
            R.SetSkillshot(0.25f, 260f, 850f, false, SpellType.Line);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);

            var MenuRyze = new Menu("Nami", "[7UP]Nami", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("Combo Q", "Use Q").SetValue(true));
            ComboMenu.Add(new MenuBool("Combo W", "Use W").SetValue(true));
            ComboMenu.Add(new MenuBool("Combo E", "Use E").SetValue(true));
            ComboMenu.Add(new MenuBool("Combo R", "Use R").SetValue(true));
            //ComboMenu.Add(new MenuBool("Combo R Count", "Auto R Min Enemy is").SetValue(true));
            ComboMenu.Add(new MenuSlider("Combo R Count1", "R Min Enemy is", 3, 0, 5));
            ComboMenu.Add(new MenuKeyBind("Semi R", "Semi R", Keys.T, KeyBindType.Press));
            MenuRyze.Add(ComboMenu);
            ESettings = new Menu("E Settings", "E Settings");
            foreach (var ally in GameObjects.AllyHeroes.Where(h => !h.IsMe))
                ESettings.Add(new MenuBool("Eset" + ally.CharacterName.ToLower(), "E on" + ally.CharacterName));
            MenuRyze.Add(ESettings);
            HealMenu = new Menu("Heel Settings", "Heel Settings");
            HealMenu.Add(new MenuKeyBind("Heal Active", "Heal on me Active", Keys.S, KeyBindType.Toggle));
            HealMenu.Add(new MenuSlider("Heal Player", "Heal % HP", 25, 1, 100));
            HealMenu.Add(new MenuBool("Heal Ally", "Heal on Ally"));
            HealMenu.Add(new MenuSlider("Heal HP Ally", "Heal % HP on Ally", 25, 1, 100));
            HealMenu.Add(new MenuSlider("Heal mana", "Heal % Mana", 35));
            MenuRyze.Add(HealMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("Interupt.Q", "Use Q"));
            Misc.Add(new MenuBool("Interupt.R", "Use R", false));
            Misc.Add(new MenuBool("auto Q", "Auto Q on CC"));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuBool("ign", "Use [Ignite] KillSteal", false));
            MenuRyze.Add(KillStealMenu);
            MenuRyze.Attach();

            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            AIBaseClient.OnProcessSpellCast += ProcessSpellCast;
        }

        public static void AllyHealing()
        {
            if (Player.IsRecalling() || Player.InFountain()
                || HealMenu["Heal Ally"].GetValue<MenuBool>().Enabled)
            {
                return;
            }

            foreach (var hero in GameObjects.AllyHeroes.Where(h => !h.IsMe && (!h.IsRecalling() || !h.InFountain())))
            {
                if ((hero.Health / hero.MaxHealth) * 100
                    <= HealMenu["Heal HP Ally"].GetValue<MenuSlider>().Value
                    && W.IsReady() && hero.Distance(Player.ServerPosition) <= W.Range
                    && Player.ManaPercent >= HealMenu["Heal Mana"].GetValue<MenuSlider>().Value)
                {
                    W.Cast(hero);
                }
            }
        }
        private static void ProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var attack = sender as AIHeroClient;
            var target = args.Target as AIHeroClient;
            if (attack != null && attack.IsAlly && !attack.IsMinion())
            {
                if (ESettings["Eset" + attack.CharacterName.ToLower()].GetValue<MenuBool>().Enabled && ComboMenu["Combo E"].GetValue<MenuBool>().Enabled)
                {
                    if (target != null && args.SData.Name.Contains("BasicAttack") && attack.Distance(Player) < E.Range && !attack.IsDead)
                    {
                        E.Cast(attack);
                    }
                }
            }
        }
        /*private static void RangeAttackOnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid)
            {
                return;
            }

            var missile = (MissileClient)sender;

            // Caster ally hero / not me
            if (!missile.SpellCaster.IsValid || !missile.SpellCaster.IsAlly || missile.SpellCaster.IsMe
                || missile.SpellCaster.IsMelee())
            {
                return;
            }

            // Target enemy hero
            if (!missile.Target.IsValid || !missile.Target.IsEnemy)
            {
                return;
            }

            var caster = (AIHeroClient)missile.SpellCaster;

            if (E.IsReady() && E.IsInRange(missile.SpellCaster)
                && ESettings["Eset" + caster.CharacterName].GetValue<MenuBool>().Enabled)
            {
                E.CastOnUnit(caster); // add delay
            }
        }*/

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (!sender.IsValidTarget(Q.Range))
            {
                return;
            }

            if (sender.Distance(Player) > Q.Range)
            {
                return;
            }

            if (sender.IsValidTarget(Q.Range))
            {
                if (Misc["Interupt.Q"].GetValue<MenuBool>().Enabled && Q.IsReady())
                {
                    Q.Cast(sender);
                }

                if (Misc["Interupt.R"].GetValue<MenuBool>().Enabled && Q.IsReady()
                    && R.IsReady())
                {
                    R.Cast(sender);
                }
            }
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
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
            KillSteal();
            PlayerHealing();
            AllyHealing();
            AutoQ();
            SemiR();
        }



        public static void PlayerHealing()
        {
            if (Player.IsRecalling() || Player.InFountain()
                || !HealMenu["Heal Activate"].GetValue<MenuKeyBind>().Active)
            {
                return;
            }

            if ((Player.Health / Player.MaxHealth) * 100
                <= HealMenu["Heal Player"].GetValue<MenuSlider>().Value
                && W.IsReady()
                && ObjectManager.Player.ManaPercent
                >= HealMenu["Heal Mana"].GetValue<MenuSlider>().Value)
            {
                W.Cast();
            }
        }

        public static void SemiR()
        {
            if (ComboMenu["Semi R"].GetValue<MenuKeyBind>().Active)
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
                if (target != null)
                {
                    if (ComboMenu["Combo R Count1"].GetValue<MenuSlider>().Value > 1)
                    {
                        R.CastIfWillHit(target,
                            ComboMenu["Combo R Count1"].GetValue<MenuSlider>().Value - 1);
                    }
                    if (ComboMenu["Combo R Count1"].GetValue<MenuSlider>().Value == 1)
                    {
                        R.Cast(target);
                    }
                }

            }
        }

        public static void AutoQ()
        {
            if (Misc["auto Q"].GetValue<MenuBool>().Enabled)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(
                    t => (t.HasBuffOfType(BuffType.Charm) || t.HasBuffOfType(BuffType.Stun) ||
                          t.HasBuffOfType(BuffType.Fear) || t.HasBuffOfType(BuffType.Snare) ||
                          t.HasBuffOfType(BuffType.Taunt) || t.HasBuffOfType(BuffType.Knockback) ||
                          t.HasBuffOfType(BuffType.Suppression)) && t.IsValidTarget(Q.Range)))
                {

                    var pred = Q.GetPrediction(target);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }

            }

        }


        public static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }

            if (ComboMenu["Combo Q"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var pred = Q.GetPrediction(target);
                if (pred.Hitchance >= HitChance.VeryHigh)
                {
                    Q.Cast(pred.CastPosition);
                }
            }

            /*if (ComboMenu["Combo E"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                if (Player.CountEnemyHeroesInRange(E.Range)>1)
                {
                    var closestEnemy =
                        Player.Get(E.Range)
                            .OrderByDescending(h => (h.PhysicalDamageDealtPlayer + h.MagicDamageDealtPlayer))
                            .FirstOrDefault();

                    if (closestEnemy == null)
                    {
                        return;
                    }

                    if (closestEnemy.HasBuffOfType(BuffType.Stun))
                    {
                        return;
                    }

                    spells[Spells.E].Cast(closestEnemy);
                }

                if (Player.CountAllyHeroesInRange(E.Range)>1 && Player.CountEnemyHeroesInRange(800f) >= 1)
                {
                    var closestToTarget =
                        Player.CountAllyHeroesInRange(E.Range)
                            .OrderByDescending(h => (h.PhysicalDamageDealtPlayer + h.MagicDamageDealtPlayer))
                            .FirstOrDefault();

                    spells[Spells.E].Cast(closestToTarget);
                }
            }*/

            if (ComboMenu["Combo W"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                W.Cast(target);
            }

            if (ComboMenu["Combo R"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                foreach (var x in
                    GameObjects.EnemyHeroes.Where((hero => !hero.IsDead && hero.IsValidTarget(R.Range))))
                {
                    var pred = R.GetPrediction(x);
                    if (pred.AoeTargetsHitCount
                        >= ComboMenu["Combo R Count1"].GetValue<MenuSlider>().Value)
                    {
                        if (pred.Hitchance >= HitChance.VeryHigh)
                        {
                            R.Cast(pred.CastPosition);
                        }
                    }
                }
            }

        }



        public static void KillSteal()
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(Player.GetCurrentAutoAttackRange()) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))

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

