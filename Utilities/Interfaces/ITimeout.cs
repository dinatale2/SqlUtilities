using System;

namespace SqlUtilities
{
  public interface ITimeout
  {
    int ConnectionTimeout
    {
      get;
      set;
    }

    int CommandTimeout
    {
      get;
      set;
    }
  }
}
