namespace RIMAPI.Helpers
{
    /// <summary>
    /// A float the game never set is NaN, and Newtonsoft writes that as the
    /// quoted string "NaN", which no client can read as a number (ticket
    /// #4315). Such a value travels as null instead: the DTO field is
    /// nullable, the serializer leaves a null out, and the schema says
    /// number-or-null.
    /// </summary>
    public static class FiniteFloat
    {
        public static float? OrNull(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return null;
            return value;
        }
    }
}
