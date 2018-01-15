using System;

namespace SqlUtilities
{
  public interface IRecordset : IDisposable
  {
    bool EOF
    {
      get;
    }

    void NextRecord();
    int NextRecordset();
    void Close();

    T GetValue<T>(string strFieldName, T defValue);
    T GetValue<T>(string strFieldName);
  }
}
