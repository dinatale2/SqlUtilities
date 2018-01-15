using System;
using System.Text;

namespace SqlUtilities
{
  public abstract class ConnectionBase<ConnType, RecordsetType> : IConnection, ITransaction
    where ConnType : class
    where RecordsetType : class
  {
    private bool m_bDisposed;
    protected string m_strConnectionString;
    public string ConnectionString
    {
      get { return m_strConnectionString; }
    }

    protected bool m_bInTransaction;
    public bool InTransaction
    {
      get { return m_bInTransaction; }
    }

    protected int m_nConnectionTimeout;
    public int ConnectionTimeout
    {
      get { return m_nConnectionTimeout; }
      set { m_nConnectionTimeout = Math.Max(0, value); }
    }

    protected int m_nCommandTimeout;
    public int CommandTimeout
    {
      get { return m_nCommandTimeout; }
      set { SetCommandTimeout(value); }
    }

    protected virtual void SetCommandTimeout(int nCommTimeout)
    {
      m_nCommandTimeout = Math.Max(0, nCommTimeout);
    }

    // ConnectionBase
    public ConnectionBase()
    {
      m_pConnection = null;
      m_bInTransaction = false;
      m_bDisposed = false;
      m_nCommandTimeout = 30;
      m_nConnectionTimeout = 15;
    }

    protected ConnType m_pConnection;
    public ConnType Connection
    {
      get { Ensure(); return m_pConnection; }
    }
    protected abstract ConnType GetConnection();
    public abstract bool TestConnection();
    public abstract bool TestConnection(out string strError);

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

    // IConnection implementation
    public abstract void Close();
    public abstract void Ensure();
    public abstract int ExecuteSql(string strSql, params object[] parms);
    public abstract RecordsetType CreateRecordset(string strSql, params object[] parms);
    public abstract IRecordset CreateIRecordset(string strSql, params object[] parms);
    public abstract bool ReturnsRecords(string strSql, params object[] parms);

    // ITransacrionable implememtation
    public abstract void BeginTran();
    public abstract void RollbackTran();
    public abstract void CommitTran();

    // Utilities
    protected string GetConnectionString(string strProvider, string strServerAddress, string strDBName, string strUsername, string strPassword, bool bTrustedConn, bool bIntegratedSecurity)
    {
      //Concat connection string
      StringBuilder ConnectionString = new StringBuilder();

      // if we dont have a provider, assume SQLOLEDB, else use the specified provider
      if (string.IsNullOrEmpty(strProvider))
        ConnectionString.Append("Provider=SQLOLEDB;");
      else
        ConnectionString.AppendFormat("Provider={0};", strProvider);

      ConnectionString.AppendFormat("Server={0};Database={1};", strServerAddress, strDBName);

      if (!string.IsNullOrEmpty(strUsername))
        ConnectionString.AppendFormat("User ID={0};", strUsername);

      if (!string.IsNullOrEmpty(strPassword))
        ConnectionString.AppendFormat("Password={0};", strPassword);

      if (bIntegratedSecurity)
        ConnectionString.Append("Integrated Security=SSPI;");

      if (bTrustedConn)
        ConnectionString.Append("Trusted_Connection=True;");
      else
        ConnectionString.Append("Trusted_Connection=False;");

      return ConnectionString.ToString();
    }
  }
}