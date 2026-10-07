using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Daas.Domain.Entities;

namespace Daas.Api.Generation
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
                FieldType.Double => new DoubleGenerator(_random),
                _ => throw new NotSupportedException($"No data generator is registered for generator field type '{type}'.")
            };
        }

        public IFieldValueGenerator Get(FieldTypes schemaType)
        {
            return schemaType switch
            {
                FieldTypes.INT => Get(FieldType.Int),
                FieldTypes.FLOAT => Get(FieldType.Float),
                FieldTypes.BOOLEAN => Get(FieldType.Boolean),
                FieldTypes.STRING => Get(FieldType.String),
                FieldTypes.CHAR => Get(FieldType.Character),
                FieldTypes.GUID => Get(FieldType.Guid),
                FieldTypes.DATE => Get(FieldType.Date),
                FieldTypes.DOUBLE => Get(FieldType.Double),
                _ => throw new NotSupportedException($"No data generator is registered for schema field type '{schemaType}'.")
            };
        }

    }
}
