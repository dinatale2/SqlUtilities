using System;

namespace ConnectionHealth
{
  //readonly int DBPROPVAL_CS_INITIALIZED = 1;
  //  readonly int DBPROPVAL_CS_UNINITIALIZED = 0;
  //  readonly int DBPROPVAL_CS_COMMUNICATIONFAILURE = 2;
  //  ADODB.Property prop = conn.Properties["Connection Status"];

  [Flags]
  internal enum ObjectStatus : int
  {
    Closed = 0,
    Open = 1,
    Busy = 2,
  };

  public struct Status
  {
    internal ObjectStatus m_Status;

    public Status(ADODB.Recordset rs)
    {
      m_Status = ObjectStatus.Closed;
      if (rs != null && rs.State != (int)ADODB.ObjectStateEnum.adStateClosed)
      {
        if ((rs.State & (int)ADODB.ObjectStateEnum.adStateOpen) == (int)ADODB.ObjectStateEnum.adStateOpen)
          m_Status |= ObjectStatus.Open;

        if ((rs.State | (int)ADODB.ObjectStateEnum.adStateOpen) != (int)ADODB.ObjectStateEnum.adStateOpen)
          m_Status |= ObjectStatus.Busy;
      }
    }

    public Status(ADODB.Connection conn)
    {
      m_Status = ObjectStatus.Closed;
      if (conn != null && conn.State != (int)ADODB.ObjectStateEnum.adStateClosed)
      {
        if ((conn.State & (int)ADODB.ObjectStateEnum.adStateOpen) == (int)ADODB.ObjectStateEnum.adStateOpen)
          m_Status |= ObjectStatus.Open;

        if ((conn.State | (int)ADODB.ObjectStateEnum.adStateOpen) != (int)ADODB.ObjectStateEnum.adStateOpen)
          m_Status |= ObjectStatus.Busy;
      }
    }

    public bool IsIdle()
    {
      return IsOpen() && ((m_Status | ObjectStatus.Open) == ObjectStatus.Open);
    }

    public bool IsOpen()
    {
      return !IsClosed();
    }

    public bool IsClosed()
    {
      return (m_Status | ObjectStatus.Closed) == ObjectStatus.Closed;
    }
  }
}
