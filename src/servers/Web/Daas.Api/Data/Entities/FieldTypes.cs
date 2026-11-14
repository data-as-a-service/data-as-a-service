using System;
using System.Collections.Generic;
using System.Text;

namespace Daas.Domain.Entities;

public enum FieldTypes
{
    // Keep the original generator values stable: existing rows store these numbers.
    INT = 0,
    FLOAT = 1,
    BOOLEAN = 2,
    STRING = 3,
    CHAR = 4,
    GUID = 5,
    DATE = 6,
    DOUBLE = 7,

    DECIMAL = 8,
    TIME = 9,
    DATETIME = 10,
    UUID = 11,
    BYTE = 12,
    SHORT = 13,
    USHORT = 14,
    LONG = 15,
    LONGLONG = 16,
    ULONG = 17,
    SBYTE = 18,
    BINARY = 19
}
