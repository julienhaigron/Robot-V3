using System;
using UnityEngine;
using Sirenix.OdinInspector;

[AttributeUsage(AttributeTargets.Field)]
public class ParsingAttribute : Attribute
{
    public string ColumnName { get; }

    public ParsingAttribute ( string columnName )
    {
        ColumnName = columnName;
    }
}

public interface IParsingImportable
{
    string Id { get; }
}

public abstract class AParsableScriptableObject : ScriptableObject, IParsingImportable
{
    public string Id => name;
    [BoxGroup("Parsing")]
    public string spreadsheetId;

    protected abstract string GetSheetID ();

    public string GetUrl ()
    {
        return "https://docs.google.com/spreadsheets/d/" + spreadsheetId +"/export?format=csv&gid=" + GetSheetID();
    }

    [BoxGroup("Parsing"), Button]
    public void RefreshParsedValues ()
    {
        CsvImporter.RefreshScriptable(this);
	}

}