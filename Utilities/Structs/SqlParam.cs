using System;

namespace SqlUtilities
{
  public struct SqlParam
  {
    public object Value;
    public SqlParamType DataType;

    public SqlParam(object value, SqlParamType dataType)
    {
      Value = value;
      DataType = dataType;
    }

    public SqlParam(SqlParam ToCopy)
    {
      Value = ToCopy.Value;
      DataType = ToCopy.DataType;
    }
  }
}
