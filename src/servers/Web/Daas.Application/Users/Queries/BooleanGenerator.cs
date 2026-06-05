using System;
using System.Collections.Generic;
using System.Text;

namespace Daas.Application.Users.Queries
{
    public class BooleanGenerator : IFieldValueGenerator
    {
        private Random _random;
        public BooleanGenerator(Random random) {
            _random = random;
        }
        public object Generator()
        {
            byte randombyte = (byte) _random.Next(0, 2);
            if(randombyte == 1)
            {
            return true;
            }
            return false;
        }

      }
}
