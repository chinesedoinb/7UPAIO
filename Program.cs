using System;
using System.Net;
using System.Diagnostics;
using EnsoulSharp;
using EnsoulSharp.SDK;
using AIO7UP.Champions;
using System.Threading.Tasks;
using EnsoulSharp.SDK.MenuUI;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using Color = System.Drawing.Color;


namespace AIO7UP
{
    public class Program
    {
        public static void Main(string[] args)
        {           
            GameEvent.OnGameLoad += OnLoadingComplete;
        }
        private static void OnLoadingComplete()
        {

            if (ObjectManager.Player == null)
                return;
            try
            {
                switch (GameObjects.Player.CharacterName)
                {
                    case "Vayne":
                        Vayne.OnGameLoad();
                        break;
                    case "Cassiopeia":
                        Cassiopeia.OnGameLoad();
                    break;
                    case "Ekko":
                        Ekko.OnGameLoad();
                        break;
                    case "Jax":
                        Jax.OnGameLoad();
                        break;
                    case "Jinx":
                        Jinx.OnGameLoad();
                        break;
                    case "Fizz":
                        Fizz.OnGameLoad();
                        break;
                    case "KogMaw":
                        KogMaw.OnGameLoad();
                        break;
                    case "Leblanc":
                        Leblanc.OnGameLoad();
                        break;
                    case "Olaf":
                        Olaf.OnGameLoad();
                        break;
                    case "Oriana":
                        Oriana.OnGameLoad();
                        break;
                    case "Rumble":
                        Rumble.OnGameLoad();
                        break;
                    case "Ryze":
                        Ryze.OnGameLoad();
                        break;
                    case "Talon":
                        Talon.OnGameLoad();
                        break;
                    case "Viktor":
                        Viktor.OnGameLoad();
                        break;
                    case "Zed":
                        Zed.OnGameLoad();
                        break;
                    default:
                        Game.Print("<font color='#b756c5' size='25'>7UP AIO Does Not Support :" + ObjectManager.Player.CharacterName+ "</font>");
                        Console.WriteLine("Not Supported " + ObjectManager.Player.CharacterName);
                        break;                   
                }
            }
            catch (Exception ex)
            {
                Game.Print("Error in loading");
                Console.WriteLine("Error in loading :");
                Console.WriteLine(ex);
            }
        }
    }   
}
