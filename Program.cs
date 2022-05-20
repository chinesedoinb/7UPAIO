using System;
using EnsoulSharp;
using EnsoulSharp.SDK;
using AIO7UP.Champions;
using ElRumble;


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
                    case "Caitlyn":
                        Caitlyn.OnGameLoad();
                        break;
                    case "Cassiopeia":
                        Cassiopeia.OnGameLoad();
                    break;
                    case "Ekko":
                        Ekko.OnGameLoad();
                        break;
                    case "Hecarim":
                        Hecarim.OnGameLoad();
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
                    case "Karthus":
                        Karthus.OnGameLoad();
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
                    case "Orianna":
                        Orianna.OnGameLoad();
                        break;
                    case "Rumble":
                        Rumble.OnLoad();
                        break;
                    case "Ryze":
                        Ryze.OnGameLoad();
                        break;
                    case "Talon":
                        Talon.OnGameLoad();
                        break;
                    case "Taliyah":
                        Taliyah.OnGameLoad();
                        break;
                    case "Shyvana":
                        Shyvana.OnGameLoad();
                        break;
                    case "Viktor":
                        Viktor.OnGameLoad();
                        break;
					case "Xerath":
                        Xerath.OnGameLoad();
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
