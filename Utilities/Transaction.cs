using System;

namespace SqlUtilities
{
  public class Transaction : IDisposable
  {
    private bool m_bDisposed;
    private string m_strName;
    private ITransaction m_pConn;

    public Transaction(ITransaction conn, string strTranName = "<Unknown>")
    {
      if (conn == null)
      {
        throw new ArgumentException("Transaction: Connection cannot be null.");
      }

      m_bDisposed = false;
      m_pConn = conn;

      if (string.IsNullOrEmpty(strTranName))
      {
        strTranName = "<Unknown>";
      }

      m_strName = strTranName;
    }

    ~Transaction()
    {
      Dispose(false);
    }

    protected void Dispose(bool bDisposing)
    {
      if (bDisposing && !m_bDisposed)
      {
        if (m_pConn.InTransaction)
        {
          Rollback();
        }
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public void Begin()
    {
      try
      {
        m_pConn.BeginTran();
      }
      catch (Exception ex)
      {
        throw new Exception(string.Format("Error in Transaction:Begin %s: %s", m_strName, ex.Message), ex);
      }
    }

    public void Commit()
    {
      try
      {
        m_pConn.CommitTran();
      }
      catch (Exception ex)
      {
        throw new Exception(string.Format("Error in Transaction:Commit %s: %s", m_strName, ex.Message), ex);
      }
    }

    public void Rollback()
    {
      try
      {
        m_pConn.RollbackTran();
      }
      catch (Exception ex)
      {
        throw new Exception(string.Format("Error in Transaction:Rollback %s: %s", m_strName, ex.Message), ex);
      }
    }
  }
}