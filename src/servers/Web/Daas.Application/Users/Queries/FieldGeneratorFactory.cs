using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Daas.Application.Users.Queries
{
    public class FieldGeneratorFactory
    {
        private readonly Random _random;
        public FieldGeneratorFactory(Random random)
        {
            _random = random;
        }

        public IFieldValueGenerator Get(FieldType type)
        {

            
            return type switch
            {
                FieldType.Int => new IntGenerator(_random),
                FieldType.String=> new StringGenerator(_random)
            };
        }

    }
}
