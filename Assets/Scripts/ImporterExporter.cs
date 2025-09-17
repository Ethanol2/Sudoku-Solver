using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using EditorTools;
using System.Collections.Generic;

public static class ImporterExporter
{
    public static IBoard.State[] ImportBoardsLines(TextAsset file, int maxLines, string difficulty, Dictionary<string, string> properties)
    {
        string[] lines = file.text.Split("\n", 1);
        int count = 0;

        List<IBoard.State> states = new List<IBoard.State>();

        while (lines[0] != string.Empty && count < maxLines)
        {
            string[] split = lines[0].Split(' ');
            int boardSize;
            switch (split[0].Length)
            {
                case 16:
                    boardSize = 4;
                    break;
                case 36:
                    boardSize = 6;
                    break;
                case 81:
                    boardSize = 9;
                    break;
                case 144:
                    boardSize = 12;
                    break;
                case 625:
                    boardSize = 25;
                    break;
                default:
                    Debug.Log("Unknown board size");
                    continue;
            }

            IBoard.State newState = new IBoard.State()
            {
                Difficulty = difficulty,
                Properties = properties,
                Numbers = new int[boardSize, boardSize]
            };

            int x = 0, y = 0;
            for (int i = 0; i < lines[1].Length; i++)
            {
                if (x >= boardSize)
                    y++;
            }
        }
        return null;
    }
    public static string ImportBoardsJson(string path)
    {
        if (!File.Exists(path))
            throw new System.Exception($"[ImporterExporter] File at \"{path}\" doesn't exist");

        string fileContents;

        using StreamReader file = new StreamReader(path);
        {
            fileContents = file.ReadToEnd();
            file.Close();
        }

        return fileContents;
    }
    public static IBoard.State[] ParseBoardsJson(string json)
    {
        IBoard.State[] boards;
        try
        {
            boards = JsonConvert.DeserializeObject<IBoard.State[]>(json);
        }
        catch (System.Exception e)
        {
            Debug.Log($"[ImporterExporter] Something went wrong while importing boards =>\n{e}");
            return new IBoard.State[0];
        }

        return boards;
    }
    public static IBoard.State[] ImportBoards(string path)
    {
        string json = ImportBoardsJson(path);
        return ParseBoardsJson(json);
    }

    public static void ExportBoards(params IBoard.State[] states)
    {
        string export = JsonConvert.SerializeObject(states, Formatting.Indented);
        export = export.Replace(",\n      ", ",");

        using StreamWriter file = new StreamWriter("board-export.json");
        {
            file.Write(export);
            file.Close();
        }
    }
}
