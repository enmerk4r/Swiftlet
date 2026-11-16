// Primitive values are deliberately explicit. Never substitute ToString() for an unknown type.
object CanonicalValue(object value)
{
    if (value != null && value.GetType().GetInterfaces().Any(i => i.FullName == "Grasshopper.Kernel.Types.IGH_Goo"))
    {
        var property = value.GetType().GetProperty("Value");
        if (property == null) throw new InvalidOperationException("GH1 Goo needs an adapter: " + value.GetType());
        value = property.GetValue(value);
    }
    string kind;
    object encoded;
    if (value == null) { kind = "null"; encoded = null; }
    else if (value is bool boolean) { kind = "boolean"; encoded = boolean; }
    else if (value is string text) { kind = "text"; encoded = text; }
    else if (value is double number) { kind = "number"; encoded = number.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }
    else if (value is float single) { kind = "number"; encoded = single.ToString("R", System.Globalization.CultureInfo.InvariantCulture); }
    else if (value is byte || value is short || value is int || value is long || value is sbyte || value is ushort || value is uint || value is ulong)
    { kind = "integer"; encoded = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture); }
    else if (value is byte[] bytes) { kind = "bytes"; encoded = Convert.ToBase64String(bytes); }
    else throw new InvalidOperationException("Unsupported parity output type; add an explicit adapter: " + value.GetType().FullName);
    return new Dictionary<string, object> { ["kind"] = kind, ["value"] = encoded };
}
