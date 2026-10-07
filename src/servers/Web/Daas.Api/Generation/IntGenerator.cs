using System;
using System.Collections.Generic;
using System.Text;

namespace Daas.Api.Generation
{
    public class IntGenerator:IFieldValueGenerator
    {
        private readonly Random random;
        public IntGenerator(Random _random)
        {
            random = _random;
        }
        public object Generator()
        {
            return random.Next(0,2000);
            
        }
    }
}
