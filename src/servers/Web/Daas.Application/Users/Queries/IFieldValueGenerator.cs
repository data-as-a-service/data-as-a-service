using System;
using System.Collections.Generic;
using System.Text;

namespace Daas.Application.Users.Queries
{
    public interface IFieldValueGenerator
    {
        object Generator();
    }
}
