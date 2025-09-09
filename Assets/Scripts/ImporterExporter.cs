using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public static class ImporterExporter
{
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
