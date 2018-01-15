using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Utilities.DotNetDatabase;

namespace SqlUtilities
{
  public class Connection : IDisposable
  {
    private bool m_bDisposed;

    public string ConnectionString
    {
      get { return GetConnectionString(); }
    }

    private string m_strServerAddress;
    public string ServerAddress
    {
      get { return m_strServerAddress; }
    }

    private int m_nTimeout;
    public int Timeout
    {
      get { return m_nTimeout; }
    }

    private string m_strUsername;
    public string Username
    {
      get { return m_strUsername; }
    }

    private string m_strPassword;
    public string Password
    {
      get { return m_strPassword; }
    }

    private string m_strDBName;
    public string DatabaseName
    {
      get { return m_strDBName; }
    }

    private string m_strProvider;
    public string Provider
    {
      get { return m_strProvider; }
    }

    private CommandType m_eCommandType;
    public CommandType CommandType
    {
      get { return m_eCommandType; }
    }

    private SqlConnection m_cActiveConnection = null;
    public SqlConnection ActiveConnection
    {
      get
      {
        EnsureConnection();
        return m_cActiveConnection;
      }
    }

    public Connection(string Address, string DB_Name, string User, string Pass, int Timeout = 60, string Provider = "",
      CommandType eCmdType = CommandType.Text)
    {
      m_bDisposed = false;

      if (string.IsNullOrEmpty(Address))
        throw new Exception("Cannot create an DotNetConnection without a server address");

      if (string.IsNullOrEmpty(DB_Name))
        throw new Exception("Cannot create an DotNetConnection without a database name");

      m_nTimeout = Timeout;
      m_strProvider = Provider;
      m_strServerAddress = Address;
      m_strDBName = DB_Name;
      m_strUsername = User;
      m_strPassword = Pass;
      m_eCommandType = eCmdType;
    }

    ~Connection()
    {
      Dispose(false);
    }

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
          CloseConnection();
          m_bDisposed = true;
        }
      }
    }

    public void CloseConnection()
    {
      if (m_cActiveConnection != null)
      {
        if(m_cActiveConnection.State != ConnectionState.Closed)
          m_cActiveConnection.Close();

        m_cActiveConnection.Dispose();
        m_cActiveConnection = null;
      }
    }

    /// <summary>
    /// Returns the connection string for the connection.
    /// </summary>
    /// <returns>Connection String</returns>
    public string GetConnectionString()
    {
      //Concat connection string
      string ConnectionString = "";

      // if we dont have a provider, assume SQLOLEDB, else use the specified provider
      if (string.IsNullOrEmpty(m_strProvider))
        ConnectionString += "Provider=SQLOLEDB;";
      else
        ConnectionString += ("Provider=" + m_strProvider + ";");

      ConnectionString += ("Server=" + m_strServerAddress + ";Database=" + m_strDBName + ";");

      if (!string.IsNullOrEmpty(m_strUsername))
        ConnectionString += ("User ID=" + m_strUsername + ";");

      if (!string.IsNullOrEmpty(m_strPassword))
        ConnectionString += ("Password=" + m_strPassword + ";");

      ConnectionString += "Trusted_Connection=False;";

      return ConnectionString;
    }

    /// <summary>
    /// Gets a connection to a specified server.
    /// </summary>
    /// <param name="info">Contains the server details to create a connection.</param>
    /// <returns>An open Connection to the server if successful.</returns>
    private SqlConnection GetConnection()
    {
      //Initialize connection and reader to null
      SqlConnection ServConnect = null;

      try
      {
        ServConnect = new SqlConnection(GetConnectionString());

        // gurantee that at least a blank pass is passed
        ServConnect.Open();
        return ServConnect;
      }
      catch (Exception e)
      {
        if (ServConnect != null)
        {
          if(ServConnect.State != ConnectionState.Closed)
            ServConnect.Close();

          ServConnect.Dispose();
        }

        throw new Exception("Error in DotNetConnection.GetConnection. " + e.Message);
      }
    }

    private void EnsureConnection()
    {
      // if we dont have a connection, or the current one is closed
      if (m_cActiveConnection == null || m_cActiveConnection.State == ConnectionState.Closed)
      {
        // make a new one
        if(m_cActiveConnection != null)
          m_cActiveConnection.Dispose();

        m_cActiveConnection = null;
        m_cActiveConnection = GetConnection();
      }
    }

    public bool TestConnection(out string error)
    {
      SqlConnection TestConn = null;

      try
      {
        error = "";
        TestConn = GetConnection();

        if (TestConn != null)
          return true;
        else
          return false;
      }
      catch (Exception e)
      {
        error = e.Message;
        return false;
      }
      finally
      {
        if (TestConn != null)
        {
          if(TestConn.State != ConnectionState.Closed)
            TestConn.Close();

          TestConn.Dispose();
          TestConn = null;
        }
      }
    }

    public bool TestConnection()
    {
      try
      {
        string strError = "";
        return TestConnection(out strError);
      }
      catch
      {
        return false;
      }
    }

    public int ExecuteSql(string query)
    {
      return ExecuteParamSql(query);
    }

    public int ExecuteParamSql(string query, params object[] parms)
    {
      EnsureConnection();
      return DotNetDatabase.ExecuteSql(m_cActiveConnection, query, parms);
    }

    public int ExecuteSql(SqlFrag frag)
    {
      if (frag.SqlParams.Count < 2040)
        return ExecuteParamSql("{SQL}", frag);
      else
        return ExecuteParamSql(frag.Flatten());
    }

    public DataSet GetParamDataSet(string query, params object[] parms)
    {
      EnsureConnection();
      return DotNetDatabase.GetDataSet(m_cActiveConnection, query, parms);
    }

    public DataSet GetDataSet(string query)
    {
      return GetParamDataSet(query);
    }

    public DataSet GetDataSet(SqlFrag frag)
    {
      if (frag.SqlParams.Count < 2040)
        return GetParamDataSet("{SQL}", frag);
      else
        return GetParamDataSet(frag.Flatten());
    }

    public SqlDataReader GetParamDataReader(string query, params object[] parms)
    {
      EnsureConnection();
      return DotNetDatabase.GetDataReader(m_cActiveConnection, query, parms);
    }

    public SqlDataReader GetDataReader(string query)
    {
      return GetParamDataReader(query);
    }

    public SqlDataReader GetDataReader(SqlFrag frag)
    {
      if (frag.SqlParams.Count < 2040)
        return GetParamDataReader("{SQL}", frag);
      else
        return GetParamDataReader(frag.Flatten());
    }
  }
}
