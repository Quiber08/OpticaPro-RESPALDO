using System.Text.Json.Serialization;
using System.Collections.Generic;
using OpticaPro.Models;

namespace OpticaPro.Services
{
    // Este archivo obliga a la app portable a incluir el código de guardado
    [JsonSerializable(typeof(List<Patient>))]
    internal partial class AppJsonContext : JsonSerializerContext
    {
    }
}