using System;

namespace SqlUtilities
{
  public interface IConnection : IDisposable, ITimeout
  {
    void Close();	// ensure connection is closed
    void Ensure();  // used to ensure a connection, will likely do some form of connection health checks
    int ExecuteSql(string strSql, params object[] parms);
    IRecordset CreateIRecordset(string strSql, params object[] parms);
    bool ReturnsRecords(string strSql, params object[] parms);
  }
}