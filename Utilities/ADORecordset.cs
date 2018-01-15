using System;

namespace SqlUtilities
{
  public class ADORecordset : RecordsetBase<ADODB.Recordset>
  {
    public ADORecordset(ADODB.Recordset pRecs)
      : base()
    {
      if(pRecs == null)
      {
        throw new ArgumentNullException("ADORecordset: Recordset cannot be null.");
      }

      m_pRecordset = pRecs;
    }

    // RecordsetBase overrides
    public override bool EOF
    {
      get { return m_pRecordset.EOF; }
    }

    public override void Close()
    {
      if (!(new ConnectionHealth.Status(m_pRecordset)).IsClosed())
      {
        m_pRecordset.Close();
      }
    }

    public override void NextRecord()
    {
      m_pRecordset.MoveNext();
    }

    public override int NextRecordset()
    {
      object oRecsAffected = 0;
      m_pRecordset = m_pRecordset.NextRecordset(out oRecsAffected);

      return (int)oRecsAffected;
    }

    protected override object GetRecordsetValue(string strFieldName)
    {
      return m_pRecordset.Collect[strFieldName];
    }
  }
}
