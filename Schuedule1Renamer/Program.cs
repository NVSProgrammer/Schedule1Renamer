using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace Schuedule1Renamer
{
    internal class Program
    {
        const int SAFE_MODE = 0;
        const int UNSAFE_MODE = 1;

        const int STEP_GET_GAME = 0;
        const int STEP_GET_SAVE = 1;
        const int STEP_GET_MODE = 2;
        const int STEP_GET_NAME = 3;
        const int STEP_SET_NAME = 4;

        const string FREE_SAMPLE = "Schedule I Free Sample";
        const string PAID = "Schedule I";

        static void Main(string[] args)
        {
            int step = STEP_GET_GAME;
            int mode = 0;
            int save_num = 1;
            string game = "";
            string product_name = "";

            string user_input = "";
            string line = "";

            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS"))
            {
                Console.WriteLine("You do not have the game! Press any key to close it");
                Console.ReadKey();
                return;
            }

            // info
            Console.WriteLine("Make a backup of the save in case it fails to load backups (Optimal)");
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine("To exit, type: exit");
            Console.WriteLine("To switch settings (game/save/mode), type: switch");
            Console.WriteLine("To go one step back, type: back (Note: You cannot undo a rename or rename it again after providing a new name).");
            Console.WriteLine("------------------------------------------------------------------");
            Console.WriteLine("Important for Safe Mode:");
            Console.WriteLine("To rename a product after renaming it in Safe Mode, you need to use the *original* product name.");
            Console.WriteLine("This is because the program converts your input name to the file name for internal use.");
            Console.WriteLine("------------------------------------------------------------------");

            do
            {

                // line printing
                if (step == STEP_GET_GAME)
                    line = "What Schedule version you are playing?\n Free(input F) or Paid(input P)";
                if (step == STEP_GET_SAVE) line = "Select save from 1 to 5";
                if (step == STEP_GET_MODE)
                    line = "Edit all names(that is unsafe, becuase it rename files, input U)\n or only the name(input S)";
                if (step == STEP_GET_NAME) line = "Product name";
                if (step == STEP_SET_NAME) line = "New name";

                Console.Write(line+" > ");

                // get input
                user_input = Console.ReadLine();

                // check input
                if (user_input == "switch")
                {
                    // reset steps
                    step = STEP_GET_GAME;
                    Console.Clear();
                }
                else if(user_input == "back")
                {
                    step--;
                    continue;
                }
                else if (user_input == "") continue;

                // processing
                if (step == STEP_GET_GAME)
                {
                    string game_name;
                    if (user_input == "F") game_name = FREE_SAMPLE;
                    else if (user_input == "P") game_name = PAID;
                    else
                    {
                        Console.WriteLine("Invalid input!");
                        continue;
                    }
                    if (checkGame(game_name)) game = game_name;
                    else
                    {
                        Console.WriteLine("Game not found");
                        continue;
                    }
                }
                if (step == STEP_GET_SAVE)
                {
                    int num = int.Parse(user_input);
                    if (num > 5 || num < 1)
                    {
                        Console.WriteLine("Invalid input!");
                        continue;
                    }
                    if(checkSave(game, save_num)) save_num = num;
                    else
                    {
                        Console.WriteLine("Save do not exist!");
                        continue;
                    }
                }
                if (step == STEP_GET_MODE)
                {
                    if (user_input == "S") mode = SAFE_MODE;
                    else if(user_input == "U") mode = UNSAFE_MODE;
                    else
                    {
                        Console.WriteLine("Invalid input!");
                        continue;
                    }
                }
                if (step == STEP_GET_NAME)
                {
                    if(checkName(game, save_num, user_input)) product_name = user_input;
                    else
                    {
                        Console.WriteLine("Name do not exist!");
                        continue;
                    }
                }
                if (step == STEP_SET_NAME)
                {
                    if (setName(game, save_num, product_name, user_input, mode)) Console.WriteLine("Name change!");
                    else
                    {
                        Console.WriteLine("Something go wrong");
                    }
                    step -= 2;
                }
                step++;
            } while (user_input != "exit");
        }

        static bool setName(string game, int save, string product, string name, int mode)
        {
            if(name.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
            {
                Console.WriteLine("Invalid Name!");
                return false;
            }
            if (!makeBackup(game, save, product, mode))
            {
                Console.WriteLine("Failed to make a backup!");
                return false;
            }
            try
            {
                string file_name = getName(name);
                string file_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString()
                                    + "\\Products\\CreatedProducts\\" + getName(product) + ".json";
                dynamic json = JsonConvert.DeserializeObject(File.ReadAllText(file_path));
                json["Name"] = name;
                if (mode == UNSAFE_MODE) json["ID"] = file_name;
                File.WriteAllText(file_path, JsonConvert.SerializeObject(json, Formatting.Indented));

                if(mode == UNSAFE_MODE)
                {
                    string new_file_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString()
                                    + "\\Products\\CreatedProducts\\" + file_name + ".json";
                    string main_file = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" +
                        save.ToString() + "\\Products\\Products.json";
                    string main_file_content = File.ReadAllText(main_file).Replace(getName(product), file_name);
                    File.WriteAllText(main_file, main_file_content);
                    File.Move(file_path, new_file_path);
                }
                return true;
            }
            catch (Exception e)
            {
                if(!loadBackup(game, save, product, mode))
                    Console.WriteLine("Failed to load backup! Plase replace the save with your backup!");
            }
            return false;
        }
        static bool checkName(string game, int save, string product)
        {
            Console.WriteLine(product);
            string name = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString()
                + "\\Products\\CreatedProducts\\" + getName(product) + ".json";
            Console.WriteLine(name);
            return File.Exists(name);
        }
        static bool checkSave(string game, int save)
        {
            return Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString());
        }
        static bool checkGame(string game)
        {
            return Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game);
        }
        static string getName(string name)
        {
            return name.ToLower().Replace(" ", "");
        }
        static bool makeBackup(string game, int save, string product, int mode)
        {
            try
            {
                try
                {
                    File.Delete(".\\product.json.bak");
                    if (mode == UNSAFE_MODE) File.Delete(".\\main.json.bak");
                }
                catch (Exception e) { }
                File.Copy(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString()
                    + "\\Products\\CreatedProducts\\" + getName(product) + ".json",
                    ".\\product.json.bak"
                    );
                if(mode == UNSAFE_MODE) File.Copy(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString()
                    + "\\Products\\Products.json",
                    ".\\main.json.bak"
                    );
                return true;
            }
            catch (Exception e) { }
            return false;
        }
        static bool loadBackup(string game, int save, string product, int mode)
        {
            try
            {
                File.Copy(
                    ".\\product.json.bak",
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString()
                    + "\\Products\\CreatedProducts\\" + getName(product) + ".json"
                    );
                if (mode == UNSAFE_MODE) File.Copy(
                    ".\\main.json.bak",
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)+"\\AppData\\LocalLow\\TVGS\\" + game + "\\Saves\\SaveGame_" + save.ToString()
                    + "\\Products\\Products.json"
                    );
                return true;
            } catch (Exception e) { }
            return false;
        }
    }
}
