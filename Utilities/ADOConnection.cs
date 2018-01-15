using System;
using System.Text;
using ADODB;

namespace SqlUtilities
{
  public class ADOConnection : ConnectionBase<ADODB.Connection, ADORecordset>
  {
    // constructors
    public ADOConnection(ADODB.Connection conn)
      : base()
    {
      if (conn == null)
      {
        throw new ArgumentNullException("ADOConnection: Connection cannot be null.");
      }

      m_pConnection = conn;
      m_strConnectionString = conn.ConnectionString;
    }

    public ADOConnection(string strConnString)
      : base()
    {
      if (string.IsNullOrEmpty(strConnString))
      {
        throw new ArgumentException("ADOConnection: Connection string cannot be null or empty.");
      }

      m_strConnectionString = strConnString;
    }

    public ADOConnection(string strProvider, string strServerAddress, string strDBName, string strUsername, string strPassword)
      : this(strProvider, strServerAddress, strDBName, strUsername, strPassword, false, false)
    {
    }

    public ADOConnection(string strProvider, string strServerAddress, string strDBName, string strUsername, string strPassword, bool bTrusted, bool bIntegratedSecurity)
      : base()
    {
      m_strConnectionString = base.GetConnectionString(strProvider, strServerAddress, strDBName, strUsername, strPassword, bTrusted, bIntegratedSecurity);
    }

    // destructors
    ~ADOConnection()
    {
    }

    // Connection base Implementation
    protected override ADODB.Connection GetConnection()
    {
      // Initialize connection and reader to null
      ADODB.Connection ServConnect = null;

      try
      {
        ServConnect = new ADODB.Connection();

        // have the client maintain the cursor by default, for performance reasons
        ServConnect.CursorLocation = CursorLocationEnum.adUseClient;
        ServConnect.ConnectionTimeout = m_nConnectionTimeout;
        ServConnect.CommandTimeout = m_nCommandTimeout;

        // gurantee that at least a blank pass is passed
        ServConnect.Open(m_strConnectionString, "", "", (int)ConnectOptionEnum.adConnectUnspecified);
        return ServConnect;
      }
      catch (Exception e)
      {
        if (ServConnect != null && ServConnect.State != (int)ObjectStateEnum.adStateClosed)
          ServConnect.Close();

        throw new Exception("Error in ADOConnection.GetConnection. " + e.Message, e);
      }
    }

    public override bool TestConnection(out string error)
    {
      ADODB.Connection TestConn = null;
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
        ConnectionHealth.Status connStatus = new ConnectionHealth.Status(TestConn);
        if (!connStatus.IsClosed())
        {
          TestConn.Close();
          TestConn = null;
        }
      }
    }

    public override bool TestConnection()
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

    protected override void SetCommandTimeout(int nCommTimeout)
    {
      base.SetCommandTimeout(nCommTimeout);

      m_pConnection.CommandTimeout = m_nCommandTimeout;
    }

    // IConnection Implementation
    public override void Ensure()
    {
      ConnectionHealth.Status status = new ConnectionHealth.Status(m_pConnection);
      if (status.IsClosed())
      {
        m_bInTransaction = false;
        m_pConnection = null;
        m_pConnection = GetConnection();
      }

      // TODO: potentially reaffirm connection?
    }

    public override void Close()
    {
      ConnectionHealth.Status status = new ConnectionHealth.Status(m_pConnection);
      if (!status.IsClosed())
      {
        RollbackTran();
        m_pConnection.Close();
      }

      m_pConnection = null;
    }

    private void AppendParameter(SqlParam sqlParam, Command SqlCmd)
    {
      Parameter ToAppend = null;
      object o = sqlParam.Value;

      switch (sqlParam.DataType)
      {
        case SqlParamType.Boolean:
          ToAppend = SqlCmd.CreateParameter("", DataTypeEnum.adBoolean, ParameterDirectionEnum.adParamInput, 8, o);
          break;
        case SqlParamType.Double:
          ToAppend = SqlCmd.CreateParameter("", DataTypeEnum.adDouble, ParameterDirectionEnum.adParamInput, 8, o);
          ToAppend.Precision = 4;
          break;
        case SqlParamType.Decimal:
          ToAppend = SqlCmd.CreateParameter("", DataTypeEnum.adNumeric, ParameterDirectionEnum.adParamInput, 8, o);
          ToAppend.Precision = 9;
          break;
        case SqlParamType.DateTime:
          ToAppend = SqlCmd.CreateParameter("", DataTypeEnum.adDBTimeStamp, ParameterDirectionEnum.adParamInput, 8, o);
          break;
        case SqlParamType.String:
          ToAppend = SqlCmd.CreateParameter("", DataTypeEnum.adLongVarWChar, ParameterDirectionEnum.adParamInput, 4000, o);
          break;
        case SqlParamType.Integer:
          ToAppend = SqlCmd.CreateParameter("", DataTypeEnum.adInteger, ParameterDirectionEnum.adParamInput, 4, o);
          break;
        case SqlParamType.ConstantString:
        case SqlParamType.ConstantInt:
        case SqlParamType.Sql:
        case SqlParamType.Date:
        default:
          throw new Exception("Unsupported Sql Parameter Type.");
      }

      if(ToAppend != null)
        SqlCmd.Parameters.Append(ToAppend);
    }

    private void ProcessParameters(Command SqlCmd, SqlFrag frag)
    {
      if (frag == null || frag.SqlParams.Count == 0)
        return;

      for (int i = 0; i < frag.SqlParams.Count; i++)
      {
        AppendParameter(frag.SqlParams[i], SqlCmd);
      }
    }

    protected ADODB.Command GenerateCommand(CommandTypeEnum eCmdType, SqlFrag frag)
    {
      ADODB.Command SqlCmd = new Command();
      string strQuery = "";

      if (frag.SqlParams.Count < 2040)
      {
        ProcessParameters(SqlCmd, frag);

        // replace our tags
        strQuery = frag.m_strSql.Replace("{", "");
        strQuery = strQuery.Replace("}", "");
      }
      else
      {
        strQuery = frag.Flatten();
      }

      // ensure we have a valid connections
      Ensure();

      // set up the object
      SqlCmd.ActiveConnection = m_pConnection;
      SqlCmd.CommandText = strQuery;
      SqlCmd.CommandType = eCmdType;

      return SqlCmd;
    }

    public int ExecuteSql(CommandTypeEnum eCmdType, SqlFrag frag)
    {
      ADODB.Command sqlCmd = GenerateCommand(eCmdType, frag);
      object obRecsAffected = 0;
      sqlCmd.Execute(out obRecsAffected);

      return (int)obRecsAffected;
    }

    public override int ExecuteSql(string query, params object[] parms)
    {
      return ExecuteSql(CommandTypeEnum.adCmdText, new SqlFrag(parms, query));
    }

    public override ADORecordset CreateRecordset(string query, params object[] parms)
    {
      return new ADORecordset(CreateRecordset(new SqlFrag(parms, query)));
    }

    public override IRecordset CreateIRecordset(string query, params object[] parms)
    {
      return (IRecordset)CreateRecordset(new SqlFrag(parms, query));
    }

    public Recordset CreateRecordset(SqlFrag frag, CommandTypeEnum eCmdType = CommandTypeEnum.adCmdText, 
      CursorLocationEnum eCursorLoc = CursorLocationEnum.adUseClient, CursorTypeEnum eCursorType = CursorTypeEnum.adOpenDynamic, 
      LockTypeEnum eLockType = LockTypeEnum.adLockOptimistic, ExecuteOptionEnum eExecOptions = ExecuteOptionEnum.adOptionUnspecified)
    {
      ADODB.Command SqlCmd = GenerateCommand(eCmdType, frag);
      Recordset rRecords = new Recordset();
      rRecords.CursorLocation = eCursorLoc;

      rRecords.Open(SqlCmd, Type.Missing, eCursorType, eLockType, (int)eExecOptions);
      return rRecords;
    }

    public override bool ReturnsRecords(string strSql, params object[] parms)
    {
      return ReturnsRecords(new SqlFrag(strSql, parms));
    }

    public bool ReturnsRecords(SqlFrag frag)
    {
      ADORecordset rs = CreateRecordset("IF EXISTS({SQL}) (SELECT CAST(1 AS BIT) AS Val) ELSE (SELECT CAST(0 AS BIT) AS Val)", frag);
      bool bExists = false;
      if (!rs.EOF)
      {
        bExists = rs.GetValue<bool>("Val");
      }
      return bExists;
    }

    // ITranscationable Implementation
    public override void BeginTran()
    {
      Ensure();
      m_pConnection.BeginTrans();
      m_bInTransaction = true;
    }

    public override void RollbackTran()
    {
      if (m_bInTransaction && m_pConnection != null)
      {
        m_pConnection.RollbackTrans();
      }

      m_bInTransaction = false;
    }

    public override void CommitTran()
    {
      if (m_bInTransaction && m_pConnection != null)
      {
        m_pConnection.CommitTrans();
      }

      m_bInTransaction = false;
    }
  }
}
