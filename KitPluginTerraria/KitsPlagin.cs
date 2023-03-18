using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.ComponentModel;
using ReLogic.Peripherals.RGB.Logitech;
using System.Text;

namespace KitPlugin
{
    [ApiVersion(2, 1)]
    public class KitPlugin : TerrariaPlugin
    {
        public override string Name => "KitPlugin";
        public override string Author => "Синий Ёж";
        public override string Description => "Плагин на киты с настройкой через ConfigKits.json";
        public override Version Version => new Version(1, 0, 0);

        public Dictionary<string, DateTime> lastUsedKitDates = new Dictionary<string, DateTime>();
        public int CountNoSaveKit = 0;
        public KitPlugin(Main game) : base(game)
        {

        }
        public override void Initialize()
        {
            Config.LoadConfig();
            if (File.Exists("lastUsedKitDates.json"))
            {
                JsonlastUsedKitDatesRemoveOld();
            }
            Commands.ChatCommands.Add(new Command(KitCommand, "kit")
            {
                HelpText = "Использование: /kit <имя> - выдаёт соответствующий набор. /kit list - выдаст список всех наборов. /kit help - выдаст помощь"
            });
            ServerApi.Hooks.WorldSave.Register(this, OnSaveUsedKitDates);
            string jsonString = JsonConvert.SerializeObject(Config.config);
            TShock.Log.ConsoleInfo(jsonString);
        }

        private void JsonlastUsedKitDatesRemoveOld()
        {
            try
            {
                lastUsedKitDates = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(File.ReadAllText("lastUsedKitDates.json"));
                int maxCooldown = 0;
                foreach (var kit in Config.config.Kits.Values)
                {
                    if (kit.Cooldown > maxCooldown)
                    {
                        maxCooldown = kit.Cooldown;
                    }
                }
                int count = 0;
                foreach (string key in lastUsedKitDates.Keys.ToList())
                {
                    TimeSpan timeSinceSave = DateTime.Now - lastUsedKitDates[key];
                    double minutes = maxCooldown - timeSinceSave.TotalMinutes;
                    if (minutes <= 0)
                    {
                        lastUsedKitDates.Remove(key);
                        count++;
                    }
                }
                if (count > 0)
                {
                    TShock.Log.ConsoleInfo($"[KitPlugin] Успешно удалено {count} бесполезных запересей в lastUsedKitDates");
                }
            }
            catch (Exception e)
            {
                TShock.Log.Error($"[KitPlugin] Ошибка в методе JsonlastUsedKitDatesRemoveOld:{e}");
            }
        }
        private void OnSaveUsedKitDates(WorldSaveEventArgs args)
        {
            TShock.Utils.Broadcast("СОХРАНЕНИЕ!", 0,0,255);
            if (CountNoSaveKit > 0)
            {
                TShock.Utils.Broadcast("CountNoSaveKit > 0!", 0, 0, 255);
                if (File.Exists("lastUsedKitDates.json"))
                {
                    TShock.Utils.Broadcast("File.Exists(\"lastUsedKitDates.json\")!", 0, 0, 255);
                    //lastUsedKitDates = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(File.ReadAllText("lastUsedKitDates.json"));
                }
                string str = JsonConvert.SerializeObject(lastUsedKitDates);
                TShock.Utils.Broadcast(str, 0, 0, 255);
                File.WriteAllText("lastUsedKitDates.json", str);
                CountNoSaveKit = 0;
                TShock.Utils.Broadcast("CountNoSaveKit = 0", 0, 0, 255);
            }
        }

        protected override void Dispose(bool disposing)
        {
            ServerApi.Hooks.WorldSave.Deregister(this, OnSaveUsedKitDates);
            if (disposing)
            {
                base.Dispose(disposing);
            }
        }
        private void KitCommand(CommandArgs args)
        {
            bool ignoreCoolDown = false;
            int playerIndex = args.Player.Index;
            string groupName = TShock.Players[playerIndex].Group.Name;
            if (args.Parameters.Count < 1) // Проверка наличия необходимых параметров команды
            {
                args.Player.SendErrorMessage("Использование: /kit <название набора>, /kit help, /kit list");
                return;
            }
            if (Config.config.PermissionsKitload.Contains(groupName)) //есть ли право на загрузку и изменение кита
            {
                ignoreCoolDown = true;
                args.Player.SendSuccessMessage("Кажется у вас есть права на редактирование конфига для KitPlagin, расширенная справка - /kit help");
                if (args.Parameters[0] == "help")
                {
                    args.Player.SendSuccessMessage("/kit load - просто загружает текущий файл конфига ConfigKits.json, будьте осторожны если хотите редактировать его напрямую.");
                    args.Player.SendSuccessMessage("/kit del <название кита> - полностью удаляет кит.");
                    args.Player.SendSuccessMessage("/kit additem <название кита> <netID> <stack> - добавляет новый предмет в кит, если кита нет он будет создан. stack по умолчанию равен 1.");
                    args.Player.SendSuccessMessage("/kit delitem <название кита> <netID> - удаляет предмет из кита.");
                    args.Player.SendSuccessMessage("/kit setcd <название кита> <задержка использования> - устанавливает задержку в минутах для кита.");
                    args.Player.SendSuccessMessage("/kit showperm <название кита> - показывает список групп которые могут получать этот кит.");
                    args.Player.SendSuccessMessage("/kit addperm <название кита> <название группы> - добавляет новую группу в список тех кому можно брать кит, осторожно - чувствительно к регистру.");
                    args.Player.SendSuccessMessage("/kit delperm <название кита> <название группы> - удаляет группу из списка тех кому можно брать кит, осторожно - чувствительно к регистру.");
                    return;
                }
                if (args.Parameters[0] == "load")
                {
                    Config.LoadConfigWithMessages(args);
                    return;
                }
                else if (args.Parameters[0] == "additem")
                {
                    Config.GameAddkit(args);
                    return;
                }
                else if (args.Parameters[0] == "del")
                {
                    Config.GameDeleteKit(args);
                    return;
                }
                else if (args.Parameters[0] == "delitem")
                {
                    Config.GameDelItemConfig(args);
                    return;
                }
                else if (args.Parameters[0] == "setcd")
                {
                    Config.SetKitCooldown(args);
                    return;
                }
                else if (args.Parameters[0] == "showperm")
                {
                    Config.ShowKitPermissionGroups(args);
                }
                else if (args.Parameters[0] == "addperm")
                {
                    Config.GameAddPermToKitCommand(args);
                    return;
                }
                else if (args.Parameters[0] == "delperm")
                {
                    Config.GameDelPermToKitCommand(args);
                    return;
                }
            }
            string kitName = args.Parameters[0];
            if (kitName == "help")
            {
                args.Player.SendSuccessMessage($"Команда /kit <имя> нужна для выдачи наборов. Список наборов - /kit list");
            }
            if (kitName == "list")
            {
                string message = "Наборы: ";
                var kitNames = Config.config.Kits.Keys.Select(namekit => $"/kit {namekit}");
                message += kitNames.Any() ? string.Join(", ", kitNames) : "null";
                args.Player.SendSuccessMessage($"{message}");
                return;

            }
            if (!Config.config.Kits.ContainsKey(kitName)) // Проверка существования кита с указанным именем
            {
                args.Player.SendErrorMessage($"Кит с именем \"{kitName}\" не найден.");
                return;
            }
            if (!Config.config.Kits[kitName].PermissionGroups.Contains(groupName)) // Проверка наличия прав для использования кита
            {
                args.Player.SendErrorMessage("У вас нет прав на использование этого кита.");
                return;
            }

            string KeyLastDates = args.Player.Name + "-" + kitName;
            if (lastUsedKitDates.ContainsKey(KeyLastDates) && !ignoreCoolDown)
            {
                TimeSpan timeSinceSave = DateTime.Now - lastUsedKitDates[KeyLastDates];
                double minutes = Config.config.Kits[kitName].Cooldown - timeSinceSave.TotalMinutes;
                if (minutes > 0)
                {
                    args.Player.SendErrorMessage($"Вы недавно уже брали этот кит! {GetWaitTimeString(minutes)}");
                    return;
                }

                else
                {
                    lastUsedKitDates[KeyLastDates] = DateTime.Now;
                }
            }
            else
            {
                //lastUsedKitDates.Remove(KeyLastDates);
                lastUsedKitDates.Add(KeyLastDates, DateTime.Now);
            }
            // Выдача предметов из кита
            GiveKitItems(args.Player, Config.config.Kits[kitName].Items);

            args.Player.SendSuccessMessage($"Кит \"{kitName}\" успешно получен.");
            CountNoSaveKit++;
        }

        // Метод для выдачи предметов из кита
        private void GiveKitItems(TSPlayer player, List<Item> items)
        {
            foreach (Item item in items)
            {
                player.GiveItem(item.NetID, item.Stack);
            }
        }
        // Метод для создания итоговой строки при ошибке неистечения кулдовна
        public static string GetWaitTimeString(double minutes)
        {
            var waitTime = TimeSpan.FromMinutes(minutes);
            var hours = (int)waitTime.TotalHours;
            var minutesLeft = waitTime.Minutes;
            var secondsLeft = waitTime.Seconds;

            var sb = new StringBuilder();
            if (hours > 0)
            {
                sb.Append($" {hours} {GetDeclension(hours, new[] { "час", "часа", "часов" })}");
            }
            if (minutesLeft > 0)
            {
                sb.Append($" {minutesLeft} {GetDeclension(minutesLeft, new[] { "минута", "минуты", "минут" })}");
            }
            if (secondsLeft > 0)
            {
                sb.Append($" {secondsLeft} {GetDeclension(secondsLeft, new[] { "секунда", "секунды", "секунд" })}");
            }

            var result = sb.ToString().Trim();
            if (string.IsNullOrEmpty(result))
            {
                return string.Empty;
            }
            return $"Ждите {result}";
        }
        // Метод для определения склонения минут, секунд, часов
        public static string GetDeclension(double value, string[] words)
        {
            value = Math.Abs(value) % 100;
            var rem = value % 10;
            if (rem > 4 || rem == 0 || (value >= 11 && value <= 14))
            {
                return words[2];
            }
            else if (rem == 1)
            {
                return words[0];
            }
            else
            {
                return words[1];
            }
        }
    }
}
