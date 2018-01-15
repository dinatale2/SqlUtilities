using System;

namespace SqlUtilities
{
  public interface ITransaction
  {
    bool InTransaction
    {
      get;
    }

    void BeginTran();
    void RollbackTran();
    void CommitTran();
  }
}