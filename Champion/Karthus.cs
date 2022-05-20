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
    internal class Karthus
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, Items;
        //public static Helper Helper;
        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell Ignite;

        public const float SpellQWidth = 160f;
        public const float SpellWWidth = 160f;

        public static bool _comboE;
        public static Vector2 PingLocation;
        public static int LastPingT = 0;


        public static void OnGameLoad()
        {
            if (!_Player.CharacterName.Contains("Karthus")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 875);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 505);
            R = new Spell(SpellSlot.R, 20000);
            Q.SetSkillshot(1f, 160, float.MaxValue, false, SpellType.Circle);
            W.SetSkillshot(.5f, 70, float.MaxValue, false, SpellType.Circle);
            E.SetSkillshot(1f, 505, float.MaxValue, false, SpellType.Circle);
            R.SetSkillshot(3f, float.MaxValue, float.MaxValue, false, SpellType.Circle);
            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            var MenuRyze = new Menu("Karthus", "[7UP]Karthus", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("comboQ", "Use Q"));
            ComboMenu.Add(new MenuBool("comboW", "Use W"));
            ComboMenu.Add(new MenuBool("comboE", "Use E"));
            //ComboMenu.Add(new MenuBool("comboAA", "Use AA"));
            ComboMenu.Add(new MenuSlider("comboWPercent", "Use W until Mana %", 10));
            ComboMenu.Add(new MenuSlider("comboEPercent", "Use E until Mana %", 15));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("harassQ", "Use Q"));
            HarassMenu.Add(new MenuSlider("harassQPercent", "Use Q until Mana %", 15));
            HarassMenu.Add(new MenuBool("harassQLasthit", "Prioritize Last Hit"));
            HarassMenu.Add(new MenuKeyBind("harassQToggle", "Toggle Q", Keys.G, KeyBindType.Toggle)).Permashow();
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuList("farmQ", "Use Q", new[] { "Last Hit", "Lane Clear", "Both", "No" }, 1)).Permashow();
            LaneClearMenu.Add(new MenuBool("farmE", "Use E in Lane Clear"));
            LaneClearMenu.Add(new MenuBool("farmAA", "Use AA in Lane Clear"));
            LaneClearMenu.Add(new MenuSlider("farmQPercent", "Use Q until Mana %", 10));
            LaneClearMenu.Add(new MenuSlider("farmEPercent", "Use E until Mana %", 20));
            MenuRyze.Add(LaneClearMenu);
            JungleClearMenu = new Menu("JungleClear Settings", "JungleClear");
            JungleClearMenu.Add(new MenuBool("useQ", "Use Q ", true));
            JungleClearMenu.Add(new MenuBool("useE", "Use E ", true));
            MenuRyze.Add(JungleClearMenu);
            Misc = new Menu("Misc Settings", "Misc");
            //Misc.Add(new MenuBool("ultKS", "Ultimate KS")).Permashow();
            Misc.Add(new MenuBool("autoCast", "Auto Combo/LaneClear if dead", false));
            Misc.Add(new MenuBool("packetCast", "Packet Cast"));
            MenuRyze.Add(Misc);
            MenuRyze.Attach();

            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.OnBeforeAttack += Orbwalker_OnAfterAttack;
        }



        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            //if (Misc["ultKS"].GetValue<MenuBool>().Enabled)
                //UltKs();

            /*if (_menu.Item("notifyPing").GetValue<bool>())
                foreach (
                    var enemy in
                        HeroManager.Enemies.Where(
                            t =>
                                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready &&
                                t.IsValidTarget() && _spellR.GetDamage(t) > t.Health &&
                                t.Distance(ObjectManager.Player.Position) > _spellQ.Range))
                {
                    Ping(enemy.Position.To2D());
                }*/

            if(ObjectManager.Player.ManaPercent >= HarassMenu["harassQPercent"].GetValue<MenuSlider>().Value)
            {
                if (Q.IsReady() && HarassMenu["harassQToggle"].GetValue<MenuKeyBind>().Active &&
                    Orbwalker.ActiveMode != OrbwalkerMode.Combo)
                {
                    CastQ(TargetSelector.GetTarget(Q.Range, DamageType.Magical));
                }
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    logicE();
                    return;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
                default:
                    //RegulateEState();

                    if (Misc["autoCast"].GetValue<MenuBool>().Enabled)
                        if (IsInPassiveForm())
                        { Combo();
                                LaneClear();
                        }
                    break;

            }
            if (GameObjects.Player.Level > 1)
            {
                if (GameObjects.Player.ManaPercent > 10 && !GameObjects.Player.IsUnderEnemyTurret() && enemyobj() == 0
                    && Orbwalker.ActiveMode == OrbwalkerMode.Combo
                    || Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
                {
                    Orbwalker.AttackEnabled = false;
                }
                else
                {
                    Orbwalker.AttackEnabled = true;
                }
            }
            loigcee();
            logicR();
        }

        public static void loigcee()
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.None && E.ToggleState == SpellToggleState.On)
            {
                E.Cast();
            }
        }
        public static int enemyobj()
        {
            var target = Q.GetTarget();
            bool logic = GameObjects.Player.InAutoAttackRange(target);
            int logicf;

            if (logic && !target.HasBuffOfType(BuffType.Poison))
            {
                logicf = 1;
            }
            else
            {
                logicf = 0;
            }

            var inhibs = GameObjects.EnemyInhibitors
                .Where(x => x.IsValidTarget(650f))
                .ToList();

            var nex = Q.IsInRange(GameObjects.EnemyNexus);

            int nexint;

            if (nex == false)
            {
                nexint = 0;
            }
            else
            {
                nexint = 1;
            }

            return inhibs.Count + nexint + logicf;
        }

        /*public static void RegulateEState(bool ignoreTargetChecks = false)
        {
            if (!E.IsReady() || IsInPassiveForm() && E.ToggleState == SpellToggleState.Off)
                return;
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var minions = GameObjects.EnemyMinions.Where(e => e.IsValidTarget(E.Range) && e.IsMinion())
                            .Cast<AIBaseClient>().ToList();

            if (!ignoreTargetChecks && (target != null || (!_comboE && minions.Count != 0)))
                return;
            E.CastOnUnit(ObjectManager.Player);
            _comboE = false;
        }*/

        public static void CastQ(AIBaseClient target, int minManaPercent = 0)
        {
            if (!Q.IsReady() || !(GetManaPercent() >= minManaPercent))
                return;
            if (target == null)
                return;
            Q.Width = GetDynamicQWidth(target);
            Q.Cast(target);
        }

        public static void CastQ(Vector2 pos, int minManaPercent = 0)
        {
            if (!Q.IsReady())
                return;
            if (GetManaPercent() >= minManaPercent)
                Q.Cast(pos);
        }

        public static void CastW(AIBaseClient target, int minManaPercent = 0)
        {
            if (!W.IsReady() || !(GetManaPercent() >= minManaPercent))
                return;
            if (target == null)
                return;
            W.Width = GetDynamicWWidth(target);
            W.Cast(target);
        }

        public static  float GetDynamicWWidth(AIBaseClient target)
        {
            return Math.Max(70, (1f - (ObjectManager.Player.Distance(target) / W.Range)) * SpellWWidth);
        }

        public static float GetDynamicQWidth(AIBaseClient target)
        {
            return Math.Max(30, (1f - (ObjectManager.Player.Distance(target) / Q.Range)) * Q.Width);
        }

        public static float GetManaPercent()
        {
            return (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana) * 100f;
        }

        public bool PacketsNoLel()
        {
            return Misc["packetCast"].GetValue<MenuBool>().Enabled;
        }

        public static bool IsInPassiveForm()
        {
            return ObjectManager.Player.IsZombie(); //!ObjectManager.Player.IsHPBarRendered;
        }

        /*public static void UltKs()
        {
            if (!R.IsReady())
                return;
            var time = Game.Time;

            List<AIHeroClient> ultTargets = new List<AIHeroClient>();

            foreach (
                var target in
                    Program.Helper.EnemyInfo.Where(
                        x => //need to check if recently recalled (for cases when no mana for baseult)
                            x.Player.IsValid &&
                            !x.Player.IsDead &&
                            x.Player.IsEnemy &&
                            //!(x.RecallInfo.Recall.Status == Packet.S2C.Recall.RecallStatus.RecallStarted && x.RecallInfo.GetRecallCountdown() < 3100) && //let BaseUlt handle this one
                            ((!x.Player.IsVisible && time - x.LastSeen < 10000) ||
                             (x.Player.IsVisible && x.Player.IsValidTarget())) &&
                            ObjectManager.Player.GetSpellDamage(x.Player, SpellSlot.R) >=
                            Program.Helper.GetTargetHealth(x, (int)(R.Delay * 1000f))))
            {
                if (target.Player.IsVisible || (!target.Player.IsVisible && time - target.LastSeen < 2750))
                    //allies still attacking target? prevent overkill
                    if (Program.Helper.OwnTeam.Any(x => !x.IsMe && x.Distance(target.Player) < 1600))
                        continue;

                if (IsInPassiveForm() ||
                    !Program.Helper.EnemyTeam.Any(
                        x =>
                            x.IsValid && !x.IsDead &&
                            (x.IsVisible || (!x.IsVisible && time - Program.Helper.GetPlayerInfo(x).LastSeen < 2750)) &&
                            ObjectManager.Player.Distance(x) < 1600))
                    //any other enemies around? dont ult unless in passive form
                    ultTargets.Add(target.Player);
            }

            int targets = ultTargets.Count();

            if (targets > 0)
            {
                //dont ult if Zilean is nearby the target/is the target and his ult is up
                var zilean =
                    Program.Helper.EnemyTeam.FirstOrDefault(
                        x =>
                            x.CharacterName == "Zilean" &&
                            (x.IsVisible || (!x.IsVisible && time - Program.Helper.GetPlayerInfo(x).LastSeen < 3000)) &&
                            (x.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready ||
                             (x.Spellbook.GetSpell(SpellSlot.R).Level > 0 &&
                              x.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Surpressed &&
                              x.Mana >= ObjectManager.Player.ManaPercent)));

                if (zilean != null)
                {
                    int inZileanRange = ultTargets.Count(x => x.Distance(zilean) < 2500);
                    //if multiple, shoot regardless

                    if (inZileanRange > 0)
                        targets--; //remove one target, because zilean can save one
                }

                if (targets > 0)
                    R.Cast();
            }
        }*/

        public static void Combo()
        {
            bool anyQTarget = false;

            if (ComboMenu["comboW"].GetValue<MenuBool>().Enabled)
                CastW(TargetSelector.GetTarget(W.Range, DamageType.Magical),
                    ComboMenu["comboWPercent"].GetValue<MenuSlider>().Value);

            if (ComboMenu["comboQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (target != null)
                {
                    anyQTarget = true;
                    CastQ(target);
                }
            }

            //return anyQTarget;
        }

        public static void logicE()
        {
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);

            if (!target.IsValidTarget(E.Range))
                return;
            if (ObjectManager.Player.ManaPercent > ComboMenu["comboEPercent"].GetValue<MenuSlider>().Value && ComboMenu["comboE"].GetValue<MenuBool>().Enabled)
            {
                if (target.InAutoAttackRange(E.Range) && E.ToggleState == SpellToggleState.Off)
                {
                    E.Cast();   
                }
            }

            if (GameObjects.Player.CountEnemyHeroesInRange(E.Range) == 0 && E.ToggleState == SpellToggleState.On)
            {
                E.Cast(target);
            }
        }

        public static void logicR()
        {
            if (R.IsReady())
            {
                var target = R.GetTarget();

                if (!target.IsValidTarget())
                    return;

                var dmg = R.GetDamage(target);
                var dmg1 = target.Health;

                if (target.HasBuffOfType(BuffType.SpellImmunity))
                {
                    return;
                }

                if (!W.IsInRange(target, W.Range + 250f)
                    && dmg >= dmg1
                    && GameObjects.Player.CountEnemyHeroesInRange(W.Range) == 0
                    && !GameObjects.Player.IsUnderEnemyTurret()
                    && target.CountAllyHeroesInRange(W.Range) == 0)
                {
                    R.Cast();
                }

                if (dmg >= dmg1
                    && GameObjects.Player.HasBuff("KarthusDeathDefiedBuff")
                    && target.CountAllyHeroesInRange(W.Range) == 0)
                {
                    R.Cast();
                }
            }
        }

        public static void JungleClear()
        {
            var final = GameObjects.Jungle.Where(x => x.IsValidTarget(650f)).Cast<AIBaseClient>().ToHashSet();
            var target = Q.GetTarget(Q.Range);

            if (E.ToggleState == SpellToggleState.On && final.Count == 0 && target == null)
            {
                E.Cast();
            }

            if (final.Count <= 0)
            {
                return;
            }

            var farmloc = Q.GetCircularFarmLocation(final, 150f);

            if (farmloc.MinionsHit >= 1 && JungleClearMenu["useQ"].GetValue<MenuBool>().Enabled)
            {
                Q.Cast(farmloc.Position);
            }


            foreach (var mob in final)
            {
                if (Q.IsReady() && JungleClearMenu["useQ"].GetValue<MenuBool>().Enabled)
                {
                    var input = Q.GetPrediction(mob);
                    Q.Cast(input.CastPosition);
                }
                if (E.ToggleState == SpellToggleState.Off && mob.InRange(E.Range) && JungleClearMenu["useE"].GetValue<MenuBool>().Enabled)
                {
                    E.Cast();
                }
            }

            return;
        }
        public static void LaneClear(bool ignoreConfig = false)
        {
            var farmQ = ignoreConfig || LaneClearMenu["farmQ"].GetValue<MenuList>().Index == 1 ||
            LaneClearMenu["farmQ"].GetValue<MenuList>().Index == 2;
            var farmE = ignoreConfig || LaneClearMenu["farmE"].GetValue<MenuBool>().Enabled;

            List<AIBaseClient> minions;

            bool jungleMobs;
            if (farmQ && Q.IsReady())
            {
                minions = GameObjects.EnemyMinions.Where(e => e.IsValidTarget(Q.Range) && e.IsMinion())
                            .Cast<AIBaseClient>().ToList();
                 //filter wards the ghetto method lel

                jungleMobs = minions.Any(x => x.Team == GameObjectTeam.Neutral);

                Q.Width = SpellQWidth;
                var farmInfo = Q.GetCircularFarmLocation(minions, Q.Width);

                if (farmInfo.MinionsHit >= 1)
                    CastQ(farmInfo.Position, jungleMobs ? 0 : LaneClearMenu["farmQPercent"].GetValue<MenuSlider>().Value);
            }

            if (!farmE || !E.IsReady() || IsInPassiveForm())
                return;
            _comboE = false;

            minions = GameObjects.EnemyMinions.Where(e => e.IsValidTarget(E.Range) && e.IsMinion())
                            .Cast<AIBaseClient>().ToList();

            jungleMobs = minions.Any(x => x.Team == GameObjectTeam.Neutral);

            var enoughMana = GetManaPercent() > LaneClearMenu["farmEPercent"].GetValue<MenuSlider>().Value;

            if (enoughMana &&
                ((minions.Count >= 3 || jungleMobs) &&
                 E.ToggleState == SpellToggleState.On))
                E.CastOnUnit(ObjectManager.Player);
            else if (!enoughMana ||
                     ((minions.Count <= 2 && !jungleMobs) &&
                      E.ToggleState == SpellToggleState.Off)) ;
            //RegulateEState(!enoughMana);
            else
            {
                Harass();
            }
        }


        public static void Harass()
        {
            {
                if (HarassMenu["harassQLasthit"].GetValue<MenuBool>().Enabled)
                    LastHit();

                if (HarassMenu["harassQ"].GetValue<MenuBool>().Enabled)
                    CastQ(TargetSelector.GetTarget(Q.Range, DamageType.Magical),
                        HarassMenu["harassQPercent"].GetValue<MenuSlider>().Value);
            }

        }

        public static void LastHit()
        {
            var farmQ = LaneClearMenu["farmQ"].GetValue<MenuList>().Index == 0 ||
                        LaneClearMenu["farmQ"].GetValue<MenuList>().Index == 2;

            if (!farmQ || !Q.IsReady())
                return;

            var minions = GameObjects.EnemyMinions.Where(e => e.IsValidTarget(Q.Range) && e.IsMinion())
                            .Cast<AIBaseClient>().ToList();

            {
                CastQ(TargetSelector.GetTarget(Q.Range, DamageType.Magical), (LaneClearMenu["farmQPercent"].GetValue<MenuSlider>().Value));
            }
        }

        public static void Orbwalker_OnAfterAttack(object e, BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                args.Process = !Q.IsReady();
            }
            else if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                bool farmQ = LaneClearMenu["farmQ"].GetValue<MenuList>().Index == 0 ||
                             LaneClearMenu["farmQ"].GetValue<MenuList>().Index == 2;
                args.Process =
                    !(farmQ && Q.IsReady() &&
                      GetManaPercent() >= LaneClearMenu["farmQPercent"].GetValue<MenuSlider>().Value);
            }
        }

    }
}
