﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace Tzkt.Api
{
    [ModelBinder(BinderType = typeof(AnyOfBinder))]
    [JsonSchemaType(typeof(string))]
    public class AnyOfParameter
    {
        public IEnumerable<string> Fields { get; set; }

        public int Value { get; set; }
    }
}