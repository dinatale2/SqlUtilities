using System;

namespace SqlUtilities
{
  public class IncreaseCommandTimeout : IDisposable
  {
    private int m_nOldTimeout;
    private bool m_bDisposed;
    private IConnection m_pConn;

    public IncreaseCommandTimeout(IConnection conn, int nNewTimeout)
    {
      m_bDisposed = false;
      m_nOldTimeout = conn.CommandTimeout;
      nNewTimeout = Math.Max(0, nNewTimeout);

      m_pConn = conn;

      if (nNewTimeout == 0 || (m_nOldTimeout > 0 && m_nOldTimeout < nNewTimeout))
      {
        conn.CommandTimeout = nNewTimeout;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected void Dispose(bool bDisposing)
    {
      if (bDisposing && !m_bDisposed)
      {
        m_pConn.CommandTimeout = m_nOldTimeout;
      }
    }
  }
}