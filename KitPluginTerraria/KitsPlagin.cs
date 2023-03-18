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
        public int CountKit = 0;
        public KitPlugin(Main game) : base(game)
        {

        }
        public override void Initialize()
        {
            ServerApi.Hooks.WorldSave.Register(this, OnSaveUsedKitDates);
            Commands.ChatCommands.Add(new Command(KitCommand, "kit")
            {
                HelpText = "Использование: /kit <имя> выдаёт соответствующий набор. /kit list выдаст список всех наборов"
            });
            Config.LoadConfig();
            string jsonString = JsonConvert.SerializeObject(Config.config);
            TShock.Log.ConsoleInfo(jsonString);
        }

        private void OnSaveUsedKitDates(WorldSaveEventArgs args)
        {
            if (CountKit > 0)
            {
                if (File.Exists("lastUsedKitDates.json"))
                {
                    lastUsedKitDates = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(File.ReadAllText("lastUsedKitDates.json"));
                }
                File.WriteAllText("lastUsedKitDates.json", JsonConvert.SerializeObject(lastUsedKitDates));
                CountKit = 0;
            }
        }

        protected override void Dispose(bool disposing)
        {
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
                args.Player.SendSuccessMessage($"Команда /kit нужна для выдачи наборов. Список наборов - /kit list");
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
                    args.Player.SendErrorMessage($"Вы недавно уже брали этот кит! Ждите {minutes} минут");
                    return;
                }
                else
                {
                    //lastUsedKitDates.Remove(KeyLastDates);
                    lastUsedKitDates[args.Player.Name + "-" + kitName] = DateTime.Now;
                }
            }
            // Выдача предметов из кита
            GiveKitItems(args.Player, Config.config.Kits[kitName].Items);

            args.Player.SendSuccessMessage($"Кит \"{kitName}\" успешно получен.");
            CountKit++;
        }

        // Метод для выдачи предметов из кита
        private void GiveKitItems(TSPlayer player, List<Item> items)
        {
            foreach (Item item in items)
            {
                player.GiveItem(item.NetID, item.Stack);
            }
        }

    }
}
