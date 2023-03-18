using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using ReLogic.Peripherals.RGB.Logitech;
using System.Text.Json;
using System.Xml;

public class Kit
{
    public int Cooldown { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<Item>? Items { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? PermissionGroups { get; set; }
}

public class Item
{
    public int NetID { get; set; }
    public int Stack { get; set; }
}

public class Config
{
    public Dictionary<string, Kit>? Kits { get; set; }
    public List<string>? PermissionsKitload { get; set; }

    public static string pathConfig = "ConfigKits.json";
    public static Config? config;

    // Сохраняет конфиг
    public static void SaveConfig()
    {
        string json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(pathConfig, json);
    }
    // Метод для загрузки конфига из JSON файла при инициализации
    public static void ConfigIsNull()
    {
        Config.config = new Config
        {
            Kits = new Dictionary<string, Kit>(),
            PermissionsKitload = new List<string> { "superadmin", "allperm" }
        };
        string json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText(pathConfig, json);
    }
    public static void LoadConfigWithMessages(CommandArgs args)
    {
        if (!File.Exists(pathConfig))
        {
            args.Player.SendErrorMessage($"[KitPlugin] Конфиг файл {pathConfig} не найден! Создание нового конфига...");
            ConfigIsNull();
            args.Player.SendInfoMessage($"[KitPlugin] Конфиг {pathConfig} успешно создан и загружен!");
        }
        else
        {
            string json = File.ReadAllText(pathConfig);
            config = JsonConvert.DeserializeObject<Config>(json);
            if (config == null)
            {
                ConfigIsNull();
                args.Player.SendInfoMessage($"[KitPlugin] Конфиг загружен успешно!");
            }
        }

        try
        {
            foreach (var kit in config.Kits.Values)
            {
                // Проверяем, что список предметов не является null
                if (kit.Items == null)
                {
                    throw new Exception($"список предметов набора {kit} не найден.");
                }
            }

            args.Player.SendInfoMessage($"[KitPlugin] Конфиг загружен успешно!");
        }
        catch (Exception e)
        {
            args.Player.SendErrorMessage($"[KitPlugin] Ошибка при загрузке конфига: {e.Message}");
        }
    }

    public static void LoadConfig()
    {
        if (!File.Exists(pathConfig))
        {
            TShock.Log.ConsoleError($"[KitPlugin] Конфиг файл {pathConfig} не найден! Создание нового конфига...");
            ConfigIsNull();
            TShock.Log.ConsoleInfo($"[KitPlugin] Конфиг {pathConfig} успешно создан и загружен!");
        }
        else
        {
            string json = File.ReadAllText(pathConfig);
            config = JsonConvert.DeserializeObject<Config>(json);
            if (config == null)
            {
                ConfigIsNull();
                TShock.Log.ConsoleInfo($"[KitPlugin] Конфиг загружен успешно!");
            }
        }

        try
        {
            foreach (var kit in config.Kits.Values)
            {
                // Проверяем, что список предметов не является null
                if (kit.Items == null)
                {
                    throw new Exception($"список предметов набора {kit} не найден.");
                }
            }

            TShock.Log.ConsoleInfo($"[KitPlugin] Конфиг загружен успешно!");
        }
        catch (Exception e)
        {
            TShock.Log.ConsoleError($"[KitPlugin] Ошибка при загрузке конфига: {e.Message}");
        }
    }
    public static void GameAddkit(CommandArgs args)
    {
        try
        {
            int netId = 0;
            int stack = 0;
            if (args.Parameters.Count == 4)
            {
                if (!int.TryParse(args.Parameters[2], out netId) || !int.TryParse(args.Parameters[3], out stack))
                {
                    args.Player.SendErrorMessage("Вы используете не верные параметры: /kit add <название кита> <netID> <stack> - netID и stack должны быть целыми числами, stack по умолчанию равен 1");
                    return;
                }
            }
            else if (args.Parameters.Count == 3)
            {
                stack = 1;
                if (!int.TryParse(args.Parameters[2], out netId))
                {
                    args.Player.SendErrorMessage("Вы используете не верные параметры: /kit add <название кита> <netID> <stack> - netID и stack должны быть целыми числами, stack по умолчанию равен 1");
                    return;
                }
            }
            else if (args.Parameters.Count == 2)
            {
                netId = 0;
                stack = 1;
            }
            string kitName = args.Parameters[1];
            // Проверяем существует ли кит с указанным именем
            if (!config.Kits.ContainsKey(kitName))
            {
                // Создаем новый кит
                config.Kits[kitName] = new Kit
                {
                    Cooldown = 60 * 15, // Задаем значения по умолчанию 15 часов
                    Items = new List<Item>(),
                    PermissionGroups = new List<string>()
                };
            }

            // Создаем новый предмет и добавляем его в список предметов кита
            var newItem = new Item
            {
                NetID = netId,
                Stack = stack
            };
            config.Kits[kitName].Items.Add(newItem);
            // Сохраняем изменения в файл конфигурации
            SaveConfig();
            args.Player.SendSuccessMessage("Файл конфига успешно модернизирован!");
        }
        catch (Exception e)
        {
            args.Player.SendErrorMessage($"Что-то пошло не так, ошибка:{e}");
        }
    }
    public static void GameDeleteKit(CommandArgs args)
    {
        if (args.Parameters.Count != 2)
        {
            args.Player.SendErrorMessage($"Использование: /kit del <название кита>, это удалит кит и тут же сохранит это в конфиг");
            return;
        }
        string kitName = args.Parameters[1];
        // Проверяем существует ли кит с указанным именем
        if (config.Kits.ContainsKey(kitName))
        {
            // Удаляем кит из конфигурации
            config.Kits.Remove(kitName);

            // Сохраняем изменения в файл конфигурации
            SaveConfig();

            args.Player.SendSuccessMessage($"Кит {kitName} удален.");
        }
        else
        {
            args.Player.SendErrorMessage($"Кит {kitName} не найден.");
        }
    }
    public static void GameDelItemConfig(CommandArgs args)
    {
        if (args.Parameters.Count != 3)
        {
            args.Player.SendErrorMessage("Неверное количество параметров. Использование: /kit delitem <название кита> <netID>");
        }
        // Получаем аргументы команды
        string kitName = args.Parameters[1];

        // Проверяем, было ли передано число в качестве второго аргумента, и преобразуем его из строки в число
        if (!int.TryParse(args.Parameters[2], out int netId))
        {
            args.Player.SendErrorMessage("Неверный формат аргумента netID.");
            return;
        }

        // Проверяем, существует ли кит с указанным именем
        if (!config.Kits.ContainsKey(kitName))
        {
            args.Player.SendErrorMessage($"Кит '{kitName}' не существует.");
            return;
        }

        // Ищем предмет с указанным netID в списке предметов кита
        int indexToRemove = -1;
        for (int i = 0; i < config.Kits[kitName].Items.Count; i++)
        {
            if (config.Kits[kitName].Items[i].NetID == netId)
            {
                indexToRemove = i;
                break;
            }
        }

        // Если предмет не найден, выводим сообщение об ошибке
        if (indexToRemove == -1)
        {
            args.Player.SendErrorMessage($"Предмет с netID {netId} не найден в ките '{kitName}'.");
            return;
        }

        // Удаляем предмет из списка предметов кита
        config.Kits[kitName].Items.RemoveAt(indexToRemove);

        // Сохраняем изменения в файл конфигурации
        SaveConfig();

        // Отправляем сообщение об успешном удалении предмета
        args.Player.SendSuccessMessage($"Предмет с netID {netId} успешно удален из кита '{kitName}'.");
    }
    public static void SetKitCooldown(CommandArgs args)
    {
        string kitName = args.Parameters[1];

        // Проверяем существует ли кит с указанным именем
        if (!config.Kits.ContainsKey(kitName))
        {
            args.Player.SendErrorMessage($"Кит с названием \"{kitName}\" не существует.");
            return;
        }

        if (!int.TryParse(args.Parameters[2], out int cooldown))
        {
            args.Player.SendErrorMessage($"\"{args.Parameters[1]}\" не является числом.");
            return;
        }

        config.Kits[kitName].Cooldown = cooldown;
        SaveConfig();

        args.Player.SendSuccessMessage($"Задержка для кита \"{kitName}\" установлена на {cooldown} минут.");
    }
    public static void ShowKitPermissionGroups(CommandArgs args)
    {
        string kitName = args.Parameters[1];
        if (config.Kits.ContainsKey(kitName))
        {
            var kit = config.Kits[kitName];
            var permissionGroups = kit.PermissionGroups;

            if (permissionGroups.Count == 0)
            {
                args.Player.SendErrorMessage($"К киту '{kitName}' не добавлено ни одной группы.");
                return;
            }

            var groupList = string.Join(", ", permissionGroups);
            args.Player.SendSuccessMessage($"Группы, имеющие доступ к киту '{kitName}': {groupList}");
        }
        else
        {
            args.Player.SendErrorMessage($"Кит '{kitName}' не найден в конфигурации.");
        }
    }

    public static void GameAddPermToKitCommand(CommandArgs args)
    {
        // Проверяем правильность использования команды
        if (args.Parameters.Count != 3)
        {
            args.Player.SendErrorMessage("Использование: /kit addperm <название кита> <название группы>");
            return;
        }

        string kitName = args.Parameters[1];
        string groupName = args.Parameters[2];

        // Проверяем существует ли кит с указанным именем
        if (!config.Kits.ContainsKey(kitName))
        {
            args.Player.SendErrorMessage($"Кит \"{kitName}\" не найден.");
            return;
        }

        // Добавляем группу в список групп, которым доступен кит
        config.Kits[kitName].PermissionGroups.Add(groupName);

        // Сохраняем изменения в файл конфигурации
        SaveConfig();

        args.Player.SendSuccessMessage($"Группа \"{groupName}\" добавлена в список групп, которым доступен кит \"{kitName}\".");
    }
    public static void GameDelPermToKitCommand(CommandArgs args)
    {
        if (args.Parameters.Count != 3)
        {
            args.Player.SendErrorMessage("Использование: /kit delperm <название кита> <название группы>");
            return;
        }

        string kitName = args.Parameters[1];
        string groupName = args.Parameters[2];

        if (!config.Kits.ContainsKey(kitName))
        {
            args.Player.SendErrorMessage($"Кит '{kitName}' не существует.");
            return;
        }

        if (!config.Kits[kitName].PermissionGroups.Contains(groupName))
        {
            args.Player.SendErrorMessage($"Группа '{groupName}' не была добавлена к киту '{kitName}'.");
            return;
        }

        config.Kits[kitName].PermissionGroups.Remove(groupName);
        SaveConfig();

        args.Player.SendSuccessMessage($"Группа '{groupName}' удалена из списка групп кита '{kitName}'.");
    }
}