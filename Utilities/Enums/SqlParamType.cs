using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SqlUtilities
{
  public enum SqlParamType
  {
    ConstantString,
    ConstantInt,
    Boolean,
    Double,
    Decimal,
    Date,
    DateTime,
    Sql,
    String,
    Integer
  }
}
