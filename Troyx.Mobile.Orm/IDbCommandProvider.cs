using System;
using System.Data.Common;

namespace Troyx.Mobile.Orm
{
    public interface IDbCommandProvider
    {
        DbCommand CreateDbCommand();
    }
}
