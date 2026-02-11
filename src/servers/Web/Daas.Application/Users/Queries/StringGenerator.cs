using System;
using System.Collections.Generic;
using System.Text;

namespace Daas.Application.Users.Queries
{
    public class StringGenerator:IFieldValueGenerator
    {

        private readonly Random random;
        public StringGenerator(Random _random)
        {
            random = _random;
        }
        public object Generator()
        {
            string g = "string";
            return g + "_" + random.Next(0, 20000);
        }
    }
}
