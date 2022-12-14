using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;

namespace AIO7UP.Champions
{
    internal class Jinx
    {
        public static Menu Menu, QMenu, WMenu, EMenu, RMenu, drawMenu;
        private static bool FishBoneActive = false, Combo = false, Farm = false;
        private static AIHeroClient blitz = null;
        private static float WCastTime = Game.Time, grabTime = 0;
        private static string[] Spells =
{
            "katarinar","drain","consume","absolutezero", "staticfield","reapthewhirlwind","jinxw","jinxr","shenstandunited","threshe","threshrpenta","threshq","meditate","caitlynpiltoverpeacemaker", "volibearqattack",
            "cassiopeiapetrifyinggaze","ezrealtrueshotbarrage","galioidolofdurand","luxmalicecannon", "missfortunebullettime","infiniteduress","alzaharnethergrasp","lucianq","velkozr","rocketgrabmissile"
        };
        private static List<AIHeroClient> Enemies = new List<AIHeroClient>();
        private static float bigGunRange;
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Jinx")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 925f);
            R = new Spell(SpellSlot.R, 3000f);

            W.SetSkillshot(0.6f, 60f, 3300f, true, SpellType.Line);
            E.SetSkillshot(1.2f, 100f, 1750f, false, SpellType.Circle);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SpellType.Line);

            var MenuRyze = new Menu("Jinx", "[7UP]Jinx", true);
            QMenu = new Menu("QMenu", "QMenu");
            QMenu.Add(new MenuBool("Qcombo", "Combo Q"));
            QMenu.Add(new MenuBool("Qharass", "Harass Q"));
            QMenu.Add(new MenuBool("farmQout", "Farm Q out range AA minion"));
            QMenu.Add(new MenuSlider("Qlaneclear", "Lane clear x minions", 2, 4, 10));
            QMenu.Add(new MenuList("Qchange", "Q change mode FishBone -> MiniGun", new[] { "Real Time", "Before AA"}, 1));
            QMenu.Add(new MenuSlider("Qaoe", "Force FishBone if can hit x target", 3, 0, 5));
            QMenu.Add(new MenuSlider("QmanaIgnore", "Ignore mana if can kill in x AA", 3, 0, 10));
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
                QMenu.Add(new MenuBool("harassQ", "Harass Q enemy:" + enemy.CharacterName));
            QMenu.Add(new MenuSlider("QmanaCombo", "Q combo mana", 10, 0, 100));
            QMenu.Add(new MenuSlider("QmanaHarass", "Q harass mana", 40, 0, 100));
            QMenu.Add(new MenuSlider("QmanaLC", "Q lane clear mana", 80, 0, 100));
            MenuRyze.Add(QMenu);
            WMenu = new Menu("WMenu", "WMenu");
            WMenu.Add(new MenuKeyBind("useW", "Semi cast W key", Keys.S, KeyBindType.Press));
            WMenu.Add(new MenuBool("Wcombo", "Combo W"));
            WMenu.Add(new MenuBool("Wharass", "W harass"));
            WMenu.Add(new MenuBool("Wks", "W KS"));
            WMenu.Add(new MenuList("Wts", "Harass mode", new[] { "Target selector", "All in range" }, 0));
            WMenu.Add(new MenuList("Wmode", "W mode", new[] { "Out range MiniGun", "Out range FishBone", "Custome range" }, 0));
            WMenu.Add(new MenuSlider("Wcustome", "Custome minimum range", 600, 0, 1500));
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
                WMenu.Add(new MenuBool("harassW", "Harass W enemy:" + enemy.CharacterName));
            WMenu.Add(new MenuSlider("WmanaCombo", "W combo mana", 20, 0, 100));
            WMenu.Add(new MenuSlider("WmanaHarass", "W harass mana", 40, 0, 100));
            MenuRyze.Add(WMenu);
            EMenu = new Menu("EMenu", "EMenu");
            EMenu.Add(new MenuBool("Ecombo", "Combo E"));
            EMenu.Add(new MenuBool("AutoEWhenEnemyCastAAM", "Use Auto E When Melee Enemy Cast AA On Me"));
            EMenu.Add(new MenuKeyBind("useE", "Semi cast E key", Keys.G, KeyBindType.Press));
            EMenu.Add(new MenuBool("Etel", "E on enemy teleport"));
            EMenu.Add(new MenuBool("Ecc", "E on CC"));
            //EMenu.Add(new MenuBool("Eslow", "E on slow"));
            //EMenu.Add(new MenuBool("Edash", "E on dash"));
            //EMenu.Add(new MenuBool("Espell", "E on special spell detection"));
            //EMenu.Add(new MenuSlider("Eaoe", "E if can catch x enemies", 3, 0, 5));
            EMenu.Add(new MenuBool("E Gap", "E Gap"));
            /*EMenu.Add(new MenuList("EmodeGC", "Gap Closer position mode", new[] { "Dash end position", "Jinx position" }, 0));
            foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy))
                EMenu.Add(new MenuBool("EGCchampion", "Cast on enemy:" + enemy.CharacterName));
            EMenu.Add(new MenuSlider("EmanaCombo", "E mana", 30, 0, 100));*/
            MenuRyze.Add(EMenu);
            RMenu = new Menu("RMenu", "RMenu");
            RMenu.Add(new MenuBool("Rks", "R KS"));
            RMenu.Add(new MenuKeyBind("useR", "Semi-manual cast R key", Keys.T, KeyBindType.Press));
            /*RMenu.Add(new MenuList("semiMode", "Semi-manual cast mode", new[] { "Low hp target", "AOE" }, 0));
            RMenu.Add(new MenuList("Rmode", "R mode", new[] { "Out range MiniGun ", "Out range FishBone ", "Custome range " }, 0));
            RMenu.Add(new MenuSlider("Rcustome", "Custome minimum range", 1000, 0, 1600));
            RMenu.Add(new MenuSlider("RcustomeMax", "Max range", 3000, 0, 10000));
            RMenu.Add(new MenuSlider("Raoe", "R if can hit x target and can kill", 2, 0, 5));
            RMenu.Add(new MenuSlider("Rover", "Don't R if allies near target in x range ", 500, 1000, 1000));*/
            RMenu.Add(new MenuBool("ComboRTeam", "Use R|Team Fight"));
            RMenu.Add(new MenuBool("ComboRSolo", "Use R|Solo Mode"));
            RMenu.Add(new MenuSlider("rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500));
            RMenu.Add(new MenuSlider("rMenuMax", "Use R| Man Range <= x", 3000, 1500, 3500));
            MenuRyze.Add(RMenu);
            drawMenu = new Menu("Drawing", "Drawing");
            drawMenu.Add(new MenuBool("DrawQ", "Draw Q").SetValue(true));
            drawMenu.Add(new MenuBool("DrawW", "Draw W").SetValue(true));
            drawMenu.Add(new MenuBool("DrawE", "Draw E").SetValue(true));
            drawMenu.Add(new MenuBool("DrawR", "Draw E").SetValue(true));
            drawMenu.Add(new MenuBool("onlyRdy", "Draw only ready spells").SetValue(true));
            drawMenu.Add(new MenuBool("noti", "Show notification").SetValue(true));
            MenuRyze.Add(drawMenu);
            MenuRyze.Attach();
            //Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.OnAfterAttack += Orbwalker_BeforeAttack;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            AIBaseClient.OnDoCast += Obj_AI_Base_OnProcessSpellCast;
        }

        /*private static void Drawing_OnDraw(EventArgs args)
        {
            var qRange = drawMenu["DrawQ"].GetValue<MenuBool>().Enabled;
            var wRange = drawMenu["DrawW"].GetValue<MenuBool>().Enabled;
            var eRange = drawMenu["DrawE"].GetValue<MenuBool>().Enabled;
            var rRange = drawMenu["DrawR"].GetValue<MenuBool>().Enabled;
            var onlyRdy = drawMenu["onlyRdy"].GetValue<MenuBool>().Enabled;
            var noti = drawMenu["noti"].GetValue<MenuBool>().Enabled;
            if (qRange)
            {
                if (onlyRdy)
                {
                    Render.Circle.DrawCircle(Player.Position, 590f + Player.BoundingRadius, System.Drawing.Color.Cyan, 1);
                }
            }

            if (wRange)
            {
                if (onlyRdy)
                {
                    if (W.IsReady())
                    {
                        Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                    }
                }
                else
                {
                    Render.Circle.DrawCircle(Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                }
            }

            if (eRange)
            {
                if (onlyRdy)
                {
                    if (E.IsReady())
                    {
                        Render.Circle.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                    }
                }
                else
                {
                    Render.Circle.DrawCircle(Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                }
            }

            if (noti)
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);

                if (t.IsValidTarget(2000) && W.GetDamage(t) > t.Health)
                {
                    Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "W can kill: " + t.CharacterName + " have: " + t.Health + " hp");
                    Drawing.DrawLine(Drawing.WorldToScreen(Player.Position), Drawing.WorldToScreen(t.Position), 3, System.Drawing.Color.Yellow);
                }
                else if (R.IsReady() && t.IsValidTarget())
                {
                    Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.CharacterName + " have: " + t.Health + " hp");
                    Drawing.DrawLine(Drawing.WorldToScreen(Player.Position), Drawing.WorldToScreen(t.Position), 5, System.Drawing.Color.Red);
                }
            }
        }*/

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            {

                if (EMenu["E Gap"].GetValue<MenuBool>().Enabled && E.IsReady())
                {
                    if (sender.IsValidTarget(E.Range))
                    {
                        E.Cast(args.EndPosition);
                    }
                }
            }
        }
        public static void Orbwalker_BeforeAttack(object e, AfterAttackEventArgs args)
        {
            if (!FishBoneActive)
                return;

            if (Q.IsReady() && args.Target is AIHeroClient && QMenu["Qchange"].GetValue<MenuList>().Index == 1)
            {
                var t = (AIHeroClient)args.Target;
                if (t.IsValidTarget())
                {
                    FishBoneToMiniGun(t);
                }
            }

            if (!Combo && args.Target is AIMinionClient)
            {
                var t = (AIMinionClient)args.Target;
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Player.ManaPercent > QMenu["QmanaLC"].GetValue<MenuSlider>().Value && CountMinionsInRange(250, t.Position) >= QMenu["Qlaneclear"].GetValue<MenuSlider>().Value)
                {

                }
                else if (GetRealDistance(t) < GetRealPowPowRange(t))
                {
                    if (Q.IsReady())
                        Q.Cast();
                }
            }

        }
        private static void Obj_AI_Base_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {

            if (sender.IsMe)
            {
                if (args.SData.Name == "JinxWMissile")
                    WCastTime = Game.Time;
            }

            if (!E.IsReady() || !sender.IsEnemy || !EMenu["Espell"].GetValue<MenuBool>().Enabled || Player.ManaPercent < EMenu["EmanaCombo"].GetValue<MenuSlider>().Value || !sender.IsValid() || !sender.IsValidTarget(E.Range))
                return;

            var foundSpell = Spells.Find(x => args.SData.Name.ToLower() == x);
            if (foundSpell != null)
            {
                E.Cast(sender.Position);
            }
            if (EMenu["AutoEWhenEnemyCastAAM"].GetValue<MenuBool>().Enabled && (sender.IsValid() && !sender.IsValidTarget(E.Range)) && sender.IsEnemy && args.Target.IsMe && Player.Spellbook.IsAutoAttack && E.IsReady() && Player.Distance(sender) < 300)
            {
                E.Cast(Player.ServerPosition);
            }

            /*if (Config.Item("Jinx.AutoEWhenEnemyCastAAR").GetValue<bool>() && (unit.IsValid<AIHeroClient>() && !unit.IsValid<Obj_AI_Turret>()) && unit.IsEnemy && args.Target.IsMe && args.SData.IsAutoAttack() && E.IsReady() && Player.Distance(unit) > 300 && Player.Distance(unit) <= E.Range && Player.CountEnemiesInRange(300) == 0)
            {
                E.Cast(unit, true, true);
            }*/

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                if (EMenu["Ecombo"].GetValue<MenuBool>().Enabled &&  
                        (sender.IsValid() && !sender.IsValidTarget(E.Range) && sender.IsEnemy && args.Target.IsMe && Player.Spellbook.IsAutoAttack && E.IsReady() && Player.Distance(sender) < 300))
                {
                    E.Cast(Player.ServerPosition);
                }

                if (EMenu["Ecombo"].GetValue<MenuBool>().Enabled && (sender.IsValid() && !sender.IsValidTarget(E.Range)) && sender.IsEnemy && args.Target.IsMe && Player.Spellbook.IsAutoAttack && E.IsReady() && Player.Distance(sender) > 300 && Player.Distance(sender) <= E.Range && Player.CountEnemyHeroesInRange(300) == 0)
                {
                    E.Cast(sender, true, true);
                }
            }
        
    }
    private static void Game_OnUpdate(EventArgs args)
        {
            if (R.Level > 0)
            {
                R.Range = RMenu["rMenuMax"].GetValue<MenuSlider>().Value;
            }

            SetValues();

            if (Q.IsReady())
                Qlogic();
            if (W.IsReady())
                Wlogic();
            if (E.IsReady())
                Elogic();
            if (R.IsReady())
                Rlogic();
        }



        private static void Rlogic()
        {

            if (RMenu["useR"].GetValue<MenuKeyBind>().Active && R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (t.IsValidTarget() && R.GetPrediction(t).Hitchance >= HitChance.High)
                {
                    R.Cast(t);
                }
            }
            var t1 = TargetSelector.GetTarget(R.Range, DamageType.Physical);

            if (RMenu["Rks"].GetValue<MenuBool>().Enabled && GetKsDamage(t1, R) > t1.Health && R.IsReady() && R.GetPrediction(t1).Hitchance >= HitChance.High)
            {
                if (!R.IsInRange(R.GetTarget(), W.Range + 100f))
                    {
                        R.Cast(t1);
                    }
            }
            if(Combo && R.IsReady())
            {
                foreach (var target in Enemies.Where(x => x.IsValidTarget(1200)))
                {
                    if (RMenu["ComboRTeam"].GetValue<MenuBool>().Enabled && target.IsValidTarget(600) && target.CountEnemyHeroesInRange(600) >= 2 && target.CountAllyHeroesInRange(200) <= 3 && target.HealthPercent < 50)
                    {
                        R.Cast(target);
                    }
                    if (RMenu["ComboRSolo"].GetValue<MenuBool>().Enabled && target.CountEnemyHeroesInRange(1500) <= 2 && target.DistanceToPlayer() > Q.Range && target.DistanceToPlayer() < bigGunRange && target.Health > target.GetAutoAttackDamage(target) && target.Health < R.GetDamage(target) + target.GetAutoAttackDamage(target) * 3)
                    {
                        R.Cast(target);
                    }
                }
            }
        }
        private static bool RValidRange(AIBaseClient t)
        {
            var range = GetRealDistance(t);
            var Rready = R.IsReady();

            if (RMenu["Rmode"].GetValue<MenuList>().Index == 0 && Rready)
            {
                if (range > GetRealPowPowRange(t))
                    return true;
                else
                    return false;

            }
            else if (RMenu["Rmode"].GetValue<MenuList>().Index == 1 && Rready)
            {
                if (range > Q.Range)
                    return true;
                else
                    return false;
            }
            else if (RMenu["Rmode"].GetValue<MenuList>().Index == 2 && Rready)
            {
                if (range > RMenu["Rcustome"].GetValue<MenuSlider>().Value && Rready)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static void Elogic()
        {

            if (EMenu["useE"].GetValue<MenuKeyBind>().Active && E.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (t.IsValidTarget())
                {
                    E.Cast(t);
                }
            }

            if (Combo && EMenu["Ecombo"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                /*if (t.IsValidTarget(E.Range) && E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                {
                    if (Player.Position.Distance(t.Position) > Player.Position.Distance(t.Position))
                    {
                        if (t.Position.Distance(Player.Position) < t.Position.Distance(Player.Position))
                            E.Cast(t);
                    }
                    else
                    {
                        if (t.Position.Distance(Player.Position) > t.Position.Distance(Player.Position))
                            E.Cast(t);
                    }
                }*/


                if (t.IsValidTarget(E.Range))
                {
                    if (!t.CanMove)
                    {
                        E.Cast(t);
                    }
                    else
                    {
                        if (E.GetPrediction(t).Hitchance >= HitChance.VeryHigh && E.GetPrediction(t).CastPosition.Distance(t.Position) > 200)
                        {
                            //if (Player.Position.Distance(t.ServerPosition) > Player.Position.Distance(t.Position))
                            {
                                E.Cast(t);
                            }
                            /*else
                                if (Player.Position.Distance(t.ServerPosition) < Player.Position.Distance(t.Position))
                            {
                                E.Cast(t);
                            }*/
                        }
                        
                    }
                }
            }
            if (EMenu["Ecc"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                foreach (var target in Enemies.Where(x => x.IsValidTarget(E.Range) && !x.CanMove))
                {
                    E.Cast(target);
                }
            }
            if (EMenu["Etel"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                foreach (
                    var obj in
                    ObjectManager.Get<AIBaseClient>()
                        .Where(
                            x =>
                                x.IsEnemy && x.DistanceToPlayer() < E.Range &&
                                (x.HasBuff("teleport_target") || x.HasBuff("Pantheon_GrandSkyfall_Jump"))))
                {
                    E.Cast(obj.Position);
                }
            }
        }

        private static bool WValidRange(AIBaseClient t)
        {
            var range = GetRealDistance(t);
            var Wmode = WMenu["Wmode"].GetValue<MenuList>().Index;
            

            if (Wmode == 0 && W.IsReady())
            {
                if (range > GetRealPowPowRange(t) && Player.CountEnemyHeroesInRange(GetRealPowPowRange(t)) == 0)
                    return true;
                else
                    return false;

            }
            else if (Wmode == 1 && W.IsReady())
            {
                if (range > Q.Range + 50 && Player.CountEnemyHeroesInRange(Q.Range + 50) == 0)
                    return true;
                else
                    return false;
            }
            else if (Wmode == 2 && W.IsReady())
            {
                if (range > WMenu["Wcustome"].GetValue<MenuSlider>().Value && Player.CountEnemyHeroesInRange(WMenu["Wcustome"].GetValue<MenuSlider>().Value) == 0 && W.IsReady())
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        private static void Wlogic()
        {
            var t = TargetSelector.GetTarget(W.Range, DamageType.Physical);

            if (WMenu["useW"].GetValue<MenuKeyBind>().Active && W.IsReady())
            {
                W.Cast(t);
            }

            if (t.IsValidTarget() && WValidRange(t))
            {
                if (WMenu["Wks"].GetValue<MenuBool>().Enabled && GetKsDamage(t, W) > t.Health && W.IsReady())
                {
                    W.Cast(t);
                }

                if (Combo && W.IsReady() && WMenu["Wcombo"].GetValue<MenuBool>().Enabled && Player.ManaPercent > WMenu["WmanaCombo"].GetValue<MenuSlider>().Value && W.GetPrediction(t).Hitchance >= HitChance.High)
                {
                    W.Cast(t);
                }
                else if (Farm && Orbwalker.CanAttack() && W.IsReady() && !Player.Spellbook.IsAutoAttack && WMenu["Wharass"].GetValue<MenuBool>().Enabled && Player.ManaPercent > WMenu["WmanaHarass"].GetValue<MenuSlider>().Value)
                {
                    if (W.IsReady() && WMenu["Wts"].GetValue<MenuList>().Index == 0)
                    {
                        if (WMenu["harassW" + t.CharacterName].GetValue<MenuBool>().Enabled)
                            W.Cast(t);
                    }
                    else
                    {
                        foreach (var enemy in Enemies.Where(enemy => enemy.IsValidTarget(W.Range) && WValidRange(t) && W.IsReady() && WMenu["harassW" + t.CharacterName].GetValue<MenuBool>().Enabled))
                            W.Cast(t);
                    }
                }

            }
        }
        private static void Qlogic()
        {
            if (FishBoneActive)
            {
                var orbT = Orbwalker.GetTarget();
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Player.ManaPercent > QMenu["QmanaLC"].GetValue<MenuSlider>().Value && orbT.IsMinion())
                {

                }
                else if (QMenu["Qchange"].GetValue<MenuList>().Index == 0 && orbT.IsEnemy)
                {
                    var t = (AIHeroClient)Orbwalker.GetTarget();
                    FishBoneToMiniGun(t);
                }
                else
                {
                    if (!Combo && Orbwalker.ActiveMode != OrbwalkerMode.None)
                        Q.Cast();
                }
            }
            else
            {
                var t = TargetSelector.GetTarget(Q.Range + 40, DamageType.Physical);
                if (t.IsValidTarget())
                {
                    if ((!Player.InAutoAttackRange(t) || t.CountEnemyHeroesInRange(250) >= QMenu["Qaoe"].GetValue<MenuSlider>().Value))
                    {
                        if (Combo && QMenu["Qcombo"].GetValue<MenuBool>().Enabled && (Player.ManaPercent > QMenu["QmanaCombo"].GetValue<MenuSlider>().Value || Player.GetAutoAttackDamage(t) * QMenu["QmanaIgnore"].GetValue<MenuSlider>().Value > t.Health))
                        {
                            Q.Cast();
                        }
                        if (Orbwalker.ActiveMode == OrbwalkerMode.Harass && Farm && Orbwalker.CanAttack() && !Player.Spellbook.IsAutoAttack && QMenu["harassQ" + t.CharacterName].GetValue<MenuBool>().Enabled && QMenu["Qharass"].GetValue<MenuBool>().Enabled && (Player.ManaPercent > QMenu["QmanaHarass"].GetValue<MenuSlider>().Value || Player.GetAutoAttackDamage(t) * QMenu["QmanaIgnore"].GetValue<MenuSlider>().Value > t.Health))
                        {
                            Q.Cast();
                        }
                    }
                }
                else
                {
                    if (Combo && Player.ManaPercent > QMenu["QmanaCombo"].GetValue<MenuSlider>().Value)
                    {
                        Q.Cast();
                    }
                    else if (Farm  && !Player.Spellbook.IsAutoAttack && QMenu["farmQout"].GetValue<MenuBool>().Enabled && Orbwalker.CanAttack())
                    {
                        foreach (var minion in GameObjects.GetMinions(Q.Range + 30).Where(
                        minion => !Player.InAutoAttackRange(minion) && minion.Health < Player.GetAutoAttackDamage(minion) * 1.2 && GetRealPowPowRange(minion) < GetRealDistance(minion) && Q.Range < GetRealDistance(minion)))
                        {

                            Q.Cast();
                            return;
                        }
                    }
                    if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Player.ManaPercent > QMenu["QmanaLC"].GetValue<MenuSlider>().Value)
                    {
                        var orbT = Orbwalker.GetTarget();
                        if (orbT.IsMinion() && CountMinionsInRange(250, orbT.Position) >= QMenu["Qlaneclear"].GetValue<MenuSlider>().Value)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static int CountMinionsInRange(float range, Vector3 pos)
        {
            var minions = GameObjects.GetMinions(pos, range);
            int count = 0;
            foreach (var minion in minions)
            {
                count++;
            }
            return count;
        }

        public static float GetKsDamage(AIBaseClient t, Spell QWER)
        {
            var totalDmg = QWER.GetDamage(t);

            if (Player.HasBuff("summonerexhaust"))
                totalDmg = totalDmg * 0.6f;

            if (t.HasBuff("ferocioushowl"))
                totalDmg = totalDmg * 0.7f;

            if (t is AIHeroClient)
            {
                var champion = (AIHeroClient)t;
                if (champion.CharacterName == "Blitzcrank" && !champion.HasBuff("BlitzcrankManaBarrierCD") && !champion.HasBuff("ManaBarrier"))
                {
                    totalDmg -= champion.Mana / 2f;
                }
            }

            var extraHP = t.Health - HealthPrediction.GetPrediction(t, 500);

            totalDmg += extraHP;
            totalDmg -= t.HPRegenRate;
            totalDmg -= t.PercentLifeStealMod * 0.005f * t.FlatPhysicalDamageMod;

            return totalDmg;
        }


        private static void FishBoneToMiniGun(AIBaseClient t)
        {
            var realDistance = GetRealDistance(t);

            if (realDistance < GetRealPowPowRange(t) && t.CountEnemyHeroesInRange(250) < QMenu["Qaoe"].GetValue<MenuSlider>().Value)
            {
                if (Player.ManaPercent < QMenu["QmanaCombo"].GetValue<MenuSlider>().Value || Player.GetAutoAttackDamage(t) * QMenu["QmanaIgnore"].GetValue<MenuSlider>().Value < t.Health)
                    Q.Cast();

            }
        }

        private static float GetRealDistance(AIBaseClient target) { return Player.Position.Distance(target.Position) + Player.BoundingRadius + target.BoundingRadius; }

        private static float GetRealPowPowRange(GameObject target) { return 650f + Player.BoundingRadius + target.BoundingRadius; }

        private static float GetBonudRange()
        {
            return 670f + Player.BoundingRadius + 25 * Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private static void SetValues()
        {
            if (WMenu["Wmode"].GetValue<MenuList>().Index == 2)
                WMenu["Wcustome"].GetValue<MenuSlider>().Value = 1500;
            else
                WMenu["Wcustome"].GetValue<MenuSlider>().Value = 600;

            /*if (RMenu["Rmode"].GetValue<MenuList>().Index == 2)
                RMenu["Rcustome"].GetValue<MenuSlider>().Value = 1600;
            else
                RMenu["Rcustome"].GetValue<MenuSlider>().Value = 1000;*/


            if (Player.HasBuff("JinxQ"))
                FishBoneActive = true;
            else
                FishBoneActive = false;

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                Combo = true;
            else
                Combo = false;

            if (
                (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear) ||
                (Orbwalker.ActiveMode == OrbwalkerMode.LastHit) ||
                (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
               )
                Farm = true;
            else
                Farm = false;

            Q.Range = 685f + Player.BoundingRadius + 25f * Player.Spellbook.GetSpell(SpellSlot.Q).Level;

            QMANA = 20f;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
        }

    }
}
