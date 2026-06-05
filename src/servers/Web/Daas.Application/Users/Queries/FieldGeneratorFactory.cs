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
                FieldType.String=> new StringGenerator(_random),
                FieldType.Boolean=> new BooleanGenerator(_random),

                FieldType.Float=> new FloatGenerator(_random),
                FieldType.Character=> new CharacterGenerator(_random),
                FieldType.Guid=> new GuidGenerator(),
                FieldType.Date => new DateGenerator(_random),
                FieldType.Double => new DoubleGenerator(_random)
            };
        }

    }
}
