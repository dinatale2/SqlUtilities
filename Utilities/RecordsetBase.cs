using System;
using System.Diagnostics;

namespace SqlUtilities
{
  public abstract class RecordsetBase<RecordsetType> : IRecordset
  {
    protected RecordsetType m_pRecordset;
    private bool m_bDisposed;

    public RecordsetBase()
    {
      m_bDisposed = false;
    }

    // IDisposable Implementation
    public void Dispose()
    {
      // we are now disposing for real
      Dispose(true);

      // suppress garbage collection
      GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
      // if not disposed
      if (!m_bDisposed)
      {
        // if disposing
        if (disposing)
        {
          Close();
          m_bDisposed = true;
        }
      }
    }

    // IRecordset implementation
    public abstract bool EOF
    {
      get;
    }
    public abstract void NextRecord();
    public abstract int NextRecordset();
    public abstract void Close();
    protected abstract object GetRecordsetValue(string strFieldName);

    // Value getting
    public T GetValue<T>(string strFieldName)
    {
      return GetValueInternal<T>(GetRecordsetValue(strFieldName));
    }

    public T GetValue<T>(string strFieldName, T defValue)
    {
      return GetValueInternal<T>(GetRecordsetValue(strFieldName), defValue);
    }

    protected T GetValueInternal<T>(object obj)
    {
      Debug.Assert((obj != null && obj != DBNull.Value) && obj.GetType() == typeof(T));

      if ((obj != null && obj != DBNull.Value) && obj.GetType() == typeof(T))
      {
        return (T)obj;
      }

      throw new Exception(string.Format("Error in SqlUtilities.GetValue: Unable to convert object to type {0}.", typeof(T)));
    }

    protected T GetValueInternal<T>(object obj, T DefaultVal)
    {
      Debug.Assert((obj == null || obj == DBNull.Value) || obj.GetType() == typeof(T));

      if (obj == null || obj == DBNull.Value)
      {
        return DefaultVal;
      }
      else
      {
        if (obj.GetType() == typeof(T))
        {
          return (T)obj;
        }
      }

      throw new Exception(string.Format("Error in SqlUtilities.GetValue: Unable to convert object to type {0}.", typeof(T)));
    }
  }
}
