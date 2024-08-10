using System;

public interface IJsonObject<T>
{
    string ConverToJSON();

    static T FromJSON(string jsonString) => throw new NotImplementedException();
}