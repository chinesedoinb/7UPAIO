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
    internal class Fizz
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, draw;
        private static Vector3? LastHarassPos { get; set; }
        private static AIHeroClient DrawTarget { get; set; }
        public static Geometry.Polygon RRectangle
        {
            get; set;
        }

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
            if (!Player.CharacterName.Contains("Fizz")) return;
            Bootstrap.Init(null);

            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W, Player.GetRealAutoAttackRange(Player));
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 1300);

            E.SetSkillshot(0.25f, 330, float.MaxValue, false, SpellType.Circle);
            R.SetSkillshot(0.25f, 80, 1200, false, SpellType.Line);


            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Fizz", "[7UP]One Key To Fish", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQCombo", "Use Q"));
            ComboMenu.Add(new MenuBool("UseWCombo", "Use W"));
            ComboMenu.Add(new MenuBool("UseECombo", "Use E"));
            ComboMenu.Add(new MenuBool("UseRCombo", "Use R"));
            //ComboMenu.Add(new MenuKeyBind("RKey", "Semi R Key", Keys.T, KeyBindType.Press));
            ComboMenu.Add(new MenuBool("UseREGapclose", "Use R, then E for gapclose if killable"));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("UseQMixed", "Use Q"));
            HarassMenu.Add(new MenuBool("UseWMixed", "Use W"));
            HarassMenu.Add(new MenuBool("UseEMixed", "Use E"));
            HarassMenu.Add(new MenuBool("LastW", "LastHit W"));
            HarassMenu.Add(new MenuList("UseEHarassMode", "E Mode: ", new[] { "Back to Position", "On Enemy" }));
            MenuRyze.Add(HarassMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuSeparator("AntiGap Settings", "AntiGap Settings"));
            Misc.Add(new MenuBool("UseETower", "Dodge tower shots with E"));
            Misc.Add(new MenuList("UseWWhen", "Use W: ", new[] { "Before Q", "After Q" }));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuSeparator("KillSteal Settings", "KillSteal Settings"));
            KillStealMenu.Add(new MenuBool("KsQ", "Use Q KillSteal"));
            KillStealMenu.Add(new MenuBool("KsW", "Use W KillSteal"));
            KillStealMenu.Add(new MenuBool("KsR", "Use R KillSteal"));
            KillStealMenu.Add(new MenuBool("ign", "Use Ignite KillSteal"));
            MenuRyze.Add(KillStealMenu);
            draw = new Menu("draw", "draw");
            draw.Add(new MenuBool("DrawQ", "Draw Q"));
            draw.Add(new MenuBool("DrawE", "Draw E"));
            draw.Add(new MenuBool("DrawR", "Draw R"));
            draw.Add(new MenuBool("DrawRPred", "Draw R Prediction"));
            MenuRyze.Add(draw);
            MenuRyze.Attach();


            RRectangle = new Geometry.Polygon();

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            //Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;
            //AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQ = draw["DrawQ"].GetValue<MenuBool>().Enabled;
            var drawE = draw["DrawE"].GetValue<MenuBool>().Enabled;
            var drawR = draw["DrawR"].GetValue<MenuBool>().Enabled;
            var drawRPred = draw["DrawRPred"].GetValue<MenuBool>().Enabled;
            var p = Player.Position;
            if (drawQ)
            {
                Render.Circle.DrawCircle(p, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawE)
            {
                Render.Circle.DrawCircle(p, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawR)
            {
                Render.Circle.DrawCircle(p, R.Range, R.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawRPred && R.IsReady() && DrawTarget.IsValidTarget())
            {
                RRectangle.Draw(Color.CornflowerBlue, 3);
            }
        }
        private static float DamageToUnit(AIHeroClient target)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            }

            return (float)damage;
        }

        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender is AITurretClient && args.Target.IsMe && E.IsReady() && Misc["UseETower"].GetValue<MenuBool>().Enabled)
            {
                E.Cast(Game.CursorPos);
            }

            if (!sender.IsMe)
            {
                return;
            }

            if (args.SData.Name == "FizzPiercingStrike")
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    DelayAction.Add((int)(sender.Spellbook.CastEndTime - Game.Time) + Game.Ping / 2 + 250, () => W.Cast());
                }
                else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass &&
                         HarassMenu["UseEHarassMode"].GetValue<MenuList>().Index == 0)
                {
                    DelayAction.Add(
                        (int)(sender.Spellbook.CastEndTime - Game.Time) + Game.Ping / 2 + 250, () => { JumpBack = true; });
                }
            }

            if (args.SData.Name == "fizzjumptwo" || args.SData.Name == "fizzjumpbuffer")
            {
                LastHarassPos = null;
                JumpBack = false;
            }
        }
        public static bool JumpBack { get; set; }


        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }

            if (!Player.CanCast)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.Combo:
                    DoCombo();
                    break;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
            }
        }
        public static void CastRSmart(AIHeroClient target)
        {
            var castPosition = R.GetPrediction(target).CastPosition;
            castPosition = Player.Position.Extend(castPosition, R.Range);

            R.Cast(castPosition);
        }
        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            if (R.IsReady() && ComboMenu["RKey"].GetValue<MenuKeyBind>().Active)
            {
                R.Cast(target);
            }

            if (!target.IsValidTarget())
            {
                return;
            }


            if (ComboMenu["UseREGapclose"].GetValue<MenuBool>().Enabled && CanKillWithUltCombo(target) && Q.IsReady() && W.IsReady() &&
                E.IsReady() && R.IsReady() && (Player.Distance(target) < Q.Range + E.Range * 2))
            {
                CastRSmart(target);

                E.Cast(Player.Position.Extend(target.Position, E.Range - 1));
                E.Cast(Player.Position.Extend(target.Position, E.Range - 1));

                W.Cast();
                Q.Cast(target);
            }
            else
            {
                if (R.IsReady() && ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled)
                {
                    if (Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                        CastRSmart(target);
                    }

                    if (DamageToUnit(target) > target.Health)
                    {
                        CastRSmart(target);
                    }

                    if ((Q.IsReady() || E.IsReady()))
                    {
                        CastRSmart(target);
                    }

                    if (Player.InAutoAttackRange(target))
                    {
                        CastRSmart(target);
                    }
                }

                // Use W Before Q
                if (W.IsReady() && Misc["UseWWhen"].GetValue<MenuList>().Index == 0 &&
                    (Q.IsReady() || Player.InAutoAttackRange(target)))
                {
                    W.Cast();
                }

                if (Q.IsReady() && ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled)
                {
                    Q.Cast(target);
                }

                if (E.IsReady() && ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled)
                {
                    E.Cast(target);
                }
                // Use W After Q
                if (W.IsReady() && Misc["UseWWhen"].GetValue<MenuList>().Index == 1 &&
                    (Q.IsReady() || Player.InAutoAttackRange(target)))
                {
                    Q.Cast();
                }

                if (W.IsReady() && ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled)
                {
                    W.Cast(target);
                }
                if (E.IsReady() && ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled)
                {
                    E.Cast(target);
                }
            }
        }
        public static bool CanKillWithUltCombo(AIHeroClient target)
        {
            return Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.W) + Player.GetSpellDamage(target, SpellSlot.R) >
                   target.Health;
        }
        private static void DoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (LastHarassPos == null)
            {
                LastHarassPos = ObjectManager.Player.Position;
            }

            if (JumpBack)
            {
                E.Cast((Vector3)LastHarassPos);
            }

            // Use W Before Q
            if (W.IsReady() && Misc["UseWWhen"].GetValue<MenuList>().Index == 0 &&
                (Q.IsReady() || Player.InAutoAttackRange(target)))
            {
                W.Cast();
            }

            if (Q.IsReady() && HarassMenu["UseQMixed"].GetValue<MenuBool>().Enabled)
            {
                Q.Cast(target);
            }

            if (E.IsReady() && HarassMenu["UseEMixed"].GetValue<MenuBool>().Enabled)
            {
                E.Cast(target);
            }
            // Use W After Q
            if (W.IsReady() && Misc["UseWWhen"].GetValue<MenuList>().Index == 1 &&
                (Q.IsReady() || Player.InAutoAttackRange(target)))
            {
                Q.Cast();
            }

            if (W.IsReady() && HarassMenu["UseWMixed"].GetValue<MenuBool>().Enabled)
            {
                W.Cast(target);
            }
            if (E.IsReady() && HarassMenu["UseEMixed"].GetValue<MenuBool>().Enabled)
            {
                E.Cast(target);
            }
        }
        private static void LastHit()
        {
            var MHR = GameObjects.EnemyMinions.Where(a => a.Distance(Player) <= W.Range).OrderBy(a => a.Health).FirstOrDefault();
            if (MHR != null)
            {
                if (HarassMenu["LastW"].GetValue<MenuBool>().Enabled && W.IsReady() &&  MHR.IsValidTarget(W.Range) && Player.GetSpellDamage(MHR, SpellSlot.Q) >= MHR.Health)

                {
                    W.Cast(MHR);
                }

            }
        }

        public static double QDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Magical,
                    (float)(new[] { 0, 10, 25, 40, 55, 70 }[Q.Level] + 0.55f * Player.FlatMagicDamageMod));

        }

        public static double WDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Magical,
                (float)(new[] { 0, 50, 70, 90, 110, 130 }[W.Level] + 0.5f * Player.FlatMagicDamageMod));
        }

        public static double RDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Magical,
                (float)(new[] { 0, 300, 400, 160 }[R.Level] + 1.2f * Player.FlatMagicDamageMod));
        }

        public static void KillSteal()
        {
            if (Player.HasBuff("FishNoHope"))
            {
                return;
            }
            var KsQ = KillStealMenu["KsQ"].GetValue<MenuBool>().Enabled;
            var KsW = KillStealMenu["KsW"].GetValue<MenuBool>().Enabled;
            var KsR = KillStealMenu["KsR"].GetValue<MenuBool>().Enabled;
            var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(Q.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
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
                if (KsR && R.IsReady() && target.IsValidTarget(500))
                {

                        if (target != null)
                    {
                        if (target.Health + target.AllShield <= RDamage(target))
                        {
                            CastRSmart(t);
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