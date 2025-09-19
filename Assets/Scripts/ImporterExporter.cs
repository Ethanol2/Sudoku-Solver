using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using EditorTools;
using System.Collections.Generic;

public static class ImporterExporter
{
    public static IBoard.State[] ImportBoardsLines(TextAsset file, int maxLines, Dictionary<string, string> properties) => ImportBoardsLines(file.text, maxLines, properties);
    public static IBoard.State[] ImportBoardsLines(string file, int maxLines, Dictionary<string, string> properties)
    {
        string[] lines = file.Split("\n", 2);
        int count = 0;

        List<IBoard.State> states = new List<IBoard.State>();

        while (lines.Length == 2 && count <= maxLines)
        {
            string[] split = lines[0].Split(' ');
            int boardSize;
            switch (split[1].Length)
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

            float difficulty = -1;
            if (float.TryParse(split[3], out float diff))
                difficulty = diff;


            IBoard.State newState = new IBoard.State()
            {
                Properties = properties,
                Numbers = new int[boardSize, boardSize],
                Difficulty = difficulty
            };

            int x = 0, y = 0;
            for (int i = 0; i < split[1].Length; i++)
            {
                newState.Numbers[x, y] = int.Parse(split[1][i].ToString());

                x++;
                if (x >= boardSize)
                {
                    x = 0;
                    y++;
                }
            }
            states.Add(newState);
            count++;
            lines = lines[1].Split("\n", 2);
        }
        return states.ToArray();
    }
    public static List<PuzzleBook.Board> ImportBoardLinesToPuzzleBook(string file)
    {
        string[] lines = file.Split("\n", 2);

        List<PuzzleBook.Board> boards = new List<PuzzleBook.Board>();

        while (lines.Length == 2)
        {
            PuzzleBook.Board newBoard = new PuzzleBook.Board();
            string[] split = lines[0].Split(' ');

            if (float.TryParse(split[3], out float diff))
                newBoard.Difficulty = diff;

            newBoard.Numbers = new int[split[1].Length];

            int i = 0;
            foreach (char c in split[1])
            {
                newBoard.Numbers[i] = int.Parse(c.ToString());
                i++;
            }

            boards.Add(newBoard);
            lines = lines[1].Split("\n", 2);
        }

        return boards;
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
