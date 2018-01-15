using System;
using System.Collections.Generic;
using System.Text;

namespace SqlUtilities
{
  public class SqlFrag
  {
    internal string m_strSql; // the sql string
    internal List<SqlParam> m_Params;  // the objects that are going to be passed in as parameters

    public string Sql
    {
      get { return m_strSql; }
    }

    public List<SqlParam> SqlParams
    {
      get { return m_Params; }
    }

    public SqlFrag(string strSql, params object[] parms)
      : this()
    {
      ProcessParamSql(strSql, ref parms);
    }

    internal SqlFrag(object[] parms, string strSql)
      : this()
    {
      ProcessParamSql(strSql, ref parms);
    }

    public SqlFrag(SqlFrag FragToClone)
    {
      m_strSql = FragToClone.Sql;
      m_Params = new List<SqlParam>(FragToClone.SqlParams);
    }

    public SqlFrag()
    {
      m_strSql = "";
      m_Params = new List<SqlParam>();
    }

    public static implicit operator SqlFrag(string strQuery)
    {
      return new SqlFrag(strQuery);
    }

    ~SqlFrag()
    {
      m_strSql = "";
      m_Params.Clear();
    }

    public static SqlFrag operator +(SqlFrag Frag1, string strToAdd)
    {
      SqlFrag Cloned = new SqlFrag(Frag1);
      Cloned.m_strSql += " ";
      Cloned.m_strSql += strToAdd;

      return Cloned;
    }

    public static SqlFrag operator +(SqlFrag Frag1, SqlFrag Frag2)
    {
      SqlFrag Cloned = new SqlFrag(Frag1);
      Cloned.m_strSql += " ";
      Cloned.m_strSql += Frag2.m_strSql;
      Cloned.m_Params.AddRange(Frag2.m_Params);

      return Cloned;
    }

    private string FlattenParameter(SqlParam param)
    {
      string strValue = "";
      object o = param.Value;

      if (o == null)
        strValue = "NULL";
      else
      {
        // TODO: Add other types to this list
        switch (param.DataType)
        {
          case SqlParamType.ConstantString:
          case SqlParamType.ConstantInt:
            strValue = o.ToString();
            break;
          case SqlParamType.Boolean:
            strValue = (bool)o ? "1" : "0";
            break;
          case SqlParamType.Double:
            strValue = o.ToString();
            break;
          case SqlParamType.Decimal:
            strValue = o.ToString();
            break;
          case SqlParamType.DateTime:
            {
              DateTime dt = (DateTime)o;
              strValue = "'" + dt + "'";
            }
            break;
          case SqlParamType.Sql:
            strValue = ((SqlFrag)o).Flatten();
            break;
          case SqlParamType.String:
            strValue = (string)o;
            strValue = strValue.Replace("'", "''");
            strValue = ("'" + strValue + "'");
            break;
          case SqlParamType.Integer:
            strValue = o.ToString();
            break;
          case SqlParamType.Date:
          default:
            throw new Exception("Unsupported Sql Parameter Type.");
        }
      }

      return strValue;
    }

    public string Flatten()
    {
      int CurrIndexOpenCurly = -1;
      int CurrIndexCloseCurly = -1;

      string strQuery = m_strSql;

      foreach (SqlParam CurrParam in m_Params)
      {
        CurrIndexOpenCurly = strQuery.IndexOf('{', CurrIndexOpenCurly + 1);

        if (CurrIndexOpenCurly == -1)
          break;

        CurrIndexCloseCurly = strQuery.IndexOf('}', CurrIndexOpenCurly + 1);

        if (CurrIndexCloseCurly == -1)
          throw new Exception("An incomplete parameter tag exists in this query");

        if (CurrIndexCloseCurly <= CurrIndexOpenCurly)
          throw new Exception("The index of the closing brace is less than the index of the opening brace.");

        strQuery = strQuery.Remove(CurrIndexOpenCurly, CurrIndexCloseCurly - CurrIndexOpenCurly + 1);
        strQuery = strQuery.Insert(CurrIndexOpenCurly, FlattenParameter(CurrParam));
      }

      return strQuery;
    }

    private SqlParamType GetParameterType(int IndexOpenCurly, int IndexCloseCurly, ref string query)
    {
      if (IndexCloseCurly <= IndexOpenCurly)
        throw new Exception("The index of the closing brace is less than the index of the opening brace.");

      string strType = query.Substring(IndexOpenCurly + 1, IndexCloseCurly - IndexOpenCurly - 1);

      // lets not promote bad behavior
      if (strType.Length < 3)
        throw new Exception("Invalid parameter type.");

      switch (strType[0])
      {
        case 'C':
        case 'c':
          // CONST
          switch (strType[6])
          {
            case 's':
            case 'S':
              return SqlParamType.ConstantString;
            case 'i':
            case 'I':
              return SqlParamType.ConstantInt;
          }
          break;
        case 'B':
        case 'b':
          // BOOL
          return SqlParamType.Boolean;
        case 'D':
        case 'd':
          switch (strType[1])
          {
            // DOUBLE
            case 'O':
            case 'o':
              return SqlParamType.Double;
            // DECIMAL
            case 'e':
            case 'E':
              return SqlParamType.Decimal;
            // DATETIME
            case 'A':
            case 'a':
              return SqlParamType.DateTime;
          }
          break;
        case 'S':
        case 's':
          switch (strType[1])
          {
            // SQL
            case 'q':
            case 'Q':
              return SqlParamType.Sql;
            // STRING
            case 't':
            case 'T':
              return SqlParamType.String;
          }
          break;
        case 'I':
        case 'i':
          // INTEGER
          return SqlParamType.Integer;
      }

      // if we end up down here, then we have a problem
      throw new Exception("Invalid parameter type.");
    }

    public void ProcessParamSql(string strSql, ref object[] parms)
    {
      // TODO: Use StringBuilder for better performance/less of a memory hit?
      int CurrIndexOpenCurly = -1;
      int CurrIndexCloseCurly = -1;

      foreach (object oParam in parms)
      {
        CurrIndexOpenCurly = strSql.IndexOf('{', CurrIndexOpenCurly + 1);

        if (CurrIndexOpenCurly == -1)
          break;

        CurrIndexCloseCurly = strSql.IndexOf('}', CurrIndexOpenCurly + 1);

        if (CurrIndexCloseCurly == -1)
          throw new Exception("An incomplete parameter tag exists in this query");

        if (CurrIndexCloseCurly <= CurrIndexOpenCurly)
          throw new Exception("The index of the closing brace is less than the index of the opening brace.");

        SqlParam sqlParam;
        sqlParam.Value = oParam;
        sqlParam.DataType = GetParameterType(CurrIndexOpenCurly, CurrIndexCloseCurly, ref strSql);

        string strInsert = "{?}";
        switch (sqlParam.DataType)
        {
          case SqlParamType.Sql:
            {
              SqlFrag frag = (SqlFrag)sqlParam.Value;
              strInsert = frag.m_strSql;
              m_Params.AddRange(frag.m_Params);
            }
            break;
          case SqlParamType.ConstantString:
          case SqlParamType.ConstantInt:
            strInsert = FlattenParameter(sqlParam);
            break;
          default:
            strInsert = "{?}";
            m_Params.Add(sqlParam);
            break;
        }

        strSql = strSql.Remove(CurrIndexOpenCurly, CurrIndexCloseCurly - CurrIndexOpenCurly + 1);
        strSql = strSql.Insert(CurrIndexOpenCurly, strInsert);

        CurrIndexOpenCurly += strInsert.Length;
      }

      m_strSql = strSql;
    }
  }
}
