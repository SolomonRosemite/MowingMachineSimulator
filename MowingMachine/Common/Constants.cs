﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MowingMachine.Models;

namespace MowingMachine.Common
{
    public static class Constants
    {
        private static readonly Dictionary<FieldType, int> _ExpensesDictionary = new()
        {
            { FieldType.Grass, 10 },
            { FieldType.ChargingStation, 10 },
            { FieldType.MowedLawn, 10 },

            { FieldType.CobbleStone, 5 },
            { FieldType.Sand, 20 },
        };

        public static double TurnExpense => 4;

        public static double TranslateMoveToExpense(FieldType fieldType)
        {
            if (fieldType is FieldType.Water)
            {
                throw new ArgumentException("Water is a field which cant be stepped on");
            }
            return _ExpensesDictionary.First(e => e.Key == fieldType).Value;
        }

        public static int[][] DefaultMapSample => GetMapFromJson();

        private static int[][] GetMapFromJson()
        {
            Directory.CreateDirectory(@".\Maps\");

            try
            {
                var json = File.ReadAllText(
                    GetJsonFileName(@".\Maps\", false));

                Console.WriteLine(GetJsonFileName(@".\Maps\", false));
                return JsonSerializer.Deserialize<int[][]>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load existing map. See Exception for more info.");
                Console.WriteLine(e);
            }

            return null;
        }

        public static void SaveMapAsJson(int[][] map)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(map);
                var fileName = GetJsonFileName(@".\Maps\");

                File.WriteAllText(fileName, jsonContent);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to save map. See Exception for more info.");
                Console.WriteLine(e);
            }
        }

        private static string GetJsonFileName(string path, bool newFileName = true)
        {
            var x = Directory.GetFiles(path);
            var files = Directory.GetFiles(path).Select(p
                => p.Substring(p.LastIndexOf('-') + 1, p.LastIndexOf('.') - p.LastIndexOf('-') - 1));
            var mapIds = files.Select(int.Parse);

            if (!newFileName)
            {
                if (!mapIds.Any())
                    throw new Exception("No maps found");

                return path + $"Map-{mapIds.Max()}.json";
            }

            if (!mapIds.Any())
                return path + "Map-1.json";

            return path + $"Map-{mapIds.Max() + 1}.json";
        }
    }
}